# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Source of truth for requirements

`docs/TZ_Dental_System_1.md` is an **earlier draft** of the spec (.NET 9, MediatR-only, entity names like `Attachment`/`Tooth`/`Visit`-only). The actual code was built against a *later, final* version of the spec titled `TZ_DentalSystem.md` — that file is **not present in this repo** (it lived only in a chat conversation). Signs the final spec is the one actually implemented: entity names `ToothState`/`ToothStateHistory`/`PatientFile`/`Appointment`/`TreatmentItem` (not `Tooth`/`ToothSurfaceHistory`/`Attachment`/`Visit`-only/`TreatmentPlanItem`), MudBlazor + ApexCharts.Blazor on the client, a WPF launcher project, Postgres schema `dental`, default access code `1998` via PBKDF2 (not bcrypt), and target framework `net10.0` (not net9.0 as either spec text says).

**When in doubt about intended behavior, trust the existing code/entities over the text of `docs/TZ_Dental_System_1.md`.** If the user pastes spec text into chat, it is likely the outdated draft again — cross-check against actual entities in `src/DentalLocal.Domain/Entities/` before treating pasted text as authoritative.

## Solution structure

No `.sln` — this repo uses the newer `DentalLocal.slnx` format. Open/build via that file or by targeting individual `.csproj` files.

Clean Architecture layers (`src/`):
- `DentalLocal.Domain` — entities, enums, value objects, repository interfaces (`IXxxRepository`, `IUnitOfWork`, `ITransactionScope`). No dependencies on other layers.
- `DentalLocal.Application` — CQRS via MediatR, under `Features/{Area}/` (`Analytics`, `Auth`, `Backups`, `Materials`, `Patients`, `Payments`, `Procedures`, `Teeth`, `Visits`). Commands/queries + handlers live together in one file per feature (e.g. `Features/Patients/CreatePatientCommand.cs` contains the command, validator, and handler). Cross-cutting concerns are `IPipelineBehavior`s in `Behaviors/`: `ValidationBehavior` (FluentValidation), `TransactionBehavior`, `LoggingBehavior`, `PerformanceBehavior`. Business results use `Result`/`Error` in `Common/` — not exceptions. Service abstractions consumed by Server/Client but implemented in lower layers live in `Application/Common/` (`IBackupService`, `IPdfService`, `IPostgresBootstrapService`) and `Application/Interfaces/` (e.g. `IDashboardService`).
- `DentalLocal.Infrastructure` — cross-layer services not tied to persistence: `ITokenService`/`HmacTokenService` (auth tokens), password hashing helpers, `Services/BackupService.cs`, `Services/PdfService.cs`, `Database/PostgresBootstrapService.cs` (embedded/managed Postgres lifecycle), `BackgroundServices/DailyMaintenanceBackgroundService.cs`.
- `DentalLocal.Persistence` — EF Core `DentalDbContext` (Npgsql, snake_case naming convention, everything under Postgres schema `"dental"`), `IEntityTypeConfiguration`s, migrations, repository implementations, `Services/DashboardService.cs` (implements `Application.Interfaces.IDashboardService`), `DbSeeder`.
- `DentalLocal.Server` — Minimal API host. Endpoints are grouped as extension methods in `Endpoints/*Endpoints.cs` (e.g. `MapPatientsEndpoints()`), all wired up in `Program.cs`. Route paths are centralized in `DentalLocal.Shared.Common.ApiRoutes`. On startup, `Program.cs` resolves `IPostgresBootstrapService` to bootstrap/shutdown the managed Postgres instance, then runs EF Core migrations, before mapping endpoints.
- `DentalLocal.Client` — Blazor WebAssembly, MudBlazor UI kit, talks to the server via `Services/ApiClient.cs`. Pages live in `Pages/` (Home/dashboard, Patients, PatientDetail, Calendar, Procedures, Inventory, Debtors, Login); reusable dialogs/components in `Components/` (e.g. `CreateAppointmentFlowDialog`, `RegisterPaymentDialog`, `MaterialDialog`, `Odontogram/`).
- `DentalLocal.Shared` — DTOs/contracts/route constants shared by Client and Server (no dependency on other project layers), organized under `Contracts/{Area}/`.
- `DentalLocal.Launcher` — WPF desktop entry point (net10.0-windows) intended to bundle/launch the portable Postgres + server + browser UI per the spec's local-deployment model; still a bare skeleton.

Dependency registration is one static class per layer: `AddApplication()`, `AddInfrastructure()`, `AddPersistence(connectionString)`, all called from `Server/Program.cs`.

## Build / run / test

```powershell
# Restore & build everything
dotnet build DentalLocal.slnx

# Run the server (bootstraps Postgres and applies EF Core migrations automatically at startup, see Program.cs)
dotnet run --project src/DentalLocal.Server

# Run the Blazor WASM client dev server separately (CORS is configured for this in Program.cs)
dotnet run --project src/DentalLocal.Client

# Add a new EF Core migration (run from repo root; DbContext is in Persistence, startup project is Server)
dotnet ef migrations add <Name> --project src/DentalLocal.Persistence --startup-project src/DentalLocal.Server

# Run all tests
dotnet test DentalLocal.slnx

# Run a single test project
dotnet test tests/DentalLocal.Application.Tests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~MethodOrClassName"
```

Test projects (`tests/`) currently exist as scaffolding only (`UnitTest1.cs` placeholders) — no real coverage yet despite the spec calling for xUnit/Testcontainers/bUnit across Application, Domain, and Integration.

## Local dev environment notes

- The spec calls for a portable/embedded PostgreSQL bundled with the app (started by the Launcher). `Infrastructure/Database/PostgresBootstrapService.cs` + `Application/Common/IPostgresBootstrapService.cs` implement this lifecycle (`BootstrapAsync`/`ShutdownAsync`, called from `Server/Program.cs`); the Launcher project itself is still a skeleton.
- The connection string comes from `ConnectionStrings:Default` (set via user-secrets on the Server project, not committed).
- Default PIN/access code for login is `1998` (hashed with PBKDF2, see `Application/Common/PasswordHasher.cs` and `Infrastructure/Auth/HmacTokenService.cs`).
- There's a known TODO in `Server/Program.cs`: log path and other file paths are currently relative to `ContentRootPath` for dev convenience; the final Launcher-based deployment layout (spec section on install/backup folder structure) will need `Data/Logs` etc. under the install root instead.
- Uploaded patient files are served from `Data/Files` under `ContentRootPath` via static files at `/api/v1/files`.

## Conventions to follow

- New backend features go under `Application/Features/{Area}/` as a single file combining command/query + validator + handler, following the existing files there as templates.
- New HTTP routes: add the path constant to `DentalLocal.Shared.Common.ApiRoutes`, then add a `MapXxxEndpoints()` extension in `Server/Endpoints/`, then call it from `Program.cs`.
- Repository interfaces live in `Domain/Repositories/`; implementations in `Persistence/Repositories/`; register new ones in `Persistence/DependencyInjection.cs`.
- All EF Core reads should use `AsNoTracking()`; table/column names follow snake_case via the configured Npgsql naming convention — don't hand-write snake_case in C# code, it's applied automatically.
- Medical-history-bearing entities are meant to use soft delete/archival (`IsArchived`-style flags), not hard deletes — follow this pattern for any new entity holding patient history.
- AutoMapper profiles live in `Application/Mapping/`, one per area (`PatientMappingProfile`, `VisitMappingProfile`, `ProcedureMappingProfile`, `TeethMappingProfile`, `FinancialMappingProfile`, `MaterialMappingProfile`) — add new entity/DTO mappings to the matching profile rather than creating a new one per feature.
