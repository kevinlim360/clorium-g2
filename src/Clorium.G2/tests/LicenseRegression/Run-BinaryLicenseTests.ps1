param([Parameter(Mandatory=$true)][string]$CompatibilityKeyFile)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$out=Join-Path $PSScriptRoot 'BinaryLicenseTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 "/out:$out" (Join-Path $PSScriptRoot 'BinaryLicenseTests.cs')
if($LASTEXITCODE -ne 0){throw 'License test compilation failed'}
& $out $root $CompatibilityKeyFile | Tee-Object -FilePath (Join-Path $PSScriptRoot 'fixed-binary.log')
if($LASTEXITCODE -ne 0){throw 'License binary tests failed'}
