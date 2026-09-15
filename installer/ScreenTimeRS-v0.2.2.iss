#define MyAppName "ScreenTime RS"
#define MyAppVersion "0.2.2"
#define MyAppPublisher "ScreenTime RS"
#define MyAppExeName "ScreenTimeRS.UI.exe"

[Setup]
AppId={{8A0A5F6B-7D1B-4E1C-9B91-0A5E9E8A0B10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\ScreenTime RS
DefaultGroupName={#MyAppName}
OutputDir=..\installer-output
OutputBaseFilename=ScreenTimeRS-v0.2.2-Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
DisableProgramGroupPage=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startup"; Description: "Start ScreenTime RS automatically when I sign in to Windows"; GroupDescription: "Windows startup:"

[Files]
Source: "..\dist\ui\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"
Source: "..\CHANGELOG.md"; DestDir: "{app}"
Source: "..\assets\ScreenTimeRS.ico"; DestDir: "{app}"

[Icons]
Name: "{autoprograms}\ScreenTime RS"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\ScreenTimeRS.ico"
Name: "{autodesktop}\ScreenTime RS"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\ScreenTimeRS.ico"
[Registry]
; Use the per-user Run key for reliable Windows sign-in startup.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ScreenTimeRS"; ValueData: "{app}\{#MyAppExeName}"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch ScreenTime RS"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
