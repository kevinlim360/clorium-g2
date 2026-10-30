param([string]$Python = 'C:\Users\user\Documents\Codex\2026-10-25\github-rea-x20\work\matting-venv\Scripts\python.exe')
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Python)) { throw 'Python 실행 경로를 -Python 인자로 지정해 주세요.' }
& $Python (Join-Path $PSScriptRoot 'app.py')
exit $LASTEXITCODE
