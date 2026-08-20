$ErrorActionPreference = "Stop"

$InstallerDir = $PSScriptRoot
$PublishScript = Join-Path $InstallerDir "Publish-Application.ps1"
$SetupScript = Join-Path $InstallerDir "Build-Setup.ps1"
$VersionFile = Join-Path $InstallerDir "ReleaseVersion.txt"
$OutputDir = Join-Path $InstallerDir "Output"

try {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host " ManaChaiLeasing - ONE-CLICK RELEASE SETUP" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""

    & $PublishScript

    if ($LASTEXITCODE -ne 0) {
        throw "Publish step failed."
    }

    & $SetupScript

    if ($LASTEXITCODE -ne 0) {
        throw "Setup build step failed."
    }

    $Version = (Get-Content $VersionFile -Raw).Trim()
    $SetupFile = Join-Path $OutputDir "ManaChaiLeasing_Setup_$Version.exe"

    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Green
    Write-Host " RELEASE READY" -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Setup: $SetupFile"
    Write-Host ""
    Write-Host "Reminder:" -ForegroundColor Yellow
    Write-Host "- Setup.exe does NOT contain the Vendor Private Key."
    Write-Host "- Customer still needs a valid machine-bound .license."
    Write-Host "- Test this Setup on a clean/pilot PC before distribution."
    Write-Host ""

    Start-Process explorer.exe -ArgumentList "/select,`"$SetupFile`""
}
catch {
    Write-Host ""
    Write-Host "RELEASE BUILD FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
