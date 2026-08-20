@echo off
setlocal
title ManaChai - Build License Generator

cd /d "%~dp0"

echo.
echo ==========================================
echo   ManaChai License Generator
echo   Build + Open
echo ==========================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ManaChaiLicenseGenerator\Build-LicenseGenerator.ps1"

if errorlevel 1 (
    echo.
    echo Build/Open failed.
    echo.
    pause
    exit /b 1
)

exit /b 0
