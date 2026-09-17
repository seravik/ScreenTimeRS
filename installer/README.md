# ScreenTime RS v0.5.1 安装程序

本安装程序使用 Inno Setup 6 构建。构建前会先编译 Rust 后台采集器，并发布 WinUI 3 前端，然后由 Inno Setup 生成可安装的 Windows x64 EXE。

## 构建

在项目根目录执行：

```powershell
.\build-installer.bat
```

输出：

```text
installer-output\ScreenTimeRS-v0.5.1-Setup.exe
```

安装程序使用管理员权限安装（`PrivilegesRequired=admin`），默认安装到 `C:\Program Files\ScreenTime RS`，支持可选的桌面快捷方式和 Windows 登录后自动启动。

安装程序不会使用 `SetupIconFile` 覆盖 Setup.exe 图标，以避免部分系统出现 `EndUpdateResource 110` 资源更新错误。安装后的应用程序、开始菜单快捷方式和桌面快捷方式仍会使用 `ScreenTimeRS.ico`。
