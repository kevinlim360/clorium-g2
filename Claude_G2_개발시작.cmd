@echo off
chcp 65001 >nul
cd /d "%~dp0"
"%USERPROFILE%\.local\bin\claude.exe" --name "클로리움 G2" "Read CLAUDE.md and handoff/START_HERE.txt. Continue the Clorium G2 handoff in Korean."
if errorlevel 1 pause