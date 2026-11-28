# Setup

Install .NET SDK 10 and PostgreSQL. For the local server, configure `ConnectionStrings__Default` for an isolated database. The committed `CHANGE_ME` value is not a secure deployment secret. Run `dotnet restore src/DentalLocal.Server/DentalLocal.Server.csproj`, then `dotnet run --project src/DentalLocal.Server`. Run the Blazor client separately with `dotnet run --project src/DentalLocal.Client`.

The server-only build now passes after adding backup feature and contract sources. The Windows installer needs Inno Setup and `Database/pgsql`; see `installer/README.md`. Do not install with patient records until auth, backup and restore have been verified on a disposable machine.
