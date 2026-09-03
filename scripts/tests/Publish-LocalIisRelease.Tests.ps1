Import-Module Pester

Describe 'Publish-LocalIisRelease' {
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
