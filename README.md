# ScreenTime RS v0.2.0

Windows screen-time and application-usage tracker built around a Rust monitoring core and a native **WinUI 3 / Fluent UI** desktop interface.

## v0.2.0 highlights

- Native Windows 11-style WinUI 3 interface.
- Fluent-style navigation: Overview, App Usage, Statistics, Settings.
- Rust collector remains responsible for foreground tracking, SQLite persistence, lock-state handling, CPU and memory metrics.
- Application entries now retain the executable path so the UI can resolve friendly Windows application names.
- Friendly names such as `Firefox`, `Google Chrome`, `Visual Studio Code`, and `文件资源管理器` instead of exposing only `.exe` names.
- Executable icons are extracted and shown in the application list when Windows exposes an icon.
- Searchable application usage list with progress indicators.
- System tray/background collector is retained.
- Windows login startup continues to be supported.
- Windows x64 GitHub Actions build and Inno Setup installer updated for v0.2.0.
- Uses Windows App SDK 2.2 for the WinUI 3 interface.

## Architecture

```text
WinUI 3 / C# frontend
        │
        │ snapshot.json
        ▼
Rust monitoring core
        │
        ├── foreground process detection
        ├── lock/activity detection
        ├── CPU / memory metrics
        └── SQLite local database
```

The UI is a real WinUI 3 application; the monitoring and persistence logic remains in Rust so the v0.1.0 data/monitoring foundation is preserved.

## Build on Windows

Requirements:

- Windows 10/11 x64
- Rust stable + MSVC toolchain
- .NET 8 SDK
- Windows App SDK dependencies restored through NuGet
- Inno Setup 6 for the installer

Build the application:

```powershell
.\build-windows.bat
```

Build the installer:

```powershell
.\build-installer.bat
```

Expected outputs:

```text
dist\ui\ScreenTimeRS.UI.exe
dist\ui\screentime-rs.exe
dist\ScreenTimeRS-v0.2.0-Setup.exe
```

## Data

Statistics remain local:

`%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\screentime.db`

The Rust collector also publishes a short-lived UI snapshot at:

`%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\snapshot.json`

No usage data is sent to a remote service by the application.

## Version

Current release: **v0.2.0**
