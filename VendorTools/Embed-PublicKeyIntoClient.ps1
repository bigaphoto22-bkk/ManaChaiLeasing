$ErrorActionPreference = "Stop"

$VendorKeyDir = Join-Path $env:LOCALAPPDATA "ManaChaiLicenseVendor\Keys"
$PublicKeyFile = Join-Path $VendorKeyDir "vendor-public-key.pem"
$KeyInfoFile = Join-Path $VendorKeyDir "key-info.json"

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$TargetDir = Join-Path $ProjectRoot "Licensing"
$TargetFile = Join-Path $TargetDir "VendorPublicKey.cs"

if (-not (Test-Path $PublicKeyFile)) {
    throw "Public Key not found: $PublicKeyFile`nRun/restore Phase 2L.2 Vendor Key Manager first."
}

if (-not (Test-Path $KeyInfoFile)) {
    throw "Key metadata not found: $KeyInfoFile"
}

$Info = Get-Content $KeyInfoFile -Raw | ConvertFrom-Json
$KeyId = [string]$Info.KeyId

if ([string]::IsNullOrWhiteSpace($KeyId)) {
    throw "KeyId is missing from key-info.json"
}

if ($KeyId -notmatch '^MC-KEY-[0-9A-F]+$') {
    throw "Unexpected KeyId format: $KeyId"
}

$PemText = Get-Content $PublicKeyFile -Raw

if ($PemText -notmatch 'BEGIN PUBLIC KEY' -or
    $PemText -match 'PRIVATE KEY') {
    throw "vendor-public-key.pem does not look like a PUBLIC key."
}

$PemBytes = [System.Text.Encoding]::UTF8.GetBytes($PemText)
$PemBase64 = [Convert]::ToBase64String($PemBytes)

New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

$Source = @"
namespace ManaChaiLeasing.Licensing;

internal static class VendorPublicKey
{
    public const string KeyId = "$KeyId";

    private const string PemBase64 = "$PemBase64";

    public static bool IsConfigured =>
        KeyId != "NOT-CONFIGURED" &&
        !string.IsNullOrWhiteSpace(PemBase64);

    public static string Pem =>
        string.IsNullOrWhiteSpace(PemBase64)
            ? string.Empty
            : System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(PemBase64));
}
"@

$Source | Set-Content -Path $TargetFile -Encoding UTF8

Write-Host ""
Write-Host "Vendor Public Key embedded into client source." -ForegroundColor Green
Write-Host "Key ID: $KeyId"
Write-Host "File: $TargetFile"
Write-Host ""
Write-Host "This is a PUBLIC key. It is safe and required to commit this generated source file." -ForegroundColor Yellow
Write-Host "No Private Key or password was copied into the customer application." -ForegroundColor Yellow
Write-Host ""
