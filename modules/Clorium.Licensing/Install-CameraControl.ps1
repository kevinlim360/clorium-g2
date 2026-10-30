param([Parameter(Mandatory=$true)][string]$InstallDirectory, [switch]$StateAlreadyInitialized)
$ErrorActionPreference = 'Stop'
$targetRoot = [IO.Path]::GetFullPath($InstallDirectory).TrimEnd('\')
if (-not (Test-Path -LiteralPath (Join-Path $targetRoot 'CameraControl.exe') -PathType Leaf)) { throw '기존 CameraControl.exe가 있는 클로리움 설치 폴더를 지정하세요.' }
foreach ($process in @(Get-Process -Name CameraControl -ErrorAction SilentlyContinue)) {
    if (-not $process.Path -or [IO.Path]::GetFullPath($process.Path) -eq (Join-Path $targetRoot 'CameraControl.exe')) { throw '클로리움을 정상 종료한 뒤 적용하세요. 진행 중인 촬영·저장을 강제로 종료하지 않습니다.' }
}
$sourceRoot = Join-Path $PSScriptRoot 'CameraControl-modules'
$files = @(Get-ChildItem -LiteralPath $sourceRoot -File | Where-Object { $_.Extension -in @('.exe','.dll') })
if ($files.Count -lt 7) { throw '고객 모듈 패키지가 완전하지 않습니다.' }
$backup = Join-Path $targetRoot ('license-module-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$replaced = @()
try {
    if (-not $StateAlreadyInitialized) { & (Join-Path $PSScriptRoot 'Initialize-CustomerState.ps1') }
    foreach ($file in $files) {
        $destination = Join-Path $targetRoot $file.Name
        if (Test-Path -LiteralPath $destination) { Copy-Item -LiteralPath $destination -Destination (Join-Path $backup $file.Name) }
        $replaced += $file.Name
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    }
    [IO.File]::WriteAllText((Join-Path $backup 'changes.txt'), ($replaced -join "`r`n"))
    Write-Output "라이선스 모듈 적용 완료. 기존 파일 백업: $backup"
} catch {
    foreach ($name in $replaced) { $original = Join-Path $backup $name; $destination = Join-Path $targetRoot $name; if (Test-Path -LiteralPath $original) { Copy-Item -LiteralPath $original -Destination $destination -Force } elseif (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination } }
    throw
}
