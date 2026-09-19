#define MyAppName "ScreenTime RS"
#define MyAppVersion "0.8.0"
#define MyAppPublisher "ScreenTime RS"
#define MyAppExeName "ScreenTimeRS.UI.exe"

[Setup]
AppId={{8A0A5F6B-7D1B-4E1C-9B91-0A5E9E8A0B10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ScreenTime RS
DefaultGroupName={#MyAppName}
OutputDir=..\installer-output
OutputBaseFilename=ScreenTimeRS-v0.8.0-Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2/ultra64
LZMAUseSeparateProcess=yes
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
LanguageDetectionMethod=none
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimp"; MessagesFile: "Languages\ChineseSimplified.isl"
Name: "chinesetrad"; MessagesFile: "Languages\ChineseTraditional.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:ShortcutGroup}"
Name: "startup"; Description: "{cm:StartupDescription}"; GroupDescription: "{cm:StartupGroup}"

[CustomMessages]
english.ShortcutGroup=Shortcuts:
english.StartupDescription=Start ScreenTime RS silently in the background when I sign in to Windows
english.StartupGroup=Windows startup:
chinesesimp.ShortcutGroup=快捷方式：
chinesesimp.StartupDescription=登录 Windows 时在后台静默启动 ScreenTime RS
chinesesimp.StartupGroup=Windows 启动：
chinesetrad.ShortcutGroup=捷徑：
chinesetrad.StartupDescription=登入 Windows 時在背景靜默啟動 ScreenTime RS
chinesetrad.StartupGroup=Windows 啟動：

[Files]
Source: "..\dist\ui\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"
Source: "..\README.zh-CN.md"; DestDir: "{app}"
Source: "..\CHANGELOG.md"; DestDir: "{app}"
Source: "..\TERMS.md"; DestDir: "{app}"
Source: "..\PRIVACY.md"; DestDir: "{app}"
Source: "..\assets\ScreenTimeRS.ico"; DestDir: "{app}"

[Icons]
Name: "{autoprograms}\ScreenTime RS"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\ScreenTimeRS.ico"
Name: "{autodesktop}\ScreenTime RS"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\ScreenTimeRS.ico"
[Registry]
; Use the per-user Run key for reliable Windows sign-in startup.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ScreenTimeRS"; ValueData: """{app}\screentime-rs.exe"" --background"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch ScreenTime RS"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
