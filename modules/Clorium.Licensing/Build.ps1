param()
$ErrorActionPreference = 'Stop'
foreach ($licensingProject in @('Server','Admin','Tests')) {
    & dotnet build (Join-Path $PSScriptRoot "$licensingProject/$licensingProject.csproj") -c Release --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $licensingProject" }
}
