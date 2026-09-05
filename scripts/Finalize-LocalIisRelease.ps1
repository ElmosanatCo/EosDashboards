[CmdletBinding()]
param(
    [string]$ReleaseId = (Get-Date -Format 'yyyyMMdd-HHmmss'),

    [string]$ExpectedMigration = '20260905170000_AddJobDescriptionReviewWarning'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-ElevatedSelf {
    param(
        [string]$ScriptPath
    )

    $currentPowerShell = (Get-Process -Id $PID).Path
    if ([string]::IsNullOrWhiteSpace($currentPowerShell)) {
        throw 'The current PowerShell executable could not be determined.'
    }

    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $ScriptPath,
        '-ReleaseId',
        $ReleaseId,
        '-ExpectedMigration',
        $ExpectedMigration
    )

    $elevatedProcess = Start-Process -FilePath $currentPowerShell -Verb RunAs -ArgumentList $arguments -Wait -PassThru
    exit $elevatedProcess.ExitCode
}

$scriptRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$scriptPath = [IO.Path]::GetFullPath($PSCommandPath)

if (-not (Test-IsAdministrator)) {
    Write-Host 'Administrator permission is required. Requesting UAC elevation for the complete local IIS release.'
    Invoke-ElevatedSelf -ScriptPath $scriptPath
}

$apiArtifact = Join-Path $repositoryRoot 'backend\artifacts\api-local-credential'
$uiArtifact = Join-Path $repositoryRoot 'frontend\dist'
$apiProject = Join-Path $repositoryRoot 'backend\src\EosDashboards.Api\EosDashboards.Api.csproj'
$publisher = Join-Path $repositoryRoot 'scripts\Publish-LocalIisRelease.ps1'
$currentPowerShell = (Get-Process -Id $PID).Path

if ([string]::IsNullOrWhiteSpace($currentPowerShell)) {
    throw 'The current PowerShell executable could not be determined.'
}

$sourceCommit = (& git -C $repositoryRoot rev-parse --short HEAD).Trim()
Write-Host "Building API artifact from commit $sourceCommit."
& dotnet publish $apiProject -c Release --no-restore -o $apiArtifact
if ($LASTEXITCODE -ne 0) {
    throw 'The Release API artifact build failed.'
}

Write-Host 'Building the IIS UI artifact.'
& npm --prefix (Join-Path $repositoryRoot 'frontend') run build:iis
if ($LASTEXITCODE -ne 0) {
    throw 'The IIS UI artifact build failed.'
}

Write-Host "Publishing local IIS release $ReleaseId."
& $currentPowerShell -NoProfile -ExecutionPolicy Bypass -File $publisher `
    -ApiArtifact $apiArtifact `
    -UiArtifact $uiArtifact `
    -ReleaseId $ReleaseId `
    -ExpectedMigration $ExpectedMigration
if ($LASTEXITCODE -ne 0) {
    throw 'The local IIS publisher failed.'
}
