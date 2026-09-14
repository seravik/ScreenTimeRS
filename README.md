# ScreenTime RS v0.1.0

Rust Windows screen-time tracker.

## Included

- Windows-style GUI
- SQLite persistence
- Today / yesterday / week / month statistics
- 14-day visual trend
- Application usage ranking
- Foreground process identification
- Current window title
- Lock-state-aware collection
- Conservative monitor activity status
- Real-time CPU and memory
- System tray
- Background mode
- Windows login auto-start
- Light/dark theme
- Windows x64 GitHub Actions build
- Release build without a console window

## Build

```powershell
cargo build --release
```

or:

```powershell
.\build-windows.bat
```

Output:

`dist\ScreenTimeRS-v0.1.0.exe`

## Background

```powershell
.\dist\ScreenTimeRS-v0.1.0.exe --background
```

## Data

`%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\screentime.db`

All statistics stay local.

## Chinese font support

The Windows GUI bundles Noto Sans CJK SC under the SIL Open Font License so Simplified Chinese text renders correctly even when the host system font configuration is different.
