# ScreenTime RS v0.6.0

ScreenTime RS 是一款面向 Windows 的本地屏幕使用时间与应用使用统计工具，采用 **Rust 监控核心 + WinUI 3 / Fluent UI**。数据保存在本机 SQLite 中，WinUI 前端通过实时快照展示统计结果。

## v0.6.0 更新

- Statistics 支持 **最近 30 天 / 最近 90 天 / 近半年 / 近一年 / 全部时间 (Last 30 days / Last 90 days / Last 6 months / Last year / All time)** 统计范围。
- 新增周期总计、日均使用、活跃天数与最高单日使用时长。
- 重构每日使用趋势为紧凑的竖状柱形图，并保留周期内 Top Apps。
- App Usage 支持今天 / 本周 / 本月 / 近半年 / 近一年 / 全部时间查询范围。
- 后台采集加入 Windows 最近输入检测，连续 **5 分钟**无键鼠输入时暂停计时，减少离开电脑后的误计时。
- 历史统计扩展至年度及全部时间范围，并降低历史应用查询刷新频率，减少后台数据库开销。
- Overview 趋势范围扩展至最近 30 天，并保持锁定 / 空闲状态反馈与实时 CPU / 内存信息。

## 项目架构

```text
WinUI 3 / Fluent UI
        │
        │ snapshot.json
        ▼
Rust 后台监控核心
        │
        ├── 前台进程检测
        ├── 键鼠空闲 / 锁定状态检测
        ├── CPU / 内存指标
        ├── SQLite 本地数据库
        └── 系统托盘
```

## Windows 构建环境

要求：

- Windows 10 / 11 x64
- Rust Stable + MSVC 工具链
- .NET 8 SDK
- Windows App SDK（通过 NuGet 自动还原）
- Inno Setup 6（用于生成安装程序）

### 构建程序

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
installer-output\ScreenTimeRS-v0.6.0-Setup.exe
```

## 数据位置

SQLite：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\screentime.db
```

实时快照：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\snapshot.json
```

项目不会主动将统计数据上传到远程服务器。

## 当前版本

**v0.6.0**
