[CmdletBinding()]
param(
    [string]$ApiArtifact,

    [string]$UiArtifact,

    [string]$ReleaseId = (Get-Date -Format 'yyyyMMdd-HHmmss'),

    [string]$ExpectedMigration = '20260905170000_AddJobDescriptionReviewWarning',

    [string]$StatusFile = (Join-Path $env:LOCALAPPDATA 'Temp\EosDashboards-local-publish-status.txt')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$deploymentStage = 'startup'

function Set-DeploymentStatus {
    param([string]$Status)

    $statusDirectory = Split-Path -Parent $StatusFile
    if (-not [string]::IsNullOrWhiteSpace($statusDirectory)) {
        New-Item -ItemType Directory -Path $statusDirectory -Force | Out-Null
    }

    [IO.File]::WriteAllText($StatusFile, $Status, [Text.Encoding]::UTF8)
}

trap {
    Set-DeploymentStatus "$deploymentStage|$($_.Exception.GetType().Name)"
    [Console]::Error.WriteLine("Local IIS publication failed during $deploymentStage.")
    exit 1
}

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $PSCommandPath
}

if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'The local publish script location could not be determined.'
}

if ([string]::IsNullOrWhiteSpace($ApiArtifact)) {
    $ApiArtifact = Join-Path $scriptRoot '..\backend\artifacts\api-local-credential'
}

if ([string]::IsNullOrWhiteSpace($UiArtifact)) {
    $UiArtifact = Join-Path $scriptRoot '..\frontend\dist'
}

$siteName = 'Default Web Site'
$apiPath = '/EosDashboardsApi'
$uiPath = '/EosDashboards'
$apiPool = 'EosDashboardsApiPool'
$uiPool = 'EosDashboardsUiPool'
$apiReleaseRoot = 'C:\inetpub\wwwroot\EosDashboards\Api\releases'
$uiReleaseRoot = 'C:\inetpub\wwwroot\EosDashboards\Ui\releases'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-NewReleasePath {
    param(
        [string]$ReleaseRoot,
        [string]$Version
    )

    if ($Version -notmatch '^[0-9]{8}-[0-9]{6}$') {
        throw 'ReleaseId must use yyyyMMdd-HHmmss format.'
    }

    $rootPath = [IO.Path]::GetFullPath($ReleaseRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $releasePath = [IO.Path]::GetFullPath((Join-Path $ReleaseRoot $Version))
    if (-not $releasePath.StartsWith($rootPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The release path is outside the approved release root.'
    }

    if (Test-Path -LiteralPath $releasePath) {
        throw 'The requested release path already exists.'
    }

    return $releasePath
}

function Test-ExpectedDevelopmentMigration {
    param([string]$MigrationId)

    $configurationPath = Join-Path $scriptRoot '..\backend\src\EosDashboards.Api\appsettings.Development.json'
    if (-not (Test-Path -LiteralPath $configurationPath -PathType Leaf)) {
        throw 'The development API configuration required for migration verification was not found.'
    }

    $configuration = Get-Content -Raw -Encoding utf8 $configurationPath | ConvertFrom-Json
    $connectionString = [string]$configuration.ConnectionStrings.EosDashboard
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw 'The development database connection required for migration verification was not configured.'
    }

    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $command = $connection.CreateCommand()
    $command.CommandText = 'SELECT COUNT(*) FROM [__EFMigrationsHistory] WHERE [MigrationId] = @migrationId'
    [void]$command.Parameters.AddWithValue('@migrationId', $MigrationId)
    try {
        $connection.Open()
        if ([int]$command.ExecuteScalar() -ne 1) {
            throw 'The expected development database migration has not been applied.'
        }
    }
    finally {
        $connection.Dispose()
    }
}

$deploymentStage = 'administrator-check'
if (-not (Test-IsAdministrator)) {
    throw 'Run this script from an elevated PowerShell window.'
}

$deploymentStage = 'development-migration-verification'
Test-ExpectedDevelopmentMigration -MigrationId $ExpectedMigration

$deploymentStage = 'artifact-check'
foreach ($artifactPath in @($ApiArtifact, $UiArtifact)) {
    if (-not (Test-Path -LiteralPath $artifactPath -PathType Container)) {
        throw 'A required published artifact directory was not found.'
    }
}

$uiIndexPath = Join-Path $UiArtifact 'index.html'
if (-not (Test-Path -LiteralPath $uiIndexPath -PathType Leaf) -or
    -not ([IO.File]::ReadAllText($uiIndexPath, [Text.Encoding]::UTF8).Contains('/EosDashboards/assets/'))) {
    throw 'The UI artifact must be built with VITE_PUBLIC_BASE=/EosDashboards/ for local IIS hosting.'
}

$uiJavaScriptRelativePath = ([regex]::Match(
    [IO.File]::ReadAllText($uiIndexPath, [Text.Encoding]::UTF8),
    'src="/EosDashboards/(?<path>assets/[^\"]+\.js)"').Groups['path'].Value)
$uiJavaScriptPath = Join-Path $UiArtifact $uiJavaScriptRelativePath.Replace('/', '\\')
if (-not (Test-Path -LiteralPath $uiJavaScriptPath -PathType Leaf) -or
    -not ([IO.File]::ReadAllText($uiJavaScriptPath, [Text.Encoding]::UTF8).Contains('/EosDashboardsApi'))) {
    throw 'The UI artifact must be built with VITE_API_BASE_URL=/EosDashboardsApi for local IIS hosting.'
}

$deploymentStage = 'release-path-validation'
$apiReleasePath = Get-NewReleasePath -ReleaseRoot $apiReleaseRoot -Version $ReleaseId
$uiReleasePath = Get-NewReleasePath -ReleaseRoot $uiReleaseRoot -Version $ReleaseId

$deploymentStage = 'iis-application-check'
Import-Module WebAdministration
$applications = @(Get-WebApplication -Site $siteName)
$apiApplication = $applications | Where-Object { $_.Path -eq $apiPath } | Select-Object -First 1
$uiApplication = $applications | Where-Object { $_.Path -eq $uiPath } | Select-Object -First 1
if ($null -eq $apiApplication -or $apiApplication.ApplicationPool -ne $apiPool) {
    throw 'The expected API IIS application or application pool was not found.'
}

if ($null -eq $uiApplication -or $uiApplication.ApplicationPool -ne $uiPool) {
    throw 'The expected UI IIS application or application pool was not found.'
}

$deploymentStage = 'copy-api-artifact'
New-Item -ItemType Directory -Path $apiReleasePath -Force | Out-Null
New-Item -ItemType Directory -Path $uiReleasePath -Force | Out-Null
Copy-Item -Path (Join-Path $ApiArtifact '*') -Destination $apiReleasePath -Recurse -Force

$deploymentStage = 'copy-ui-artifact'
Copy-Item -Path (Join-Path $UiArtifact '*') -Destination $uiReleasePath -Recurse -Force

$deploymentStage = 'switch-iis-paths'
$appCmd = Join-Path $env:WINDIR 'System32\inetsrv\appcmd.exe'
foreach ($release in @(
    @{ ApplicationName = "$siteName$apiPath/"; PhysicalPath = $apiReleasePath },
    @{ ApplicationName = "$siteName$uiPath/"; PhysicalPath = $uiReleasePath }
)) {
    & $appCmd set vdir "/vdir.name:$($release.ApplicationName)" "/physicalPath:$($release.PhysicalPath)"
    if ($LASTEXITCODE -ne 0) {
        throw 'IIS could not switch an application to its new release.'
    }
}

$deploymentStage = 'api-readiness-check'
$apiStatusCode = & curl.exe --insecure --silent --output NUL --write-out '%{http_code}' 'https://localhost/EosDashboardsApi/health/ready'
if ($LASTEXITCODE -ne 0 -or $apiStatusCode -ne '200') {
    throw 'The local API did not return HTTP 200.'
}

$deploymentStage = 'ui-readiness-check'
Restart-WebAppPool -Name $uiPool
$uiStatusCode = & curl.exe --insecure --silent --output NUL --write-out '%{http_code}' 'https://localhost/EosDashboards/'
if ($LASTEXITCODE -ne 0 -or $uiStatusCode -ne '200') {
    throw 'The local UI did not return HTTP 200.'
}

$deploymentStage = 'ui-spa-refresh-check'
$uiSpaRefreshStatusCode = & curl.exe --insecure --silent --output NUL --write-out '%{http_code}' 'https://localhost/EosDashboards/__eos-spa-refresh-probe'
if ($LASTEXITCODE -ne 0 -or $uiSpaRefreshStatusCode -ne '200') {
    throw 'The local UI did not return the SPA entry point for a refreshed internal route.'
}

Set-DeploymentStatus "completed|$ReleaseId"
Write-Host "Local IIS release $ReleaseId is ready."
