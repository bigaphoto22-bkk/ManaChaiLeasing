$ErrorActionPreference = "Stop"

$InstallerDir = $PSScriptRoot
$ProjectRoot = Split-Path $InstallerDir -Parent
$ProjectFile = Join-Path $ProjectRoot "ManaChaiLeasing.csproj"
$EmbedScript = Join-Path $ProjectRoot "VendorTools\Embed-PublicKeyIntoClient.ps1"
$PublicKeySource = Join-Path $ProjectRoot "Licensing\VendorPublicKey.cs"
$PublishDir = Join-Path $ProjectRoot "Publish\ManaChaiLeasing-win-x64"
$PublishedExe = Join-Path $PublishDir "ManaChaiLeasing.exe"


if (-not (Test-Path $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

if (-not (Test-Path $EmbedScript)) {
    throw "Public Key embedding script not found: $EmbedScript"
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " ManaChaiLeasing - Publish Application" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/4] Embedding Vendor Public Key..." -ForegroundColor Yellow
& powershell -NoProfile -ExecutionPolicy Bypass -File $EmbedScript

if ($LASTEXITCODE -ne 0) {
    throw "Vendor Public Key embedding failed."
}

if (-not (Test-Path $PublicKeySource)) {
    throw "VendorPublicKey.cs was not created."
}

$PublicKeyText = Get-Content $PublicKeySource -Raw

$KeyIdMatch = [regex]::Match(
    $PublicKeyText,
    'public\s+const\s+string\s+KeyId\s*=\s*"(?<keyid>MC-KEY-[0-9A-F]+)"\s*;'
)

$PemMatch = [regex]::Match(
    $PublicKeyText,
    'private\s+const\s+string\s+PemBase64\s*=\s*"(?<pem>[A-Za-z0-9+/=]+)"\s*;'
)

if (-not $KeyIdMatch.Success) {
    throw "Vendor Public Key KeyId is not configured. Release build stopped."
}

if (-not $PemMatch.Success -or
    [string]::IsNullOrWhiteSpace($PemMatch.Groups["pem"].Value)) {
    throw "Vendor Public Key PEM is not configured. Release build stopped."
}

$KeyId = $KeyIdMatch.Groups["keyid"].Value

Write-Host "      Public Key: $KeyId" -ForegroundColor Green

Write-Host "[2/4] Cleaning Release build..." -ForegroundColor Yellow

& dotnet clean $ProjectFile `
    "-c" "Release"

if ($LASTEXITCODE -ne 0) {
    throw "dotnet clean failed with exit code $LASTEXITCODE"
}

Write-Host "[3/4] Removing stale Publish output..." -ForegroundColor Yellow

if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

Write-Host "[4/4] Publishing Release win-x64 self-contained..." -ForegroundColor Yellow

& dotnet publish $ProjectFile `
    "-c" "Release" `
    "-r" "win-x64" `
    "--self-contained" "true" `
    "-o" $PublishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $PublishedExe)) {
    throw "Publish finished but ManaChaiLeasing.exe was not found: $PublishedExe"
}

$ExeInfo = Get-Item $PublishedExe

Write-Host ""
Write-Host "Publish completed." -ForegroundColor Green
Write-Host "Public Key : $KeyId"
Write-Host "Application: $PublishedExe"
Write-Host ("EXE size   : {0:N2} MB" -f ($ExeInfo.Length / 1MB))
Write-Host ""
