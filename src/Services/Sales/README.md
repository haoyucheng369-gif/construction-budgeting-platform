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

This implements the inward dependency rule of Clean Architecture and the ports/adapters boundary of Hexagonal Architecture. Domain now contains the immutable `Quotations/Quantity` value object: a decimal value strictly greater than zero, with value equality and no rounding. It represents the numeric quantity in a work item's unit; unit association belongs to the upcoming quote-line model. Quote, QuoteLine and Money remain planned. These architecture styles and DDD are not separate framework installations.

External database, message-broker and browser contracts must not replace or leak into the domain model. MediatR, EF Core and messaging packages are added when their use cases are introduced, not to otherwise empty layers.

## Current verification

The integration test project starts the real ASP.NET Core pipeline in memory. It checks liveness without external dependencies and ProblemDetails for unknown routes. `/health` does not yet check database or broker readiness.

`tests/Unit/ConstructionBudgeting.Sales.Domain.Tests` references only Domain. Quantity tests cover positive integer/fractional values, rejection of zero and negative values, and value equality. They run without the API, databases or Docker.
