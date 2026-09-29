# AGENTS.md

Trønder Leikan: internal tournament/scoring platform. Domain rules (point rules, scoreboard ties) are in `docs/TRONDER_LEIKAN.md`; user stories in `docs/backlog.md`. Docs, comments and error messages are written in Norwegian — keep new ones in Norwegian.

## Communication
- Always respond in English, even if the codebase, docs, or backlog contain Norwegian text.
- When asking me questions, ask ONE question at a time and wait for my answer before asking the next.

## Development workflow
- Implement backend features test-first. Write a failing test in the test project, run it to confirm it fails, then implement and re-run.
- Build features in thin vertical slices (domain → endpoint → frontend) and verify each slice before moving on.

## Scope & assumptions
- Implement the simplest solution that meets the story. Don't add extras such as URL state, persistence, or config options unless I ask for them.
- When I ask for a NEW backlog item, don't re-analyze the previous story. Go straight to listing candidate tasks.

## Environment (Windows)
- The shell is PowerShell on Windows. Use PowerShell syntax, not bash-isms.
- Start the app with Aspire: `aspire run`. If a rebuild hangs, stop running dotnet processes (`Get-Process dotnet | Stop-Process`) before retrying.
- For UI verification, use the Playwright MCP server and take screenshots of the changed views.

## Commands

```bash
# Full stack (Postgres, Zitadel, DbMigrator, API, frontend) — requires Docker
aspire run      # or: dotnet run --project src/TronderLeikan.AppHost

# Backend
dotnet build
dotnet test                                          # Api/Infrastructure tests use Testcontainers → Docker must run
dotnet test tests/TronderLeikan.Domain.Tests         # one project
dotnet test --filter "FullyQualifiedName~GameTests"  # one class/test

# EF migrations (dotnet-ef is a local tool: `dotnet tool restore`)
dotnet ef migrations add <Name> --project src/TronderLeikan.Infrastructure

# Frontend (src/frontend, npm, Node 24+)
npm run dev | npm run lint | npm run build           # lint uses --max-warnings 0
```

CI (`.github/workflows/ci.yml`) runs `dotnet build -c Release`, `dotnet test`, and in `src/frontend` `npm ci && npm run lint && npm run build`.

Reset local env (Postgres volume, Zitadel bootstrap): `./reset-local.sh` / `pwsh -File .\reset-local.ps1`. See README "Feilsøking" for known startup errors. Run the AppHost from the main clone, not a worktree (Zitadel port and Postgres volume are shared; `zitadel-bootstrap/` only exists in the main clone).

## Architecture

Clean Architecture, .NET 10: `Domain` ← `Application` ← `Infrastructure` ← `API`. `AppHost` (Aspire) orchestrates everything; `DbMigrator` applies migrations and seeds demo data (`DemoDataSeeder`) before the API starts.

**Custom CQRS mediator (no MediatR).** `Application/Common/Sender.cs` resolves `ICommandHandler<>`/`ICommandHandler<,>`/`IQueryHandler<,>` by reflection and wraps them in `IPipelineBehavior`s (`ObservabilityBehavior` outermost, then `ValidationBehavior` running FluentValidation). Handlers and validators are auto-registered via Scrutor in `Application/Common/DependencyInjection.cs` — just create the class.

**Feature folders.** `Application/<Aggregate>/Commands/<Name>/{Command,CommandHandler,CommandValidator}.cs`, `Queries/<Name>/...`, `Responses/`. Handlers depend on `IAppDbContext`, never on the concrete `AppDbContext` (which is `internal` in Infrastructure).

**Result pattern, no exceptions for business errors.** Handlers return `Result`/`Result<T>`; `Error` and `T` convert implicitly. Errors are defined per aggregate in `Application/Common/Errors/<X>Errors.cs`. `ErrorType` maps to HTTP status in `API/Common/ErrorTypeExtensions.cs`.

**Controllers** inherit `ApiControllerBase` (route `api/v{version}/[controller]`, v1 via URL segment), inject only `ISender`, and end with `.Match(Ok, Problem)` → RFC 9457 ProblemDetails. Enums serialize as strings.

**Domain events → outbox.** Entities inherit `Domain/Common/Entity` and raise `IDomainEvent`s. `AppDbContext.SaveChangesAsync` writes each event to both `OutboxMessages` and the append-only `EventStore` in the same transaction. Every event must have a `Guid` property ending in `Id` (used as stream id), and its namespace must be `Domain.<Aggregate>.Events`. No outbox processor exists yet; `Application/*/EventHandlers` are placeholders.

**EF Core.** If you change the model, also update `tests/TronderLeikan.Application.Tests/TestAppDbContext.cs`, which mirrors the Infrastructure config for the InMemory provider.
## Frontend

`src/frontend` (Next.js) calls the API server-side via `API_BASE_URL`, set by Aspire; `/admin` requires login through Zitadel (`zitadel-admin@zitadel.localhost` / `Password1!`). Details in `src/frontend/AGENTS.md`.

