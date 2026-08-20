@echo off
setlocal
title ManaChai - One-click Release Setup

cd /d "%~dp0"

echo.
echo ==============================================
echo   ManaChaiLeasing - ONE-CLICK RELEASE SETUP
echo ==============================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Release-Setup.ps1"

if errorlevel 1 (
    echo.
    echo Release build failed.
    echo.
    pause
    exit /b 1
)

echo.
echo Release build completed successfully.
echo.
pause
exit /b 0
