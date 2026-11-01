@echo off
chcp 65001 >nul
cd /d "%~dp0projects\클로리움라이선스관리"
"%USERPROFILE%\.local\bin\claude.exe" --add-dir "%~dp0." --permission-mode manual --tools default --resume "e21eb88f-704e-441d-b3ec-2a7c154e0206" --name "클로리움라이선스관리"
if errorlevel 1 pause
