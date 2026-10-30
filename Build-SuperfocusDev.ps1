param([switch]$SkipBuild, [string]$StackPython = '')
$ErrorActionPreference = 'Stop'
$g2Root = Join-Path $PSScriptRoot 'src/Clorium.G2'
$g2Runtime = Join-Path $PSScriptRoot '.local/g2-runtime'
$g2Baseline = Join-Path $PSScriptRoot 'baseline/Clorium_Source_4.7.1.5'
function Invoke-G2Tool([string[]]$ToolArgs) {
    & dotnet @ToolArgs
    if ($LASTEXITCODE -ne 0) { throw 'G2 build/postprocessing failed.' }
}
Push-Location $g2Root
try {
    if (-not $SkipBuild) {
        if (-not (Test-Path -LiteralPath '.local/legacy-license.key')) { throw 'Existing verified license compatibility input is required; see tools/LicenseCompatibilityRecovery.' }
        foreach ($g2Tool in @('InteropMetadataRestore','WiaMetadataFix','CoreWpfCompatibility')) {
            Invoke-G2Tool @('build', "tools/$g2Tool/$g2Tool.csproj", '-c', 'Release', '--configfile', 'NuGet.Config', '-v:q')
        }
        Invoke-G2Tool @('build','projects/CameraControl/CameraControl.csproj','-c','Release','--no-restore','-m:1','-nodeReuse:false','-p:CloriumLegacyLicenseKeyFile=../../.local/legacy-license.key','-v:q')
    }
    if (-not (Test-Path -LiteralPath $g2Runtime)) { throw 'Stage an existing installed runtime in .local/g2-runtime first. Never point this script at the installed folder.' }
    [IO.File]::WriteAllText((Join-Path $g2Runtime 'g2-development-runtime.marker'), 'Isolated G2 development runtime: skip installed-app maintenance and restart.')
    if ($StackPython) {
        if (-not (Test-Path -LiteralPath $StackPython -PathType Leaf)) { throw 'Stack Python executable not found.' }
        [IO.File]::WriteAllText((Join-Path $g2Runtime 'stack-python.txt'), [IO.Path]::GetFullPath($StackPython))
    }
    foreach ($g2Module in @('CameraControl.Core','Canon.Eos.Framework','PortableDeviceLib')) {
        foreach ($g2Ext in @('dll','pdb')) { Copy-Item -LiteralPath "projects/$g2Module/bin/Release/net48/$g2Module.$g2Ext" -Destination $g2Runtime -Force }
    }
    Copy-Item -LiteralPath 'projects/CameraControl/bin/Release/net48/ZoomAndPan.dll' -Destination $g2Runtime -Force
    Invoke-G2Tool @('tools/CoreWpfCompatibility/bin/Release/net10.0/CoreWpfCompatibility.dll', "$g2Baseline/releases/Clorium_4.7.1.4_20261023/files/CameraControl.Core.dll", "$g2Runtime/CameraControl.Core.dll")
    Invoke-G2Tool @('tools/WiaMetadataFix/bin/Release/net10.0/WiaMetadataFix.dll', 'projects/CameraControl.Devices/bin/Release/net48/CameraControl.Devices.dll', "$g2Runtime/CameraControl.Devices.dll")
    $g2MainStaged = Join-Path $g2Runtime ('CameraControl-staged-' + [guid]::NewGuid().ToString('N') + '.exe')
    Invoke-G2Tool @('tools/InteropMetadataRestore/bin/Release/net10.0/InteropMetadataRestore.dll', 'restore', "$g2Baseline/metadata/wmplib-metadata-spec.json", 'projects/CameraControl/bin/Release/net48/CameraControl.exe', $g2MainStaged)
    Copy-Item -LiteralPath $g2MainStaged -Destination "$g2Runtime/CameraControl.exe" -Force
    Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($g2MainStaged, '.pdb')) -Destination "$g2Runtime/CameraControl.pdb" -Force
    Remove-Item -LiteralPath $g2MainStaged,([IO.Path]::ChangeExtension($g2MainStaged, '.pdb'))
    Invoke-G2Tool @('tools/InteropMetadataRestore/bin/Release/net10.0/InteropMetadataRestore.dll', 'verify', "$g2Baseline/metadata/wmplib-metadata-spec.json", "$g2Runtime/CameraControl.exe")
    Write-Output "Development runtime prepared: $g2Runtime"
} finally { Pop-Location }
