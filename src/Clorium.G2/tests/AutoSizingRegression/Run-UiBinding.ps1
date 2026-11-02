param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$r = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$bin = Join-Path $r "projects/CameraControl/bin/$Configuration/net48"
$core = Join-Path $r "projects/CameraControl.Core/bin/$Configuration/net48"
$out = Join-Path ([IO.Path]::GetTempPath()) ('G2-UiBinding-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Force $out | Out-Null
$fx = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& (Join-Path $fx 'csc.exe') /nologo /target:exe /platform:x64 "/out:$out\UiBinding.exe" "/reference:$core\CameraControl.Core.dll" "/reference:$bin\CameraControl.Devices.dll" "/reference:$r\dependencies\Managed\Newtonsoft.Json.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\WPF\PresentationCore.dll" "/reference:$fx\WPF\WindowsBase.dll" "/reference:$fx\System.Xaml.dll" /reference:System.Xml.dll (Join-Path $PSScriptRoot 'UiBinding.cs')
if ($LASTEXITCODE -ne 0) { throw 'UI binding check compile failed' }
& "$out\UiBinding.exe" $r $bin $core
$code = $LASTEXITCODE
Remove-Item -LiteralPath $out -Recurse -Force
if ($code -ne 0) { throw 'UI binding checks failed' }
