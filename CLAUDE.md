# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Source of truth for requirements

The actual source of truth is **the code** plus `docs/TZ_Files_Structure.md`. `docs/TZ_Original_Vision.md` (formerly `TZ_Dental_System.md`) is the original spec — kept for historical context only, explicitly marked as such in its own header, and not to be treated as current requirements. Decisions where the code intentionally diverges from that original vision are recorded as they come up in `docs/ADR/`. Known diffs between the original vision and the actual code:
- Spec says .NET 9; code targets **net10.0** (net10.0-windows for the Launcher).
- Spec describes a Blazor WebAssembly **Hosted** model (Client and Server in one solution/process). In dev, Client and Server run as **separate processes** (see the comment in `src/DentalLocal.Client/Program.cs`) with CORS configured on the Server for this; the installer build (`installer/build.ps1`) now produces the final hosted layout by publishing the Client and copying its `wwwroot` into the Server's `wwwroot` (`Server/Program.cs` already calls `UseBlazorFrameworkFiles()`/`UseStaticFiles()`/`MapFallbackToFile("index.html")` for this).
- Spec calls the API-hosting layer `WebApi`; the actual project is named `DentalLocal.Server`.
- Spec's default PIN of `1998` is outdated — the actual default access code is **`111111`** (see `Domain/Common/DentalDefaults.cs` and `Shared/Common/DentalConstants.cs`).
- The 3D odontogram (§17.3) now matches the spec's intent: geometry is generated offline by `tools/teeth-model-generator/` and committed as `wwwroot/models/teeth[-deciduous].glb`, loaded at runtime via `GLTFLoader` in `Client/Components/Odontogram/Odontogram3D.razor` + `wwwroot/js/odontogram3d.js`. Three.js itself (`wwwroot/lib/three/`) is pulled in via a dynamic `import()` only when the 3D view actually mounts, not on initial page load — see `docs/ADR/0004-3d-odontogram-fate.md`.

When in doubt about intended behavior, trust the existing code/entities over the spec text. If the user pastes spec text into chat that conflicts with the code (e.g. different entity names, `1998` as the PIN, ApexCharts as a hard requirement), treat the code as authoritative and flag the discrepancy.

## Solution structure

No `.sln` — this repo uses the `DentalLocal.slnx` format. Open/build via that file or by targeting individual `.csproj` files.

Clean Architecture layers (`src/`):
- `DentalLocal.Domain` — entities, enums, value objects, repository interfaces (`IXxxRepository`, `IUnitOfWork`, `ITransactionScope`), shared defaults (`Common/DentalDefaults.cs`). No dependencies on other layers.
- `DentalLocal.Application` — CQRS via MediatR, under `Features/{Area}/` (`Analytics`, `Auth`, `Backups`, `Discounts`, `DoctorSchedule`, `Icd`, `Materials`, `Patients`, `Payments`, `Prescriptions`, `Procedures`, `Reminders`, `Settings`, `Teeth`, `TreatmentPlans`, `Visits`, `WaitingListing`, `Warranties`). Commands/queries + handlers live together in one file per feature (e.g. `Features/Patients/CreatePatientCommand.cs` contains the command, validator, and handler). Cross-cutting concerns are `IPipelineBehavior`s in `Behaviors/`: `ValidationBehavior` (FluentValidation), `TransactionBehavior`, `LoggingBehavior`, `PerformanceBehavior`. Business results use `Result`/`Error` in `Common/` — not exceptions. Service abstractions consumed by Server/Client but implemented in lower layers live in `Application/Common/` (`IBackupService`, `IPdfService`, `IExcelExportService`, `IPostgresBootstrapService`, `ITokenService`, `ILoginAttemptTracker`, `PasswordHasher`) and `Application/Interfaces/` (`IDashboardService`). There is no `Currencies` feature/entity: multi-currency was deliberately ripped out as dead CRUD nothing referenced (see `docs/ADR/0002-single-currency.md`) — the displayed currency is just a plain string in `AppSettings` (`AppSettingKeys.Currency`), not an entity with an exchange rate.
- `DentalLocal.Infrastructure` — cross-layer services not tied to persistence: `Auth/HmacTokenService.cs` (implements `ITokenService`), `Services/BackupService.cs`, `Services/PdfService.cs`, `Services/ExcelExportService.cs` (ClosedXML), `Database/PostgresBootstrapService.cs` + `Database/ProcessJobObject.cs` (embedded/managed Postgres lifecycle, incl. a Windows Job Object so `postgres.exe` is killed if the Server process dies unexpectedly), `BackgroundServices/DailyMaintenanceBackgroundService.cs`.
- `DentalLocal.Persistence` — EF Core `DentalDbContext` (Npgsql, snake_case naming convention, everything under Postgres schema `"dental"`), `IEntityTypeConfiguration`s, migrations, repository implementations, `Services/DashboardService.cs` (implements `Application.Interfaces.IDashboardService`), `DbSeeder` (idempotent: procedure categories/catalog, ICD codes, default `AppSettings` incl. the access-code hash).
- `DentalLocal.Server` — Minimal API host. Endpoints are grouped as extension methods in `Endpoints/*Endpoints.cs` (e.g. `MapPatientsEndpoints()`), all wired up in `Program.cs`. Route paths are centralized in `DentalLocal.Shared.Common.ApiRoutes`. On startup, `Program.cs` resolves `IPostgresBootstrapService` to bootstrap/shutdown the managed Postgres instance, then runs EF Core migrations, before mapping endpoints.
- `DentalLocal.Client` — standalone Blazor WebAssembly (`Microsoft.NET.Sdk.BlazorWebAssembly`), MudBlazor UI kit, talks to the server via `Services/ApiClient.cs`. Pages live in `Pages/` (Home/dashboard, Patients, PatientDetail, Calendar, Procedures, Inventory, Debtors, Analytics, Settings, NotesSearch, Login); reusable dialogs/components in `Components/` (e.g. `CreateAppointmentFlowDialog`, `RegisterPaymentDialog`, `MaterialDialog`, `Odontogram/` — 2D `OdontogramSvg.razor` and 3D `Odontogram3D.razor`, `Shared/`). Localization (RU/EN/UZ) goes through `Resources/SharedResource.cs` (marker class) + `SharedResource.resx`/`.en.resx`/`.uz.resx`, injected as `IStringLocalizer<SharedResource>` — RU is the default/fallback resx, keys are prefixed by page/area (`Nav_*`, `Page_*`, `Dashboard_*`, `Patients_*`, `Calendar_*`, `Debtors_*`, `Settings_*`, `Common_*`); add new keys to all three files together. `Services/CultureService.cs` drives the active culture from the `Language` app setting.
- `DentalLocal.Shared` — DTOs/contracts/route constants shared by Client and Server (no dependency on other project layers), organized under `Contracts/{Area}/`.
- `DentalLocal.Launcher` — WPF desktop entry point (`net10.0-windows`). `MainWindow.xaml.cs` launches the Server as a child process, polls it via an `HttpClient` health check, opens the default browser to it, and exposes a system-tray icon (open / restart server / exit) via `System.Windows.Forms.NotifyIcon`. If `DentalLocal.Server.exe` isn't found next to the Launcher (e.g. install was moved), it shows a "locate server" button instead of just failing; the user-picked path is persisted to `%LocalAppData%\DentalLocal\launcher.config.json` and reused on future launches.

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

Test projects (`tests/DentalLocal.Domain.Tests`, `DentalLocal.Application.Tests`, `DentalLocal.Integration.Tests`) all have real coverage now:
- `Domain.Tests`/`Application.Tests` — pure unit tests; `Application.Tests` uses self-written in-memory fakes in `TestSupport/` (no Moq/NSubstitute dependency), not a real database.
- `Integration.Tests` — spins up a real Postgres via **Testcontainers** (`TestSupport/DatabaseFixture.cs`, one container shared across the `[Collection("Database")]` collection), runs actual EF Core migrations + `DbSeeder`, and drives real CQRS handlers through `IMediator` end-to-end (login, patient creation, visit+payment flow). **Requires Docker running locally** — these tests will fail with a Docker-endpoint error if the Docker daemon isn't up, independent of any code issue.

## Building the Windows installer

`installer/` contains the Inno Setup packaging (ТЗ §13 / acceptance criterion §21.8) — not wired into `dotnet build`, run separately:

```powershell
# 1. Publish Server + Client (WASM, merged into Server/wwwroot) + Launcher, self-contained win-x64, into installer/build/*
powershell -File installer/build.ps1

# 2. Build the installer itself (requires Inno Setup 6+, ISCC.exe on PATH) -> installer/output/DentalLocal-Setup-<version>.exe
ISCC installer/DentalLocal.iss /DAppVersion=1.0.0
```

Installed layout: Server files live flat under `{app}`, Launcher under `{app}\Launcher`, portable PostgreSQL under `{app}\Database\pgsql` (must be dropped there manually before building the installer — see `installer/README.md`; the folder is `skipifsourcedoesntexist` so the build won't fail if it's absent, but the app won't be able to start its own Postgres without it). This layout matches how `PostgresBootstrapService.FindProjectRootDir()` already resolves paths in production (falls back to `ContentRootPath` when no `src/`/`DentalLocal.slnx` marker is found nearby).

## Local dev environment notes

- The spec calls for a portable/embedded PostgreSQL bundled with the app, started by the Launcher. `Infrastructure/Database/PostgresBootstrapService.cs` + `Application/Common/IPostgresBootstrapService.cs` implement this lifecycle (`BootstrapAsync`/`ShutdownAsync`, called from `Server/Program.cs`); `DentalLocal.Launcher` (WPF) spawns and supervises the Server process rather than just being a stub.
- The connection string comes from `ConnectionStrings:Default` (set via user-secrets on the Server project in dev, not committed; the checked-in `appsettings.json` ships a working default for the portable/installed deployment — `Host=127.0.0.1;Port=5544;Database=dental;Username=dental;Password=CHANGE_ME` — already consistent with `PostgresBootstrapService`'s own defaults).
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
- Domain/Shared enum name clashes are real (e.g. `Gender`, `PaymentMethod`, `PaymentKind` exist in both `DentalLocal.Domain.Enums` and `DentalLocal.Shared.Enums` with different underlying representations — Application handlers cast between them explicitly). Code that references both namespaces needs explicit `using X = ...` aliases or fully-qualified names to avoid `CS0104` ambiguous-reference errors.
- New user-visible Client strings go through the localization system, not hardcoded Russian — inject `IStringLocalizer<SharedResource>` and add matching keys to all three `.resx` files (see the Client bullet above for the key-naming convention).
