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

`Quotations/QuoteLine` combines a non-empty line ID, work-item code, description, unit, Quantity and sales unit price. It rejects missing inputs and negative prices, then rounds the product of quantity and unit price to obtain its read-only line amount. Unit codes currently require non-empty text; validation against a work-item/recipe catalog remains planned. The entity has no editing methods or repository. The upcoming Quote aggregate will control line membership, ID uniqueness, edits and totals.

External database, message-broker and browser contracts must not replace or leak into the domain model. MediatR, EF Core and messaging packages are added when their use cases are introduced, not to otherwise empty layers.

## Current verification

The integration test project starts the real ASP.NET Core pipeline in memory. It checks liveness without external dependencies and ProblemDetails for unknown routes. `/health` does not yet check database or broker readiness.

`tests/Unit/ConstructionBudgeting.Sales.Domain.Tests` references only Domain. Quantity tests cover positive integer/fractional values, rejection of zero and negative values, and value equality. They run without the API, databases or Docker.

Money tests cover precision retention, zero/negative amounts, rejected currencies, positive/negative midpoint rounding, immutability and value equality. A unit-price multiplication example checks that rounding is deferred until the final amount.

QuoteLine tests cover line calculation and rounding, zero/negative prices, required identity and metadata, null value objects, separate identities for identical work items, and decimal overflow. These tests do not require infrastructure.
