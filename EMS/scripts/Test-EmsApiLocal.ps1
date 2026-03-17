param(
    [string]$BaseUrl,
    [string]$Email,
    [string]$Password
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Load-EmsLocalEnv.ps1")

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    $BaseUrl = if ($env:EMS_API_BASE_URL) { $env:EMS_API_BASE_URL } else { "http://localhost:5161" }
}

if ([string]::IsNullOrWhiteSpace($Email)) {
    $Email = $env:EMS_BOOTSTRAP_ADMIN_EMAIL
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    $Password = $env:EMS_BOOTSTRAP_ADMIN_PASSWORD
}

if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
    throw "Email and Password are required. Set EMS_BOOTSTRAP_ADMIN_EMAIL and EMS_BOOTSTRAP_ADMIN_PASSWORD in EMS/.env or pass them explicitly."
}

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

Write-Host "GET /"
Invoke-RestMethod "$BaseUrl/"

Write-Host "`nGET /health"
Invoke-RestMethod "$BaseUrl/health"

Write-Host "`nPOST /auth/login"
$loginResponse = Invoke-WebRequest `
    -Uri "$BaseUrl/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $loginBody `
    -WebSession $session

$loginResponse.Content

Write-Host "`nGET /auth/antiforgery"
Invoke-RestMethod "$BaseUrl/auth/antiforgery" -WebSession $session

Write-Host "`nGET /employees"
Invoke-RestMethod "$BaseUrl/employees" -WebSession $session

Write-Host "`nGET /employees?includePrimaryAddress=true"
Invoke-RestMethod "$BaseUrl/employees?includePrimaryAddress=true" -WebSession $session

Write-Host "`nPOST /auth/logout"
Invoke-WebRequest `
    -Uri "$BaseUrl/auth/logout" `
    -Method POST `
    -ContentType "application/json" `
    -Body "{}" `
    -WebSession $session | Out-Null

Write-Host "`nGET /auth/antiforgery after logout (expect 401)"
try {
    Invoke-WebRequest "$BaseUrl/auth/antiforgery" -WebSession $session | Out-Null
    throw "Expected 401 after logout, but the request succeeded."
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -ne 401) {
        throw
    }

    Write-Host "Received expected 401 Unauthorized."
}
