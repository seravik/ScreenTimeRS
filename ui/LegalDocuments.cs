using System;
using System.IO;
using System.Text;

namespace ScreenTimeRS.UI;

/// <summary>
/// Provides the legal documents shown inside the application.
/// The shipped Markdown files are English-only; localized versions are embedded
/// here so the installed application can present the same documents in the
/// user's selected UI language without shipping duplicate legal files.
/// </summary>
internal static class LegalDocuments
{
    private static string ReadDocument(string fileName, string fallback)
    {
        try
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Environment.CurrentDirectory, fileName)
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return File.ReadAllText(path, Encoding.UTF8);
            }
        }
        catch
        {
            // Use the embedded English fallback if the shipped file cannot be read.
        }

        return fallback;
    }

    private const string TermsFallback = """
# ScreenTime RS Terms of Use

Version: v0.9.2
Last updated: November 11, 2026

## 1. Definitions
"Software" means the ScreenTime RS application as distributed by the Project, including the Rust monitoring core, the WinUI front end, the installer, the bundled documentation, and any scripts shipped with them. "Data" means usage records, focus records, settings, snapshots, backups, diagnostics output, and exported files produced by or for the Software. "Project" means the maintainers and contributors who distribute ScreenTime RS. "You" means the individual who installs, launches, or uses the Software.

## 2. Acceptance
By installing, launching, or using the Software, you confirm that you have had the opportunity to read these Terms of Use and the Privacy Policy, and that you accept them. Where the Software presents an acceptance dialog, the Software stores a local acceptance flag and keeps the main interface unavailable until that flag is set. If you do not accept these Terms, do not install or use the Software, and uninstall it if it is already installed.

## 3. Eligibility and Authority
You must be legally able to enter into these Terms. If you install the Software on a device that you do not own or fully control, such as an employer, school, library, or family device, you must have the permission of the device owner or administrator and must comply with that organization's policies. You are responsible for determining whether recording activity on that device is permitted.

## 4. Licence to Use
Subject to the project licence and these Terms, you may install and use the Software on devices you control, for personal or internal purposes. These Terms grant no rights in the Software beyond those granted by the project licence. If the project licence conflicts with these Terms regarding copying, modification, or redistribution of source code, the project licence prevails for those activities.

## 5. Intended Use and Monitoring of Other People
The Software is intended to help a user record and review their own activity on their own device. You must not use the Software to monitor another person covertly, or without a lawful basis and any notice or consent required by applicable law, contract, or policy. Where the Software is deployed by an organization, the deployer is responsible for providing required notices, obtaining required consents, and complying with labour, privacy, and data-protection law. The Project does not provide, endorse, or authorize covert surveillance of any person.

## 6. What the Software Records
The monitoring core samples the foreground window at an interval of approximately one second, identifies the process that owns that window, and stores the process name and, when it can be resolved, the executable path, together with accumulated active seconds and timestamps. The current window title is read in memory while resolving the foreground application and is not written to the database. Accumulation pauses while the workstation is locked, while there has been no user input for approximately 60 seconds, while recording is paused, and while the device is asleep or shut down. The Software does not capture screen contents, keystrokes, clipboard contents, audio, camera input, browsing history, document contents, or message contents, and it does not transmit data over a network.

## 7. Focus Feature
The Focus feature is a local timer and productivity aid. It is not a security control, parental-control system, access-control system, application blocker, or disciplinary tool. Starting a focus session does not prevent you or anyone else from opening or switching applications. Focus time is counted only while the conditions in Section 6 are met, so a focus session may record less elapsed time than its wall-clock duration.

## 8. Accuracy and Suitability
Statistics are derived from periodic sampling and from the state reports available on the device. Sleep, hibernation, fast startup, session switching, permission changes, driver behaviour, application behaviour, and Windows updates may cause activity to be omitted, attributed to the wrong application, or counted at boundaries that differ from the actual transition. The Software is therefore not suitable as an audit record, billing record, compliance record, or evidence in a dispute. Verify independently before relying on it for any decision that requires precise records.

## 9. No Professional Advice
The Software provides no medical, legal, financial, employment, educational, or other professional advice. Health, work, study, and time-management decisions that you make using the Software remain your own responsibility.

## 10. Exported, Imported, and Backup Data
Exported JSON files and backups may contain application names, executable file paths, timestamps, durations, and focus history. Executable paths can reveal which software is installed and may include your Windows user name. Treat exported files as sensitive, store them with appropriate access controls, and delete them when they are no longer needed. You are responsible for verifying the origin and integrity of any file before importing it. Importing may replace supported categories of existing data according to the confirmation shown in the application, and the Software may create a local backup first; that backup is provided as a convenience and is not a guarantee against data loss.

## 11. Destructive Actions
Clearing focus history, deleting individual records, deleting all data, and uninstalling may permanently remove local records. Confirmation dialogs and typed confirmation codes are the last safeguard before an irreversible action. Create your own copy of any record that matters before proceeding.

## 12. Third-Party Components and Platform Dependencies
The Software depends on Windows system components, the Windows App SDK, SQLite, and third-party Rust and .NET libraries. Those components are governed by their own licences and terms. Changes to Windows, to those components, or to the device configuration may change or break Software behaviour. The Project is not responsible for the independent acts, licensing, security, or availability of third parties.

## 13. Intellectual Property
The ScreenTime RS name, project materials, source code, documentation, artwork, and other assets remain subject to the project licence and to any third-party notices shipped with the Software. Using or modifying the Software grants no right to use project names, logos, or branding, and no right to imply endorsement or official status.

## 14. Modification and Redistribution
You may modify, fork, and redistribute the Software only as the project licence permits. Redistributors must preserve required attribution, licence texts, and notices, and must clearly mark modified builds as modified. A modified build must not be presented as an official release, and must not state or imply that the Project maintains or endorses it.

## 15. Disclaimer of Warranty
To the maximum extent permitted by applicable law, the Software is provided "as is" and "as available", without warranty of any kind, whether express, implied, statutory, or otherwise, including any warranty of merchantability, fitness for a particular purpose, title, non-infringement, accuracy, reliability, freedom from defects, or uninterrupted availability. Some jurisdictions do not allow the exclusion of certain warranties, so parts of this section may not apply to you; in that case the exclusions apply to the fullest extent that jurisdiction permits.

## 16. Limitation of Liability
To the maximum extent permitted by applicable law, the Project and its contributors are not liable for indirect, incidental, special, consequential, exemplary, or punitive damages, or for loss of data, records, profits, revenue, goodwill, or productivity, arising from the use of, inability to use, or reliance on the Software, even if advised of the possibility of such loss. Where liability cannot lawfully be excluded, the total aggregate liability arising from the Software is limited to the amount you paid for the Software, if any, in the twelve months preceding the event that gave rise to the claim.

## 17. Security and Device Administration
You are responsible for the security of your Windows account, device, backups, and exported files. Local records are stored in an unencrypted SQLite database protected by Windows account and file-system permissions, not by application-level encryption. Malware, administrative access, a compromised account, an unsecured backup location, disk theft, or another user of the same device may expose or alter those records.

## 18. Updates, Changes, and Re-acceptance
The Software and these Terms may change between versions. Material changes to these Terms or to the Privacy Policy are reflected in a new document version and a new last-updated date. Where the change is material, the acceptance flag used by the Software is version-specific, so you may be asked to review and accept the updated documents before continuing. Continued use after an update means you accept the documents shipped with that version, to the extent permitted by applicable law.

## 19. Termination
You may stop using the Software at any time. Uninstalling removes the application files, but does not necessarily remove locally stored usage and focus data, automatic backups, or files you exported, unless you delete those separately. Sections that by their nature should survive termination, including Sections 8, 10, 13, 15, 16, and 17, survive it.

## 20. Consumer Rights and Mandatory Law
Nothing in these Terms excludes or limits any right that cannot lawfully be excluded or limited, including mandatory consumer-protection rights in your place of residence. If you are a consumer, statutory rights apply in addition to these Terms and prevail over any conflicting provision.

## 21. Severability and Entire Agreement
If any provision of these Terms is held unenforceable or invalid, it is limited or removed to the minimum extent necessary, and the remaining provisions continue in effect. These Terms, together with the Privacy Policy and the project licence, are the entire agreement between you and the Project regarding the Software, and replace prior statements about it on the same subjects.

## 22. Language
The English version of these Terms is the reference text. Simplified Chinese and Traditional Chinese versions are provided inside the application for convenience. If a translation conflicts with the English version, the English version controls, except where mandatory local law requires otherwise.

## 23. Contact
The Project is maintained at https://github.com/wuchen128/ScreenTimeRS, where issues and enquiries can be raised. The Project does not guarantee a response time or a remedy for any particular report.
""";

    private const string PrivacyFallback = """
# ScreenTime RS Privacy Policy

Version: v0.9.2
Last updated: November 11, 2026

## 1. Scope and Controller
ScreenTime RS is a local Windows application that records and presents screen-time, application-usage, and focus-session statistics. This Privacy Policy explains what the Software stores, why it stores it, where it is stored, how long it is kept, and what controls you have. All records created by the Software stay on the device where they were recorded. The Project does not receive, host, or have access to them, so the person who controls that Windows account is effectively the controller of the local records.

## 2. Summary
- No account, sign-in, subscription, or licence key is required.
- No usage, focus, or diagnostic data is sent to the Project or to any third party.
- No analytics, telemetry, advertising, crash reporting, or update-check traffic is generated.
- The Software works entirely offline; it contains no networking code.
- All records are stored locally and can be inspected, exported, or deleted by you.

## 3. Information Stored
To provide its features, the Software stores the following on the local device:
- Application names, and the executable file path of the foreground application when it can be resolved.
- Recorded active seconds and timestamps used for daily, hourly, historical, and per-application statistics.
- Session identifiers, session start and end times, and continuous-use measurements.
- Focus-session start times, planned durations, actual accumulated focus time, completion state, and pause counts.
- Local runtime state required for background monitoring, refresh, and recovery, including pause and refresh flags.
- Snapshots and aggregates written for the interface, such as the current snapshot and analytics files.
- Import backups created before a supported import operation.
- Application preferences such as language, theme mode, accent colour, navigation state, and the legal-document acceptance flag, stored in the Windows registry under the current user's settings key.
- A local diagnostic log written only when the interface encounters an unhandled error.

## 4. Information Not Collected
The Software does not intentionally collect, record, or transmit:
- Screen contents, screenshots, or recordings.
- Keystrokes, clipboard contents, or input history.
- Audio, camera, or other sensor data.
- Window titles. The foreground window title is read in memory while identifying the owning process and is not written to storage.
- Browsing history, page contents, URLs, or network traffic.
- Document contents, message contents, contact lists, or calendar data.
- Location data, advertising identifiers, hardware fingerprints, or account credentials.

## 5. How Recording Works
The monitoring core samples the foreground window approximately once per second, resolves the owning process, and accumulates active time for that application. Accumulation stops while the workstation is locked, while there has been no user input for approximately 60 seconds, while recording is paused, and while the device is asleep or shut down. Sampling gaps are capped so that a suspended session cannot be recorded as a long usage block. Focus time uses the same activity conditions but is kept as a separate metric.

## 6. Purposes
Stored information is used only to operate the Software and to provide its features: daily summaries, per-application totals and rankings, hourly and multi-period charts, continuous-use and unlock measurements, focus timers and focus history, import and export, local backups, and diagnostics.

## 7. Storage Location and Format
By default, data is stored under the current Windows user's local application-data directory, in the ScreenTime RS data folder. The data folder lives under your Windows profile. Records are kept in a local SQLite database, with JSON files used for snapshots, exports, analytics, and backups. The database is not encrypted by the application; see Section 10.

## 8. Retention
Usage and focus records remain on the device until you delete them or remove the underlying files. The Software does not silently delete historical records because they become numerous or old. Interface limits, such as showing only the most recent focus records on the main Focus page, are presentation limits and do not delete data.

## 9. Deletion
The Software provides controls to delete individual focus records, clear supported history, and delete all data. The Delete All Data action is destructive and permanently removes recorded usage and focus records from the local database after confirmation. It does not necessarily remove settings, snapshots, existing backups, files you exported, or copies already captured by file-history, backup, or sync tools; those must be removed separately. Deleting a file does not guarantee that every physical copy is erased from the storage device.

## 10. Security
Records are protected by Windows account boundaries and file-system permissions rather than by application-level encryption. Anyone who can sign in as your Windows user, obtain administrator access, read the disk directly, or open an exported file can read those records. Reasonable care is taken to keep data local and to avoid unnecessary duplication, but no local application can guarantee confidentiality against malware, an administrator, a compromised account, or physical access.

## 11. Backups, Export, and Import
When you export data, the Software writes a JSON file to the location you choose. Exported files include application names, executable paths, timestamps, durations, and focus history, and are not encrypted by the Software. Keep them in a location with appropriate access controls and delete them when they are no longer needed. Before a supported import, the Software may create a local backup so that the previous state can be restored. Imported data is applied according to the confirmation shown in the application; verify the origin and integrity of a file before importing it.

## 12. Third-Party Access on the Device
Windows, security software, backup and sync tools, device administrators, and other users of the same device may have their own permissions and may access files stored by the Software. Such access is controlled by the operating system or by those third parties and is not initiated by the Software.

## 13. Children and Protected Users
The Software is a general-purpose local utility and is not directed at children. It is not designed to collect information about children or other protected groups. Parents, guardians, organizations, and device administrators are responsible for ensuring that any monitoring they carry out is lawful in their jurisdiction and is accompanied by the notices and consents that applicable law requires.

## 14. Your Controls
You can export records, import records, delete individual records, clear history, delete all data, and uninstall the Software. You can also delete the data folder directly with Windows. Uninstalling removes the application but not necessarily the data folder or exported files.

## 15. International Users
Because the Software does not transmit records off the device, no international transfer of records is carried out by the Software. If you export records and move them to another service or jurisdiction, you are responsible for that transfer and for complying with the rules that apply to it.

## 16. Changes to This Policy
This Privacy Policy is versioned. Material changes to what is stored, how it is stored, or how it is used are reflected in a new version number and last-updated date, and are accompanied by an updated acceptance step in the Software where required. The version shipped with your installed release describes that release.

## 17. Contact
ScreenTime RS is maintained at https://github.com/wuchen128/ScreenTimeRS, where questions and reports about data handling can be raised.
""";

    private const string TermsZhCn = """
# ScreenTime RS 使用条款

版本：v0.9.2
最后更新：2026年11月11日

## 1. 定义
“本软件”指由本项目分发的 ScreenTime RS 应用程序，包括 Rust 监控核心、WinUI 前端、安装程序、随附文档以及随上述内容一同提供的脚本。“数据”指本软件产生或为本软件产生的使用记录、专注记录、设置、快照、备份、诊断输出和导出文件。“本项目”指分发 ScreenTime RS 的维护者与贡献者。“您”指安装、启动或使用本软件的个体。

## 2. 接受条款
安装、启动或使用本软件，即表示您确认已获得阅读本使用条款与隐私政策的机会，并接受二者。若本软件显示接受对话框，本软件会保存一个本地接受标记，并在该标记写入前保持主界面不可用。若您不接受本条款，请勿安装或使用本软件；如已安装，请将其卸载。

## 3. 资格与权限
您必须具有订立本条款的法律能力。若您在非本人所有或不能完全控制的设备上安装本软件，例如单位、学校、公共图书馆或家庭设备，您必须取得设备所有者或管理员的许可，并遵守该组织的政策。您有责任判断在该设备上记录活动是否被允许。

## 4. 使用许可
在项目许可证与本条款的约束下，您可以在自己控制的设备上安装并使用本软件，用于个人或内部目的。本条款不授予项目许可证之外的任何权利。若项目许可证与本条款在源代码的复制、修改或再分发方面存在冲突，就该等活动以项目许可证为准。

## 5. 预期用途与他人监控
本软件用于帮助用户记录和查看其本人在自有设备上的活动。您不得在未告知他人或缺乏合法依据的情况下秘密监控他人，也不得规避适用法律、合同或政策所要求的告知与同意义务。若本软件由组织部署，部署方负责提供必要告知、取得必要同意，并遵守劳动、隐私与数据保护法律。本项目不提供、不认可、也不授权对任何人进行秘密监控。

## 6. 本软件记录的内容
监控核心以约一秒的间隔采样前台窗口，识别拥有该窗口的进程，并保存进程名称以及在可解析时的可执行文件路径，同时保存累计的活跃秒数与时间戳。当前窗口标题仅在识别前台应用的过程中于内存中读取，不会写入数据库。当工作站被锁定、约 60 秒内无用户输入、记录暂停，或设备休眠、关机时，累计会停止。本软件不截取屏幕内容、不记录键盘输入、不读取剪贴板、不采集音频或摄像头数据、不读取浏览历史、文档内容或消息内容，也不会通过网络传输数据。

## 7. 专注功能
专注功能是本地计时器与生产力辅助工具。它不是安全控制、家长控制、访问控制、应用屏蔽或纪律管理工具。开始专注不会阻止您或他人打开或切换应用。专注时间仅在满足第 6 条所述条件时计入，因此一次专注记录的时间可能少于其实际经过的时间。

## 8. 准确性与适用性
统计数据来源于周期性采样以及设备可提供的状态报告。睡眠、休眠、快速启动、会话切换、权限变更、驱动行为、应用行为以及 Windows 更新，都可能导致活动被漏记、归属到错误的应用，或在与实际切换时刻不同的边界上被计入。因此，本软件不适合作为审计记录、计费记录、合规记录或争议中的证据。在依赖其作出需要精确记录的决策前，请自行核实。

## 9. 不构成专业建议
本软件不提供医疗、法律、金融、就业、教育或其他专业建议。您使用本软件作出的健康、工作、学习与时间管理决定，由您自行负责。

## 10. 导出、导入与备份数据
导出的 JSON 文件与备份可能包含应用名称、可执行文件路径、时间戳、时长以及专注历史。可执行文件路径可能暴露已安装的软件，也可能包含您的 Windows 用户名。请将导出文件视为敏感信息，采取适当的访问控制，并在不再需要时删除。您有责任在导入前核实文件来源与完整性。导入可能按应用内确认说明替换受支持类别的现有数据，本软件也可能先创建本地备份；该备份仅为便利而提供，并不构成数据不会丢失的保证。

## 11. 破坏性操作
清理专注历史、删除单条记录、删除全部数据以及卸载，都可能永久移除本地记录。确认对话框与需要手动输入的确认码是不可逆操作前的最后一道保护。操作前请自行复制任何重要的记录。

## 12. 第三方组件与平台依赖
本软件依赖 Windows 系统组件、Windows App SDK、SQLite 以及第三方 Rust 与 .NET 库。这些组件受其各自的许可证与条款约束。Windows、相关组件或设备配置的变更，可能改变或破坏本软件的行为。本项目不对第三方的独立行为、许可、安全性或可用性负责。

## 13. 知识产权
ScreenTime RS 名称、项目材料、源代码、文档、美术资源及其他资产，均受项目许可证以及随本软件提供的第三方声明约束。使用或修改本软件，并不授予使用项目名称、标识或品牌的任何权利，也不授予暗示获得认可或官方地位的权利。

## 14. 修改与再分发
只有在项目许可证允许的范围内，您才可以修改、复刻和再分发本软件。再分发者必须保留所要求的署名、许可证文本与声明，并须清楚标明修改版本已被修改。修改版本不得被表述为官方发行版，也不得声明或暗示本项目维护或认可该版本。

## 15. 免责声明
在适用法律允许的最大范围内，本软件按“现状”与“可用”状态提供，不附带任何形式的明示、默示、法定或其他保证，包括对适销性、特定用途适用性、所有权、不侵权、准确性、可靠性、无缺陷或持续可用性的保证。部分司法管辖区不允许排除某些保证，因此本条部分内容可能不适用于您；在此情况下，排除条款在该司法管辖区允许的最大范围内适用。

## 16. 责任限制
在适用法律允许的最大范围内，本项目及其贡献者不对因使用、无法使用或依赖本软件而产生的间接、附带、特殊、后果性、示范性或惩罚性损害，以及数据、记录、利润、收入、商誉或生产力损失承担责任，即使已被告知此类损失的可能性。在责任无法依法排除的情形下，与本软件相关的责任总额以您在引起索赔的事件发生前十二个月内为本软件实际支付的金额为限（如未支付，则为零）。

## 17. 安全与设备管理
您负责 Windows 账户、设备、备份及导出文件的安全。本地记录保存在未加密的 SQLite 数据库中，其保护依赖 Windows 账户与文件系统权限，而非应用层加密。恶意软件、管理员访问、账户被入侵、备份位置不安全、磁盘失窃或同一设备上的其他用户，都可能泄露或改动这些记录。

## 18. 更新、变更与重新接受
本软件与本条款可能随版本变化。对本条款或隐私政策的实质性变更，会体现为新的文档版本号与新的最后更新日期。若变更具有实质性，本软件使用的接受标记带有版本区分，因此您可能需要在继续前重新阅读并接受更新后的文档。在适用法律允许的范围内，更新后继续使用即表示您接受该版本随附的文档。

## 19. 终止
您可以随时停止使用本软件。卸载会移除应用文件，但除非您另行删除，否则并不一定会移除本地保存的使用与专注数据、自动备份或您导出的文件。依其性质应当继续有效的条款，包括第 8、10、13、15、16 及 17 条，在终止后继续有效。

## 20. 消费者权利与强制性法律
本条款不排除或限制任何依法不得排除或限制的权利，包括您居住地的强制性消费者保护权利。若您属于消费者，法定权利与本条款并存适用，并优于任何冲突的约定。

## 21. 可分割性与完整协议
若本条款的任何规定被认定为不可执行或无效，该规定应在必要的最小范围内被限制或删除，其余规定继续有效。本条款连同隐私政策与项目许可证，构成您与本项目就本软件达成的完整协议，并取代此前就同一事项所作的陈述。

## 22. 语言
本条款的英文版本为基准文本。简体中文与繁体中文版本在应用内提供，仅为方便阅读。若译文与英文版本存在冲突，以英文版本为准，但强制性当地法律另有要求的除外。

## 23. 联系
本项目维护于 https://github.com/wuchen128/ScreenTimeRS，可在该处提出问题或反馈。本项目不保证响应时间，也不保证对任何具体反馈提供救济。
""";

    private const string TermsZhTw = """
# ScreenTime RS 使用條款

版本：v0.9.2
最後更新：2026年11月11日

## 1. 定義
「本軟體」指由本專案分發的 ScreenTime RS 應用程式，包括 Rust 監控核心、WinUI 前端、安裝程式、隨附文件以及隨上述內容一同提供的指令碼。「資料」指本軟體產生或為本軟體產生的使用記錄、專注記錄、設定、快照、備份、診斷輸出與匯出檔案。「本專案」指分發 ScreenTime RS 的維護者與貢獻者。「您」指安裝、啟動或使用本軟體的個人。

## 2. 接受條款
安裝、啟動或使用本軟體，即表示您確認已獲得閱讀本使用條款與隱私權政策的機會，並接受二者。若本軟體顯示接受對話方塊，本軟體會儲存一個本機接受標記，並在該標記寫入前保持主介面不可用。若您不接受本條款，請勿安裝或使用本軟體；如已安裝，請將其解除安裝。

## 3. 資格與權限
您必須具備訂立本條款的法律能力。若您在非本人所有或不能完全控制的裝置上安裝本軟體，例如公司、學校、公共圖書館或家庭裝置，您必須取得裝置所有者或管理員的許可，並遵守該組織的政策。您有責任判斷在該裝置上記錄活動是否被允許。

## 4. 使用授權
在專案授權與本條款的約束下，您可以在自己控制的裝置上安裝並使用本軟體，用於個人或內部目的。本條款不授予專案授權之外的任何權利。若專案授權與本條款在原始碼的複製、修改或再散布方面存在衝突，就該等活動以專案授權為準。

## 5. 預期用途與他人監控
本軟體用於協助使用者記錄及檢視其本人在自有裝置上的活動。您不得在未告知他人或缺乏合法依據的情況下秘密監控他人，也不得規避適用法律、契約或政策所要求的告知與同意義務。若本軟體由組織部署，部署方負責提供必要告知、取得必要同意，並遵守勞動、隱私與資料保護法律。本專案不提供、不認可、亦不授權對任何人進行秘密監控。

## 6. 本軟體記錄的內容
監控核心以約一秒的間隔取樣前景視窗，識別擁有該視窗的處理程序，並儲存處理程序名稱以及在可解析時的可執行檔路徑，同時儲存累計的活躍秒數與時間戳記。目前視窗標題僅在識別前景應用程式的過程中於記憶體中讀取，不會寫入資料庫。當工作站被鎖定、約 60 秒內無使用者輸入、記錄暫停，或裝置休眠、關機時，累計會停止。本軟體不擷取螢幕內容、不記錄鍵盤輸入、不讀取剪貼簿、不蒐集音訊或攝影機資料、不讀取瀏覽歷史、文件內容或訊息內容，也不會透過網路傳輸資料。

## 7. 專注功能
專注功能是本機計時器與生產力輔助工具。它不是安全控制、家長控制、存取控制、應用程式封鎖或紀律管理工具。開始專注不會阻止您或他人開啟或切換應用程式。專注時間僅在符合第 6 條所述條件時計入，因此一次專注記錄的時間可能少於其實際經過的時間。

## 8. 準確性與適用性
統計資料來自週期性取樣以及裝置可提供的狀態回報。睡眠、休眠、快速啟動、工作階段切換、權限變更、驅動程式行為、應用程式行為以及 Windows 更新，都可能導致活動被漏記、歸屬到錯誤的應用程式，或在與實際切換時刻不同的邊界上被計入。因此，本軟體不適合作為稽核記錄、計費記錄、合規記錄或爭議中的證據。在依賴其作出需要精確記錄的決策前，請自行核實。

## 9. 不構成專業建議
本軟體不提供醫療、法律、金融、就業、教育或其他專業建議。您使用本軟體作出的健康、工作、學習與時間管理決定，由您自行負責。

## 10. 匯出、匯入與備份資料
匯出的 JSON 檔案與備份可能包含應用程式名稱、可執行檔路徑、時間戳記、時長以及專注歷史。可執行檔路徑可能暴露已安裝的軟體，也可能包含您的 Windows 使用者名稱。請將匯出檔案視為敏感資訊，採取適當的存取控制，並在不再需要時刪除。您有責任在匯入前核實檔案來源與完整性。匯入可能依應用程式內確認說明取代受支援類別的現有資料，本軟體也可能先建立本機備份；該備份僅為便利而提供，並不構成資料不會遺失的保證。

## 11. 破壞性操作
清理專注歷史、刪除單筆記錄、刪除全部資料以及解除安裝，都可能永久移除本機記錄。確認對話方塊與需要手動輸入的確認碼是不可逆操作前的最後一道保護。操作前請自行複製任何重要的記錄。

## 12. 第三方元件與平台相依性
本軟體相依於 Windows 系統元件、Windows App SDK、SQLite 以及第三方 Rust 與 .NET 程式庫。這些元件受其各自的授權與條款約束。Windows、相關元件或裝置設定的變更，可能改變或破壞本軟體的行為。本專案不對第三方的獨立行為、授權、安全性或可用性負責。

## 13. 智慧財產權
ScreenTime RS 名稱、專案素材、原始碼、文件、美術資源及其他資產，均受專案授權以及隨本軟體提供的第三方聲明約束。使用或修改本軟體，並不授予使用專案名稱、標誌或品牌的任何權利，也不授予暗示獲得認可或官方地位的權利。

## 14. 修改與再散布
只有在專案授權允許的範圍內，您才可以修改、複製和再散布本軟體。再散布者必須保留所要求的署名、授權文本與聲明，並須清楚標明修改版本已被修改。修改版本不得被表述為官方發行版，也不得聲明或暗示本專案維護或認可該版本。

## 15. 免責聲明
在適用法律允許的最大範圍內，本軟體依「現狀」與「可用」狀態提供，不附帶任何形式的明示、默示、法定或其他保證，包括對適售性、特定用途適用性、所有權、不侵權、準確性、可靠性、無瑕疵或持續可用性的保證。部分司法管轄區不允許排除某些保證，因此本條部分內容可能不適用於您；在此情況下，排除條款在該司法管轄區允許的最大範圍內適用。

## 16. 責任限制
在適用法律允許的最大範圍內，本專案及其貢獻者不對因使用、無法使用或依賴本軟體而產生的間接、附帶、特殊、後果性、示範性或懲罰性損害，以及資料、記錄、利潤、收入、商譽或生產力損失承擔責任，即使已被告知此類損失的可能性。在責任無法依法排除的情形下，與本軟體相關的責任總額以您在引起索賠的事件發生前十二個月內為本軟體實際支付的金額為限（如未支付，則為零）。

## 17. 安全性與裝置管理
您負責 Windows 帳戶、裝置、備份及匯出檔案的安全。本機記錄儲存於未加密的 SQLite 資料庫中，其保護依賴 Windows 帳戶與檔案系統權限，而非應用程式層級加密。惡意軟體、管理員存取、帳戶遭入侵、備份位置不安全、磁碟遭竊或同一裝置上的其他使用者，都可能洩露或竄改這些記錄。

## 18. 更新、變更與重新接受
本軟體與本條款可能隨版本變化。對本條款或隱私權政策的實質性變更，會體現為新的文件版本號與新的最後更新日期。若變更具有實質性，本軟體使用的接受標記帶有版本區分，因此您可能需要在繼續前重新閱讀並接受更新後的文件。在適用法律允許的範圍內，更新後繼續使用即表示您接受該版本隨附的文件。

## 19. 終止
您可以隨時停止使用本軟體。解除安裝會移除應用程式檔案，但除非您另行刪除，否則不一定會移除本機儲存的使用與專注資料、自動備份或您匯出的檔案。依其性質應繼續有效的條款，包括第 8、10、13、15、16 及 17 條，於終止後繼續有效。

## 20. 消費者權利與強制性法律
本條款不排除或限制任何依法不得排除或限制的權利，包括您居住地的強制性消費者保護權利。若您屬於消費者，法定權利與本條款併存適用，並優先於任何衝突的約定。

## 21. 可分割性與完整協議
若本條款的任何規定被認定為不可執行或無效，該規定應在必要的最小範圍內被限制或刪除，其餘規定繼續有效。本條款連同隱私權政策與專案授權，構成您與本專案就本軟體達成的完整協議，並取代先前就同一事項所作的陳述。

## 22. 語言
本條款的英文版本為基準文本。簡體中文與繁體中文版本在應用程式內提供，僅為方便閱讀。若譯文與英文版本存在衝突，以英文版本為準，但強制性當地法律另有要求者除外。

## 23. 聯絡
本專案維護於 https://github.com/wuchen128/ScreenTimeRS，可在該處提出問題或回饋。本專案不保證回應時間，也不保證對任何具體回饋提供救濟。
""";

    private const string PrivacyZhCn = """
# ScreenTime RS 隐私政策

版本：v0.9.2
最后更新：2026年11月11日

## 1. 适用范围与控制者
ScreenTime RS 是一款本地 Windows 应用，用于记录并展示屏幕使用时间、应用使用情况与专注时段统计。本隐私政策说明本软件存储哪些信息、为何存储、存储在何处、保留多久，以及您拥有哪些控制手段。本软件创建的所有记录都留在记录产生的设备上。本项目不接收、不托管、也无权访问这些记录，因此控制该 Windows 账户的人实际上是本地记录的控制者。

## 2. 概要
- 不需要账户、登录、订阅或授权码。
- 不会向本项目或任何第三方发送使用数据、专注数据或诊断数据。
- 不会产生分析统计、遥测、广告、崩溃上报或更新检查流量。
- 本软件完全离线工作，不包含任何网络代码。
- 所有记录都保存在本地，您可以查看、导出或删除。

## 3. 存储的信息
为提供相关功能，本软件会在本地设备上存储以下内容：
- 应用名称，以及在可解析时前台应用的可执行文件路径。
- 用于每日、按小时、历史与按应用统计的活跃秒数与时间戳。
- 会话标识、会话开始与结束时间，以及连续使用测量值。
- 专注时段的开始时间、计划时长、实际累计专注时间、完成状态与暂停次数。
- 后台监控、刷新与恢复所需的本地运行状态，包括暂停与刷新标记文件。
- 为界面生成的快照与聚合数据，例如当前快照与分析数据文件。
- 在受支持的导入操作前创建的导入备份。
- 语言、主题模式、强调色、导航状态以及法律文档接受标记等应用偏好，保存在当前用户设置键下的 Windows 注册表中。
- 仅在界面遇到未处理错误时写入的本地诊断日志。

## 4. 不收集的信息
本软件不会有意收集、记录或传输：
- 屏幕内容、截图或录屏。
- 键盘输入、剪贴板内容或输入历史。
- 音频、摄像头或其他传感器数据。
- 窗口标题。前台窗口标题仅在识别所属进程时于内存中读取，不会写入存储。
- 浏览历史、页面内容、网址或网络流量。
- 文档内容、消息内容、联系人列表或日历数据。
- 位置数据、广告标识符、硬件指纹或账户凭据。

## 5. 记录方式
监控核心约每秒采样一次前台窗口，识别所属进程，并为该应用累计活跃时间。当工作站被锁定、约 60 秒内无用户输入、记录暂停，或设备休眠、关机时，累计会停止。采样间隔造成的缺口会被限制，因此挂起的会话不会被记录为一段很长的使用时间。专注时间使用相同的活动条件，但作为独立指标保存。

## 6. 处理目的
存储的信息仅用于运行本软件并提供其功能：每日摘要、按应用汇总与排行、按小时与多周期图表、连续使用与解锁统计、专注计时器与专注历史、导入与导出、本地备份以及诊断。

## 7. 存储位置与格式
默认情况下，数据保存在当前 Windows 用户本地应用数据目录下的 ScreenTime RS 数据文件夹中。数据文件夹位于您的 Windows 用户配置目录下。记录保存在本地 SQLite 数据库中，快照、导出、分析与备份则使用 JSON 文件。数据库未由应用加密，详见第 10 条。

## 8. 保留期限
使用记录与专注记录会一直保留在设备上，直到您删除它们或移除底层文件。本软件不会因为历史记录数量变多或时间变旧而静默删除记录。界面限制，例如主专注页面只显示最近的专注记录，仅属于展示限制，不会删除数据。

## 9. 删除
本软件提供删除单条专注记录、清理受支持的历史以及删除全部数据的控制。删除全部数据属于破坏性操作，确认后会从本地数据库中永久移除记录的使用与专注数据。它不一定移除设置、快照、已有备份、您导出的文件，或已被文件历史、备份、同步工具捕获的副本；这些需要另行删除。删除文件并不保证存储设备上的每一份物理副本都被抹除。

## 10. 安全
记录的保护依赖 Windows 账户边界与文件系统权限，而非应用层加密。任何能够以您的 Windows 用户身份登录、取得管理员权限、直接读取磁盘或打开导出文件的人，都可以读取这些记录。本软件会合理地将数据保留在本地并避免不必要的复制，但任何本地应用都无法保证在恶意软件、管理员、账户被入侵或物理接触面前仍然保密。

## 11. 备份、导出与导入
当您导出数据时，本软件会将 JSON 文件写入您选择的位置。导出文件包含应用名称、可执行文件路径、时间戳、时长与专注历史，且未由本软件加密。请将其存放在具有适当访问控制的位置，并在不再需要时删除。在受支持的导入之前，本软件可能创建本地备份，以便恢复之前的状态。导入数据会按照应用内显示的确认说明应用；导入前请核实文件来源与完整性。

## 12. 设备上的第三方访问
Windows、安全软件、备份与同步工具、设备管理员以及同一设备上的其他用户，可能拥有各自的权限并访问本软件存储的文件。此类访问由操作系统或上述第三方控制，并非由本软件发起。

## 13. 儿童与受保护用户
本软件是通用型本地工具，并非面向儿童。它并非为收集儿童或其他受保护群体的信息而设计。父母、监护人、组织与设备管理员负责确保其进行的任何监控在其司法管辖区内合法，并附带适用法律要求的告知与同意。

## 14. 您的控制手段
您可以导出记录、导入记录、删除单条记录、清理历史、删除全部数据以及卸载本软件。您也可以直接用 Windows 删除数据文件夹。卸载会移除应用，但不一定移除数据文件夹或导出文件。

## 15. 国际用户
由于本软件不会将记录传输到设备之外，本软件不进行记录的国际传输。若您导出记录并将其转移到其他服务或司法管辖区，该传输由您负责，您也需遵守适用于该传输的规则。

## 16. 本政策的变更
本隐私政策带有版本。对存储内容、存储方式或使用目的的实质性变更，会体现为新的版本号与最后更新日期，并在需要时伴随本软件中更新的接受步骤。您所安装版本随附的文本描述该版本的做法。

## 17. 联系
ScreenTime RS 维护于 https://github.com/wuchen128/ScreenTimeRS，可在该处提出有关数据处理的问题或反馈。
""";

    private const string PrivacyZhTw = """
# ScreenTime RS 隱私權政策

版本：v0.9.2
最後更新：2026年11月11日

## 1. 適用範圍與控制者
ScreenTime RS 是一款本機 Windows 應用程式，用於記錄並顯示螢幕使用時間、應用程式使用情況與專注時段統計。本隱私權政策說明本軟體儲存哪些資訊、為何儲存、儲存於何處、保留多久，以及您擁有哪些控制方式。本軟體建立的所有記錄都留在記錄產生的裝置上。本專案不接收、不代管、亦無權存取這些記錄，因此控制該 Windows 帳戶的人實際上是本機記錄的控制者。

## 2. 概要
- 不需要帳戶、登入、訂閱或授權碼。
- 不會向本專案或任何第三方傳送使用資料、專注資料或診斷資料。
- 不會產生分析統計、遙測、廣告、當機回報或更新檢查流量。
- 本軟體完全離線運作，不包含任何網路程式碼。
- 所有記錄都儲存於本機，您可以檢視、匯出或刪除。

## 3. 儲存的資訊
為提供相關功能，本軟體會在本機裝置上儲存以下內容：
- 應用程式名稱，以及在可解析時前景應用程式的可執行檔路徑。
- 用於每日、每小時、歷史與按應用程式統計的活躍秒數與時間戳記。
- 工作階段識別碼、工作階段開始與結束時間，以及連續使用測量值。
- 專注時段的開始時間、計畫時長、實際累計專注時間、完成狀態與暫停次數。
- 背景監控、重新整理與復原所需的本機執行狀態，包括暫停與重新整理標記檔案。
- 為介面產生的快照與彙總資料，例如目前快照與分析資料檔案。
- 在受支援的匯入操作前建立的匯入備份。
- 語言、主題模式、強調色、導覽狀態以及法律文件接受標記等應用程式偏好，儲存於目前使用者設定機碼下的 Windows 登錄中。
- 僅在介面遇到未處理錯誤時寫入的本機診斷記錄檔。

## 4. 不蒐集的資訊
本軟體不會刻意蒐集、記錄或傳送：
- 螢幕內容、截圖或螢幕錄影。
- 鍵盤輸入、剪貼簿內容或輸入歷史。
- 音訊、攝影機或其他感測器資料。
- 視窗標題。前景視窗標題僅在識別所屬處理程序時於記憶體中讀取，不會寫入儲存。
- 瀏覽歷史、頁面內容、網址或網路流量。
- 文件內容、訊息內容、聯絡人清單或行事曆資料。
- 位置資料、廣告識別碼、硬體指紋或帳戶憑證。

## 5. 記錄方式
監控核心約每秒取樣一次前景視窗，識別所屬處理程序，並為該應用程式累計活躍時間。當工作站被鎖定、約 60 秒內無使用者輸入、記錄暫停，或裝置休眠、關機時，累計會停止。取樣間隔造成的缺口會被限制，因此暫停的作業不會被記錄為一段很長的使用時間。專注時間使用相同的活動條件，但作為獨立指標儲存。

## 6. 處理目的
儲存的資訊僅用於執行本軟體並提供其功能：每日摘要、按應用程式彙總與排行、每小時與多週期圖表、連續使用與解鎖統計、專注計時器與專注歷史、匯入與匯出、本機備份以及診斷。

## 7. 儲存位置與格式
預設情況下，資料儲存於目前 Windows 使用者本機應用程式資料目錄下的 ScreenTime RS 資料夾中。資料夾位於您的 Windows 使用者設定檔目錄下。記錄儲存於本機 SQLite 資料庫中，快照、匯出、分析與備份則使用 JSON 檔案。資料庫未由應用程式加密，詳見第 10 條。

## 8. 保留期限
使用記錄與專注記錄會一直保留在裝置上，直到您刪除它們或移除底層檔案。本軟體不會因為歷史記錄數量變多或時間變舊而靜默刪除記錄。介面限制，例如主專注頁面只顯示最近的專注記錄，僅屬於呈現限制，不會刪除資料。

## 9. 刪除
本軟體提供刪除單筆專注記錄、清理受支援的歷史以及刪除全部資料的控制功能。刪除全部資料屬於破壞性操作，確認後會從本機資料庫中永久移除記錄的使用與專注資料。它不一定會移除設定、快照、既有備份、您匯出的檔案，或已被檔案歷程、備份、同步工具擷取的副本；這些需要另行刪除。刪除檔案並不保證儲存裝置上的每一份實體副本都被抹除。

## 10. 安全性
記錄的保護依賴 Windows 帳戶邊界與檔案系統權限，而非應用程式層級加密。任何能夠以您的 Windows 使用者身分登入、取得管理員權限、直接讀取磁碟或開啟匯出檔案的人，都可以讀取這些記錄。本軟體會合理地將資料保留於本機並避免不必要的複製，但任何本機應用程式都無法保證在惡意軟體、管理員、帳戶遭入侵或實體接觸面前仍能保密。

## 11. 備份、匯出與匯入
當您匯出資料時，本軟體會將 JSON 檔案寫入您選擇的位置。匯出檔案包含應用程式名稱、可執行檔路徑、時間戳記、時長與專注歷史，且未由本軟體加密。請將其存放在具有適當存取控制的位置，並在不再需要時刪除。在受支援的匯入之前，本軟體可能建立本機備份，以便復原之前的狀態。匯入資料會依照應用程式內顯示的確認說明套用；匯入前請核實檔案來源與完整性。

## 12. 裝置上的第三方存取
Windows、安全性軟體、備份與同步工具、裝置管理員以及同一裝置上的其他使用者，可能擁有各自的權限並存取本軟體儲存的檔案。此類存取由作業系統或上述第三方控制，並非由本軟體發起。

## 13. 兒童與受保護使用者
本軟體是通用型本機工具，並非針對兒童設計。它並非為蒐集兒童或其他受保護群體的資訊而設計。父母、監護人、組織與裝置管理員負責確保其進行的任何監控在其司法管轄區內合法，並附帶適用法律要求的告知與同意。

## 14. 您的控制方式
您可以匯出記錄、匯入記錄、刪除單筆記錄、清理歷史、刪除全部資料以及解除安裝本軟體。您也可以直接使用 Windows 刪除資料夾。解除安裝會移除應用程式，但不一定移除資料夾或匯出檔案。

## 15. 國際使用者
由於本軟體不會將記錄傳送到裝置之外，本軟體不進行記錄的國際傳輸。若您匯出記錄並將其轉移到其他服務或司法管轄區，該傳輸由您負責，您也需遵守適用於該傳輸的規則。

## 16. 本政策的變更
本隱私權政策具有版本。對儲存內容、儲存方式或使用目的的實質性變更，會體現為新的版本號與最後更新日期，並在需要時伴隨本軟體中更新的接受步驟。您所安裝版本隨附的文字描述該版本的做法。

## 17. 聯絡
ScreenTime RS 維護於 https://github.com/wuchen128/ScreenTimeRS，可在該處提出有關資料處理的問題或回饋。
""";

    public static string Terms(LanguageMode language)
        => SelectEnglishFile(language, "TERMS.md", TermsFallback, TermsZhCn, TermsZhTw);

    public static string Privacy(LanguageMode language)
        => SelectEnglishFile(language, "PRIVACY.md", PrivacyFallback, PrivacyZhCn, PrivacyZhTw);

    public static string Combined(LanguageMode language)
    {
        var selectedTerms = Terms(language).TrimEnd();
        var selectedPrivacy = Privacy(language).TrimStart();
        return ToPlainText(selectedTerms + "\n\n\n" + selectedPrivacy);
    }

    private static string SelectEnglishFile(LanguageMode language, string fileName, string englishFallback, string simplifiedChinese, string traditionalChinese)
    {
        var effective = LanguageResolver.Resolve(language);
        return effective switch
        {
            LanguageMode.TraditionalChinese => ToPlainText(traditionalChinese),
            LanguageMode.SimplifiedChinese => ToPlainText(simplifiedChinese),
            _ => ToPlainText(ReadDocument(fileName, englishFallback))
        };
    }

    private static string ToPlainText(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(markdown.Length);
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("### ", StringComparison.Ordinal))
                sb.AppendLine(trimmed[4..]);
            else if (trimmed.StartsWith("## ", StringComparison.Ordinal))
                sb.AppendLine(trimmed[3..]);
            else if (trimmed.StartsWith("# ", StringComparison.Ordinal))
                sb.AppendLine(trimmed[2..]);
            else if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                sb.AppendLine("• " + trimmed[2..]);
            else
                sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }
}
