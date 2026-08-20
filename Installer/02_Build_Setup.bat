@echo off
setlocal
title ManaChai - Build Setup

cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Setup.ps1"

if errorlevel 1 (
    echo.
    echo Setup build failed.
    echo.
    pause
    exit /b 1
)

echo.
echo Setup build completed.
echo.
pause
exit /b 0
