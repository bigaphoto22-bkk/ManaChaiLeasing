@echo off
setlocal
title ManaChai - Open Key Manager

set "EXE=%LOCALAPPDATA%\ManaChaiLicenseVendor\Tool\ManaChaiVendorKeyManager.exe"

if not exist "%EXE%" (
    echo.
    echo ManaChaiVendorKeyManager.exe not found.
    echo.
    echo Please run:
    echo   01_Build_and_Open_Key_Manager.bat
    echo.
    pause
    exit /b 1
)

start "" "%EXE%"
exit /b 0
