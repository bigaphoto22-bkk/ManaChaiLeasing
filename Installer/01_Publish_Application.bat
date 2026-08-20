@echo off
setlocal
title ManaChai - Publish Application

cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish-Application.ps1"

if errorlevel 1 (
    echo.
    echo Publish failed.
    echo.
    pause
    exit /b 1
)

echo.
echo Publish completed.
echo.
pause
exit /b 0
