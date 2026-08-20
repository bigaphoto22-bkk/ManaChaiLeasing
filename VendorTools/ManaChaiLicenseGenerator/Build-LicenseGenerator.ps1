$ErrorActionPreference = "Stop"

$ToolName = "ManaChaiLicenseGenerator"
$SourceFile = Join-Path $PSScriptRoot "Program.cs.txt"

if (-not (Test-Path $SourceFile)) {
    throw "Program.cs.txt not found: $SourceFile"
}

$Dotnet = Get-Command dotnet -ErrorAction Stop

$TempRoot = Join-Path $env:TEMP ("ManaChaiLicenseGeneratorBuild_" + [Guid]::NewGuid().ToString("N"))
$PublishDir = Join-Path $env:LOCALAPPDATA "ManaChaiLicenseVendor\Tool"

New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

try {
    Copy-Item $SourceFile (Join-Path $TempRoot "Program.cs") -Force

    $ProjectFile = Join-Path $TempRoot "$ToolName.csproj"

    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>ManaChaiLicenseGenerator</AssemblyName>
  </PropertyGroup>
</Project>
'@ | Set-Content -Path $ProjectFile -Encoding UTF8

    Write-Host ""
    Write-Host "Building ManaChai License Generator..." -ForegroundColor Cyan

    & $Dotnet.Source publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $PublishDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    $ExePath = Join-Path $PublishDir "ManaChaiLicenseGenerator.exe"

    if (-not (Test-Path $ExePath)) {
        throw "Build completed but EXE was not found: $ExePath"
    }

    Write-Host ""
    Write-Host "Build completed." -ForegroundColor Green
    Write-Host "Tool: $ExePath"
    Write-Host "Vendor Key: $env:LOCALAPPDATA\ManaChaiLicenseVendor\Keys"
    Write-Host "Default license output: Documents\ManaChai Licenses"
    Write-Host ""

    Start-Process $ExePath
}
finally {
    if (Test-Path $TempRoot) {
        Remove-Item $TempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
