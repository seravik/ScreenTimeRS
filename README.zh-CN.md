# ScreenTime RS v0.7.4

ScreenTime RS 是一款面向 Windows 的本地屏幕使用时间与应用使用统计工具，采用 **Rust 监控核心 + WinUI 3 / Fluent UI** 架构。使用数据保存在本机 SQLite 数据库中，并通过 Windows 桌面界面进行展示。

## v0.7.4

- 修复按钮长按时错误显示主题色的问题，普通按钮按下时恢复为原生非主题色状态。
- 新增**繁体中文**支持，并将原“中文”选项调整为**简体中文**。
- 新增本地使用数据**导出与导入**，支持 JSON 备份。
- 支持界面语言：跟随系统、简体中文、繁体中文和 English。
- 新增**删除全部数据**功能，采用随机生成的确认码进行二次确认，并使用红色高亮显示删除操作。
- 移除重复的顶部页面条幅，将主要操作整合至自定义标题栏。
- 简化导航栏宽度调整交互，采用隐藏式边缘拖动区域和标准水平调整光标。
- 完善导航栏、设置、统计、使用时间范围及颜色选择器相关界面的多语言支持。

## 主要功能

- 概览：今天、昨天、本周、本月使用统计。
- 应用使用时间：今天、本周、本月、近半年、近一年、全部时间。
- 统计：最近 30 天、最近 90 天、近半年、近一年、全部时间。
- 每日使用趋势与应用排行。
- Windows 空闲及锁定状态检测，提升使用时间统计准确性。
- CPU 与内存监控。
- 本地 SQLite 数据存储，以及 JSON 数据备份与恢复。
- Windows 系统托盘与可选后台启动。
- 浅色、深色、跟随系统及自定义主题色。

## 项目架构

```text
WinUI 3 / Fluent UI
        │
        │ snapshot.json
        ▼
Rust 监控核心
        │
        ├── 前台应用检测
        ├── 键鼠空闲 / 锁定状态检测
        ├── CPU / 内存监控
        ├── SQLite 本地数据库
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
installer-output\ScreenTimeRS-v0.7.4-Setup.exe
```

## 使用数据备份

在**设置 → 数据管理**中可以导出或导入使用数据。

备份文件采用 JSON 格式，仅包含本机记录的应用使用历史。导入数据后，会以所选备份替换当前使用历史；应用设置不会包含在备份中。

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

ScreenTime RS 不会主动将使用统计数据上传到远程服务器。

## 许可证

详见 [LICENSE](LICENSE)。

## 当前版本

**v0.7.4**
