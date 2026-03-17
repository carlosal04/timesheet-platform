[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-(alpha|beta|rc)\.(0|[1-9]\d*))?$')]
    [string]$Version,

    [string]$Target = "HEAD",

    [string]$Message,

    [switch]$Push
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')"
    }
}

$normalizedVersion = if ($Version.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    "v" + $Version.Substring(1)
}
else {
    "v$Version"
}

if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = "Release $normalizedVersion"
}

$repoRoot = Invoke-Git rev-parse --show-toplevel
$repoRoot = $repoRoot.Trim()

$status = Invoke-Git status --porcelain
if (-not [string]::IsNullOrWhiteSpace($status)) {
    throw "Working tree must be clean before creating a release tag."
}

Invoke-Git rev-parse --verify $Target | Out-Null

$existingTag = & git tag --list $normalizedVersion
if ($LASTEXITCODE -ne 0) {
    throw "Unable to verify existing tags."
}

if (-not [string]::IsNullOrWhiteSpace($existingTag)) {
    throw "Tag '$normalizedVersion' already exists. Release tags must never be reused."
}

if ($PSCmdlet.ShouldProcess($repoRoot, "Create annotated tag $normalizedVersion on $Target")) {
    Invoke-Git tag -a $normalizedVersion $Target -m $Message
}

if ($Push) {
    if ($PSCmdlet.ShouldProcess("origin", "Push tag $normalizedVersion")) {
        Invoke-Git push origin $normalizedVersion
    }
}

Write-Host "Created tag $normalizedVersion on $Target."
if ($Push) {
    Write-Host "Pushed tag $normalizedVersion to origin."
}
