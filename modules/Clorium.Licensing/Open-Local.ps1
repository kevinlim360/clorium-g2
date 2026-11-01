$ErrorActionPreference = 'Stop'
$buildConfiguration = 'Release'
$buildMarker = Join-Path $PSScriptRoot '.local/active-build.txt'
if (Test-Path -LiteralPath $buildMarker) { $buildConfiguration = [IO.File]::ReadAllText($buildMarker).Trim(); if ($buildConfiguration -notmatch '^[A-Za-z0-9_-]+$') { throw 'Invalid validated build configuration.' } }
$dataDirectory = Join-Path $PSScriptRoot '.local/company-data'
$serverExe = Join-Path $PSScriptRoot 'server/Clorium.LicenseServer.exe'
if (-not (Test-Path -LiteralPath $serverExe)) { $serverExe = Join-Path $PSScriptRoot "Server/bin/$buildConfiguration/net10.0/Clorium.LicenseServer.exe" }
$serverUrl = 'http://127.0.0.1:5088'
$ready = $false
try { $ready = (Invoke-RestMethod -Uri "$serverUrl/health" -TimeoutSec 2).Status -eq 'ok' } catch { }
if (-not $ready) {
    if (-not (Test-Path -LiteralPath $serverExe)) { throw '회사 앱/서버를 먼저 빌드해 주세요.' }
    New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
    $serverProcess = Start-Process -FilePath $serverExe -ArgumentList @('--data',('"'+$dataDirectory+'"'),'--urls',$serverUrl) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dataDirectory 'server.log') -RedirectStandardError (Join-Path $dataDirectory 'server-error.log')
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        Start-Sleep -Milliseconds 250
        try { if ((Invoke-RestMethod -Uri "$serverUrl/health" -TimeoutSec 1).Status -eq 'ok') { $ready = $true; break } } catch { }
    }
    if (-not $ready) { throw "서버를 시작하지 못했습니다. $dataDirectory 의 로그를 확인해 주세요." }
    [IO.File]::WriteAllText((Join-Path $dataDirectory 'server.pid'), $serverProcess.Id.ToString())
}
& (Join-Path $PSScriptRoot 'Open-Manager.ps1') -ServerUrl $serverUrl -AdminTokenFile (Join-Path $dataDirectory 'admin-token.txt')
