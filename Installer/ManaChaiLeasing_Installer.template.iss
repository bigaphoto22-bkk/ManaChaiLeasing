; ============================================================
; ManaChaiLeasing - Release Installer Template
; Version is injected from Installer\ReleaseVersion.txt
; by Build-Setup.ps1.
;
; IMPORTANT:
; Source application files must already exist in:
; ..\Publish\ManaChaiLeasing-win-x64\
; ============================================================

#define MyAppName "มานะชัย ลิสซิ่ง"
#define MyAppVersion "__APP_VERSION__"
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
OutputBaseFilename=ManaChaiLeasing_Setup_{#MyAppVersion}

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

SetupIconFile=..\Resources\ManaChaiLeasing.ico

UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

VersionInfoVersion={#MyAppVersion}.0
VersionInfoDescription={#MyAppName} Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Tasks]
Name: "desktopicon"; Description: "สร้างไอคอนบน Desktop"; GroupDescription: "ตัวเลือกเพิ่มเติม:"; Flags: unchecked

[Files]
Source: "..\Publish\ManaChaiLeasing-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "เปิด {#MyAppName}"; Flags: nowait postinstall skipifsilent
