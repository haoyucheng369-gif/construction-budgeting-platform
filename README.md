# Construction Budgeting Platform

Construction ERP services for project quotations, resource costing, budget comparison, and margin calculation. Changes to quantities and resource prices propagate between services through an event bus, with real-time updates to the quotation workspace.

**Project status:** Sales service skeleton and local infrastructure are implemented and verified (T01). The domain includes `Quantity`, EUR `Money`, `QuoteLine`, and a minimal `Quote` aggregate that creates and adds lines, rejects duplicate line IDs within a quote, and totals rounded line amounts. Unit tests cover these rules. Editing/removal operations, versioning, business endpoints, persistence, the remaining service hosts, messaging integration and the React application are not yet implemented. See the [delivery checklist](docs/TODO.md) for progress and the [six implementation stages](docs/implementation-plan.zh-CN.md) for the roadmap.

## Core workflow

1. Create a project and a draft quotation.
2. Add work items with quantities, sales prices, and material/labor compositions.
3. Change a quote quantity, sales price, or resource cost.
4. Recalculate estimated cost, compare it with the existing budget, and update margin.
5. Notify connected clients and refresh the affected quotation data.

The initial release uses EUR, tax-exclusive amounts, fixed sales prices, and linear resource consumption. Resource-cost changes affect costs and margin without automatically changing customer sales prices.

## Architecture

Four independently runnable .NET services share versioned integration contracts:

| Service | Responsibility |
| --- | --- |
| Sales / Vente | Projects, draft quotations, sales amounts, final margin view, REST API and SignalR |
| Library | Material and labor resources and their cost prices |
| Compositions | Work-item recipes, resource-price projections and quote cost calculations |
| Budget | Read existing budget baselines and calculate budget variance |

Each service keeps business rules separate from persistence, transport and API adapters. MediatR dispatches commands, queries and domain-event handlers within a process; MassTransit/RabbitMQ carries integration events between services.

Sales retains its existing Clean Architecture skeleton (`Domain`, `Application`, `Infrastructure`, `Api`). Compositions is planned with explicit Hexagonal Architecture boundaries: a business core, inbound/outbound ports, and messaging/persistence adapters. Library and Budget will use simple layered structures. All services follow inward dependencies and common integration conventions; see the [architecture decisions](docs/architecture-decisions.zh-CN.md) for rationale and tradeoffs.

```mermaid
flowchart LR
    UI[React workspace] -->|REST| Sales[Sales]
    UI -->|REST| Library[Library]
    Sales -->|QuoteInputsChanged| MQ[(RabbitMQ)]
    Library -->|ResourcePriceChanged| MQ
    MQ -->|input events| Compositions[Compositions]
    Compositions -->|CompositionCosted| MQ
    MQ -->|cost snapshot| Budget[Budget]
    SQL[(SQL Server)] -->|read-only baseline| Budget
    Budget -->|BudgetImpactCalculated| MQ
    MQ -->|result| Sales
    Sales -->|SignalR notification| UI
```

## Technology

| Area | Target stack |
| --- | --- |
| Backend | .NET 8, C#, ASP.NET Core REST APIs |
| Frontend | React, TypeScript, React Query |
| Architecture | Hexagonal / Clean Architecture, DDD, CQRS |
| In-process dispatch | MediatR |
| Integration events | MassTransit, RabbitMQ |
| Persistence | EF Core, PostgreSQL, SQL Server read adapter |
| Real-time updates | SignalR |
| Local runtime | Docker, Docker Compose |
| Verification | Focused unit and integration tests; browser acceptance checks |

## Data ownership

PostgreSQL is the read/write Vente store. The `sales`, `library`, `compositions`, and `budget_projection` schemas are owned by their respective services. Services do not read or write each other's tables; cross-service updates use integration events.

SQL Server supplies the existing budget baseline through a read-only application account. Initialization scripts own baseline setup. Calculated budget variance and message-processing state belong in PostgreSQL, without updating the SQL Server baseline.

Sharing one PostgreSQL database simplifies local operation while retaining service-level ownership. It does not provide independent database availability for each service.

## Initial release boundary

The release covers draft quotations, resource cost changes, composition calculations, budget comparison, margin, single-instance SignalR, basic concurrency protection, and duplicate/stale-message handling.

Kubernetes, cloud deployment, CI/CD pipelines, authentication/authorization, Redis, service replication, approval workflows, purchasing, invoicing and automatic sales-price adjustment are outside this release. The runtime target is a local Docker Compose environment.

## Development and progress

### Prerequisites

- .NET SDK 8.0.425 (pinned in `global.json`); the projects target `net8.0`.
- Docker Desktop with the Linux engine running and Docker Compose v2.
- Node 22.22.0 / npm 10.9.4 for the upcoming React application; Node is pinned in `.node-version`.

On this Windows environment the .NET 8 SDK is installed separately at `%LOCALAPPDATA%\Microsoft\dotnet`. The session helper selects it without changing the machine's existing .NET installation. A system installation matching `global.json` works as well.

### Start local infrastructure

From the repository root in PowerShell:

```powershell
. ./scripts/Use-Dotnet.ps1
./scripts/Initialize-LocalEnvironment.ps1
docker compose up -d --wait --wait-timeout 180 postgres sqlserver rabbitmq
docker compose run --rm sqlserver-init
./scripts/Test-Infrastructure.ps1
```

The environment initializer creates an ignored `.env` with generated local passwords and preserves an existing file. PostgreSQL initializes service schemas on its first empty-volume startup; SQL Server initialization can be run again safely. Changing `.env` does not rotate credentials already stored in database or RabbitMQ volumes.

| Component | Local endpoint | Account |
| --- | --- | --- |
| PostgreSQL / `vente` | `127.0.0.1:5432` | `sales_app`, `library_app`, `compositions_app`, `budget_projection_app` |
| SQL Server / `Budget` | `127.0.0.1:1433` | `budget_reader` (read-only) |
| RabbitMQ / `construction` vhost | `127.0.0.1:5672` | `cb_app` |
| RabbitMQ management | `http://127.0.0.1:15672` | `cb_app` |

Passwords are in the corresponding variables in `.env`; administrative credentials are used only for initialization and container checks. SQL Server uses the Developer edition for local development. Published container ports bind to localhost.

### Build, verify and run Sales

```powershell
. ./scripts/Use-Dotnet.ps1
dotnet restore ConstructionBudgeting.sln --locked-mode
dotnet build ConstructionBudgeting.sln --no-restore
dotnet test ConstructionBudgeting.sln --no-build --no-restore
dotnet run --no-build --project src/Services/Sales/ConstructionBudgeting.Sales.Api --launch-profile Sales
```

`GET http://127.0.0.1:5080/health` returns `200 Healthy`. This is host liveness, not database/broker readiness. The Sales host currently has no database or message-broker integration; `Test-Infrastructure.ps1` verifies those components independently, including schema isolation and rejected SQL Server writes.

Stop the API with Ctrl+C. `docker compose stop` stops the infrastructure and retains its data. The React app and business endpoints have not been created yet.

The Sales projects enforce the inward dependency boundary: API references Application and Infrastructure; Infrastructure references Application; Application references Domain. See [Sales service boundaries](src/Services/Sales/README.md).

### Project records

- [Project context and working agreements](docs/CONTEXT.md)
- [Delivery checklist and current handoff](docs/TODO.md)
- [Two-week implementation plan](docs/implementation-plan.zh-CN.md)
- [Architecture and business decisions](docs/architecture-decisions.zh-CN.md)

Planned capabilities are tracked as open checklist items until validated. NuGet dependency graphs are committed in `packages.lock.json`; framework packages and container images are version-pinned.
