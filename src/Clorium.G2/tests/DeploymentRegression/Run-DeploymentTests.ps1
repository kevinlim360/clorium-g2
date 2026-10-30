param([Parameter(Mandatory=$true)][string]$PreviousUpdateZip,[string]$OriginalInstallFolder='C:\Program Files (x86)\Clorium')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$package=Join-Path $root 'release-r2/patch'
$update=Join-Path $package 'Update-Clorium.ps1'
$names=@('CameraControl.exe','CameraControl.Core.dll','CameraControl.pdb','CameraControl.Core.pdb')
$runs=Join-Path $PSScriptRoot ('runs-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runs|Out-Null
$results=@()
function Check($name,$value){if(-not $value){throw "FAIL: $name"};$script:results+=[PSCustomObject]@{test=$name;passed=$true}}
function Hashes($folder){$map=@{};foreach($name in $names){$map[$name]=(Get-FileHash -LiteralPath (Join-Path $folder $name)).Hash};return $map}
function Same($folder,$expected){foreach($name in $names){if((Get-FileHash -LiteralPath (Join-Path $folder $name)).Hash -ne $expected[$name]){return $false}};return $true}
function Target($name,$base){$folder=Join-Path $runs $name;New-Item -ItemType Directory -Path $folder|Out-Null;foreach($file in $names){Copy-Item -LiteralPath (Join-Path $base $file) -Destination $folder};[IO.File]::WriteAllText((Join-Path $folder 'existing-settings.txt'),'existing settings must survive');return $folder}
$previous=Join-Path $runs 'previous-release';New-Item -ItemType Directory -Path $previous|Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($PreviousUpdateZip)
try{foreach($name in $names){[IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry('files/'+$name),(Join-Path $previous $name),$false)}}finally{$zip.Dispose()}
$payload=Hashes (Join-Path $package 'files')
foreach($scenario in @(@{name='original-install';base=$OriginalInstallFolder},@{name='previous-4.7.1.4';base=$previous})){
 $target=Target $scenario.name $scenario.base;$before=Hashes $target
 & $update -InstallPath $target|Out-Null
 Check ($scenario.name+' update hashes') (Same $target $payload)
 Check ($scenario.name+' preserves unrelated settings') ([IO.File]::ReadAllText((Join-Path $target 'existing-settings.txt')) -eq 'existing settings must survive')
 $backups=@(Get-ChildItem -LiteralPath (Join-Path $target 'UpdateBackups') -Directory)
 Check ($scenario.name+' creates one backup') ($backups.Count -eq 1)
 & $update -InstallPath $target|Out-Null
 Check ($scenario.name+' identical payload no-op') (@(Get-ChildItem -LiteralPath (Join-Path $target 'UpdateBackups') -Directory).Count -eq 1)
 & $update -InstallPath $target -Rollback -BackupPath $backups[0].FullName|Out-Null
 Check ($scenario.name+' rollback restores exact base') (Same $target $before)
}
$target=Target 'corrupt-payload' $previous;$before=Hashes $target;$pdb=Join-Path $package 'files/CameraControl.pdb';$blocked=$false
try{[IO.File]::AppendAllText($pdb,'tamper');try{& $update -InstallPath $target|Out-Null}catch{$blocked=$true};Check 'corrupt payload rejected' $blocked;Check 'corrupt payload leaves target intact' (Same $target $before)}finally{Copy-Item -LiteralPath (Join-Path $root 'build-output/installed/CameraControl.pdb') -Destination $pdb -Force}
$target=Target 'unknown-same-version' $previous;$exe=Join-Path $target 'CameraControl.exe';[IO.File]::AppendAllText($exe,'unknown-build');$before=Hashes $target;$blocked=$false;try{& $update -InstallPath $target|Out-Null}catch{$blocked=$true};Check 'unknown 4.7.1.4 revision rejected' $blocked;Check 'unknown revision remains untouched' (Same $target $before)
$target=Target 'damaged-backup' $previous;& $update -InstallPath $target|Out-Null;$backup=Get-ChildItem -LiteralPath (Join-Path $target 'UpdateBackups') -Directory|Select-Object -First 1;[IO.File]::AppendAllText((Join-Path $backup.FullName 'CameraControl.pdb'),'tamper');$blocked=$false;try{& $update -InstallPath $target -Rollback -BackupPath $backup.FullName|Out-Null}catch{$blocked=$true};Check 'damaged backup rejected before restore' $blocked;Check 'damaged backup does not modify current files' (Same $target $payload)
$target=Target 'locked-file-auto-rollback' $previous;$before=Hashes $target;$handle=[IO.File]::Open((Join-Path $target 'CameraControl.pdb'),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$failed=$false
try{try{& $update -InstallPath $target|Out-Null}catch{$failed=$true};Check 'locked symbol file reports failed update' $failed;Check 'partial update automatically restores binaries' (Same $target $before)}finally{$handle.Dispose()}
$target=Target 'mixed-binary-versions' $previous;Copy-Item -LiteralPath (Join-Path $OriginalInstallFolder 'CameraControl.Core.dll') -Destination (Join-Path $target 'CameraControl.Core.dll') -Force;$before=Hashes $target;$blocked=$false;try{& $update -InstallPath $target|Out-Null}catch{$blocked=$true};Check 'mixed existing versions rejected' $blocked;Check 'mixed versions remain untouched' (Same $target $before)
@{tests=$results.Count;passed=$results.Count;failed=0;results=$results}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'results.json') -Encoding UTF8
Write-Output ('DEPLOYMENT_TESTS_PASSED='+$results.Count)