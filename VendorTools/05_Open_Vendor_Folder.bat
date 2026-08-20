@echo off
setlocal
title ManaChai - Vendor Folder

set "FOLDER=%LOCALAPPDATA%\ManaChaiLicenseVendor"

if not exist "%FOLDER%" (
    mkdir "%FOLDER%"
)

start "" "%FOLDER%"
exit /b 0
