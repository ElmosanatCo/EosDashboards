Import-Module Pester

Describe 'Publish-LocalIisRelease' {
    It 'allows a publication that reuses existing IIS runtime configuration' {
        $statusFile = Join-Path $TestDrive 'publish-status.txt'
        $scriptPath = Join-Path $PSScriptRoot '..\Publish-LocalIisRelease.ps1'

        $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $scriptPath `
            -PrivateDataFile '' `
            -Utf8PowerShell 'Z:\missing\pwsh.exe' `
            -StatusFile $statusFile 2>&1 | Out-String

        $output | Should Not Match 'Cannot bind argument'
        $output | Should Match 'Local IIS publication failed during administrator-check'
        (Get-Content -Raw $statusFile) | Should Match '^administrator-check\|'
    }
}
