param([string]$DataDirectory = '', [string]$ListenUrl = 'http://127.0.0.1:5088')
$ErrorActionPreference = 'Stop'
if (-not $DataDirectory) { $DataDirectory = Join-Path $PSScriptRoot '.local/company-data' }
$serverExe = Join-Path $PSScriptRoot 'server/Clorium.LicenseServer.exe'
if (-not (Test-Path -LiteralPath $serverExe)) { $serverExe = Join-Path $PSScriptRoot 'Server/bin/Release/net10.0/Clorium.LicenseServer.exe' }
if (-not (Test-Path -LiteralPath $serverExe)) { throw 'Build.ps1을 먼저 실행해 주세요.' }
$listenUri = [Uri]$ListenUrl
if ($listenUri.Scheme -ne 'https' -and -not $listenUri.IsLoopback) { throw '공개 HTTP 바인딩은 허용하지 않습니다. 루프백에 연결하고 HTTPS 역방향 프록시를 사용해 주세요.' }
& $serverExe --data ([IO.Path]::GetFullPath($DataDirectory)) --urls $ListenUrl
exit $LASTEXITCODE
