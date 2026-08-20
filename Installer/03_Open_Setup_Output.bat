@echo off
setlocal
title ManaChai - Setup Output

set "OUTPUT=%~dp0Output"

if not exist "%OUTPUT%" (
    mkdir "%OUTPUT%"
)

start "" "%OUTPUT%"
exit /b 0
