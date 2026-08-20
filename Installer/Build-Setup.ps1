$ErrorActionPreference = "Stop"

$InstallerDir = $PSScriptRoot
$ProjectRoot = Split-Path $InstallerDir -Parent
$VersionFile = Join-Path $InstallerDir "ReleaseVersion.txt"
$TemplateFile = Join-Path $InstallerDir "ManaChaiLeasing_Installer.template.iss"
$GeneratedIss = Join-Path $InstallerDir "ManaChaiLeasing_Installer.generated.iss"
$PublishDir = Join-Path $ProjectRoot "Publish\ManaChaiLeasing-win-x64"
$PublishedExe = Join-Path $PublishDir "ManaChaiLeasing.exe"
$OutputDir = Join-Path $InstallerDir "Output"

if (-not (Test-Path $VersionFile)) {
    throw "ReleaseVersion.txt not found: $VersionFile"
}

$Version = (Get-Content $VersionFile -Raw).Trim()

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "ReleaseVersion.txt must contain X.Y.Z, for example 0.2.0. Current: '$Version'"
}

if (-not (Test-Path $TemplateFile)) {
    throw "Installer template not found: $TemplateFile"
}

if (-not (Test-Path $PublishedExe)) {
    throw "Published application not found.`nRun 01_Publish_Application.bat first:`n$PublishedExe"
}

function Find-Iscc {
    $Command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue

    if ($Command) {
        return $Command.Source
    }

    $Candidates = @()

    $ProgramFilesX86 = ${env:ProgramFiles(x86)}
    if (-not [string]::IsNullOrWhiteSpace($ProgramFilesX86)) {
        $Candidates += (Join-Path $ProgramFilesX86 "Inno Setup 6\ISCC.exe")
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $Candidates += (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    }

    if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $Candidates += (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    }

    foreach ($Candidate in $Candidates) {
        if (Test-Path $Candidate) {
            return $Candidate
        }
    }

    return $null
}

$Iscc = Find-Iscc

if ([string]::IsNullOrWhiteSpace($Iscc)) {
    throw @"
ไม่พบ Inno Setup Compiler (ISCC.exe)

กรุณาตรวจว่า Inno Setup 6 ยังติดตั้งอยู่
จากนั้นลองรัน 02_Build_Setup.bat ใหม่
"@
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$TemplateText = Get-Content $TemplateFile -Raw

if ($TemplateText -notmatch '__APP_VERSION__') {
    throw "Installer template does not contain __APP_VERSION__ placeholder."
}

$GeneratedText = $TemplateText.Replace(
    "__APP_VERSION__",
    $Version
)

$GeneratedText | Set-Content -Path $GeneratedIss -Encoding UTF8

$ExpectedSetup = Join-Path $OutputDir "ManaChaiLeasing_Setup_$Version.exe"

try {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host " ManaChaiLeasing - Build Setup.exe" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Version : $Version"
    Write-Host "Compiler: $Iscc"
    Write-Host ""

    if (Test-Path $ExpectedSetup) {
        Remove-Item $ExpectedSetup -Force
    }

    & $Iscc $GeneratedIss

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup Compiler failed with exit code $LASTEXITCODE"
    }

    if (-not (Test-Path $ExpectedSetup)) {
        throw "Compiler finished but expected Setup.exe was not found: $ExpectedSetup"
    }

    $SetupInfo = Get-Item $ExpectedSetup

    Write-Host ""
    Write-Host "Setup build completed." -ForegroundColor Green
    Write-Host "Setup: $ExpectedSetup"
    Write-Host ("Size : {0:N2} MB" -f ($SetupInfo.Length / 1MB))
    Write-Host ""
}
finally {
    if (Test-Path $GeneratedIss) {
        Remove-Item $GeneratedIss -Force -ErrorAction SilentlyContinue
    }
}
