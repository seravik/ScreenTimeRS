## v0.6.1

### Corrective update
- 修复 `Pages.cs` 中 `ProgressBar` 主题轨道画刷引用错误导致的 `CS0103` 编译失败。
- 修复统计趋势图在切换较短统计周期后可能残留悬浮提示的问题。
- 修复统计页最高单日数据的可空引用警告。

- 新增最近 30 天、最近 90 天、近半年、近一年与全部时间统计视图，提供周期总计、日均、活跃天数与最高单日使用时长。
- 重构 Statistics 每日使用趋势为竖状柱形图，并保留周期内 Top Apps。
- 应用使用时间支持今天、本周、本月、近半年、近一年与全部时间范围。
- 改进后台采集：加入输入空闲检测，连续 5 分钟无键鼠输入时暂停计时，减少离开电脑后的误计时。
- 历史统计扩展至 30 天，并降低后台历史数据刷新频率，减少数据库与 UI 刷新开销。
- 优化锁定、活动与空闲状态展示，并保持本地 SQLite 数据结构兼容。
- 优化后台进程结构，由 Rust 主进程统一管理采集与系统托盘，避免重复启动采集器。
- 优化 Statistics 趋势图日期标签：图表使用紧凑日期格式，避免全部时间范围跨年份时标签重叠，完整日期仍可通过悬浮提示查看。

## v0.5.3

- 修复深色模式下 Custom color 颜色名称提示文字不可见的问题，优化提示框的深浅色主题适配。
- 保持颜色悬浮预览与点击/拖动选择交互稳定。

# Changelog


### v0.5.2 — Build correction
- Fixed `CS0104/CS0019` build errors in `Pages.cs` related to ambiguous point/input API types.
- Fixed the nullable initialization warning for the `ColorPicker` field.
- Hardened pointer release handling so only an active left-button drag/click can commit a color.

### v0.5.2 — ColorPicker localization stability fix
- 修复 WinUI 3 项目在 `Pages.cs` 中 `Point` 类型与 `System.Drawing.Point` 产生的编译歧义。
- 移除旧的原生 ColorSpectrum ToolTip 方案，改为应用自有悬浮颜色昵称层；悬浮显示名称，左键点击/拖动直接选择颜色。
- 不再修改或清理 ColorPicker 内部 ToolTip，也不让 ColorSpectrum 接收实际指针输入，避免系统中文颜色名称与自定义提示并存。
- 保持语言切换不重启、不退出，版本仍为 v0.5.2。


## v0.5.2

### Fixed
- Replaced the native ColorSpectrum color-name tooltip with an application-owned hover label.
- Hover only previews the localized color name; left-click/drag commits the selected color through the existing ColorPicker.Color pipeline.
- The native ColorSpectrum is kept for rendering, but pointer input is intercepted by a transparent surface so no second system tooltip can appear.
- Simplified runtime language switching: no process-wide language override, no ColorPicker recreation, and no application restart/exit when changing language or color.
- Kept ColorPicker color selection, sliders, and text inputs on the native control while localizing its visible labels in place.
- Removed unused bundled font/release-note/dev-profile files from the source package to reduce distribution size.

## v0.2.5 — 2026-09-15

### 修复

- 修复关闭窗口后导航栏收起/展开状态不能及时保存的问题。
- 优化导航栏状态持久化，点击关闭窗口后可立即正确恢复上次状态。

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
