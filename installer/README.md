# ScreenTime RS v0.2.2 Installer

The installer is built with Inno Setup 6 after publishing the WinUI 3 frontend and copying the Rust monitoring executable into `dist\ui`.

Build from the project root:

```powershell
.\build-installer.bat
```

Output:

```text
dist\ScreenTimeRS-v0.2.2-Setup.exe
```

The installer is per-user (`PrivilegesRequired=lowest`) and creates optional desktop and Windows startup shortcuts.


Note: The installer intentionally does not override Setup.exe with SetupIconFile. This avoids Windows resource-update failures (EndUpdateResource 110) observed on some systems. The installed application, Start Menu shortcut, and desktop shortcut still use ScreenTimeRS.ico.
