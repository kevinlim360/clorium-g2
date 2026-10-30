# Dot-source this script to populate the current PowerShell process only.
param([Parameter(Mandatory=$true)][string]$SettingsPath)
$ErrorActionPreference = 'Stop'
$settings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
$entries = @($settings.PSObject.Properties)
if ($entries.Count -lt 1 -or $entries.Count -gt 64) { throw 'Unexpected settings field count.' }
foreach ($entry in $entries) {
    if ($entry.Name -notmatch '^CLORIUM_[A-Z0-9_]+$' -or $entry.Value -isnot [string]) {
        throw 'Invalid local settings entry.'
    }
}
foreach ($entry in $entries) {
    [Environment]::SetEnvironmentVariable($entry.Name, $entry.Value, 'Process')
}
Write-Host ('Loaded {0} settings into this PowerShell process. Values are not printed.' -f $entries.Count)
