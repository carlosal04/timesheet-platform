param(
    [string]$AdminEmail = "admin@example.com",
    [string]$AdminPassword = "P@ssw0rd123!",
    [string]$AdminName = "Local Admin",
    [switch]$StartPostgres
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

if ($StartPostgres) {
    Push-Location $repoRoot
    try {
        docker compose -f deploy/compose.yaml up -d postgres
    }
    finally {
        Pop-Location
    }
}

$env:BootstrapAdmin__Email = $AdminEmail
$env:BootstrapAdmin__Password = $AdminPassword
$env:BootstrapAdmin__Name = $AdminName
$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet"
$env:NUGET_PACKAGES = Join-Path $repoRoot ".nuget\packages"

Push-Location $repoRoot
try {
    dotnet run --project src/TimeSheet.Modules.EmploymentManagement.Api
}
finally {
    Pop-Location
}
