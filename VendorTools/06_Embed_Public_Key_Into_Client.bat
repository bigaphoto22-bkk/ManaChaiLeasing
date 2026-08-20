@echo off
setlocal
title ManaChai - Embed Public Key Into Client

cd /d "%~dp0"

echo.
echo ==========================================
echo   ManaChai Client Public Key Integration
echo ==========================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Embed-PublicKeyIntoClient.ps1"

if errorlevel 1 (
    echo.
    echo Public Key integration failed.
    echo.
    pause
    exit /b 1
)

echo.
echo Public Key integration completed.
echo You can now build ManaChaiLeasing.
echo.
pause
exit /b 0
