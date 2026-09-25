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
