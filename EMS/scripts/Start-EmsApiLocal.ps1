param(
    [string]$AdminEmail,
    [string]$AdminPassword,
    [string]$AdminName,
    [switch]$StartPostgres
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "Load-EmsLocalEnv.ps1")

$dbHost = if ($env:EMS_DB_HOST) { $env:EMS_DB_HOST } else { "localhost" }
$dbPort = if ($env:EMS_DB_PORT) { $env:EMS_DB_PORT } else { "54329" }
$dbName = if ($env:EMS_DB_NAME) { $env:EMS_DB_NAME } else { "ems" }
$dbUser = if ($env:EMS_DB_USER) { $env:EMS_DB_USER } else { "ems" }
$dbPassword = $env:EMS_DB_PASSWORD

if ([string]::IsNullOrWhiteSpace($dbPassword)) {
    throw "EMS_DB_PASSWORD is required. Set it in EMS/.env or the current shell."
}

if ([string]::IsNullOrWhiteSpace($AdminEmail)) {
    $AdminEmail = $env:EMS_BOOTSTRAP_ADMIN_EMAIL
}

if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    $AdminPassword = $env:EMS_BOOTSTRAP_ADMIN_PASSWORD
}

if ([string]::IsNullOrWhiteSpace($AdminName)) {
    $AdminName = if ($env:EMS_BOOTSTRAP_ADMIN_NAME) { $env:EMS_BOOTSTRAP_ADMIN_NAME } else { "Local Admin" }
}

if ($StartPostgres) {
    Push-Location $repoRoot
    try {
        docker compose -f deploy/compose.yaml up -d postgres
    }
    finally {
        Pop-Location
    }
}

$env:ConnectionStrings__EmploymentManagement = "Host=$dbHost;Port=$dbPort;Database=$dbName;Username=$dbUser;Password=$dbPassword"

if (-not [string]::IsNullOrWhiteSpace($AdminEmail)) {
    $env:BootstrapAdmin__Email = $AdminEmail
}

if (-not [string]::IsNullOrWhiteSpace($AdminPassword)) {
    $env:BootstrapAdmin__Password = $AdminPassword
}

if (-not [string]::IsNullOrWhiteSpace($AdminName)) {
    $env:BootstrapAdmin__Name = $AdminName
}

$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet"
$env:NUGET_PACKAGES = Join-Path $repoRoot ".nuget\packages"

Push-Location $repoRoot
try {
    dotnet run --project src/TimeSheet.Modules.EmploymentManagement.Api
}
finally {
    Pop-Location
}
