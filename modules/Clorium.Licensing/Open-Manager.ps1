param([string]$ServerUrl = 'http://127.0.0.1:5088', [string]$AdminTokenFile = '')
$ErrorActionPreference = 'Stop'
if (-not $AdminTokenFile) { $AdminTokenFile = Join-Path $PSScriptRoot '.local/company-data/admin-token.txt' }
$managerExe = Join-Path $PSScriptRoot 'admin/Clorium.LicenseManager.exe'
if (-not (Test-Path -LiteralPath $managerExe)) { $managerExe = Join-Path $PSScriptRoot 'Admin/bin/Release/net10.0-windows/Clorium.LicenseManager.exe' }
if (-not (Test-Path -LiteralPath $AdminTokenFile)) { throw '서버를 먼저 시작해 관리자 인증키를 생성하거나 -AdminTokenFile에 회사에서 받은 키 파일을 지정해 주세요.' }
& $managerExe --server $ServerUrl --token-file ([IO.Path]::GetFullPath($AdminTokenFile))
