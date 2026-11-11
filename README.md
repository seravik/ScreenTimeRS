# ScreenTime RS v0.9.2

ScreenTime RS is a local Windows screen-time and application-usage tracker built with a **Rust monitoring core + WinUI 3 / Fluent UI**. Usage records are stored locally in SQLite and presented through the Windows desktop interface.

## v0.9.2

- Reorganized navigation into Overview, Usage, Focus, and Settings, with Settings moved to the bottom of the navigation pane.
- Merged Statistics into Usage as a Trends tab and moved the full focus history into a Focus tab, removing the duplicated pages.
- Reduced the Overview to today's figures plus a top-5 application list, and removed the duplicate 30-day chart.
- Replaced the application-detail dialog with a side panel that follows the selected row.
- Fixed the application usage share so it is measured against the selected range total.
- Stopped charts and top-application bars from redrawing on every refresh.
- Grouped Settings into General, Data, and About.
- Rewrote the Terms of Use and Privacy Policy with clearer scope, data handling, retention, and liability terms. The acceptance flag is now version-specific, so the documents are presented again on first launch after upgrading.

## v0.9.1

- Fixed JSON import appending focus history instead of replacing it; importing the same backup twice no longer duplicates focus records.
- Fixed JSON export silently stopping at 100 focus sessions; exports now include the complete focus history.

## v0.9.0

- Added Focus with 15/25/30/45/60/90/120-minute presets and custom durations from 1 to 1440 minutes.
- Added a persistent focus countdown managed by the Rust background collector.
- Added pause, resume, end, daily focus totals, completion count, pause count, and paged focus history with deletion and safe cleanup.
- Focus automatically stops accumulating time while Windows is locked, the computer is inactive for a prolonged period, or global usage tracking is paused.
- Focus history is stored locally and included in JSON backup and restore; history is loaded 20 records at a time to keep the interface responsive.
- Screen-time accounting and focus time remain separate metrics.

## v0.8.0

- View application usage for any selected historical date.
- Open application details with usage totals, period share, and recorded daily history.
- Added application usage percentages to Statistics.
- Improved application name recognition for common Windows and third-party applications.
- Improved Windows lock/session-state detection and screen-time accounting accuracy.
- Split the live runtime snapshot from slower historical analytics to reduce refresh overhead as history grows.
- Safer data import with validation and an automatic local SQLite backup before replacement.
- Improved the Settings page with clearer functional sections, data-location access, and advanced controls.

## Core features

- Today's overview with active time, current and longest continuous use, unlock count, hourly distribution, and a top-5 application list.
- Application usage ranges: Today, This week, This month, Last 6 months, Last year, All time, and Selected day.
- Trend ranges: Last 30 days, Last 90 days, Last 6 months, Last year, and All time.
- Daily usage charts, application rankings, and usage-share percentages.
- Historical daily application usage that can be queried back to the oldest recorded data.
- Master-detail application view with recorded daily history.
- Windows idle and lock/session-state awareness.
- CPU and memory monitoring.
- Local SQLite storage with JSON export/import and automatic pre-import backup.
- Windows system-tray support and optional background startup.
- Light, dark, system, and custom accent-color themes.
- Focus sessions with persistent countdowns, pause/resume, automatic inactivity pause, and focus history.

## Architecture

```text
WinUI 3 / Fluent UI
        │
        ├── snapshot.json   ← lightweight live state, written by the core every second
        └── analytics.json  ← cached historical ranges, refreshed periodically
        │
        ▼
Rust monitoring core
        │
        ├── Foreground application detection
        ├── Keyboard / mouse idle and Windows lock/session-state detection
        ├── CPU / memory metrics
        ├── Local SQLite historical database
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
installer-output\ScreenTimeRS-v0.9.2-Setup.exe
```

## Usage-data backup

Export and import are available from **Settings → Data management**.

The backup format is JSON and contains the locally recorded application-usage and focus history. Importing validates the data and automatically creates a local SQLite backup before replacing the current recorded history. Settings are not included in the backup.

The delete-all-data action removes recorded usage and focus history. It does not remove application settings or uninstall the program.

## Local data

SQLite database:

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\screentime.db
```

Live runtime snapshot:

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\snapshot.json
```

Historical analytics cache:

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\analytics.json
```

Automatic import backups:

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\backups\
```

ScreenTime RS does not actively upload usage statistics to a remote server.

## License

See [LICENSE](LICENSE).

## Version

**v0.9.2**

