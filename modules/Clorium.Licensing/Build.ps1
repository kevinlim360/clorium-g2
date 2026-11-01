param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
foreach ($licensingProject in @('Server','Admin','Tests','PolicyChecks')) {
    & dotnet build (Join-Path $PSScriptRoot "$licensingProject/$licensingProject.csproj") -c $Configuration --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $licensingProject" }
}
