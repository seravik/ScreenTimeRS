#define MyAppName "ScreenTime RS"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "ScreenTime RS"
#define MyAppExeName "screentime-rs.exe"

[Setup]
AppId={{8A0A5F6B-7D1B-4E1C-9B91-0A5E9E8A0B10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\ScreenTime RS
DefaultGroupName={#MyAppName}
OutputDir=..\dist
OutputBaseFilename=ScreenTimeRS-v0.1.0-Setup
SetupIconFile=..\assets\ScreenTimeRS.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startup"; Description: "Start ScreenTime RS automatically when I sign in to Windows"; GroupDescription: "Windows startup:"; Flags: unchecked

[Files]
Source: "..\target\release\screentime-rs.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: isreadme
Source: "..\CHANGELOG.md"; DestDir: "{app}"
Source: "..\assets\ScreenTimeRS.ico"; DestDir: "{app}"

[Icons]
Name: "{autoprograms}\ScreenTime RS"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\ScreenTimeRS.ico"
Name: "{autodesktop}\ScreenTime RS"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\ScreenTimeRS.ico"
Name: "{userstartup}\ScreenTime RS"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--background"; Tasks: startup; IconFilename: "{app}\ScreenTimeRS.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch ScreenTime RS"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
