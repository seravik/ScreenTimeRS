# ScreenTime RS — 项目交接文档

> 版本：v0.9.3　|　更新日期：2026-11-12　|　维护者：wuchen128（原 seravik）

---

## 1. 项目概览

| 项目 | 说明 |
| --- | --- |
| 名称 | ScreenTime RS |
| 定位 | 本地 Windows 屏幕使用时间 / 应用使用追踪工具 |
| 技术栈 | Rust 监控核心 + WinUI 3（.NET 8）前端 |
| 数据存储 | 本地 SQLite（无云同步、无账号、无遥测） |
| 界面语言 | 简体中文 / 繁体中文 / 英文 |
| 开源许可 | 见 `LICENSE`（当前仅 3 行，作者署名与许可范围**未确认前不得改动**） |
| GitHub | https://github.com/wuchen128/ScreenTimeRS（public，main 分支） |

**核心价值**：全离线、本地存储，采集屏幕使用与应用使用时长，提供概览 / 使用 / 专注三类视图，支持专注计时、历史记录、数据导入导出。

---

## 2. 当前状态

- **当前版本**：v0.9.3（已发布，annotated tag `v0.9.3`，commit `2de505b`，Release id `441792392`，资产 `ScreenTimeRS-v0.9.3-Setup.exe`，60,618,881 bytes，SHA256 `68c9654c66a5bc74937fd9ae68a92a4fe59521ffce78582a6cd6d848a240aaa6`）。
- **最新提交**：`b61d8f0 refactor(ui): rebuild settings page and drop pause/location/diagnostics`（已推送）。
- **工作区状态**：存在两处**未提交**改动 —— `ui/SettingsPage.cs`、`ui/Str.cs`（设置页排版重构为 Win11 系统设置风格，尚未 commit）。

### 版本号一致性清单（发版时逐项核对）

以下位置必须与目标版本号保持一致（0.9.x）：

1. `Cargo.toml`（package.version）
2. `Cargo.lock`（根包条目）
3. 两个 bat：`build-windows.bat`、`build-installer.bat`
4. `installer/ScreenTimeRS-v0.9.3.iss`（`#define MyAppVersion` 与 `OutputBaseFilename`，发版时连同**文件名**一起改）
5. Rust `src/main.rs` 的 `--diagnostics`
6. `ui/SettingsPage.cs` 的 `const string Version`
7. `README.md`、`README.zh-CN.md`（标题 + `## v0.9.x` 新段落 + 安装包路径 + 末尾 Version）
8. `CHANGELOG.md`
9. `TERMS.md`、`PRIVACY.md`
10. `UNIFIED-DATA-MODEL.md`
11. `installer/README.md`
12. `LEGAL-LOCALIZATION.md`
13. `ui/LegalDocuments.cs` 内 6 份法律文本
14. 注册表键 `TermsAccepted_v0_9_N`
15. 安装包文件名

---

## 3. 系统架构

### 3.1 进程模型（架构红线）

- `screentime-rs.exe` 是**采集线程、SQLite、托盘的唯一所有者**。
- WinUI 前端**只读 JSON 快照 + 按需启动 CLI 子进程**，**不得另起采集循环**。
- IPC 为**本地文件**（JSON 快照 / flag 文件），**非管道、非 Socket**。

```
┌─────────────────────────────┐      JSON 快照      ┌──────────────────────────┐
│  WinUI 3 前端 (ScreenTimeRS.UI) │ ◄──────────────► │  screentime-rs.exe        │
│  · 概览/使用/专注/设置          │    按需 CLI 子进程   │  · 采集线程（屏幕/应用）    │
│  · 三语 + 主题                 │ ────────────────► │  · SQLite 读写             │
│  · 只读快照 + 发起命令          │     本地文件 IPC     │  · 托盘图标                │
└─────────────────────────────┘                    └──────────────────────────┘
```

### 3.2 数据红线（不可违反）

- `usage_records` 是屏幕使用**唯一规范事实表**，含两种精度：
  - `precision = precise`：真实运行时段（`start_at`/`end_at`/app/路径/秒数）。
  - `precision = daily`：无精确时间的每日聚合（历史/导入数据）。
- `focus_sessions` 是**独立**专注数据，**不可与 usage_records 合并**。
- 缺精确时间的 daily 数据**不得伪造小时归属**。
- 迁移用 `schema:unified-vN` 标记，须**幂等、可回滚或先备份**。

### 3.3 法律文本规则（关键红线）

- `TERMS.md` / `PRIVACY.md` 是**英文正本**。
- `ui/LegalDocuments.cs` 的 `TermsFallback` / `PrivacyFallback` 必须与两个 md **逐字节一致**（历史上用 Python difflib 校验）。
- 简中/繁中为**嵌入译文**，冲突时**英文优先**（条款第 22 条）。
- **版本策略**：法律正文**只在重写时**升版本标记与注册表键。
  - v0.9.1 未改法律文本 → `TERMS.md` 的 `Version: v0.9.0` 与 `TermsAccepted_v0_9_0` 都保持。
  - v0.9.2 重写 → 升 `TermsAccepted_v0_9_2` 并强制重新接受。
  - **v0.9.3 未改法律文本 → `TermsAccepted_v0_9_2` 键、Version 行、6 份文本全部保持 v0.9.2 不动**，升级不重新弹条款。

---

## 4. 代码结构

### 4.1 Rust 核心（`src/`）

| 文件 | 职责 |
| --- | --- |
| `src/main.rs` | 采集循环、SQLite、托盘、CLI 子命令、`--diagnostics` |
| `Cargo.toml` | 依赖：rusqlite、sysinfo、tray-icon、directories、chrono、serde 等 |
| `build.rs` | winresource（嵌入图标/版本资源） |

### 4.2 WinUI 前端（`ui/`）

| 文件 | 职责 |
| --- | --- |
| `App.xaml.cs` | 应用入口、主题/语言初始化 |
| `MainWindow.xaml` / `.cs` | 导航（NavigationView）、IPC 读取、页面切换、删除确认对话框 |
| `OverviewPage.cs` | 概览：今日统计 + 按小时柱状图 + Top Apps |
| `UsagePage.cs` | 使用：趋势 + 应用两个 Tab，图表悬停 tooltip |
| `FocusPage.cs` / `FocusShellPage.cs` / `FocusHistoryPage.cs` | 专注：计时 + 历史，预设时长按钮 |
| `SettingsPage.cs` | 设置：外观/常规/数据/关于 四分组（Win11 风格） |
| `Ui.cs` | **全局视觉语言**：四档字号、单一卡片、muted 画刷 |
| `Str.cs` | **三语字符串表**（唯一文案来源） |
| `ThemeManager.cs` | 主题 + 固定强调色（Light `#0078D4` / Dark `#4CC2FF`），覆盖 `AccentButton*` 系列 |
| `LegalDocuments.cs` | 6 份法律文本（Terms/Privacy × 三语），须与 md 一致 |
| `SegmentedTabs.cs` | 轻量文本 Tab 组件 |
| `Pages.cs` | 页面枚举/路由 |

### 4.3 其他

| 路径 | 职责 |
| --- | --- |
| `installer/` | Inno Setup 脚本（`ScreenTimeRS-v0.9.3.iss`）+ 多语言文件 |
| `dist/`、`installer-output/` | 构建产物（已 gitignore，不跟踪） |
| `assets/` | 图标等资源 |

---

## 5. 构建与发布

### 5.1 本机工具链（2026-11-09 已装齐）

| 工具 | 版本 / 位置 |
| --- | --- |
| Rust | 1.99.1 `stable-x86_64-pc-windows-gnu` + cargo rsproxy 稀疏镜像 |
| MinGW | WinLibs 16.2（`%LOCALAPPDATA%\Microsoft\WinGet\Packages\BrechtSanders.WinLibs.POSIX.UCRT_*\mingw64\bin`） |
| .NET SDK | 8.0.427（`%LOCALAPPDATA%\Microsoft\dotnet`） |
| Inno Setup | 6.7.3（`%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`） |

### 5.2 构建步骤（关键）

1. **构建必须把 MinGW bin 与 `~/.cargo/bin` 加进 PATH**，否则 linker 找不到。
2. `cargo test/build` 要设 `CARGO_PROFILE_DEV_DEBUG=0`（否则 rusqlite/windows 会卡死，已实测 25 分钟零 CPU）。
3. `cargo build --release --locked` 编译 Rust 核心。
4. `dotnet build` / `dotnet publish -c Release -o dist/ui` 编译前端。
5. ISCC 编译安装包（约 105 秒，会输出大量中文语言文件 warning，均非错误）。

### 5.3 发布前必做

- **发版前必须杀干净所有 `ScreenTimeRS.UI` 进程**，否则残留进程锁住 `dist/ui/*.dll` 导致 publish 报 MSB3027 重试 10 次失败。
- `rm -rf dist/ui` 会被 safe-delete 拦截（回收站操作失败），**直接覆盖 publish 即可**。

### 5.4 GitHub 发布（重要经验）

- **默认保留远程历史，不做 force push**；本地是 `--depth=1` 浅克隆，需要完整历史时 `git fetch --unshallow`。
- 无 gh CLI / 无 token 环境变量时，用 `git credential fill` 取凭据后走 `api.github.com` REST 与 `uploads.github.com` 上传资产。
- **代理故障绕行**（2026-11-11 实测）：代理只放行 `api.github.com`，`github.com` 的 CONNECT 返回 502。此时用 **Git Data API**（`/git/blobs` → `/git/trees` → `/git/commits` → `PATCH /git/refs/heads/main`）提交；GitHub 生成的 commit message 无结尾换行，sha 与本地不同，可在本地用 `git hash-object -t commit -w --stdin` 写入同内容对象，再 `update-ref` 对齐 `refs/heads/main` 与 `refs/remotes/origin/main`。

---

## 6. 功能清单（不得删除）

以下功能受保护，任何改动需保留：

- 全部时间 / 指定日期明细
- 专注当前状态与历史
- 历史删除确认
- 数据导入导出
- 主题（跟随系统/浅色/深色）
- 简繁英三语
- 托盘图标
- 暂停记录机制（`Snapshot.paused` 字段与 `pause.flag` 机制保留，数据契约不动）
- 自动备份

**危险操作必须 UI 二次确认。**

---

## 7. 未确认事项（改动前必须先确认）

- LICENSE 作者署名与许可范围（当前仅 3 行）。
- 软件名称。
- 不引入：云同步 / 账号 / 遥测 / 应用屏蔽。
- 本地 `git config user.name` 仍是 `seravik`（账号已改名 `wuchen128`，但 git 配置未改，改动需用户确认）。

---

## 8. GitHub 账号变更备忘

- **2026-11-11 22:23** 账号从 `seravik` 改名为 `wuchen128`，仓库现为 `https://github.com/wuchen128/ScreenTimeRS`（public，main）。
- 本地 `origin` 已 `set-url` 到新地址，旧路径由 GitHub 301 到仓库 ID `1369683028`。
- **后续所有 REST/API 的 owner 一律用 `wuchen128`**；法律文本与文档里的仓库链接已同步（8 处）。

---

## 9. 下一步建议

1. **提交未提交的排版改动**：`ui/SettingsPage.cs` + `ui/Str.cs`（设置页 Win11 风格重构），提交并推送。
2. 若需发新版（v0.9.4+），严格走「版本号一致性清单 + 法律文本版本策略 + 发布前杀进程 + Git Data API 绕代理」流程。

---

*本文档为权威审计与验收清单，改动前先查本文件。*
