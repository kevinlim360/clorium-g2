param([int]$Port = 5108)
$ErrorActionPreference = 'Stop'
$licensingRoot = $PSScriptRoot
$workspaceRoot = [IO.Path]::GetFullPath((Join-Path $licensingRoot '../..'))
$runRoot = Join-Path $workspaceRoot ('.local/licensing-check-' + [guid]::NewGuid().ToString('N'))
$serverData = Join-Path $runRoot 'server'
$clientData = Join-Path $runRoot 'client'
New-Item -ItemType Directory -Path $serverData -Force | Out-Null
$serverDll = Join-Path $licensingRoot 'Server/bin/Release/net10.0/Clorium.LicenseServer.dll'
$clientExe = Join-Path $licensingRoot 'Tests/bin/Release/net48/Clorium.Licensing.Tests.exe'
$baseUrl = "http://127.0.0.1:$Port"
$serverProcess = Start-Process -FilePath 'dotnet' -ArgumentList @(('"' + $serverDll + '"'), '--data', ('"' + $serverData + '"'), '--urls', $baseUrl) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runRoot 'server.log') -RedirectStandardError (Join-Path $runRoot 'server-error.log')
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        try { $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 1; if ($health.Status -eq 'ok') { $ready = $true; break } } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw "Temporary test server did not start. Logs: $runRoot" }
    & $clientExe $baseUrl $clientData (Join-Path $serverData 'admin-token.txt') | Tee-Object -FilePath (Join-Path $workspaceRoot 'evidence/licensing-integration.log')
    if ($LASTEXITCODE -ne 0) { throw 'Licensing integration checks failed.' }
    $adminExe = Join-Path $licensingRoot 'Admin/bin/Release/net10.0-windows/Clorium.LicenseManager.exe'
    $uiPng = Join-Path $workspaceRoot 'evidence/licensing-admin-connected.png'
    $uiResult = Join-Path $runRoot 'admin-ui-check.txt'
    $adminProcess = Start-Process -FilePath $adminExe -ArgumentList @('--check',$baseUrl,('"' + (Join-Path $serverData 'admin-token.txt') + '"'),('"' + $uiPng + '"'),('"' + $uiResult + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($adminProcess.ExitCode -ne 0) { throw "Admin UI check failed. $uiResult.error" }
    Copy-Item -LiteralPath $uiResult -Destination (Join-Path $workspaceRoot 'evidence/licensing-admin-ui.log') -Force
    $secretAcl = Get-Acl -LiteralPath $serverData
    $backupAcl = Get-Acl -LiteralPath ($uiResult + '.backup.zip')
    foreach ($privateAcl in @($secretAcl, $backupAcl)) {
        if (-not $privateAcl.AreAccessRulesProtected) { throw 'Private data ACL still inherits.' }
        foreach ($rule in $privateAcl.Access) { if ($rule.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value -in @('S-1-1-0','S-1-5-11','S-1-5-32-545')) { throw 'Private data is exposed to general users.' } }
    }
    Write-Output 'PASS server data and exported backup ACL restrict access'
    Stop-Process -Id $serverProcess.Id -Force
    $serverProcess.WaitForExit()
    $restoredData = Join-Path $runRoot 'restored-server'
    New-Item -ItemType Directory -Path $restoredData -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $clientData 'backup.zip'), $restoredData)
    $serverProcess = Start-Process -FilePath 'dotnet' -ArgumentList @(('"' + $serverDll + '"'), '--data', ('"' + $restoredData + '"'), '--urls', $baseUrl) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runRoot 'restored.log') -RedirectStandardError (Join-Path $runRoot 'restored-error.log')
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        try { $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 1; if ($health.Status -eq 'ok') { $ready = $true; break } } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw 'Restored server did not start.' }
    & $clientExe --restore $baseUrl $clientData (Join-Path $serverData 'admin-token.txt') | Tee-Object -FilePath (Join-Path $workspaceRoot 'evidence/licensing-restore.log')
    if ($LASTEXITCODE -ne 0) { throw 'Restored server authentication failed.' }
    Copy-Item -LiteralPath (Join-Path $clientData 'test-result.json') -Destination (Join-Path $workspaceRoot 'evidence/licensing-test-result.json') -Force
    [IO.File]::WriteAllText((Join-Path $workspaceRoot '.local/licensing-latest-test.txt'), $runRoot)
    Write-Output "Isolated test state preserved: $runRoot"
} finally { if (-not $serverProcess.HasExited) { Stop-Process -Id $serverProcess.Id -Force } }
