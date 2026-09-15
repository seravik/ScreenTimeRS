# Changelog

## v0.2.4 — 2026-09-15

### 修复

- 修复导航栏关闭状态无法在重新启动软件后保持的问题。
- 修复从桌面重新启动后导航栏总是恢复展开的问题。
- 修复从系统托盘重新打开软件后导航栏状态无法恢复的问题。

### 改进

- 将导航栏展开/收起状态保存到当前 Windows 用户的本地设置。
- 更新 README.md，统一使用中文说明项目、构建、数据位置和版本信息。
- 更新 Windows 构建脚本、GitHub Actions 和 Inno Setup 安装程序至 v0.2.4。

## v0.2.3 — 2026-09-15

### 修复

- 修复概览页面窗口较小时使用时间文本被截断的问题。
- 优化概览统计卡片的响应式布局，小窗口自动调整为多行显示。
- 修复设置页面窗口放大后内容区域自动居中导致布局位置变化的问题。
- 设置页面保持左对齐，提升不同窗口尺寸下的布局一致性。

### 改进

- 优化 Windows 不同窗口尺寸下的 UI 可读性与布局适应能力。

## v0.2.2 — Corrective rebuild

- 修复 WinUI 前端与 Rust 采集器数据目录不一致导致数据页面读取失败的问题。
- 修复托盘退出标记路径不一致导致退出联动失败的问题。
- 修复窗口标题与应用图标显示问题。
- 安装包输出目录改为 `installer-output`，降低旧 Setup.exe 被锁定导致 Inno Setup 编译失败的概率。


## v0.2.2 — Maintenance Fixes

- Fixed the WinUI frontend reading `snapshot.json` from the wrong LocalAppData directory, restoring Overview, App Usage and Statistics data.
- Fixed the native window title showing `WinUI Desktop`; the title is now `ScreenTime RS`.
- Fixed the WinUI title-bar icon by explicitly applying `assets/ScreenTimeRS.ico` at runtime.
- Reinforced the Rust tray menu so it keeps a persistent `打开 ScreenTime RS` action and a visible `退出 ScreenTime RS` action.
- Kept the release version at **v0.2.2**; this is a corrective rebuild of the same version.

## v0.2.2 - 2026-09-14

### Fixed

- Fixed continuous visual flashing across Overview, Application Usage, Statistics, and Settings caused by recreating the active WinUI page every second.
- Changed live refresh to update existing controls in place instead of replacing the page tree every refresh cycle.
- Fixed application icons being repeatedly extracted and recreated during live refresh by caching extracted icons.
- Added a closable X button to the "正在记录使用时间" status InfoBar and preserved its closed state during live refresh.
- Fixed the tray "退出" action so it writes a shutdown request, closes the WinUI window, and stops the background collector cleanly.
- Reduced unnecessary UI layout churn by keeping page instances alive while navigating between sections.

### Changed

- Version advanced from v0.2.0 to v0.2.2 as a bug-fix release focused on live UI refresh and application shutdown behavior.
- Updated Windows build scripts, GitHub Actions, and Inno Setup configuration for v0.2.2.


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
