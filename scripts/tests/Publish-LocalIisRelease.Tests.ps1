Import-Module Pester

Describe 'Publish-LocalIisRelease' {
    It 'uses UAC from the canonical finalization entry point instead of stopping before publication' {
        $scriptPath = Join-Path $PSScriptRoot '..\Finalize-LocalIisRelease.ps1'
        $scriptText = Get-Content -LiteralPath $scriptPath -Raw

        $scriptText | Should Match 'Start-Process[\s\S]*-Verb RunAs'
        $scriptText | Should Match 'Publish-LocalIisRelease.ps1'
    }

    It 'has a one-command build and publish entry point' {
        $scriptPath = Join-Path $PSScriptRoot '..\Finalize-LocalIisRelease.ps1'
        Test-Path -LiteralPath $scriptPath -PathType Leaf | Should Be $true
        (Get-Content -LiteralPath $scriptPath -Raw) | Should Match 'Publish-LocalIisRelease.ps1'
    }

    It 'requires the latest tracked development migration by default' {
        $scriptPath = Join-Path $PSScriptRoot '..\Publish-LocalIisRelease.ps1'
        (Get-Content -LiteralPath $scriptPath -Raw) | Should Match '20260905065524_AddGradientPreference'
    }

    It 'removes inherited WebDAV handlers so API write verbs reach ASP.NET Core' {
        $configurationPath = Join-Path $PSScriptRoot '..\..\backend\src\EosDashboards.Api\web.config'
        [xml]$configuration = Get-Content -LiteralPath $configurationPath -Raw
        $systemWebServer = $configuration.SelectSingleNode('/configuration/location/system.webServer')

        $moduleNames = @($systemWebServer.SelectNodes('./modules/remove') | ForEach-Object { $_.GetAttribute('name') })
        $handlerNames = @($systemWebServer.SelectNodes('./handlers/remove') | ForEach-Object { $_.GetAttribute('name') })
        ($moduleNames -contains 'WebDAVModule') | Should Be $true
        ($handlerNames -contains 'WebDAV') | Should Be $true
        ($systemWebServer.SelectSingleNode('./handlers/add[@name="aspNetCore"]').GetAttribute('verb')) | Should Be '*'
    }

    It 'reports the stage when an inspected artifact is unavailable' {
        $statusFile = Join-Path $TestDrive 'publish-status.txt'
        $scriptPath = Join-Path $PSScriptRoot '..\Publish-LocalIisRelease.ps1'

        $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $scriptPath `
            -ApiArtifact (Join-Path $TestDrive 'missing-api-artifact') `
            -UiArtifact (Join-Path $TestDrive 'missing-ui-artifact') `
            -StatusFile $statusFile 2>&1 | Out-String

        $output | Should Match 'Local IIS publication failed during administrator-check'
        (Get-Content -Raw $statusFile) | Should Match '^administrator-check\|'
    }
}
