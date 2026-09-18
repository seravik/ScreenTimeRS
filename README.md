# ScreenTime RS v0.7.3

ScreenTime RS is a local Windows screen-time and application-usage tracker built with a **Rust monitoring core + WinUI 3 / Fluent UI**. Usage records are stored locally in SQLite and presented through the Windows desktop interface.

## v0.7.3

- Added **Traditional Chinese** and renamed the original Chinese option to **Simplified Chinese**.
- Added local usage-data **export and import** using JSON backups.
- Supported interface languages: Follow system, Simplified Chinese, Traditional Chinese, and English.
- Added **Delete all data** with a generated confirmation code and an explicitly highlighted destructive action.
- Removed the duplicated top page banner and consolidated the main controls into the custom title bar.
- Simplified navigation-pane resizing with an invisible edge hit area and the standard horizontal-resize cursor.
- Improved language coverage across navigation, settings, statistics, usage periods, and color-picker related UI.

## Core features

- Today, yesterday, this week, and this month overview statistics.
- Application usage ranges: Today, This week, This month, Last 6 months, Last year, and All time.
- Statistics ranges: Last 30 days, Last 90 days, Last 6 months, Last year, and All time.
- Daily usage charts and application rankings.
- Windows idle and lock-state awareness to improve usage-time accuracy.
- CPU and memory monitoring.
- Local SQLite storage with JSON backup and restore.
- Windows system-tray support and optional background startup.
- Light, dark, system, and custom accent-color themes.

## Architecture

```text
WinUI 3 / Fluent UI
        │
        │ snapshot.json
        ▼
Rust monitoring core
        │
        ├── Foreground application detection
        ├── Keyboard / mouse idle and lock-state detection
        ├── CPU / memory metrics
        ├── Local SQLite database
        └── System tray
```

## Build requirements

- Windows 10 / 11 x64
- Rust Stable with the MSVC toolchain
- .NET 8 SDK
- Windows App SDK restored through NuGet
- Inno Setup 6 for the installer

### Build the application

```powershell
.\build-windows.bat
```

Output:

```text
dist\ui\ScreenTimeRS.UI.exe
dist\ui\screentime-rs.exe
```

### Build the installer

```powershell
.\build-installer.bat
```

Output:

```text
installer-output\ScreenTimeRS-v0.7.3-Setup.exe
```

## Usage-data backup

Export and import are available from **Settings → Data management**.

The backup format is JSON and contains the locally recorded application-usage history. Importing replaces the current usage history with the selected backup. Settings are not included in the backup.

The delete-all-data action removes the recorded usage history. It does not remove application settings or uninstall the program.

## Local data

SQLite database:

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\screentime.db
```

Runtime snapshot:

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\snapshot.json
```

ScreenTime RS does not actively upload usage statistics to a remote server.

## License

See [LICENSE](LICENSE).

## Version

**v0.7.3**
