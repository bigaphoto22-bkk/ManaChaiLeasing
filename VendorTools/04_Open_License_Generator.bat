@echo off
setlocal
title ManaChai - Open License Generator

set "EXE=%LOCALAPPDATA%\ManaChaiLicenseVendor\Tool\ManaChaiLicenseGenerator.exe"

if not exist "%EXE%" (
    echo.
    echo ManaChaiLicenseGenerator.exe not found.
    echo.
    echo Please run:
    echo   02_Build_and_Open_License_Generator.bat
    echo.
    pause
    exit /b 1
)

start "" "%EXE%"
exit /b 0
