param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$EfArgs
)

$emsRoot = Split-Path -Parent $PSScriptRoot
$loadEnvScript = Join-Path $PSScriptRoot "Load-EmsLocalEnv.ps1"

if (Test-Path $loadEnvScript) {
    . $loadEnvScript
}

$env:DOTNET_CLI_HOME = Join-Path $emsRoot ".dotnet"
$env:NUGET_PACKAGES = Join-Path $emsRoot ".nuget\\packages"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__EmploymentManagement)) {
    $requiredNames = @(
        "EMS_DB_HOST",
        "EMS_DB_PORT",
        "EMS_DB_NAME",
        "EMS_DB_USER",
        "EMS_DB_PASSWORD"
    )

    $missingNames = @()
    foreach ($name in $requiredNames) {
        $value = [System.Environment]::GetEnvironmentVariable($name, "Process")
        if ([string]::IsNullOrWhiteSpace($value) -or $value.Contains("SET_LOCAL_")) {
            $missingNames += $name
        }
    }

    if ($missingNames.Count -gt 0) {
        throw "Missing required EMS database environment values: $($missingNames -join ', '). Set ConnectionStrings__EmploymentManagement or update EMS/.env with non-placeholder values."
    }

    $env:ConnectionStrings__EmploymentManagement = "Host=$($env:EMS_DB_HOST);Port=$($env:EMS_DB_PORT);Database=$($env:EMS_DB_NAME);Username=$($env:EMS_DB_USER);Password=$($env:EMS_DB_PASSWORD)"
}

if ($env:ConnectionStrings__EmploymentManagement.Contains("SET_LOCAL_")) {
    throw "ConnectionStrings__EmploymentManagement still contains a placeholder value. Update EMS/.env before running EF commands."
}

$toolManifestPath = Join-Path $emsRoot ".config\\dotnet-tools.json"
dotnet tool restore --tool-manifest $toolManifestPath --configfile (Join-Path $emsRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not $EfArgs -or $EfArgs.Count -eq 0) {
    throw "Pass dotnet-ef arguments, for example: migrations list"
}

dotnet tool run dotnet-ef -- @EfArgs
exit $LASTEXITCODE
