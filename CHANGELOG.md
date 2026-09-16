# 更新日志

## v0.5.0
- 新增“应用主题”按钮，可在不重启程序的情况下重新应用当前主题色，修复导航栏等控件在动态换色后视觉状态不同步的问题。

### 本次修复与调整
- 修复自定义主题色无法在整个界面稳定生效的问题，统一同步导航栏、主题按钮、最近 14 天时间轴等界面区域。
- 主题色彩与自定义颜色设置改为可展开/收起的折叠区域，默认收起，减少设置页面干扰。
- 新增首次使用同意确认：首次启动显示《用户条款与隐私政策》，选择“同意并继续”后进入软件；选择“不同意并退出”则退出。

### 新增
- 新增“使用条款与隐私政策”，支持中文 / English 双语查看。
- 新增多组预设主题色，并支持 ColorPicker 自定义主题色。
- 自定义主题色会动态生成 WinUI 强调色层级，并立即应用到界面。
- 主题色设置会保存并在下次启动时恢复。

### 移除
- 移除“设置 → 常规 → 后台继续记录使用时间”选项，避免与实际后台采集机制混淆。

### 优化
- 保留 Rust 后台核心与 WinUI 3 / Fluent UI 前端架构，不需要重写现有 UI 技术栈。
- 延续 Windows 登录后后台静默启动机制。

## v0.3.0

### 新增
- 设置页面新增“语言”选项，支持中文 / English 切换并自动保存选择
- 切换语言后，导航栏、顶部操作区以及主要页面内容会即时同步更新
- 优化“概览 → 最近 14 天”，鼠标悬浮时间条可查看具体使用时长
- “应用使用时间”新增今天、本周、本月、近半年、近一年数据切换
- “统计 → 今日应用排行”新增应用图标显示
- 左侧导航栏支持鼠标拖动调整宽度，并自动保存上次宽度
- 安装程序支持中文与英文界面，并调整默认安装位置

## v0.2.6

- 修复自定义标题栏图标未显示的问题
### 修复
- 修复设置页面主题切换状态异常的问题
- 修复跟随系统主题时标题栏颜色不一致的问题
- 修复自定义标题栏重复显示应用名称的问题
- 修复标题栏图标与文字垂直对齐问题

# Changelog

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
