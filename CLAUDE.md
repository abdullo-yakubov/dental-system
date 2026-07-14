# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Source of truth for requirements

`docs/TZ_Dental_System.md` is the spec. Treat it as directional, not literal — the implemented code has diverged from it in a few confirmed ways:
- Spec says .NET 9; code targets **net10.0** (net10.0-windows for the Launcher).
- Spec describes a Blazor WebAssembly **Hosted** model (Client and Server in one solution/process). In practice Client and Server run as **separate dev processes** (see the comment in `src/DentalLocal.Client/Program.cs`) with CORS configured on the Server for this; there's a standing TODO to move toward the hosted model for the final deployment.
- Spec calls the API-hosting layer `WebApi`; the actual project is named `DentalLocal.Server`.
- Spec's default PIN of `1998` is outdated — the actual default access code is **`111111`** (see `Domain/Common/DentalDefaults.cs` and `Shared/Common/DentalConstants.cs`).

When in doubt about intended behavior, trust the existing code/entities over the spec text. If the user pastes spec text into chat that conflicts with the code (e.g. different entity names, `1998` as the PIN, ApexCharts as a hard requirement), treat the code as authoritative and flag the discrepancy.

## Solution structure

No `.sln` — this repo uses the `DentalLocal.slnx` format. Open/build via that file or by targeting individual `.csproj` files.

Clean Architecture layers (`src/`):
- `DentalLocal.Domain` — entities, enums, value objects, repository interfaces (`IXxxRepository`, `IUnitOfWork`, `ITransactionScope`), shared defaults (`Common/DentalDefaults.cs`). No dependencies on other layers.
- `DentalLocal.Application` — CQRS via MediatR, under `Features/{Area}/` (`Analytics`, `Auth`, `Backups`, `Icd`, `Materials`, `Patients`, `Payments`, `Procedures`, `Reminders`, `Settings`, `Teeth`, `TreatmentPlans`, `Visits`). Commands/queries + handlers live together in one file per feature (e.g. `Features/Patients/CreatePatientCommand.cs` contains the command, validator, and handler). Cross-cutting concerns are `IPipelineBehavior`s in `Behaviors/`: `ValidationBehavior` (FluentValidation), `TransactionBehavior`, `LoggingBehavior`, `PerformanceBehavior`. Business results use `Result`/`Error` in `Common/` — not exceptions. Service abstractions consumed by Server/Client but implemented in lower layers live in `Application/Common/` (`IBackupService`, `IPdfService`, `IPostgresBootstrapService`, `ITokenService`, `ILoginAttemptTracker`, `PasswordHasher`) and `Application/Interfaces/` (`IDashboardService`).
- `DentalLocal.Infrastructure` — cross-layer services not tied to persistence: `Auth/HmacTokenService.cs` (implements `ITokenService`), `Services/BackupService.cs`, `Services/PdfService.cs`, `Database/PostgresBootstrapService.cs` (embedded/managed Postgres lifecycle), `BackgroundServices/DailyMaintenanceBackgroundService.cs`.
- `DentalLocal.Persistence` — EF Core `DentalDbContext` (Npgsql, snake_case naming convention, everything under Postgres schema `"dental"`), `IEntityTypeConfiguration`s, migrations, repository implementations, `Services/DashboardService.cs` (implements `Application.Interfaces.IDashboardService`), `DbSeeder`.
- `DentalLocal.Server` — Minimal API host. Endpoints are grouped as extension methods in `Endpoints/*Endpoints.cs` (e.g. `MapPatientsEndpoints()`), all wired up in `Program.cs`. Route paths are centralized in `DentalLocal.Shared.Common.ApiRoutes`. On startup, `Program.cs` resolves `IPostgresBootstrapService` to bootstrap/shutdown the managed Postgres instance, then runs EF Core migrations, before mapping endpoints.
- `DentalLocal.Client` — standalone Blazor WebAssembly (`Microsoft.NET.Sdk.BlazorWebAssembly`), MudBlazor UI kit, talks to the server via `Services/ApiClient.cs`. Pages live in `Pages/` (Home/dashboard, Patients, PatientDetail, Calendar, Procedures, Inventory, Debtors, Settings, Login); reusable dialogs/components in `Components/` (e.g. `CreateAppointmentFlowDialog`, `RegisterPaymentDialog`, `MaterialDialog`, `Odontogram/`, `Shared/`).
- `DentalLocal.Shared` — DTOs/contracts/route constants shared by Client and Server (no dependency on other project layers), organized under `Contracts/{Area}/`.
- `DentalLocal.Launcher` — WPF desktop entry point (`net10.0-windows`). Not a skeleton anymore: `MainWindow.xaml.cs` launches the Server as a child process, polls it via an `HttpClient` health check, opens the default browser to it, and exposes a system-tray icon (open / restart server / exit) via `System.Windows.Forms.NotifyIcon`.

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

Test projects (`tests/DentalLocal.Application.Tests`, `DentalLocal.Domain.Tests`, `DentalLocal.Integration.Tests`) currently exist as scaffolding only (`UnitTest1.cs` placeholders) — no real coverage yet despite the spec calling for xUnit/Testcontainers/bUnit across Application, Domain, and Integration.

## Local dev environment notes

- The spec calls for a portable/embedded PostgreSQL bundled with the app, started by the Launcher. `Infrastructure/Database/PostgresBootstrapService.cs` + `Application/Common/IPostgresBootstrapService.cs` implement this lifecycle (`BootstrapAsync`/`ShutdownAsync`, called from `Server/Program.cs`); `DentalLocal.Launcher` (WPF) now actually spawns and supervises the Server process rather than just being a stub.
- The connection string comes from `ConnectionStrings:Default` (set via user-secrets on the Server project, not committed).
- Default PIN/access code for login is `111111` (hashed with PBKDF2 — 100,000 iterations, salted — see `Application/Common/PasswordHasher.cs` and `Infrastructure/Auth/HmacTokenService.cs`).
- There's a known TODO in `Server/Program.cs`: log path and other file paths are currently relative to `ContentRootPath` for dev convenience; the final Launcher-based deployment layout (spec section on install/backup folder structure) will need `Data/Logs` etc. under the install root instead.
- Uploaded patient files are served from `Data/Files` under `ContentRootPath` via static files at `/api/v1/files`.

## Conventions to follow

- New backend features go under `Application/Features/{Area}/` as a single file combining command/query + validator + handler, following the existing files there as templates.
- New HTTP routes: add the path constant to `DentalLocal.Shared.Common.ApiRoutes`, then add a `MapXxxEndpoints()` extension in `Server/Endpoints/`, then call it from `Program.cs`.
- Repository interfaces live in `Domain/Repositories/`; implementations in `Persistence/Repositories/`; register new ones in `Persistence/DependencyInjection.cs`.
- All EF Core reads should use `AsNoTracking()`; table/column names follow snake_case via the configured Npgsql naming convention — don't hand-write snake_case in C# code, it's applied automatically.
- Medical-history-bearing entities are meant to use soft delete/archival (`IsArchived`-style flags), not hard deletes — follow this pattern for any new entity holding patient history.
- AutoMapper profiles live in `Application/Mapping/`, one per area (`PatientMappingProfile`, `VisitMappingProfile`, `ProcedureMappingProfile`, `TeethMappingProfile`, `FinancialMappingProfile`, `MaterialMappingProfile`, `ReminderMappingProfile`, `TreatmentPlanMappingProfile`) — add new entity/DTO mappings to the matching profile rather than creating a new one per feature.
