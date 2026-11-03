param(
    [switch]$SkipBuild,
    [string]$Target = '',
    [string]$LightDist = 'C:\Users\user\Documents\Codex\클로리움조명조절\work\dist\CloriumLightControl',
    [string]$AutoSizingDir = 'C:\Users\user\Documents\ChatGPT\의류 사진으로 부위별 사이즈 자동 측정\Clorium_ManualMeasure_Studio_Test_20261017\실행파일',
    [string]$MattingRepo = 'C:\Users\user\Documents\ChatGPT\클로리움 배경제거',
    [string]$MattingWork = 'C:\Users\user\Documents\Codex\2026-10-25\github-rea-x20\work'
)
# Builds a separate G2 integration test runtime (.local/g2-integration-test) from the dev runtime (.local/g2-runtime,
# read only) + the current Release build, with the same compatibility post-processing as Build-SuperfocusDev.ps1.
# - Settings/sessions are isolated: Branding.xml ApplicationDataFolder = <target>\G2TestData, seeded once from a copy of
#   C:\ProgramData\Clorium (settings.json, presets, sessions, database). The installed app's data is never written.
# - Linked modules are placed at G2's default paths: CloriumLightControl, CloriumAutoSizing, CloriumBackgroundRemoval
#   (Matting Studio source from Git + junctions to the existing venv/model; nothing is copied from them).
# - The installed app, the dev runtime, baseline and .local secrets are not modified.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$src = Join-Path $root 'src/Clorium.G2'
$runtime = Join-Path $root '.local/g2-runtime'
$baseline = Join-Path $root 'baseline/Clorium_Source_4.7.1.5'
if (-not $Target) { $Target = Join-Path $root '.local/g2-integration-test' }
$Target = [IO.Path]::GetFullPath($Target)
function Run([string[]]$ToolArgs) { & dotnet @ToolArgs; if ($LASTEXITCODE -ne 0) { throw "dotnet $($ToolArgs[0]) failed" } }
function Remove-Junction([string]$Path) { if (Test-Path -LiteralPath $Path) { [IO.Directory]::Delete($Path, $false) } }

if (-not (Test-Path -LiteralPath $runtime)) { throw "dev runtime missing: $runtime" }
$running = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($Target, [StringComparison]::OrdinalIgnoreCase) })
if ($running.Count) { throw "Close the test runtime first (running: $($running | ForEach-Object { "$($_.ProcessName) $($_.Id)" }))" }

Push-Location $src
try {
    if (-not $SkipBuild) {
        if (-not (Test-Path -LiteralPath '.local/legacy-license.key')) { throw 'License compatibility input missing (src/Clorium.G2/.local).' }
        foreach ($t in @('InteropMetadataRestore', 'WiaMetadataFix', 'CoreWpfCompatibility')) { Run @('build', "tools/$t/$t.csproj", '-c', 'Release', '--configfile', 'NuGet.Config', '-v:q', '-nologo') }
        Run @('build', 'projects/CameraControl/CameraControl.csproj', '-c', 'Release', '--configfile', 'NuGet.Config', '-m:1', '-nodeReuse:false', '-p:CloriumLegacyLicenseKeyFile=../../.local/legacy-license.key', '-v:q', '-nologo')
    }

    # Fresh program files; G2TestData (isolated settings) is kept between rebuilds.
    if (Test-Path -LiteralPath $Target) {
        Remove-Junction (Join-Path $Target 'CloriumBackgroundRemoval/work/matting-venv')
        Remove-Junction (Join-Path $Target 'CloriumBackgroundRemoval/work/matting-runtime')
        Get-ChildItem -LiteralPath $Target -Force | Where-Object { $_.Name -ne 'G2TestData' } | Remove-Item -Recurse -Force
    }
    $null = robocopy $runtime $Target /E /NFL /NDL /NJH /NJS /NP /R:1 /W:1
    if ($LASTEXITCODE -ge 8) { throw "robocopy $LASTEXITCODE" }
    $global:LASTEXITCODE = 0
    Get-ChildItem -LiteralPath $Target -Filter 'G2*Check*.exe' | Remove-Item -Force
    # The dev runtime has no Tools\exiv2.exe (thumbnail metadata); copy it from the installed app, read only.
    $tools = 'C:\Program Files (x86)\Clorium\Tools'
    if (-not (Test-Path -LiteralPath (Join-Path $Target 'Tools/exiv2.exe')) -and (Test-Path -LiteralPath $tools)) { Copy-Item -LiteralPath $tools -Destination $Target -Recurse }
    [IO.File]::WriteAllText((Join-Path $Target 'g2-development-runtime.marker'), 'Isolated G2 integration test runtime: skip installed-app maintenance and restart.')

    foreach ($m in @('CameraControl.Core', 'Canon.Eos.Framework', 'PortableDeviceLib')) { foreach ($e in @('dll', 'pdb')) { Copy-Item -LiteralPath "projects/$m/bin/Release/net48/$m.$e" -Destination $Target -Force } }
    Copy-Item -LiteralPath 'projects/CameraControl/bin/Release/net48/ZoomAndPan.dll' -Destination $Target -Force
    Run @('tools/CoreWpfCompatibility/bin/Release/net10.0/CoreWpfCompatibility.dll', "$baseline/releases/Clorium_4.7.1.4_20261023/files/CameraControl.Core.dll", "$Target/CameraControl.Core.dll")
    Run @('tools/WiaMetadataFix/bin/Release/net10.0/WiaMetadataFix.dll', 'projects/CameraControl.Devices/bin/Release/net48/CameraControl.Devices.dll', "$Target/CameraControl.Devices.dll")
    $staged = Join-Path $Target ('CameraControl-staged-' + [guid]::NewGuid().ToString('N') + '.exe')
    Run @('tools/InteropMetadataRestore/bin/Release/net10.0/InteropMetadataRestore.dll', 'restore', "$baseline/metadata/wmplib-metadata-spec.json", 'projects/CameraControl/bin/Release/net48/CameraControl.exe', $staged)
    Copy-Item -LiteralPath $staged -Destination "$Target/CameraControl.exe" -Force
    Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($staged, '.pdb')) -Destination "$Target/CameraControl.pdb" -Force
    Remove-Item -LiteralPath $staged, ([IO.Path]::ChangeExtension($staged, '.pdb'))
    Run @('tools/InteropMetadataRestore/bin/Release/net10.0/InteropMetadataRestore.dll', 'verify', "$baseline/metadata/wmplib-metadata-spec.json", "$Target/CameraControl.exe")
} finally { Pop-Location }

# Isolated settings: ApplicationDataFolder in this copy's Branding.xml.
$data = Join-Path $Target 'G2TestData'
$brandingFile = Join-Path $Target 'Branding.xml'
[xml]$branding = Get-Content -LiteralPath $brandingFile -Raw -Encoding UTF8
$node = $branding.Branding.SelectSingleNode('ApplicationDataFolder')
if (-not $node) { $node = $branding.CreateElement('ApplicationDataFolder'); [void]$branding.Branding.AppendChild($node) }
$node.InnerText = $data
$branding.Save($brandingFile)
if (-not (Test-Path -LiteralPath (Join-Path $data 'settings.json'))) {
    New-Item -ItemType Directory -Force $data | Out-Null
    $shared = 'C:\ProgramData\Clorium'
    foreach ($f in @('settings.json', 'G2SettingsPresets.xml', 'database.db')) { $p = Join-Path $shared $f; if (Test-Path -LiteralPath $p) { Copy-Item -LiteralPath $p -Destination $data } }
    foreach ($d in @('Presets', 'Sessions', 'LiveViewOverlay')) { $p = Join-Path $shared $d; if (Test-Path -LiteralPath $p) { Copy-Item -LiteralPath $p -Destination $data -Recurse } }
    "Seeded isolated settings from a copy of $shared"
}

# Linked modules at G2's default locations.
$light = Join-Path $Target 'CloriumLightControl'
$null = robocopy $LightDist $light /E /XF app.log /NFL /NDL /NJH /NJS /NP /R:1 /W:1
if ($LASTEXITCODE -ge 8) { throw "robocopy light $LASTEXITCODE" }
$auto = Join-Path $Target 'CloriumAutoSizing'
$null = robocopy $AutoSizingDir $auto /E /NFL /NDL /NJH /NJS /NP /R:1 /W:1
if ($LASTEXITCODE -ge 8) { throw "robocopy autosizing $LASTEXITCODE" }
$global:LASTEXITCODE = 0
$bgr = Join-Path $Target 'CloriumBackgroundRemoval'
New-Item -ItemType Directory -Force (Join-Path $bgr 'work') | Out-Null
$zip = Join-Path ([IO.Path]::GetTempPath()) ('matting-' + [guid]::NewGuid().ToString('N') + '.zip')
& git -C $MattingRepo archive --format=zip -o $zip HEAD outputs/matting-studio
if ($LASTEXITCODE -ne 0) { throw 'git archive (Matting Studio) failed' }
Expand-Archive -LiteralPath $zip -DestinationPath $bgr -Force
Remove-Item -LiteralPath $zip
foreach ($j in @('matting-venv', 'matting-runtime')) {
    $from = Join-Path $MattingWork $j
    if (-not (Test-Path -LiteralPath $from)) { throw "Matting Studio $j missing: $from" }
    New-Item -ItemType Junction -Path (Join-Path $bgr "work/$j") -Target $from | Out-Null
}

foreach ($f in 'CameraControl.exe', 'CameraControl.Core.dll', 'CameraControl.Devices.dll', 'CloriumLightControl/CloriumLightControl.exe', 'CloriumAutoSizing/Clorium AutoSizing.exe', 'CloriumBackgroundRemoval/outputs/matting-studio/Start.ps1') {
    $p = Join-Path $Target $f
    if (-not (Test-Path -LiteralPath $p)) { throw "missing $f" }
    "SHA256 $f $((Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.Substring(0, 16))"
}
"G2 integration test runtime ready: $Target"
