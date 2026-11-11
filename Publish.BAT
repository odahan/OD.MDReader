@echo off
setlocal

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish.ps1"
set "publishExitCode=%ERRORLEVEL%"

if not "%publishExitCode%"=="0" echo Publish failed with exit code %publishExitCode%.
pause
exit /b %publishExitCode%
