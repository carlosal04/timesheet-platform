param(
    [string]$EnvironmentFilePath = (Join-Path (Split-Path -Parent $PSScriptRoot) ".env")
)

if (-not (Test-Path $EnvironmentFilePath)) {
    return
}

Get-Content $EnvironmentFilePath | ForEach-Object {
    $line = $_.Trim()
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
        return
    }

    $parts = $line.Split("=", 2)
    if ($parts.Count -ne 2) {
        return
    }

    $name = $parts[0].Trim()
    $value = $parts[1].Trim()
    if ([string]::IsNullOrWhiteSpace($name)) {
        return
    }

    [System.Environment]::SetEnvironmentVariable($name, $value, "Process")
}
