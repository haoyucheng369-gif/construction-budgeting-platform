# Sales service

The Sales service owns project quotations and the final margin view. The current host exposes `/health` as a liveness endpoint; business operations are tracked under T02.

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

`Quotations/Quote` is the aggregate root with a quote ID and project ID. `AddLine` creates each line internally, rejects duplicate line IDs within that quote, and updates the total from already-rounded line amounts. The exposed collection is read-only. Validation or calculation failures leave the quote unchanged. Line IDs are scoped to their quote; project existence and one-draft-per-project checks remain for persistence/application work.

`ChangeLineQuantity` preserves line identity, metadata and unit price while replacing the line with a newly calculated read-only instance and updating the total. It returns the updated line; callers retaining the old instance must read the new one. Equal quantities are a no-op. Missing lines, null values or calculation overflow fail before changing state. This is an in-memory operation without messaging or concurrency control.

`ChangeLineSalesUnitPrice` similarly preserves identity, metadata and quantity, accepts zero prices, and rejects negative prices through QuoteLine validation. Equal prices are a no-op; different raw prices are retained even if rounded line amounts match. Both change operations use the current line held by the aggregate.

`RemoveLine` removes a line from the current quote and subtracts its latest rounded amount. Removing the last line leaves a valid empty draft with a zero EUR total. Empty IDs and missing lines (including repeated removal) fail without changing state. The quote and project identities are retained.

`Version` is an externally read-only `long` starting at 1. Each successful addition, removal or actual quantity/price change advances it once, even if the total stays the same. Equal-value changes and failures preserve the version. The checked increment is calculated before changing state. This tracks quotation inputs in memory; persisted version restoration, atomic database concurrency checks and conflict responses remain planned.

External database, message-broker and browser contracts must not replace or leak into the domain model. MediatR, EF Core and messaging packages are added when their use cases are introduced, not to otherwise empty layers.

## Quantity-change application use case

`Application/Quotations/ChangeQuoteLineQuantity` contains the command, handler and result DTO. The command carries QuoteId, LineId, decimal Quantity and ExpectedVersion. The handler validates input, loads the quote through `IQuoteRepository`, rejects a mismatched version, calls the domain method and saves effective changes with the original version. Equal quantities skip saving. The result copies domain-calculated amounts and the resulting version; it does not expose the mutable aggregate.

`IQuoteRepository` is an application-owned port. Its future persistence implementation must load a unit-of-work-local aggregate and atomically compare the stored version when saving. The handler's initial comparison alone cannot prevent a write racing with the save. `QuoteConcurrencyException` is also the contract for save-time conflicts. Storage errors propagate; after a failed save the modified in-memory unit of work must be discarded.

`AddSalesApplication()` registers MediatR 12.5.0 and handlers. Tests exercise real `ISender.Send` dispatch with a scoped repository test double. The API has not yet called this registration or exposed the business command; no production repository adapter exists. HTTP routes, error mapping, database atomicity and integration events remain subsequent work.

## Quotation query use case

`Application/Quotations/GetQuote` contains `GetQuoteQuery`, `GetQuoteHandler`, `GetQuoteResult` and `QuoteLineDetails`. The query carries only QuoteId. Its handler reads through the same `IQuoteRepository` and copies quote/project IDs, version, currency, total and ordered line details into a detached result. It preserves unit-price precision and copies calculated amounts without recalculating them. The returned line collection is materialized and wrapped as read-only; later aggregate changes do not alter an earlier result.

Queries do not call domain mutation methods or `SaveAsync`. Empty drafts return zero EUR and an empty collection; missing quotes throw `KeyNotFoundException`. Invalid IDs, cancellation and storage errors are propagated without returning fabricated data. Existing MediatR assembly registration discovers the query handler. This is separation of application read/write responsibilities using the same repository, with no separate read database or HTTP endpoint. Loading the whole aggregate keeps the current implementation small; a dedicated read projection remains an option if later requirements justify it.

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
