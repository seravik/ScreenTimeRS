# ScreenTime RS v0.3.1

ScreenTime RS 是一款面向 Windows 的本地屏幕使用时间与应用使用统计工具。项目采用 **Rust 监控核心 + WinUI 3 / Fluent UI 原生 Windows 界面**，在本机完成使用时间采集、数据存储和可视化展示。

## v0.3.1 更新

### 修复

- 修复 English 模式下“App usage”页面应用使用时长仍显示中文“小时 / 分钟”的问题。
- 修复切换语言后应用列表使用时长未即时同步更新的问题。

## 项目架构

```text
WinUI 3 / C# 前端
        │
        │ snapshot.json
        ▼
Rust 后台监控核心
        │
        ├── 前台进程检测
        ├── 锁定/活动状态检测
        ├── CPU / 内存指标
        └── SQLite 本地数据库
```

WinUI 3 负责桌面界面和交互；Rust 负责后台监控、数据采集、持久化和系统托盘。SQLite 仍然是本地统计数据的持久化来源，`snapshot.json` 用于向 WinUI 前端提供实时状态。

## Windows 构建环境

要求：

- Windows 10 / 11 x64
- Rust Stable + MSVC 工具链
- .NET 8 SDK
- Windows App SDK 依赖（通过 NuGet 自动还原）
- Inno Setup 6（用于生成安装程序）

### 构建程序

在项目根目录执行：

```powershell
.\build-windows.bat
```

构建成功后主要输出：

```text
dist\ui\ScreenTimeRS.UI.exe
dist\ui\screentime-rs.exe
```

### 构建可安装 EXE

如果需要可以直接安装到 Windows 的安装程序，请执行：

```powershell
.\build-installer.bat
```

安装程序输出：

```text
installer-output\ScreenTimeRS-v0.3.1-Setup.exe
```

安装程序使用 Inno Setup 6，并采用当前用户安装方式，不要求管理员权限。安装时可以选择创建桌面快捷方式以及登录 Windows 后自动启动。

## 数据位置

统计数据全部保存在本机，不会由 ScreenTime RS 主动上传到远程服务器。

SQLite 数据库：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\screentime.db
```

实时界面快照：

```text
%LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data\snapshot.json
```

导航栏展开/收起状态保存在当前 Windows 用户注册表：

```text
HKEY_CURRENT_USER\Software\ScreenTimeRS\NavigationPaneOpen
```

## GitHub Actions

项目提供 Windows x64 GitHub Actions 工作流，可在推送对应版本标签后自动构建 Windows 程序和 Inno Setup 安装程序。

当前版本标签：

```text
v0.3.1
```

## 开源许可

本项目使用仓库中的 `LICENSE` 文件所声明的许可证。

## 当前版本

**v0.3.1**
