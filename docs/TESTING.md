# Testing

Run `dotnet test tests/DentalLocal.Domain.Tests` and `dotnet test tests/DentalLocal.Application.Tests` for unit-level checks. Integration tests under `tests/DentalLocal.Integration.Tests` use Testcontainers and need Docker access. Run `dotnet build DentalLocal.slnx` for the full Windows solution in an unrestricted Windows environment.

Observed in this audit: the full solution restore was blocked by registry access in the sandbox. A server-only restore and build passed after restoring the missing backup modules. Domain tests passed 64/64 and Application tests passed 67/67, including two backup handler regressions. No full app, installer, browser or isolated restore test has passed. These remain required gates for demonstration.
