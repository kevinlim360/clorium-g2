param([string]$Configuration = 'Release', [string]$Evidence = '')
# Stages the build like Run-ImageAdjust.ps1 (+ Magick native from the dev runtime, read only), renders the G2 windows in
# 밝은 화면 / 어두운 화면 and checks the measurement keyword. PNGs go to -Evidence (default: a temp folder that is kept).
$ErrorActionPreference = 'Stop'
$r = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$bin = Join-Path $r "projects/CameraControl/bin/$Configuration/net48"
$core = Join-Path $r "projects/CameraControl.Core/bin/$Configuration/net48"
$native = [IO.Path]::GetFullPath((Join-Path $r '../../.local/g2-runtime/Magick.Native-Q8-x64.dll'))
if (-not $Evidence) { $Evidence = Join-Path ([IO.Path]::GetTempPath()) ('G2-Theme-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)) }
$out = Join-Path ([IO.Path]::GetTempPath()) ('G2-ThemeCheck-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item -Path (Join-Path $bin '*') -Destination $out -Recurse
Copy-Item -LiteralPath (Join-Path $core 'CameraControl.Core.dll') -Destination $out -Force
Get-ChildItem -LiteralPath (Join-Path $r 'dependencies/Managed') -Filter *.dll | Where-Object { -not (Test-Path -LiteralPath (Join-Path $out $_.Name)) } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $out }
if (Test-Path -LiteralPath $native) { Copy-Item -LiteralPath $native -Destination $out }
$fx = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& (Join-Path $fx 'csc.exe') /nologo /target:exe /platform:x64 "/out:$out\ThemeCheck.exe" "/reference:$out\CameraControl.Core.dll" "/reference:$out\CameraControl.Devices.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\WPF\PresentationCore.dll" "/reference:$fx\WPF\WindowsBase.dll" "/reference:$fx\System.Xaml.dll" (Join-Path $PSScriptRoot 'ThemeCheck.cs')
if ($LASTEXITCODE -ne 0) { Remove-Item -LiteralPath $out -Recurse -Force; throw 'theme check compile failed' }
Push-Location $out
try { & "$out\ThemeCheck.exe" $out $Evidence; $code = $LASTEXITCODE } finally { Pop-Location }
Remove-Item -LiteralPath $out -Recurse -Force
"evidence: $Evidence"
if ($code -ne 0) { throw 'theme checks failed' }
