@echo off
chcp 65001 >nul
rem Clorium G2 integration test runtime (built by Build-G2IntegrationTest.ps1). Settings are isolated in .local\g2-integration-test\G2TestData.
set "G2TEST=%~dp0.local\g2-integration-test"
if not exist "%G2TEST%\CameraControl.exe" (
  echo 통합 테스트 실행본이 없습니다. 먼저 Build-G2IntegrationTest.ps1을 실행하세요.
  pause
  exit /b 1
)
start "" /D "%G2TEST%" "%G2TEST%\CameraControl.exe"
