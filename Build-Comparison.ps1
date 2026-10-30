$ErrorActionPreference='Stop'
$preview=Join-Path $PSScriptRoot 'preview-comparison'
$refs=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe "/out:$preview\CloriumG2.Designs.exe" "/r:$refs\PresentationFramework.dll" "/r:$refs\PresentationCore.dll" "/r:$refs\WindowsBase.dll" /r:System.Xaml.dll "$preview\Preview.cs" "$preview\Comparison.cs"
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$p=Start-Process "$preview\CloriumG2.Designs.exe" -ArgumentList '--render' -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode -ne 0){throw 'Render failed'}
