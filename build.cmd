@echo off
REM Double-click entry point. Runs build.ps1 with the execution policy relaxed for
REM this one process only, so nothing about your machine's settings is changed.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
echo.
pause
