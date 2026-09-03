[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PrivateDataFile,

    [switch]$ValidateOnly,

    [switch]$SkipAdministratorProvisioning,

    [switch]$ProvisionAdministratorFromPrivateData,

    [string]$StatusFile = (Join-Path $env:LOCALAPPDATA 'Temp\EosDashboards-local-install-status.txt')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$apiApplicationLocation = 'Default Web Site/EosDashboardsApi'
$apiApplicationPool = 'EosDashboardsApiPool'
$keyRingPath = 'C:\ProgramData\EosDashboards\keys'
$uiOrigin = 'https://localhost'
$apiReleaseRoot = 'C:\inetpub\wwwroot\EosDashboards\Api\releases'
$backendDirectory = Join-Path $PSScriptRoot '..\backend'
$deploymentStage = 'private-data-validation'

function Set-DeploymentStatus {
    param([string]$Status)

    $statusDirectory = Split-Path -Parent $StatusFile
    if (-not [string]::IsNullOrWhiteSpace($statusDirectory)) {
        New-Item -ItemType Directory -Path $statusDirectory -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($StatusFile, $Status, [System.Text.Encoding]::UTF8)
}

trap {
    Set-DeploymentStatus "$deploymentStage|failed"
    [Console]::Error.WriteLine("Local IIS configuration failed during $deploymentStage.")
    exit 1
}

function Get-RequiredValue {
    param(
        [hashtable]$Values,
        [string]$Name
    )

    $value = $Values[$Name]
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "The private data file does not contain a usable $Name value."
    }

    return $value.Trim()
}

function Get-PrivateConfiguration {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'The private data file was not found.'
    }

    $values = @{}
    $administratorValuesByName = @{}
    $expectSmsEndpoint = $false

    foreach ($rawLine in [System.IO.File]::ReadAllLines($Path)) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($expectSmsEndpoint) {
            $candidate = $line
            if ($candidate -match '^(?<key>[^:]+):\s*(?<value>.+)$' -and $matches.key -notmatch '^https?$') {
                throw 'The SMS web-service label must be followed by its HTTPS endpoint.'
            }

            $values['SmsEndpoint'] = $candidate
            $expectSmsEndpoint = $false
            continue
        }

        if ($line -match '^Server\s*:\s*(?<value>.+)$') {
            $values['Server'] = $matches.value
            continue
        }

        if ($line -match '^User\s*:\s*(?<value>.+)$') {
            $values['User'] = $matches.value
            continue
        }

        if ($line -match '^Pass\s*:\s*(?<value>.+)$') {
            $values['Pass'] = $matches.value
            continue
        }

        if ($line -match '^DataBase\s*:\s*(?<value>.+)$') {
            $values['DataBase'] = $matches.value
            continue
        }

        if ($line -match '^Sms\s+Web\s+Servise\s*:\s*(?<value>.*)$') {
            if ([string]::IsNullOrWhiteSpace($matches.value)) {
                $expectSmsEndpoint = $true
            }
            else {
                $values['SmsEndpoint'] = $matches.value
            }

            continue
        }

        if ($line -match '^Method\s*:\s*.+$') {
            continue
        }

        if ($line -match '^(?<key>[^:]+):\s*(?<value>.+)$') {
            $administratorValue = $matches.value.Trim()
            $administratorValueName = switch -Regex ($matches.key.Trim()) {
                '^(نام\s*کاربری|Username|User\s*Name)$' { 'Username'; break }
                '^(رمز(?:\s*عبور)?|پسورد|Password)$' { 'Password'; break }
                '^(نام|First\s*Name)$' { 'FirstName'; break }
                '^(نام\s+خانوادگی|Last\s*Name|Family\s*Name)$' { 'LastName'; break }
                '^(?:شماره\s*)?(?:موبایل|همراه|تلفن|تماس)(?:\s*(?:همراه|موبایل))?$|^(?:Mobile|Phone)(?:No|Number)?$' { 'Mobile'; break }
                default { $null }
            }

            if ($null -ne $administratorValueName) {
                if ($administratorValuesByName.ContainsKey($administratorValueName)) {
                    throw "The private data file contains more than one $administratorValueName administrator value."
                }

                $administratorValuesByName[$administratorValueName] = $administratorValue
            }
        }
    }

    if ($expectSmsEndpoint) {
        throw 'The private data file ends before the SMS HTTPS endpoint.'
    }

    $server = Get-RequiredValue $values 'Server'
    $database = Get-RequiredValue $values 'DataBase'
    $user = Get-RequiredValue $values 'User'
    $password = Get-RequiredValue $values 'Pass'
    $smsEndpoint = Get-RequiredValue $values 'SmsEndpoint'

    if (-not [Uri]::TryCreate($smsEndpoint, [UriKind]::Absolute, [ref]$null)) {
        throw 'The SMS endpoint is not an absolute URI.'
    }

    $smsUri = [Uri]$smsEndpoint
    if ($smsUri.Scheme -ne [Uri]::UriSchemeHttps) {
        throw 'The SMS endpoint must use HTTPS.'
    }

    $connectionBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    # SqlConnectionStringBuilder's PowerShell property adapter removes spaces
    # from property names. Set the provider's canonical keywords explicitly.
    $connectionBuilder['Data Source'] = $server
    $connectionBuilder['Initial Catalog'] = $database
    $connectionBuilder['User ID'] = $user
    $connectionBuilder['Password'] = $password
    $connectionBuilder['Encrypt'] = $true
    $connectionBuilder['TrustServerCertificate'] = $true
    $connectionBuilder['MultipleActiveResultSets'] = $false

    $administratorValues = New-Object System.Collections.Generic.List[string]
    foreach ($administratorValueName in @('Username', 'Password', 'FirstName', 'LastName', 'Mobile')) {
        if ($administratorValuesByName.ContainsKey($administratorValueName)) {
            $administratorValues.Add($administratorValuesByName[$administratorValueName])
        }
    }

    return [pscustomobject]@{
        ConnectionString = $connectionBuilder.ConnectionString
        SmsEndpoint = $smsUri.AbsoluteUri
        AdministratorValues = $administratorValues.ToArray()
    }
}

function New-Base64Key {
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $generator.Dispose()
        [Array]::Clear($bytes, 0, $bytes.Length)
    }
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-Utf8TextFromBase64 {
    param([string]$Value)
    return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Value))
}

function Invoke-ProvisionerWithPrivateInput {
    param(
        [string]$Executable,
        [string[]]$InputLines
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $Executable
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $startInfo.StandardInputEncoding = [System.Text.UTF8Encoding]::new($false)
    $startInfo.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $startInfo.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw 'The administrator provisioner could not be started.'
    }

    try {
        foreach ($line in $InputLines) {
            $process.StandardInput.WriteLine($line)
        }

        $process.StandardInput.Close()
        $standardOutput = $process.StandardOutput.ReadToEnd()
        $standardError = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            Output = $standardOutput + [Environment]::NewLine + $standardError
        }
    }
    finally {
        $process.Dispose()
    }
}

$privateConfiguration = Get-PrivateConfiguration -Path $PrivateDataFile

if ($ProvisionAdministratorFromPrivateData -and $privateConfiguration.AdministratorValues.Count -ne 5) {
    throw 'The private data file must contain exactly five administrator values after Method.'
}

if ($ValidateOnly) {
    Set-DeploymentStatus 'private-data-validation|succeeded'
    Write-Host 'Private-data validation succeeded; no changes were made.'
    exit 0
}

$deploymentStage = 'administrator-check'
if (-not (Test-IsAdministrator)) {
    throw 'Run this script from an elevated PowerShell window.'
}

$deploymentStage = 'iis-application-check'
Import-Module WebAdministration
$application = Get-WebApplication -Site 'Default Web Site' | Where-Object { $_.Path -eq '/EosDashboardsApi' }
if ($null -eq $application -or $application.ApplicationPool -ne $apiApplicationPool) {
    throw 'The expected local EosDashboards API IIS application was not found.'
}

if (-not $application.PhysicalPath.StartsWith($apiReleaseRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The API application does not point to the approved local release directory.'
}

$hashingKey = New-Base64Key
$signingKey = New-Base64Key

$deploymentStage = 'key-ring-access'
New-Item -ItemType Directory -Path $keyRingPath -Force | Out-Null
$keyRingAcl = & icacls $keyRingPath /grant 'IIS APPPOOL\EosDashboardsApiPool:(OI)(CI)M'
if ($LASTEXITCODE -ne 0) {
    throw 'Could not grant the API pool access to its key ring.'
}

$environmentValues = [ordered]@{
    'ASPNETCORE_ENVIRONMENT' = 'Production'
    'ConnectionStrings__EosDashboard' = $privateConfiguration.ConnectionString
    'ApiSecurity__AllowedOrigins__0' = $uiOrigin
    'AuthSecurity__HashingKey' = $hashingKey
    'AuthSecurity__SigningKey' = $signingKey
    'AuthSecurity__KeyRingPath' = $keyRingPath
    'Sms__Endpoint' = $privateConfiguration.SmsEndpoint
}

$configurationPath = 'MACHINE/WEBROOT/APPHOST'
$filter = 'system.webServer/aspNetCore/environmentVariables'
$deploymentStage = 'iis-runtime-configuration-read'
foreach ($entry in $environmentValues.GetEnumerator()) {
    $deploymentStage = "iis-runtime-configuration-$($entry.Key)"
    $entryFilter = "$filter/add[@name='$($entry.Key)']"
    $existingEntry = @(Get-WebConfiguration -PSPath $configurationPath -Location $apiApplicationLocation -Filter $entryFilter) | Select-Object -First 1
    if ($null -eq $existingEntry) {
        Add-WebConfiguration -PSPath $configurationPath -Location $apiApplicationLocation -Filter $filter -Value @{ name = $entry.Key; value = $entry.Value }
    }
    else {
        Set-WebConfigurationProperty -PSPath $configurationPath -Location $apiApplicationLocation -Filter $entryFilter -Name 'value' -Value $entry.Value
    }
}

$previousEnvironment = @{}
foreach ($name in @('ConnectionStrings__EosDashboard', 'AuthSecurity__HashingKey', 'AuthSecurity__SigningKey', 'AuthSecurity__Issuer', 'AuthSecurity__Audience', 'AuthSecurity__KeyRingPath', 'ProvisioningDiagnostics__ExposeFailureType')) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

try {
    $env:ConnectionStrings__EosDashboard = $privateConfiguration.ConnectionString
    $env:AuthSecurity__HashingKey = $hashingKey
    $env:AuthSecurity__SigningKey = $signingKey
    $env:AuthSecurity__Issuer = 'EosDashboards'
    $env:AuthSecurity__Audience = 'EosDashboards.Web'
    $env:AuthSecurity__KeyRingPath = $keyRingPath
    $env:ProvisioningDiagnostics__ExposeFailureType = 'true'

    Push-Location $backendDirectory
    try {
        $deploymentStage = 'database-migration'
        & dotnet ef database update --project '.\src\EosDashboards.Infrastructure\EosDashboards.Infrastructure.csproj' --startup-project '.\src\EosDashboards.Api\EosDashboards.Api.csproj'
        if ($LASTEXITCODE -ne 0) {
            throw 'The development database migration failed.'
        }

        if (-not $SkipAdministratorProvisioning) {
            $deploymentStage = 'administrator-provisioning'
            $deploymentStage = 'administrator-provisioner-build'
            & dotnet build '.\tools\EosDashboards.AdminProvisioner\EosDashboards.AdminProvisioner.csproj' -c Release --no-restore
            if ($LASTEXITCODE -ne 0) {
                throw 'The administrator provisioner build failed.'
            }

            $provisionerExecutable = Join-Path $backendDirectory 'tools\EosDashboards.AdminProvisioner\bin\Release\net10.0\EosDashboards.AdminProvisioner.exe'
            if (-not (Test-Path -LiteralPath $provisionerExecutable -PathType Leaf)) {
                throw 'The administrator provisioner executable was not produced.'
            }

            if ($ProvisionAdministratorFromPrivateData) {
                $deploymentStage = 'administrator-windows-identity'
                $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
                if ($null -eq $windowsIdentity.User -or [string]::IsNullOrWhiteSpace($windowsIdentity.Name)) {
                    throw 'The current Windows identity cannot be used for administrator provisioning.'
                }

                $deploymentStage = 'administrator-provisioner-invocation'
                $provisionerResult = Invoke-ProvisionerWithPrivateInput -Executable $provisionerExecutable -InputLines @(
                    $windowsIdentity.User.Value,
                    $windowsIdentity.Name,
                    $privateConfiguration.AdministratorValues[0],
                    $privateConfiguration.AdministratorValues[1],
                    $privateConfiguration.AdministratorValues[2],
                    $privateConfiguration.AdministratorValues[3],
                    $privateConfiguration.AdministratorValues[4],
                    'yes'
                )
                $provisionerOutput = $provisionerResult.Output
                $provisionerExitCode = $provisionerResult.ExitCode
                if ($provisionerExitCode -ne 0) {
                    $provisionerText = $provisionerOutput -join [Environment]::NewLine
                    $failureType = [regex]::Match($provisionerText, 'Diagnostic failure type: (?<type>[A-Za-z0-9.]+)\.')
                    if ($failureType.Success) {
                        $deploymentStage = "administrator-provisioner-$($failureType.Groups['type'].Value)"
                    }
                    elseif ($provisionerText.Contains((Get-Utf8TextFromBase64 '2LnZhdmE24zYp9iqINmE2LrZiCDYtNivLg=='))) {
                        $deploymentStage = 'administrator-provisioner-input-cancelled'
                    }
                    elseif ($provisionerText.Contains((Get-Utf8TextFromBase64 '2LTZhdin2LHZhyDZh9mF2LHYp9mHINmF2LnYqtio2LEg2YbbjNiz2Kou'))) {
                        $deploymentStage = 'administrator-provisioner-invalid-mobile'
                    }
                    elseif ($provisionerText.Contains((Get-Utf8TextFromBase64 '2KfZhtis2KfZhSDYudmF2YTbjNin2Kog2YXZhdqp2YYg2YbYtNiv2Jsg2b7bjNqp2LHYqNmG2K/bjCDYp9mF2YYg2Ygg2b7Yp9uM2q/Yp9mHINiv2KfYr9mHINix2Kcg2KjYsdix2LPbjCDaqdmG24zYry4='))) {
                        $deploymentStage = 'administrator-provisioner-internal-failure'
                    }
                    else {
                        $deploymentStage = 'administrator-provisioner-unclassified-failure'
                    }
                }
            }
            else {
                $deploymentStage = 'administrator-provisioner-invocation'
                & $provisionerExecutable
                $provisionerExitCode = $LASTEXITCODE
            }

            if ($provisionerExitCode -ne 0) {
                throw 'Initial administrator provisioning did not complete.'
            }
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    foreach ($name in $previousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }
}

$deploymentStage = 'iis-local-credential-authentication'
Set-WebConfigurationProperty -PSPath $configurationPath -Location $apiApplicationLocation -Filter 'system.webServer/security/authentication/anonymousAuthentication' -Name 'enabled' -Value $true
Set-WebConfigurationProperty -PSPath $configurationPath -Location $apiApplicationLocation -Filter 'system.webServer/security/authentication/windowsAuthentication' -Name 'enabled' -Value $false

$deploymentStage = 'api-readiness-check'
Restart-WebAppPool -Name $apiApplicationPool
[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
$ready = $false
for ($attempt = 1; $attempt -le 12; $attempt++) {
    try {
        $response = Invoke-WebRequest -Uri 'https://localhost/EosDashboardsApi/health/ready' -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $ready = $true
            break
        }
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $ready) {
    throw 'The API readiness check did not succeed after local configuration.'
}

Set-DeploymentStatus 'completed|succeeded'
Write-Host 'Local configuration, database migration, and API readiness check succeeded.'
