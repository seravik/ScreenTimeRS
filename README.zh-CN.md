# ScreenTime RS v0.8.0

ScreenTime RS 是一款面向 Windows 的本地屏幕使用时间与应用使用统计工具，采用 **Rust 监控核心 + WinUI 3 / Fluent UI** 架构。使用数据保存在本机 SQLite 数据库中，并通过 Windows 桌面界面进行展示。

## v0.8.0

- 新增按任意历史日期查看当天具体使用过的应用及使用时间。
- 新增应用详情，可查看应用使用时长、时间占比以及按日历史记录。
- 统计页面新增应用使用时间占比。
- 完善常见 Windows 与第三方应用的名称识别。
- 改进 Windows 锁定 / 会话状态检测，提升使用时间统计准确性。
- 将实时运行快照与历史统计缓存分离，降低历史数据增长后的刷新开销。
- 增强数据导入安全性：导入前校验数据并自动创建本地 SQLite 备份。
- 精简设置页面，折叠低频使用的功能区域。

## 主要功能

- 概览：今天、昨天、本周、本月使用统计。
- 应用使用时间：今天、本周、本月、近半年、近一年、全部时间、指定日期。
- 统计：最近 30 天、最近 90 天、近半年、近一年、全部时间。
- 每日使用趋势、应用排行及使用时间占比。
- 支持查询历史上实际记录过的任意日期的应用使用明细。
- 应用详情支持查看按日记录的历史使用时间。
- Windows 空闲及锁定 / 会话状态检测。
- CPU 与内存监控。
- 本地 SQLite 数据存储、JSON 导出 / 导入，以及导入前自动备份。
- Windows 系统托盘与可选后台启动。
- 浅色、深色、跟随系统及自定义主题色。

## 项目架构

```text
WinUI 3 / Fluent UI
        │
        ├── snapshot.json   ← 轻量实时状态，每秒刷新
        └── analytics.json  ← 历史统计缓存，周期性刷新
        │
        ▼
Rust 监控核心
        │
        ├── 前台应用检测
        ├── 键鼠空闲 / Windows 锁定与会话状态检测
        ├── CPU / 内存监控
        ├── SQLite 历史数据库
        └── 系统托盘
```

## 构建环境

- Windows 10 / 11 x64
- Rust Stable + MSVC 工具链
- .NET 8 SDK
- Windows App SDK（通过 NuGet 自动还原）
- Inno Setup 6（用于生成安装程序）

### 构建应用

```powershell
.\build-windows.bat
```

输出：

```text
dist\ui\ScreenTimeRS.UI.exe
dist\ui\screentime-rs.exe
```

### 构建安装程序

```powershell
.\build-installer.bat
```

输出：

```text
installer-output\ScreenTimeRS-v0.8.0-Setup.exe
```

## 使用数据备份

在**设置 → 数据管理**中可以导出或导入使用数据。

备份文件采用 JSON 格式，仅包含本机记录的应用使用历史。导入时会先校验数据，并在替换当前历史记录之前自动创建本地 SQLite 备份。应用设置不会包含在备份中。

“删除全部数据”仅删除已记录的使用历史，不会删除应用设置，也不会卸载程序。

## 本地数据位置

SQLite 数据库：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\screentime.db
```

实时快照：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\snapshot.json
```

历史统计缓存：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\analytics.json
```

导入前自动备份：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\backups\
```

ScreenTime RS 不会主动将使用统计数据上传到远程服务器。

## 许可证

详见 [LICENSE](LICENSE)。

## 当前版本

**v0.8.0**

### 统一使用数据模型

从本大改版开始，运行时使用记录统一写入本地 SQLite 的 `usage_records`。精确 Session 保存开始/结束时间，旧版或导入的历史数据以“按日精度”保存。概览的今日总计、应用使用时间、统计趋势、All Time 与今日按小时均从这一事实数据模型计算；历史按日数据不会被虚构分配到具体小时。
