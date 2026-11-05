param([string]$Configuration = 'ImageAdjustIntegration20261103', [string]$RuntimeFolder = (Join-Path $PSScriptRoot '../../../../.local/g2-integration-test'))
# 치수 촬영 naming + UiText check against the built Core/Devices (build CameraControl.csproj with -c $Configuration first).
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$out = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $out | Out-Null
foreach ($m in @('CameraControl.Core', 'CameraControl.Devices')) {
    Copy-Item -LiteralPath (Join-Path $root "projects/$m/bin/$Configuration/net48/$m.dll") -Destination $out -Force
}
$coreBin = Join-Path $root "projects/CameraControl/bin/$Configuration/net48"
Get-ChildItem -LiteralPath $coreBin -Filter *.dll | Where-Object { -not (Test-Path -LiteralPath (Join-Path $out $_.Name)) } | Copy-Item -Destination $out
$fx = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& (Join-Path $fx 'csc.exe') /nologo /target:exe /platform:x64 "/out:$out\MeasureCaptureCheck.exe" "/reference:$out\CameraControl.Core.dll" "/reference:$out\CameraControl.Devices.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\WPF\PresentationCore.dll" "/reference:$fx\WPF\WindowsBase.dll" "/reference:$fx\System.Xaml.dll" (Join-Path $PSScriptRoot 'MeasureCaptureCheck.cs')
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
& (Join-Path $out 'MeasureCaptureCheck.exe') ([IO.Path]::GetFullPath($RuntimeFolder))
if ($LASTEXITCODE -ne 0) { throw 'check failed' }
