@echo off
setlocal
title ManaChai - Build Key Manager

cd /d "%~dp0"

echo.
echo ==========================================
echo   ManaChai Vendor Signing Key Manager
echo   Build + Open
echo ==========================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ManaChaiVendorKeyManager\Build-KeyManager.ps1"

if errorlevel 1 (
    echo.
    echo Build/Open failed.
    echo.
    pause
    exit /b 1
)

exit /b 0
