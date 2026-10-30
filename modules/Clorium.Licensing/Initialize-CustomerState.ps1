# Run once during customer installation with administrator rights. No license bypass or key is installed.
param([string]$StateDirectory = '')
$ErrorActionPreference = 'Stop'
if (-not $StateDirectory) { $StateDirectory = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'Clorium/Licensing' }
$stateDirectory = [IO.Path]::GetFullPath($StateDirectory)
New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
$acl = New-Object System.Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true, $false)
$inherit = [Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit'
foreach ($entry in @(@('S-1-5-18','FullControl'), @('S-1-5-32-544','FullControl'), @('S-1-5-32-545','Modify'))) {
    $sid = New-Object System.Security.Principal.SecurityIdentifier($entry[0])
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($sid, [Security.AccessControl.FileSystemRights]$entry[1], $inherit, [Security.AccessControl.PropagationFlags]::None, [Security.AccessControl.AccessControlType]::Allow)
    $acl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $stateDirectory -AclObject $acl
Write-Output "PC 공용 인증 저장소 준비: $stateDirectory"
