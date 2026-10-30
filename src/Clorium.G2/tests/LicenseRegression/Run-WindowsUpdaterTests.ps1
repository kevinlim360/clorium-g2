param(
 [Parameter(Mandatory=$true)][string]$UpdateExe,
 [Parameter(Mandatory=$true)][string]$LegacyInstallFolder,
 [Parameter(Mandatory=$true)][string]$PreviousOct19Folder,
 [Parameter(Mandatory=$true)][string]$PreviousOct22Folder,
 [Parameter(Mandatory=$true)][string]$PreviousLicenseFolder,
 [Parameter(Mandatory=$true)][string]$PreviousAestheticFolder
)
$ErrorActionPreference='Stop'
$test=Join-Path $PSScriptRoot 'WindowsUpdaterTests.exe'
$fx=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& (Join-Path $fx 'csc.exe') /nologo /target:exe /platform:x64 "/out:$test" "/reference:$UpdateExe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.Linq.dll "/reference:$fx\System.IO.Compression.dll" (Join-Path $PSScriptRoot 'WindowsUpdaterTests.cs')
if($LASTEXITCODE -ne 0){throw 'Windows updater tests compilation failed'}
Copy-Item -LiteralPath $UpdateExe -Destination $PSScriptRoot -Force
& $test $UpdateExe $LegacyInstallFolder $PreviousOct19Folder $PreviousOct22Folder $PreviousLicenseFolder $PreviousAestheticFolder | Tee-Object -FilePath (Join-Path $PSScriptRoot 'windows-updater.log')
if($LASTEXITCODE -ne 0){throw 'Windows updater tests failed'}
