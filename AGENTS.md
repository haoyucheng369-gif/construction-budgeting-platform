# Repository working agreement

## Resume work

1. Read `docs/CONTEXT.md` for the agreed scope, technical depth and collaboration pace.
2. Read `docs/TODO.md` for the current handoff, blockers, next task and acceptance criteria.
3. Read `README.md` for setup and product scope.
4. Read `docs/architecture-decisions.zh-CN.md` before changing business rules, service boundaries or persistence; use `docs/implementation-plan.zh-CN.md` for sequencing.
5. Inspect the actual files and git status before editing. Preserve unrelated changes and reconcile any stale progress notes against evidence.

These files hold the durable project context. Do not depend on prior conversation memory or assume an unchecked task is complete.

## Delivery boundary

- Target roughly two weeks for the core quotation-to-cost-to-budget-to-margin workflow.
- Retain .NET 8, React/TypeScript/React Query, DDD, Clean/Hexagonal Architecture, CQRS, MediatR, MassTransit/RabbitMQ, PostgreSQL, read-only SQL Server, SignalR and Docker Compose.
- Implement independently runnable Sales, Library, Compositions and Budget services incrementally. Keep each service small.
- Keep Kubernetes, cloud deployment, CI/CD, authentication/authorization, Redis, replicas and unrelated ERP modules out of this release unless the user explicitly changes scope.
- Use backend-calculated amounts as authoritative; do not duplicate business calculation engines in the frontend.
- Prioritize the two business change flows and focused verification. Optional enhancements must not delay the core release.
- Framework and package upgrades are deliberate decisions, not automatic prerequisites.

## Documentation and evidence

- Use neutral product and engineering descriptions. Keep recruitment material, personal motivations and source-document questionnaires out of repository documentation.
- Describe functionality according to implementation evidence. Separate planned, implemented and verified states; do not imply deployment, adoption or operational guarantees that have not been established.
- Keep original external source files unchanged; they are not needed to resume the documented implementation.
- Never invent successful tests, completed tasks or working launch commands.

## Explain design decisions

- The user prioritizes understanding the design rationale as well as the resulting code.
- Before substantial implementation, explain the concrete business problem, the proposed responsibility/dependency boundary, the main tradeoff and how it will be verified. Use an example from quotation or resource-price changes.
- Distinguish business rules, architecture choices and infrastructure constraints. Do not present a framework or pattern as necessary merely because it appears in the stack.
- Introduce technical terms through their role in the current feature; avoid delivering large unexplained code changes.
- Persist important rationale in `docs/architecture-decisions.zh-CN.md`. State which parts are implemented and which remain planned.
- Work one small, explained step at a time. A conceptual question calls for explanation, not automatic business-code implementation.
- Before the first domain feature, walk the user through the existing Sales projects and their references. Do not jump from a skeleton overview to completing the entire feature set.
- When the user asks to continue implementation, complete only the next discussed small unit, verify it and summarize before moving to another unit, unless the user explicitly requests a larger batch. A roadmap is not authorization to implement all remaining tasks in one turn.

## Updating progress

- `docs/TODO.md` is the single progress record. Each task has a stable identifier.
- Before coding, identify the current task in the handoff section; do not mark it complete yet.
- Check a box only when the work and its acceptance checks are complete. Leave partial or blocked work unchecked and explain the state.
- After a meaningful work session, update the handoff (current task, verified result, next concrete action, blockers and known limitations) and add one concise work-log row.
- Record relevant file paths, exact useful verification commands and results; distinguish checks not run from checks passed.
- Update README when setup, supported functionality or current implementation status changes. Keep detailed progress in TODO only.
- Record material architecture changes in the decision document rather than relying on chat history.
- Update `docs/CONTEXT.md` only when scope, technical depth or collaboration preferences change. Keep task status and run results in TODO, not in multiple parallel logs.
- Treat previous server/container health as historical evidence; recheck it when needed after resuming. Never assume processes are still running days later.
