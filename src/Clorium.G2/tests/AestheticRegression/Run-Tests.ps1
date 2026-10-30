param([switch]$CsvBaseline)
$ErrorActionPreference='Stop'
$r=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$out=Join-Path $PSScriptRoot 'runtime'
New-Item -ItemType Directory -Force $out|Out-Null
foreach($n in @('CameraControl.exe','CameraControl.Core.dll','CameraControl.Devices.dll','Clorium AutoSizing.exe')){Copy-Item -LiteralPath (Join-Path $r "build-output/installed/$n") -Destination $out -Force}
Copy-Item -LiteralPath (Join-Path $r 'dependencies/Managed/Newtonsoft.Json.dll') -Destination $out -Force
$fx=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& (Join-Path $fx 'csc.exe') /nologo /target:exe /platform:x64 "/out:$out\AestheticTests.exe" "/reference:$out\CameraControl.Core.dll" "/reference:$out\CameraControl.Devices.dll" "/reference:$out\Newtonsoft.Json.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\WPF\PresentationCore.dll" "/reference:$fx\WPF\WindowsBase.dll" "/reference:$fx\System.Xaml.dll" /reference:System.Drawing.dll (Join-Path $PSScriptRoot 'AestheticTests.cs')
if($LASTEXITCODE -ne 0){throw 'Aesthetic tests compile failed'}
if($CsvBaseline){& (Join-Path $out 'AestheticTests.exe') $r '--csv-baseline' | Tee-Object -FilePath (Join-Path $PSScriptRoot 'csv-baseline.log')}
else {& (Join-Path $out 'AestheticTests.exe') $r | Tee-Object -FilePath (Join-Path $PSScriptRoot 'results.log')}
if($LASTEXITCODE -ne 0){throw 'Aesthetic tests failed'}