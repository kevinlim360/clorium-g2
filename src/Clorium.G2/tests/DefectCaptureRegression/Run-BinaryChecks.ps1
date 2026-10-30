param([string]$SourceRoot=(Join-Path $PSScriptRoot '../..'),[string]$RuntimeFolder='C:\Program Files (x86)\Clorium')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($SourceRoot)
$out=Join-Path $PSScriptRoot 'binary-check-output'
New-Item -ItemType Directory -Force -Path $out|Out-Null
foreach($name in @('CameraControl.exe','CameraControl.Core.dll','CameraControl.Devices.dll')){Copy-Item -LiteralPath (Join-Path (Join-Path $root 'build-output/installed') $name) -Destination $out -Force}
$fx=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& (Join-Path $fx 'csc.exe') /nologo /target:exe /platform:x64 "/out:$out\BinaryChecks.exe" "/reference:$out\CameraControl.Core.dll" "/reference:$out\CameraControl.Devices.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\WPF\PresentationCore.dll" "/reference:$fx\WPF\WindowsBase.dll" "/reference:$fx\System.Xaml.dll" (Join-Path $PSScriptRoot 'BinaryChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Binary test compilation failed'}
& (Join-Path $out 'BinaryChecks.exe') $root $RuntimeFolder
if($LASTEXITCODE -ne 0){throw 'Binary test failed'}
