param([Parameter(Mandatory=$true)][string]$BackupFile, [Parameter(Mandatory=$true)][string]$NewDataDirectory)
$ErrorActionPreference = 'Stop'
$destination = [IO.Path]::GetFullPath($NewDataDirectory)
if (Test-Path -LiteralPath $destination) { throw '기존 데이터 덮어쓰기를 방지하기 위해 존재하지 않는 새 폴더를 지정해 주세요. 서버를 멈춘 뒤 복원하세요.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($BackupFile))
try {
    $allowed = @('settings.json','signing-key.xml','licenses.json','admin-token.txt')
    foreach ($entry in $archive.Entries) { if ($entry.FullName -notin $allowed -or $entry.Length -gt 536870912) { throw '지원하지 않는 백업 항목입니다.' } }
    foreach ($name in @('settings.json','signing-key.xml','licenses.json')) { if (-not ($archive.Entries | Where-Object FullName -eq $name)) { throw "필수 백업 항목이 없습니다: $name" } }
    New-Item -ItemType Directory -Path $destination | Out-Null
    $acl = New-Object Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    $inherit = [Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit'
    foreach ($sid in @([Security.Principal.WindowsIdentity]::GetCurrent().User, [Security.Principal.SecurityIdentifier]'S-1-5-18', [Security.Principal.SecurityIdentifier]'S-1-5-32-544')) {
        $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid, [Security.AccessControl.FileSystemRights]::FullControl, $inherit, [Security.AccessControl.PropagationFlags]::None, [Security.AccessControl.AccessControlType]::Allow)))
    }
    Set-Acl -LiteralPath $destination -AclObject $acl
    foreach ($entry in $archive.Entries) { [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $destination $entry.FullName), $false) }
    Write-Output "새 데이터 폴더로 복원 완료: $destination"
    Write-Output 'Start-Server.ps1 -DataDirectory에 이 폴더를 지정하세요. 기존 고객 공개키/서버 주소는 유지합니다.'
} finally { $archive.Dispose() }
