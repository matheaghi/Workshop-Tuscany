# AGENTS.md

Trønder Leikan: internal tournament/scoring platform. Domain rules (point rules, scoreboard ties) are in `docs/TRONDER_LEIKAN.md`; user stories in `docs/backlog.md`. Docs, comments and error messages are written in Norwegian — keep new ones in Norwegian.

## Communication
- When asking me questions, ask ONE question at a time and wait for my answer before asking the next.

## Scope
- Implement the simplest solution that meets the story. Don't add extras such as URL state, persistence, or config options unless I ask for them.

## Commands

```bash
# Full stack (Postgres, Zitadel, DbMigrator, API, frontend) — requires Docker
aspire run
aspire stop     # if a rebuild hangs: stop, then aspire run again

# Api/Infrastructure tests use Testcontainers → Docker must run
dotnet test

# EF migrations (dotnet-ef is a local tool: `dotnet tool restore`)
dotnet ef migrations add <Name> --project src/TronderLeikan.Infrastructure
```

Frontend lint (`src/frontend`) fails on any warning, and CI runs it.

Reset local env (Postgres volume, Zitadel bootstrap): `./reset-local.sh` / `pwsh -File .\reset-local.ps1`. See README "Feilsøking" for known startup errors. Run the AppHost from the main clone, not a worktree (Zitadel port and Postgres volume are shared; `zitadel-bootstrap/` only exists in the main clone).

## Architecture

Clean Architecture, .NET 10: `Domain` ← `Application` ← `Infrastructure` ← `API`. `DbMigrator` applies migrations and seeds demo data (`DemoDataSeeder`) before the API starts.

**Custom CQRS mediator (no MediatR).** `Application/Common/Sender.cs` dispatches commands and queries through pipeline behaviors; validation runs FluentValidation validators. Handlers and validators are auto-registered via Scrutor — just create the class.

**Feature folders.** `Application/<Aggregate>/Commands/<Name>/{Command,CommandHandler,CommandValidator}.cs`, `Queries/<Name>/...`, `Responses/`. Handlers depend on `IAppDbContext`, never on the concrete `AppDbContext` (which is `internal` in Infrastructure).

**Result pattern, no exceptions for business errors.** Handlers return `Result`/`Result<T>`; `Error` and `T` convert implicitly. Errors are defined per aggregate in `Application/Common/Errors/<X>Errors.cs`. `ErrorType` maps to HTTP status in `API/Common/ErrorTypeExtensions.cs`.

**Controllers** inherit `ApiControllerBase` (route `api/v{version}/[controller]`, v1 via URL segment), inject only `ISender`, and end with `.Match(Ok, Problem)` → RFC 9457 ProblemDetails. Enums serialize as strings.

**Domain events → outbox.** Entities inherit `Domain/Common/Entity` and raise `IDomainEvent`s. `AppDbContext.SaveChangesAsync` writes each event to both `OutboxMessages` and the append-only `EventStore` in the same transaction. Every event must have a `Guid` property ending in `Id` (used as stream id), and its namespace must be `Domain.<Aggregate>.Events`. No outbox processor exists yet; `Application/*/EventHandlers` are placeholders.

**EF Core.** If you change the model, also update `tests/TronderLeikan.Application.Tests/TestAppDbContext.cs`, which mirrors the Infrastructure config for the InMemory provider.

## Frontend

`src/frontend` (Next.js) calls the API server-side via `API_BASE_URL`, set by Aspire; `/admin` requires login through Zitadel (`zitadel-admin@zitadel.localhost` / `Password1!`). Details in `src/frontend/AGENTS.md`.
