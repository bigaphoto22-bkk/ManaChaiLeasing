; ============================================================
; ManaChaiLeasing - Pilot Installer
; Version 0.1.0
;
; Place this file in:
; C:\Dev\PawnShop-2\ManaChaiLeasing\Installer\ManaChaiLeasing_Installer_0.1.0.iss
;
; Expected published files:
; C:\Dev\PawnShop-2\ManaChaiLeasing\Publish\ManaChaiLeasing-win-x64\
; ============================================================

#define MyAppName "มานะชัย ลิสซิ่ง"
#define MyAppVersion "0.1.0"
#define MyAppExeName "ManaChaiLeasing.exe"

[Setup]
AppId={{A37C3B29-821A-4EE0-9E9D-A01C2B77F001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}

DefaultDirName={autopf}\ManaChaiLeasing
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

PrivilegesRequired=admin

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir=Output
OutputBaseFilename=ManaChaiLeasing_Setup_0.1.0

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

VersionInfoVersion=0.1.0.0
VersionInfoDescription={#MyAppName} Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Tasks]
Name: "desktopicon"; Description: "สร้างไอคอนบน Desktop"; GroupDescription: "ตัวเลือกเพิ่มเติม:"; Flags: unchecked

[Files]
Source: "..\Publish\ManaChaiLeasing-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "เปิด {#MyAppName}"; Flags: nowait postinstall skipifsilent
