$ErrorActionPreference = 'Stop'
$preview = Join-Path $PSScriptRoot 'preview'
$refs = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe "/out:$preview\CloriumG2.Preview.exe" "/r:$refs\PresentationFramework.dll" "/r:$refs\PresentationCore.dll" "/r:$refs\WindowsBase.dll" /r:System.Xaml.dll "$preview\Preview.cs"
if ($LASTEXITCODE -ne 0) { throw 'Preview build failed' }
$process = Start-Process -FilePath "$preview\CloriumG2.Preview.exe" -ArgumentList '--render' -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw 'Preview render failed; inspect preview-error.txt' }
