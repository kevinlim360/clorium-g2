param([Parameter(Mandatory=$true)][string]$ClientConfig, [string]$OutputDirectory = '', [string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$workspaceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sourceRoot = Join-Path $workspaceRoot 'src/Clorium.G2'
$configPath = [IO.Path]::GetFullPath($ClientConfig)
$connection = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if (-not $connection.PublicKey -or -not $connection.ServerUrl) { throw 'ServerUrl/PublicKey가 있는 고객 연결 설정이 필요합니다.' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $workspaceRoot ('delivery/Clorium-Licensing-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputRoot) { throw '덮어쓰기를 방지하기 위해 새 출력 폴더를 지정하세요.' }
& (Join-Path $PSScriptRoot 'Build.ps1') -Configuration $Configuration
& dotnet build (Join-Path $sourceRoot 'projects/CameraControl/CameraControl.csproj') -c $Configuration --configfile (Join-Path $sourceRoot 'NuGet.Config') -m:1 -nodeReuse:false -p:UseSharedCompilation=false "-p:CloriumManagedLicenseConfig=$configPath" -v:q
if ($LASTEXITCODE -ne 0) { throw 'Managed CameraControl build failed.' }
New-Item -ItemType Directory -Path $outputRoot | Out-Null
[IO.File]::WriteAllText((Join-Path $outputRoot 'INCOMPLETE.txt'), 'Build in progress; do not distribute until this marker is removed.')
foreach ($app in @(@("Server/bin/$Configuration/net10.0",'company/server'),@("Admin/bin/$Configuration/net10.0-windows",'company/admin'))) {
    $target = Join-Path $outputRoot $app[1]; New-Item -ItemType Directory -Path $target -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot $app[0]) -File | Where-Object Extension -ne '.pdb' | Copy-Item -Destination $target
}
foreach ($script in @('Start-Server.ps1','Open-Manager.ps1','Open-Local.ps1','Restore-Server.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $script) -Destination (Join-Path $outputRoot 'company') }
$customer = Join-Path $outputRoot 'customer/CameraControl-modules'; New-Item -ItemType Directory -Path $customer -Force | Out-Null
$built = Join-Path $sourceRoot "projects/CameraControl/bin/$Configuration/net48"
foreach ($module in @('Clorium.Licensing.Client.dll','Clorium.Licensing.Protocol.dll','ZoomAndPan.dll','Canon.Eos.Framework.dll','PortableDeviceLib.dll')) { Copy-Item -LiteralPath (Join-Path $built $module) -Destination $customer }
$baselineRoot = Join-Path $workspaceRoot 'baseline/Clorium_Source_4.7.1.5'
function RunPost([string[]]$ToolArgs) { & dotnet @ToolArgs; if ($LASTEXITCODE -ne 0) { throw 'Interop postprocessing failed.' } }
foreach ($tool in @('InteropMetadataRestore','WiaMetadataFix','CoreWpfCompatibility')) {
    & dotnet build (Join-Path $sourceRoot "tools/$tool/$tool.csproj") -c $Configuration --configfile (Join-Path $sourceRoot 'NuGet.Config') -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $tool" }
}
Copy-Item -LiteralPath (Join-Path $sourceRoot "projects/CameraControl.Core/bin/$Configuration/net48/CameraControl.Core.dll") -Destination $customer
Copy-Item -LiteralPath (Join-Path $sourceRoot "projects/CameraControl.Core/bin/$Configuration/net48/CameraControl.Core.pdb") -Destination $customer
RunPost @((Join-Path $sourceRoot "tools/CoreWpfCompatibility/bin/$Configuration/net10.0/CoreWpfCompatibility.dll"),(Join-Path $baselineRoot 'releases/Clorium_4.7.1.4_20261023/files/CameraControl.Core.dll'),(Join-Path $customer 'CameraControl.Core.dll'))
RunPost @((Join-Path $sourceRoot "tools/WiaMetadataFix/bin/$Configuration/net10.0/WiaMetadataFix.dll"),(Join-Path $sourceRoot "projects/CameraControl.Devices/bin/$Configuration/net48/CameraControl.Devices.dll"),(Join-Path $customer 'CameraControl.Devices.dll'))
RunPost @((Join-Path $sourceRoot "tools/InteropMetadataRestore/bin/$Configuration/net10.0/InteropMetadataRestore.dll"),'restore',(Join-Path $baselineRoot 'metadata/wmplib-metadata-spec.json'),(Join-Path $built 'CameraControl.exe'),(Join-Path $customer 'CameraControl.exe'))
RunPost @((Join-Path $sourceRoot "tools/InteropMetadataRestore/bin/$Configuration/net10.0/InteropMetadataRestore.dll"),'verify',(Join-Path $baselineRoot 'metadata/wmplib-metadata-spec.json'),(Join-Path $customer 'CameraControl.exe'))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Initialize-CustomerState.ps1') -Destination (Join-Path $outputRoot 'customer')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-CameraControl.ps1') -Destination (Join-Path $outputRoot 'customer')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $outputRoot
Remove-Item -LiteralPath (Join-Path $outputRoot 'INCOMPLETE.txt')
$manifest = Get-ChildItem -LiteralPath $outputRoot -File -Recurse | ForEach-Object { [PSCustomObject]@{ Path = $_.FullName.Substring($outputRoot.Length+1); SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'manifest.json') -Encoding utf8
[IO.File]::WriteAllText((Join-Path $workspaceRoot '.local/licensing-latest-distribution.txt'), $outputRoot)
Write-Output "Distribution prepared: $outputRoot"
