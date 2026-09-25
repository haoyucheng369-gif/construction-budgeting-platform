# Sales service

The Sales service owns project quotations and the final margin view. The host exposes six quotation operations (create/query, add/remove line, change quantity/sales price), Development Swagger UI and `/health`. Project catalog operations remain pending under T02.

## Dependency boundaries

```text
Api -------------> Application --------> Domain
  |                    ^
  +--> Infrastructure -+
```

- **Domain**: quotation aggregate, entities, value objects and business rules. No framework or transport dependency.
- **Application**: commands, queries, handlers and ports required by use cases. References Domain.
- **Infrastructure**: persistence and messaging adapters implementing application ports. References Application.
- **Api**: HTTP endpoints, middleware and dependency-injection composition root. References Application and Infrastructure.

This implements the inward dependency rule of Clean Architecture and the ports/adapters boundary of Hexagonal Architecture. Domain contains the immutable `Quotations/Quantity` value object: a decimal value strictly greater than zero, with value equality and no rounding. `Quotations/Money` holds a decimal amount and an EUR-only currency code, allows zero/negative amounts, and preserves precision on construction. `RoundToCents()` returns a new amount using midpoint rounding away from zero. These architecture styles and DDD are not separate framework installations.

`Quotations/QuoteLine` combines a non-empty line ID, work-item code, description, unit, Quantity and sales unit price. It rejects missing inputs and negative prices, then rounds the product of quantity and unit price to obtain its read-only line amount. Unit codes currently require non-empty text; validation against a work-item/recipe catalog remains planned. The entity has no editing methods or repository.

`Quotations/Quote` is the aggregate root with a quote ID and project ID. `AddLine` creates each line internally, rejects duplicate line IDs within that quote, and updates the total from already-rounded line amounts. The exposed collection is read-only. Validation or calculation failures leave the quote unchanged. Line IDs are scoped to their quote. PostgreSQL now enforces one quote per project; project existence checks remain planned; duplicate quotation creation maps to HTTP 409.

`ChangeLineQuantity` preserves line identity, metadata and unit price while replacing the line with a newly calculated read-only instance and updating the total. It returns the updated line; callers retaining the old instance must read the new one. Equal quantities are a no-op. Missing lines, null values or calculation overflow fail before changing state. This is an in-memory operation without messaging or concurrency control.

`ChangeLineSalesUnitPrice` similarly preserves identity, metadata and quantity, accepts zero prices, and rejects negative prices through QuoteLine validation. Equal prices are a no-op; different raw prices are retained even if rounded line amounts match. Both change operations use the current line held by the aggregate.

`RemoveLine` removes a line from the current quote and subtracts its latest rounded amount. Removing the last line leaves a valid empty draft with a zero EUR total. Empty IDs and missing lines (including repeated removal) fail without changing state. The quote and project identities are retained.

`Version` is an externally read-only `long` starting at 1. Each successful addition, removal or actual quantity/price change advances it once, even if the total stays the same. Equal-value changes and failures preserve the version. The checked increment is calculated before changing state. Its stored value is restored by EF without replaying edits; the repository now checks the original version atomically. Quantity-edit HTTP requests return 409 for version conflicts.

External database, message-broker and browser contracts must not replace or leak into the domain model. MediatR, EF Core and messaging packages are added when their use cases are introduced, not to otherwise empty layers.

## Quantity-change application use case

`Application/Quotations/ChangeQuoteLineQuantity` contains the command, handler and result DTO. The command carries QuoteId, LineId, decimal Quantity and ExpectedVersion. The handler validates input, loads the quote through `IQuoteRepository`, rejects a mismatched version, calls the domain method and saves effective changes with the original version. Equal quantities skip saving. The result copies domain-calculated amounts and the resulting version; it does not expose the mutable aggregate.

`IQuoteRepository` is an application-owned port, implemented by `EfQuoteRepository` in Infrastructure. It returns independent aggregates and atomically compares the stored version when saving. The handler's initial comparison alone cannot prevent a write racing with the save. `QuoteConcurrencyException` is also the contract for save-time conflicts. Storage errors propagate; after a failed save the modified in-memory unit of work must be discarded.

`AddSalesApplication()` registers MediatR 12.5.0 and handlers. Unit tests exercise real `ISender.Send` dispatch with a scoped repository test double; PostgreSQL tests call both handlers with the real repository. The API now registers MediatR, the database-context factory and the repository. GET /quotes/{id} dispatches the query; PATCH /quotes/{quoteId}/lines/{lineId}/quantity dispatches the quantity command. Centralized ProblemDetails handling maps invalid inputs to 400, missing quotes/lines to 404 and version conflicts to 409. Integration events remain subsequent work.

## Additional quotation commands

CreateQuote, AddQuoteLine, RemoveQuoteLine and ChangeQuoteLineSalesUnitPrice each have a MediatR command, handler and result. Quote and line IDs are generated by the server. All existing-quote mutations carry ExpectedVersion and reuse the domain methods and atomic repository save; same-price requests skip saving. The project ID is a non-empty external reference; this step does not validate a project catalog or create budgets.

QuoteEndpoints.cs maps the six routes; Program.cs only composes dependencies and maps the endpoint group. Creation/addition return 201 with a Location pointing to the quote query; deletion returns 200 with the updated total/version. SalesExamplesOperationFilter fills Swagger parameters and JSON examples only; the server still requires real inputs and an up-to-date version.

## Quotation query use case

`Application/Quotations/GetQuote` contains `GetQuoteQuery`, `GetQuoteHandler`, `GetQuoteResult` and `QuoteLineDetails`. The query carries only QuoteId. Its handler reads through the same `IQuoteRepository` and copies quote/project IDs, version, currency, total and ordered line details into a detached result. It preserves unit-price precision and copies calculated amounts without recalculating them. The returned line collection is materialized and wrapped as read-only; later aggregate changes do not alter an earlier result.

Queries do not call domain mutation methods or `SaveAsync`. Empty drafts return zero EUR and an empty collection; missing quotes throw `KeyNotFoundException`. Invalid IDs, cancellation and storage errors are propagated without returning fabricated data. Existing MediatR assembly registration discovers the query handler. This is separation of application read/write responsibilities using the same repository, with no separate read database. The GET endpoint exposes the query result. Loading the whole aggregate keeps the current implementation small; a dedicated read projection remains an option if later requirements justify it.

## Initial PostgreSQL persistence

`Infrastructure/Persistence/SalesDbContext` maps the existing domain types through fluent configurations. `Quantity` and EUR `Money` use decimal value converters; numeric columns preserve their precision. `Quotes` owns `QuoteLines` through a composite quote/line key. A shadow `Position` column preserves collection order; `ReadQuotes()` loads all lines in that order without tracking. EF fills the private collection and restores stored amounts/version; Domain has no EF dependency or new public setters.

The `InitialSales` migration and history belong to the `sales` schema. Initial inserts receive line positions in `SalesDbContext`. For edited aggregates, `EfQuoteRepository` owns a new context per operation through `IDbContextFactory<SalesDbContext>`. Reads use one SQL query without tracking. Saves open a transaction, update the header only where Id and expected Version match, then delete/reinsert that quote's lines with their existing IDs and current positions. Zero updated headers means a concurrency conflict, including deletion since loading. ExecuteUpdate requires this explicit comparison; the concurrency-token mapping alone does not protect it.

All header/line operations commit together or roll back together. Domain-calculated amounts are copied unchanged. SaveAsync accepts only existing, effectively edited aggregates with a newer version. AddAsync inserts a new aggregate; the existing unique ProjectId index arbitrates competing creation requests and maps only that constraint violation to QuoteAlreadyExistsException. Rewriting all lines simplifies immutable replacement and ordering but costs writes proportional to quote size. It is intended for this release's small drafts, with no separate line references/history; large quotations or external line foreign keys would require revisiting this strategy. There is no automatic retry or multi-aggregate transaction.

Use the root README's Sales database commands to apply migrations and run real PostgreSQL tests. Credentials come from the local environment; `.config/dotnet-tools.json` pins EF tooling. The API obtains its connection from ConnectionStrings__Sales and does not migrate or seed during normal startup. The explicit Development-only --seed-sample command uses SalesSampleData to migrate and initialize one quote through the domain model; repeat execution preserves existing edits. Swagger UI is available at /swagger in Development. See the root walkthrough for exact commands.

## Current verification

The integration test project starts the real ASP.NET Core pipeline in memory. It checks liveness without external dependencies and ProblemDetails for unknown routes. `/health` does not yet check database or broker readiness.

`tests/Unit/ConstructionBudgeting.Sales.Domain.Tests` references only Domain. Quantity tests cover positive integer/fractional values, rejection of zero and negative values, and value equality. They run without the API, databases or Docker.

Money tests cover precision retention, zero/negative amounts, rejected currencies, positive/negative midpoint rounding, immutability and value equality. A unit-price multiplication example checks that rounding is deferred until the final amount.

QuoteLine tests cover line calculation and rounding, zero/negative prices, required identity and metadata, null value objects, separate identities for identical work items, and decimal overflow. These tests do not require infrastructure.

Quote tests cover creation, controlled line addition, duplicate rejection, aggregate isolation, sum-of-rounded-lines totals, collection protection and unchanged state on failed additions. This verifies in-memory rules, not database transactions or concurrent updates.

Quantity-change tests cover increases/decreases, preserved identity and metadata, no-op updates, rounding over repeated changes, zero-price lines, invalid inputs, overflow and isolation from other quotes.

Sales-price tests cover increases/decreases/zero, negative-price rejection, precision, no-op updates, unchanged rounded amounts, invalid inputs, overflow, aggregate isolation and interleaved quantity/price changes.

Removal tests cover remaining-line identity/order, empty drafts, zero-price lines, invalid IDs, repeated removal, current amounts after edits, rounding, aggregate isolation and rejected edits of removed lines.

Version tests cover initialization, increments across edits, unchanged amounts with changed inputs, no-op changes, validation and amount-overflow failures, repeated removal, reverting to a previous quantity and independent quotations.

`tests/Unit/ConstructionBudgeting.Sales.Application.Tests` references Application and uses a repository test double. It checks quantity-change results, expected-version forwarding, no-op behavior, stale input, missing records, invalid values, domain overflow, save failures, cancellation-token forwarding and DI/MediatR dispatch. These checks do not prove persistence or database concurrency behavior.

Query tests verify all returned fields, line order, rounded amounts, empty drafts, unchanged aggregate state, no save calls, detached read-only results, missing/invalid input, cancellation and read failures. A MediatR query→command→query test checks both handlers and confirms that only the command calls save.

`tests/Integration/ConstructionBudgeting.Sales.Persistence.Tests` checks migration-model consistency offline and, when explicitly enabled by `Test-SalesPersistence.ps1`, applies migrations against Compose PostgreSQL. Database checks cover round trips, precision/order, quote-scoped identities, constraints, real command/query handling, repeated saves, empty drafts, competing writers, deletion conflicts and rollback after an injected failure between line deletion and insertion. Cleanup deletes only each test's own quote IDs. QuoteHttpTests and QuoteEditingHttpTests exercise the complete workflow against PostgreSQL, including independent-host reload, zero prices, preserved price precision, missing/invalid payloads, stale versions, competing creates and competing additions; events remain unverified. Exact results are maintained in `docs/TODO.md`.
