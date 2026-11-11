## v0.9.2

### 优化
- 导航重整为「概览」「使用」「专注」三页，设置移入导航栏底部。
- 「统计」并入「使用」作为「趋势」标签页，「专注历史」并入「专注」作为标签页，去掉重复页面与重复入口。
- 概览精简为今日数据，新增今日应用 Top 5，删除与趋势页重复的 30 天图表。
- 应用详情由弹窗改为主从布局：左列表、右详情，选中行以强调色高亮。
- 设置页重组为「常规」「数据」「关于」三组，诊断移入「关于」。
- 暂停记录入口上移到概览标题栏，与设置页共用同一开关。
- 界面刷新间隔由 1 秒改为 2 秒；Top 5、小时柱与数字仅在数值变化时重绘。

### 修复
- 修复应用详情中的使用占比以该应用自身总时长作为分母，导致显示 100% 的问题。
- 修复切换「使用」标签页后需要等待下一次轮询才显示数据的问题。
- 修复应用列表被右侧详情面板挤窄、应用名被硬截断的问题。
- 修复今日没有任何应用记录时 Top 5 不显示空状态的问题。
- 修复切换语言后应用详情仍显示旧语言的问题。
- 修复切换时间范围或日期后右侧仍显示旧范围数据的问题。
- 修复清空选择后迟到的查询错误覆盖当前状态的问题。
- 修复修改强调色重建导航后底部「设置」选中态丢失的问题。
- 修复趋势图与 Top 5 占比条随每次刷新闪烁的问题。

### 文档与法律
- 重写使用条款与隐私政策：补充定义、记录范围的技术说明、他人监控的禁止性约定、导出数据的敏感性提示、未加密存储的披露、责任限制上限、消费者法定权利保留、语言优先级与联系方式。
- 条款接受标记改为按版本区分（`TermsAccepted_v0_9_2`），升级后首次启动会重新展示条款与隐私政策。
- 同步更新 README、README.zh-CN、安装脚本、构建脚本与数据模型文档中的版本号。

## v0.9.1
### 修复
- 修复导入 JSON 数据时专注历史被追加而非替换的问题；同一份备份重复导入不再产生重复的专注记录。
- 修复导出 JSON 数据时专注历史被静默截断到 100 条的问题，导出现在包含完整专注历史。
- 新增覆盖导入替换语义与 0 / 1 / 99 / 100 / 101 / 150 条分页边界的 Rust 单元测试。

## v0.9.0
### Fixes and refinements
- Added a confirmation dialog before deleting an individual focus-history record from the full history page.
- Kept individual deletion available in the recent-focus list on the main Focus page.
- Tightened the navigation-pane resize hit area so resizing starts only when the pointer is placed directly on the pane boundary.


### 新增
- 新增独立的“专注”功能页面，支持开始、暂停、继续和结束专注。
- 新增 15、25、30、45、60、90、120 分钟专注预设，并支持自定义 1–1440 分钟专注时长。
- 新增由 Rust 后台监控核心维护的持久化专注倒计时与实时进度。
- 新增今日专注时长、完成次数和暂停次数统计。
- 新增专注历史记录，支持在独立页面中分页查看、单条删除及清理已完成 / 已结束记录。
- 新增专注数据本地持久化，并纳入 JSON 导出 / 导入。
- 新增独立的“专注历史”页面，集中管理全部专注记录并支持继续加载更多历史。
- 新增专注完成提示铃声，专注计时自然完成时播放 Windows 信息提示音。

### 优化
- 将专注计时交由 Rust 后台监控核心维护，切换页面或关闭主界面不会中断正在进行的专注。
- 专注主页面仅显示最近 10 条记录，完整历史迁移至独立“专注历史”页面，并采用每页 50 条的按需加载，避免记录过多时页面过长并保持界面响应；历史记录不会自动删除。
- 专注计时只累计实际活跃时间；Windows 锁定、长时间无操作或全局暂停使用时间记录时自动停止累计。
- 应用切换不会结束专注时段，专注计时与屏幕使用时间统计保持独立。
- 完善简体中文、繁體中文和 English 的专注界面、状态与时长设置本地化。
- 完善备份、导入与“删除全部数据”逻辑，专注记录会随本地记录一起处理。

### 修复
- 修复专注倒计时在页面切换或监控状态变化时可能出现状态不同步的问题。
- 修复自定义专注时长输入在极端数值下可能发生整数溢出的问题。
- 修复自定义时长选择后预览与实际启动时长不同步的问题。
- 修复删除专注历史后统计汇总可能与剩余记录不一致的问题。
- 修复从“查看全部”删除专注历史后，专注主页最近历史列表未及时同步的问题。
- 恢复专注主页最近 10 条历史记录的单条删除功能，并与全部历史删除行为保持一致。
- 修复应用内部使用条款与隐私政策在简体中文、繁體中文下仍显示英文正文的问题。

## v0.8.0

### 新增
- 新增按指定日期查看历史应用使用明细。
- 新增应用详情与使用时间占比。
- 新增更完善的常见应用名称识别。
- 新增今日按小时使用时间统计，可查看 24 小时使用分布及具体时段数据。
- 新增连续使用统计，区分当前连续使用时间与今日最长连续使用时间。
- 新增今日解锁次数统计。
- 新增数据存储位置展示，并支持直接打开本地数据目录。
- 新增高级功能区域，提供暂停记录、数据库诊断、数据备份等功能。
- 新增应用分类基础支持，为后续分类统计与分析提供基础。

### 优化
- 提升使用时间统计准确性与 Windows 锁定 / 会话状态检测可靠性。
- 优化实时快照与历史统计数据的读写性能。
- 增强数据导入安全性，导入前自动创建本地备份。
- 优化统计数据模型，使每日、应用、连续使用、按小时及历史统计采用统一的数据来源。
- 优化连续使用统计逻辑，应用切换不再被错误视为连续使用中断。
- 优化“全部时间”应用使用数据加载与刷新机制，避免实时刷新导致历史数据消失。
- 优化按小时统计图表的数据展示，鼠标悬浮时显示对应时段的使用时间与占比。
- 优化统计图表的数据口径，避免按小时数据与今日总时间使用不同统计基准。
- 完善简体中文、繁體中文和 English 的新增功能文本及统计提示。
- 优化设置页面布局与功能分区，完善常规、数据与隐私、高级及相关设置内容。
- 优化应用图标及数据加载缓存，减少页面刷新时的闪烁与重复加载。
- 优化后台记录状态显示，使当前记录、暂停状态等运行状态更加直观。

### 修复
- 修复“全部时间”应用使用列表在后台刷新后无法正常显示的问题。
- 修复“今日按小时”统计图表悬浮提示无法正确显示使用数据的问题。
- 修复部分统计场景下小时使用占比计算错误的问题。
- 修复新增统计数据与历史累计数据之间统计口径不一致的问题。
- 修复部分新增功能在繁體中文界面下未完成本地化的问题。
- 修复设置页面部分高级功能及数据存储信息缺失的问题。

## v0.7.4

- 修复按钮长按时错误显示主题色的问题。

## v0.7.3

### Improved
- Restored the compact navigation bar resize interaction for more precise and predictable width adjustment.

## v0.7.2

- 修复深色模式下数据管理对话框未正确跟随应用主题的问题。

## v0.7.1

### 新增
- 新增系统托盘双击打开功能，并优化托盘菜单显示。
- 新增“跟随系统”语言选项及繁体中文支持。
- 新增删除全部数据的多级确认机制。

### 优化
- 优化导航栏宽度调整交互，仅在鼠标悬浮时显示调整提示。
- 优化删除数据确认界面，危险操作按钮统一使用红色高亮。
- 完善安装程序多语言支持。

- Reworked focus duration presets and custom duration handling.
- Added paged focus history with individual deletion and safe history cleanup.
- Improved focus duration selector alignment and responsive behavior.
- Focus history now shows the latest 10 records on the main page; the full history is available on a dedicated page with 50-record incremental loading.
- Added localized validation and live preview for custom focus durations.

### Changed
- Changed the full Focus History view to a secondary page opened from the Focus page, with history management kept on that page.
- Reworked the in-app legal notices to use the complete English Terms of Use and Privacy Policy shipped with the application. The surrounding dialog controls remain localized without duplicating full legal texts for every language.
- Simplified the empty recent-focus state so it no longer displays duplicate “no history” messages.
