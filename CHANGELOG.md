
### 修复：v0.2.0 WinUI 数据采集与统计
- 修复 WinUI 前端与 Rust 核心读取不同数据目录导致界面始终显示 `00小时 00分钟` 的问题。
- 统一使用 Windows LocalAppData 数据目录，并自动迁移旧版 RoamingAppData 中已有的 SQLite 数据库。
- 修复前端对 `screentime-rs.exe` 的进程判断，避免误把其他目录中的旧版本进程当作当前版本采集器。
- 采集周期调整为约 1 秒，使概览、应用排行和统计数据能够持续刷新。
- 统计页增加最近 14 天趋势和今日应用排行。
# Changelog

## v0.2.0 - 2026-09-14

Major Windows UI and application-identification upgrade.

### Added

- Replaced the v0.1.0 egui desktop interface with a native WinUI 3 / Fluent UI frontend.
- Added Overview, Application Usage, Statistics, and Settings navigation.
- Added Windows-friendly application display names instead of exposing only executable filenames.
- Added executable-path persistence for application records.
- Added Windows executable icon extraction for the application list.
- Added application search and usage progress indicators.
- Added a dedicated Rust-to-UI snapshot channel while keeping SQLite as the persistent source of truth.
- Updated Windows x64 GitHub Actions workflows for the WinUI 3 application.
- Updated the Inno Setup installer for v0.2.0.

### Changed

- Version advanced from v0.1.0 to v0.2.0 because the desktop UI architecture was substantially redesigned.
- Retained the Rust monitoring core, SQLite storage, foreground-window detection, lock/activity handling, CPU/memory monitoring, tray/background mode, and Windows startup support.
- Updated the application architecture to separate monitoring/persistence from the Windows UI layer.

### Fixed

- Application usage no longer presents entries such as `firefox.exe` and `explorer.exe` as the primary user-facing application names when Windows metadata can provide a better name.

### Notes

- WinUI 3 is supplied through Windows App SDK 2.2.
- Some third-party or portable executables may not expose a friendly product name or icon; the UI falls back to the executable name in that case.
