<#
.SYNOPSIS
    Publishes Server, Client (WASM) and Launcher into installer\build\* for the Inno Setup
    installer build (DentalLocal.iss). See installer/README.md.

.PARAMETER Configuration
    Build configuration (default: Release).

.PARAMETER Runtime
    RID for self-contained publish (default: win-x64).
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$buildDir = Join-Path $PSScriptRoot "build"
$serverOut = Join-Path $buildDir "server"
$clientOut = Join-Path $buildDir "client"
$launcherOut = Join-Path $buildDir "launcher"

Write-Host "== Cleaning installer\build ==" -ForegroundColor Cyan
Remove-Item -Recurse -Force $buildDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $buildDir | Out-Null

Write-Host "== Publishing Server (self-contained, $Runtime) ==" -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot "src\DentalLocal.Server\DentalLocal.Server.csproj") `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=false -p:PublishTrimmed=false `
    -o $serverOut
if ($LASTEXITCODE -ne 0) { throw "Server publish failed." }

Write-Host "== Publishing Client (Blazor WASM) ==" -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot "src\DentalLocal.Client\DentalLocal.Client.csproj") `
    -c $Configuration -o $clientOut
if ($LASTEXITCODE -ne 0) { throw "Client publish failed." }

Write-Host "== Publishing Launcher (self-contained, $Runtime) ==" -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot "src\DentalLocal.Launcher\DentalLocal.Launcher.csproj") `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=false -p:PublishTrimmed=false `
    -o $launcherOut
if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed." }

Write-Host "== Merging Client wwwroot into Server (hosted model, see Server/Program.cs) ==" -ForegroundColor Cyan
$clientWwwroot = Join-Path $clientOut "wwwroot"
$serverWwwroot = Join-Path $serverOut "wwwroot"
if (-not (Test-Path $clientWwwroot)) { throw "Client wwwroot not found at $clientWwwroot - publish did not produce it." }
Copy-Item -Path (Join-Path $clientWwwroot "*") -Destination $serverWwwroot -Recurse -Force

Write-Host "== Done ==" -ForegroundColor Green
Write-Host "Server:   $serverOut"
Write-Host "Launcher: $launcherOut"
Write-Host ""
Write-Host "Next: ISCC installer\DentalLocal.iss /DAppVersion=1.0.0"
