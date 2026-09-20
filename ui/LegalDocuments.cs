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

Version: v0.9.0
Last updated: September 20, 2026

## 1. Acceptance
By installing, launching, or using ScreenTime RS, the user acknowledges that they have had an opportunity to review these Terms of Use and the Privacy Policy. Where the application presents an acceptance dialog, continued use of the application requires acceptance through that dialog.

## 2. Intended Use
ScreenTime RS is intended to help users record, review, and manage their own Windows screen-time, application-usage, and focus-session information. The software is provided as a local productivity and analytics utility.

## 3. Recording Scope
The user is responsible for understanding what the application records in their environment. Windows behavior, permissions, power states, lock states, background execution, application behavior, and future operating-system changes may affect what can be detected or recorded. Statistics should therefore be treated as measurements produced by the application rather than as a guaranteed complete record of every computer activity.

## 4. Focus Feature
The Focus feature is a local timer and productivity aid. It is not a security control, parental-control system, access-control system, or application-blocking mechanism. Starting a focus session does not prevent the user from opening or switching applications unless a future version explicitly provides such a feature.

## 5. Accuracy and Availability
The software is provided as implemented. ScreenTime RS does not guarantee that every statistic will be complete, continuous, perfectly accurate, or available in every Windows configuration or third-party application. Users should verify important information before relying on it for decisions that require precise records.

## 6. No Professional Advice
ScreenTime RS does not provide medical, legal, financial, employment, educational, or other professional advice. Any productivity, time-management, or screen-use decisions made using the software remain the user's responsibility.

## 7. User Responsibility
Users are responsible for using the software lawfully and for ensuring that monitoring or recording is appropriate in their environment. This includes obtaining any permissions or notices required by applicable law, workplace rules, school rules, household rules, or organizational policy.

## 8. Exported and Imported Data
Exported JSON files and backups may contain application names, executable paths, usage durations, timestamps, and focus history. Users are responsible for storing such files securely and for verifying the source and integrity of a backup before importing it. Importing data may replace or merge with existing data according to the behavior described by the relevant application flow.

## 9. Destructive Actions
Actions such as clearing focus history or deleting all data may permanently remove local records. Users should review confirmation dialogs carefully and create a backup when the records may be important.

## 10. Third-Party Software and Services
ScreenTime RS may depend on Windows system components and third-party libraries. Their availability, licensing, behavior, security, and compatibility are governed by their own terms and licenses. ScreenTime RS does not assume responsibility for independent changes made by those third parties.

## 11. Intellectual Property
The ScreenTime RS name, project materials, original source code, documentation, artwork, and other project assets remain subject to their respective licenses and notices. Unless a separate license says otherwise, no license is granted to use project trademarks or branding merely by using or modifying the software.

## 12. Modification and Redistribution
Modification, forking, and redistribution are permitted only to the extent granted by the project license. Redistributors must preserve any required attribution, license notices, and references to the original ScreenTime RS project. Modified versions must not falsely imply that they are official releases or that they are endorsed by the original project maintainer.

## 13. Disclaimer of Warranty
To the maximum extent permitted by applicable law, ScreenTime RS is provided on an “AS IS” and “AS AVAILABLE” basis, without warranties of any kind, whether express, implied, statutory, or otherwise, including warranties of merchantability, fitness for a particular purpose, title, non-infringement, accuracy, reliability, or uninterrupted availability.

## 14. Limitation of Liability
To the maximum extent permitted by applicable law, the project maintainers and contributors are not liable for indirect, incidental, special, consequential, exemplary, or punitive damages, or for loss of data, productivity, revenue, access, or other benefits arising from the use of, inability to use, or reliance on the software.

## 15. Security and Device Administration
Users are responsible for the security of their Windows account, device, backups, and exported data. Administrative access, malware, compromised accounts, file-system permissions, and other system conditions may affect the confidentiality or integrity of locally stored records.

## 16. Updates and Changes
Future versions may add, remove, or change features, data models, interfaces, and requirements. The documentation and notices shipped with the applicable version control how that version operates.

## 17. Termination of Use
Users may stop using ScreenTime RS at any time and may uninstall it through Windows. Uninstalling does not necessarily remove locally stored usage data or exported backups unless the relevant files are separately deleted.

## 18. Governing Compliance
Users must comply with applicable laws, regulations, contractual obligations, and organizational policies when using ScreenTime RS. Nothing in these Terms authorizes conduct that would otherwise be prohibited.

## 19. Severability
If any provision of these Terms is found unenforceable, the remaining provisions should remain in effect to the extent permitted by applicable law.

## 20. Entire Notice
These Terms and the ScreenTime RS Privacy Policy describe the principal conditions and data practices presented by v0.9.0. Additional license notices shipped with the software may apply to particular components.
""";

    private const string PrivacyFallback = """
# ScreenTime RS Privacy Policy

Version: v0.9.0
Last updated: September 20, 2026

## 1. Scope
ScreenTime RS is a local Windows application for recording and presenting screen-time, application-usage, and focus-session statistics. This Privacy Policy explains what information the application stores, why it is stored, where it is stored, and what controls are available to the user.

## 2. Local-First Design
The application is designed around local storage. ScreenTime RS does not require an online account, does not require a cloud service to calculate its core statistics, and does not actively upload usage or focus records to a remote server as part of normal operation.

## 3. Information Stored
To provide its features, ScreenTime RS may store:
- Application names and, where available, executable file paths.
- Recorded usage durations and timestamps required for daily, hourly, historical, and application statistics.
- Continuous-use and session information used to calculate session-based statistics.
- Focus-session start times, planned durations, actual focus durations, completion state, and pause counts.
- Local runtime state required for background monitoring, refresh, or recovery.
- Application settings such as language, theme, navigation state, and other preferences selected by the user.

The application does not intentionally collect unrelated personal content such as document contents, passwords, keystrokes, message contents, or the contents of websites simply for the purpose of producing screen-time statistics.

## 4. Purpose of Processing
Stored information is used to operate the application and to provide features such as daily summaries, application-usage history, hourly charts, continuous-use measurements, focus timers, focus history, import/export, backup, and diagnostics.

## 5. Data Location
By default, application data is stored under the current Windows user's LocalAppData directory, in the ScreenTime RS data directory. The exact location is displayed in the Settings page. The user may open the directory directly from Settings.

## 6. Data Retention
Usage and focus records remain on the local device until the user deletes them or otherwise removes the underlying data. The application does not silently delete historical records merely because the history becomes large. User interface limits, such as showing only the latest records on the main Focus page, are presentation limits and do not constitute data deletion.

## 7. Backups and Import / Export
When the user exports data, the resulting JSON file is created at the user's request and stored wherever the user chooses. Users are responsible for protecting exported files and backups. Before supported import operations, ScreenTime RS may create a local backup so that the previous state can be recovered. Imported data is handled according to the confirmation shown by the application.

## 8. Deletion
The application provides controls for deleting individual focus-history entries and for clearing supported history. The Delete All Data action is destructive and is intended to permanently remove recorded usage and focus history from the local database after confirmation. Settings and unrelated application files may remain unless the user removes them separately.

## 9. System and Third-Party Access
Windows, security software, backup software, administrators, and other system components may have their own permissions and may access files stored on the device. Such access is controlled by the operating system or those third parties and is not initiated by ScreenTime RS.

## 10. Networking
Core statistics do not depend on an online service. If a future version introduces an optional online feature, that feature should provide its own notice and configuration before data is transmitted. No such transfer is required for the core local recording features described by this version.

## 11. Security
Reasonable care is taken to store application data in the user's local application-data area and to avoid unnecessary network transmission. No local application can guarantee absolute security against malware, compromised accounts, administrator access, disk theft, or other events outside the application's control.

## 12. User Controls
Users can review the stored data location, export records, import records, create supported backups, delete history, or remove application data through the available features. Users may also disable or pause recording where such controls are provided.

## 13. Children and Protected Users
ScreenTime RS is a general-purpose local utility and is not designed to collect information about children or other protected groups. Parents, guardians, organizations, and device administrators are responsible for ensuring that use of monitoring software complies with applicable rules and permissions in their environment.

## 14. Policy Changes
This Privacy Policy may be revised when the application changes materially. The version and last-updated date should be reviewed when a new release introduces changes to data handling.

## 15. Contact and Attribution
ScreenTime RS is developed and maintained under the ScreenTime RS project. This document describes the data practices of the software as implemented for v0.9.0 and should not be interpreted as a promise about unrelated third-party software or future versions.
""";

    private const string TermsZhCn = """
# ScreenTime RS 使用条款

版本：v0.9.0
最后更新：2026年9月20日

## 1. 接受条款
安装、启动或使用 ScreenTime RS，即表示用户确认已经有机会阅读本使用条款和隐私政策。如果应用显示条款同意对话框，继续使用应用需要通过该对话框确认同意。

## 2. 预期用途
ScreenTime RS 用于帮助用户记录、查看和管理其本人 Windows 屏幕使用时间、应用使用情况以及专注时段信息。本软件作为本地生产力与数据分析工具提供。

## 3. 记录范围
用户应当了解应用在其设备环境中实际记录的内容。Windows 行为、权限、电源状态、锁定状态、后台执行、应用程序行为以及未来操作系统的变化，都可能影响可检测或可记录的信息。因此，统计数据应被视为应用生成的测量结果，而不应被视为对计算机所有活动的绝对完整记录。

## 4. 专注功能
专注功能是本地计时器和生产力辅助工具。它不是安全控制系统、家长控制系统、访问控制系统，也不是应用程序屏蔽机制。开始专注并不会阻止用户打开或切换应用，除非未来版本明确提供此类功能。

## 5. 准确性与可用性
本软件按照当前实现提供。ScreenTime RS 不保证所有统计在每种 Windows 配置或第三方应用中都完整、连续、绝对准确或始终可用。在依赖精确记录作出重要决定之前，用户应自行核实相关信息。

## 6. 不构成专业建议
ScreenTime RS 不提供医疗、法律、金融、就业、教育或其他专业建议。用户依据本软件作出的生产力、时间管理或屏幕使用相关决定，由用户自行负责。

## 7. 用户责任
用户负责合法使用本软件，并确保在其环境中进行监测或记录是适当的。这包括根据适用法律、工作场所规定、学校规定、家庭规则或组织政策取得所需的许可或完成必要的告知。

## 8. 导出与导入的数据
导出的 JSON 文件和备份可能包含应用名称、可执行文件路径、使用时长、时间戳以及专注历史。用户负责安全保存这些文件，并在导入前核实备份的来源与完整性。导入数据可能按照相应操作流程所说明的行为替换或合并现有数据。

## 9. 破坏性操作
清理专注历史或删除全部数据等操作可能永久移除本地记录。对于重要数据，用户应仔细阅读确认对话框，并在需要时先创建备份。

## 10. 第三方软件与服务
ScreenTime RS 可能依赖 Windows 系统组件和第三方库。其可用性、许可、行为、安全性与兼容性受其自身条款和许可证约束。对于第三方独立进行的变更，ScreenTime RS 不承担责任。

## 11. 知识产权
ScreenTime RS 名称、项目材料、原始源代码、文档、美术资源及其他项目资产，均受各自适用的许可证和声明约束。除非另有单独许可，仅因使用或修改软件，不代表获得使用项目商标或品牌标识的许可。

## 12. 修改与再分发
只有在项目许可证明确授予的范围内，才允许修改、复刻和再分发。再分发者必须保留所要求的署名、许可证声明以及对原 ScreenTime RS 项目的引用。修改版本不得虚假声称自己是官方发行版，也不得暗示获得原项目维护者的认可或授权。

## 13. 免责声明
在适用法律允许的最大范围内，ScreenTime RS 按“现状”和“可用”基础提供，不提供任何形式的明示、默示、法定或其他保证，包括适销性、特定用途适用性、所有权、不侵权、准确性、可靠性或持续可用性的保证。

## 14. 责任限制
在适用法律允许的最大范围内，项目维护者和贡献者不对因使用、无法使用或依赖本软件而产生的间接、附带、特殊、后果性、示范性或惩罚性损害，以及数据、生产力、收入、访问权限或其他利益损失承担责任。

## 15. 安全与设备管理
用户负责其 Windows 账户、设备、备份和导出数据的安全。管理员访问、恶意软件、账户被入侵、文件系统权限及其他系统环境，都可能影响本地记录的机密性或完整性。

## 16. 更新与变更
未来版本可能新增、删除或更改功能、数据模型、界面和要求。相应版本随附的文档和声明将决定该版本的具体行为。

## 17. 停止使用
用户可以随时停止使用 ScreenTime RS，并可通过 Windows 卸载软件。除非用户另行删除相关文件，否则卸载软件并不一定会删除本地保存的使用数据或已导出的备份。

## 18. 合规使用
使用 ScreenTime RS 时，用户必须遵守适用的法律、法规、合同义务以及组织政策。本条款不授权任何原本被禁止的行为。

## 19. 可分割性
如果本条款中的任何规定被认定为不可执行，其余规定仍应在适用法律允许的范围内继续有效。

## 20. 完整声明
本使用条款和 ScreenTime RS 隐私政策说明了 v0.9.0 所呈现的主要使用条件和数据处理方式。软件随附的其他许可证声明可能适用于特定组件。
""";

    private const string TermsZhTw = """
# ScreenTime RS 使用條款

版本：v0.9.0
最後更新：2026年9月20日

## 1. 接受條款
安裝、啟動或使用 ScreenTime RS，即表示使用者確認已獲得閱讀本使用條款及隱私權政策的機會。如應用程式顯示條款同意對話方塊，繼續使用應用程式即須透過該對話方塊確認同意。

## 2. 預期用途
ScreenTime RS 用於協助使用者記錄、檢視及管理其本人 Windows 螢幕使用時間、應用程式使用情況以及專注時段資訊。本軟體作為本機生產力與資料分析工具提供。

## 3. 記錄範圍
使用者應了解應用程式在其裝置環境中實際記錄的內容。Windows 行為、權限、電源狀態、鎖定狀態、背景執行、應用程式行為，以及未來作業系統的變更，都可能影響可偵測或可記錄的資訊。因此，統計資料應視為由應用程式產生的測量結果，不應視為對電腦所有活動的絕對完整記錄。

## 4. 專注功能
專注功能是本機計時器與生產力輔助工具。它不是安全控制系統、家長控制系統、存取控制系統，也不是應用程式封鎖機制。開始專注並不會阻止使用者開啟或切換應用程式，除非未來版本明確提供此類功能。

## 5. 準確性與可用性
本軟體依目前實作提供。ScreenTime RS 不保證所有統計在每種 Windows 設定或第三方應用程式中都完整、連續、完全準確或始終可用。在依賴精確記錄作出重要決定之前，使用者應自行核實相關資訊。

## 6. 不構成專業建議
ScreenTime RS 不提供醫療、法律、金融、就業、教育或其他專業建議。使用者依據本軟體作出的生產力、時間管理或螢幕使用相關決定，由使用者自行負責。

## 7. 使用者責任
使用者負責合法使用本軟體，並確保在其環境中進行監測或記錄是適當的。這包括依適用法律、工作場所規定、學校規定、家庭規則或組織政策取得所需許可或完成必要告知。

## 8. 匯出與匯入的資料
匯出的 JSON 檔案和備份可能包含應用程式名稱、可執行檔路徑、使用時間、時間戳記以及專注歷史。使用者負責安全保存這些檔案，並在匯入前核實備份的來源與完整性。匯入資料可能依相關操作流程所說明的方式取代或合併現有資料。

## 9. 破壞性操作
清理專注歷史或刪除全部資料等操作可能永久移除本機記錄。對於重要資料，使用者應仔細閱讀確認對話方塊，並在需要時先建立備份。

## 10. 第三方軟體與服務
ScreenTime RS 可能依賴 Windows 系統元件及第三方程式庫。其可用性、授權、行為、安全性與相容性受其自身條款及授權約束。對於第三方獨立進行的變更，ScreenTime RS 不承擔責任。

## 11. 智慧財產權
ScreenTime RS 名稱、專案材料、原始碼、文件、美術資源及其他專案資產，均受各自適用的授權與聲明約束。除非另有單獨授權，僅因使用或修改軟體，不代表取得使用專案商標或品牌標識的許可。

## 12. 修改與再散布
只有在專案授權明確授予的範圍內，才允許修改、複刻與再散布。再散布者必須保留所要求的署名、授權聲明以及對原 ScreenTime RS 專案的引用。修改版本不得虛假聲稱自己是官方版本，也不得暗示獲得原專案維護者的認可或授權。

## 13. 免責聲明
在適用法律允許的最大範圍內，ScreenTime RS 按「現狀」及「可用」基礎提供，不提供任何形式的明示、默示、法定或其他保證，包括適售性、特定用途適用性、所有權、不侵權、準確性、可靠性或持續可用性的保證。

## 14. 責任限制
在適用法律允許的最大範圍內，專案維護者及貢獻者不對因使用、無法使用或依賴本軟體而產生的間接、附帶、特殊、後果性、示範性或懲罰性損害，以及資料、生產力、收入、存取權限或其他利益損失承擔責任。

## 15. 安全與裝置管理
使用者負責其 Windows 帳戶、裝置、備份及匯出資料的安全。管理員存取、惡意軟體、帳戶遭入侵、檔案系統權限及其他系統環境，都可能影響本機記錄的機密性或完整性。

## 16. 更新與變更
未來版本可能新增、移除或變更功能、資料模型、介面及要求。相應版本隨附的文件與聲明將決定該版本的具體行為。

## 17. 停止使用
使用者可以隨時停止使用 ScreenTime RS，並可透過 Windows 卸載軟體。除非使用者另行刪除相關檔案，否則卸載軟體不一定會刪除本機保存的使用資料或已匯出的備份。

## 18. 合規使用
使用 ScreenTime RS 時，使用者必須遵守適用法律、法規、合約義務及組織政策。本條款不授權任何原本被禁止的行為。

## 19. 可分割性
如本條款中的任何規定被認定為不可執行，其餘規定仍應在適用法律允許的範圍內繼續有效。

## 20. 完整聲明
本使用條款及 ScreenTime RS 隱私權政策說明了 v0.9.0 所呈現的主要使用條件與資料處理方式。軟體隨附的其他授權聲明可能適用於特定元件。
""";

    private const string PrivacyZhCn = """
# ScreenTime RS 隐私政策

版本：v0.9.0
最后更新：2026年9月20日

## 1. 适用范围
ScreenTime RS 是一款本地 Windows 应用，用于记录并展示屏幕使用时间、应用使用情况和专注时段统计。本隐私政策说明应用会存储哪些信息、为何存储、存储在哪里，以及用户可以使用哪些控制功能。

## 2. 本地优先设计
本应用以本地存储为基础。ScreenTime RS 不要求在线账户，不依赖云服务计算核心统计，也不会在正常运行过程中主动将使用记录或专注记录上传到远程服务器。

## 3. 存储的信息
为了提供相关功能，ScreenTime RS 可能存储：
- 应用名称，以及在可用情况下的可执行文件路径。
- 用于每日、按小时、历史和应用统计所需的使用时长与时间戳。
- 用于计算连续使用和会话统计的相关信息。
- 专注时段的开始时间、计划时长、实际专注时长、完成状态和暂停次数。
- 后台监控、刷新或恢复运行状态所需的本地运行数据。
- 用户选择的语言、主题、导航状态及其他应用设置。

本应用不会为了生成屏幕使用时间统计而有意收集与该目的无关的个人内容，例如文档内容、密码、键盘输入、消息内容或网站内容。

## 4. 处理目的
存储的信息用于运行本应用，并提供每日摘要、应用使用历史、按小时图表、连续使用统计、专注计时器、专注历史、导入导出、备份和诊断等功能。

## 5. 数据位置
默认情况下，应用数据保存在当前 Windows 用户的 LocalAppData 目录下的 ScreenTime RS 数据目录中。设置页面会显示确切的数据位置，用户也可以直接从设置中打开该目录。

## 6. 数据保留
使用记录和专注记录会一直保留在本地设备上，直到用户主动删除或移除底层数据。应用不会仅因为历史记录变多就静默删除历史数据。主专注页面只显示最近记录等界面限制，仅属于显示方式限制，不代表删除数据。

## 7. 备份与导入/导出
用户请求导出数据时，应用会创建相应 JSON 文件，并保存在用户选择的位置。用户负责保护导出的文件及备份。在支持的数据导入操作之前，ScreenTime RS 可能自动创建本地备份，以便恢复之前的状态。导入数据将按照应用展示的确认说明进行处理。

## 8. 删除
应用提供删除单条专注历史记录和清理支持的历史数据等控制。删除全部数据属于破坏性操作，确认后用于永久删除本地数据库中的记录使用数据和专注历史。设置及其他无关应用文件可能仍然保留，除非用户另行删除。

## 9. 系统与第三方访问
Windows、安全软件、备份软件、系统管理员及其他系统组件可能拥有自身权限并能够访问设备上的文件。此类访问由操作系统或第三方控制，并非由 ScreenTime RS 主动发起。

## 10. 网络
核心统计不依赖在线服务。如果未来版本增加可选的在线功能，应在传输数据之前提供独立的说明和配置选项。v0.9.0 所描述的核心本地记录功能不要求进行此类数据传输。

## 11. 安全
应用会合理地将数据保存在用户的本地应用数据区域，并避免不必要的网络传输。但任何本地应用都无法保证绝对安全，也无法完全防范恶意软件、账户被入侵、管理员访问、磁盘被盗或其他应用控制范围之外的事件。

## 12. 用户控制
用户可以查看数据存储位置、导出记录、导入记录、创建支持的备份、删除历史或通过已有功能移除应用数据。对于提供相应控制的场景，用户也可以禁用或暂停记录。

## 13. 儿童与受保护用户
ScreenTime RS 是通用型本地工具，并非为收集儿童或其他受保护群体的信息而设计。家长、监护人、组织及设备管理员负责确保在其环境中使用监测软件符合适用规则并具备必要权限。

## 14. 政策变更
当应用发生重大变化时，本隐私政策可能进行修订。新版本涉及数据处理方式变化时，应查看相应的版本号和最后更新时间。

## 15. 联系与署名
ScreenTime RS 由 ScreenTime RS 项目开发和维护。本隐私政策描述的是 v0.9.0 实际实现的数据处理方式，不应被解释为对无关第三方软件或未来版本的保证。
""";

    private const string PrivacyZhTw = """
# ScreenTime RS 隱私權政策

版本：v0.9.0
最後更新：2026年9月20日

## 1. 適用範圍
ScreenTime RS 是一款本機 Windows 應用程式，用於記錄及顯示螢幕使用時間、應用程式使用情況與專注時段統計。本隱私權政策說明應用程式會儲存哪些資訊、為何儲存、儲存於何處，以及使用者可使用哪些控制功能。

## 2. 本機優先設計
本應用程式以本機儲存為基礎。ScreenTime RS 不要求線上帳戶，不依賴雲端服務計算核心統計，也不會在正常執行期間主動將使用記錄或專注記錄上傳到遠端伺服器。

## 3. 儲存的資訊
為提供相關功能，ScreenTime RS 可能儲存：
- 應用程式名稱，以及在可用情況下的可執行檔路徑。
- 用於每日、每小時、歷史及應用程式統計所需的使用時間與時間戳記。
- 用於計算連續使用與工作階段統計的相關資訊。
- 專注時段的開始時間、計畫時長、實際專注時間、完成狀態及暫停次數。
- 背景監控、重新整理或復原執行狀態所需的本機執行資料。
- 使用者選擇的語言、主題、導覽狀態及其他應用程式設定。

本應用程式不會為了產生螢幕使用時間統計而刻意收集與該目的無關的個人內容，例如文件內容、密碼、鍵盤輸入、訊息內容或網站內容。

## 4. 處理目的
儲存的資訊用於執行本應用程式，並提供每日摘要、應用程式使用歷史、每小時圖表、連續使用統計、專注計時器、專注歷史、匯入匯出、備份與診斷等功能。

## 5. 資料位置
預設情況下，應用程式資料會儲存於目前 Windows 使用者 LocalAppData 目錄下的 ScreenTime RS 資料目錄。設定頁面會顯示確切資料位置，使用者也可以直接從設定中開啟該目錄。

## 6. 資料保存
使用記錄與專注記錄會一直保留在本機裝置上，直到使用者主動刪除或移除底層資料。應用程式不會僅因歷史記錄增加而靜默刪除歷史資料。主專注頁面只顯示最近記錄等介面限制，僅屬於顯示方式限制，不代表刪除資料。

## 7. 備份與匯入／匯出
使用者要求匯出資料時，應用程式會建立相應 JSON 檔案，並儲存於使用者選擇的位置。使用者負責保護匯出檔案與備份。在支援的資料匯入操作之前，ScreenTime RS 可能自動建立本機備份，以便復原之前的狀態。匯入資料將依照應用程式顯示的確認說明處理。

## 8. 刪除
應用程式提供刪除單筆專注歷史記錄及清理支援的歷史資料等控制功能。刪除全部資料屬於破壞性操作，確認後用於永久刪除本機資料庫中的使用記錄與專注歷史。設定及其他無關的應用程式檔案可能仍會保留，除非使用者另行刪除。

## 9. 系統與第三方存取
Windows、安全性軟體、備份軟體、系統管理員及其他系統元件可能擁有自身權限並能夠存取裝置上的檔案。此類存取由作業系統或第三方控制，並非由 ScreenTime RS 主動發起。

## 10. 網路
核心統計不依賴線上服務。如果未來版本增加可選的線上功能，應在傳輸資料前提供獨立說明及設定選項。v0.9.0 所描述的核心本機記錄功能不要求進行此類資料傳輸。

## 11. 安全性
應用程式會合理地將資料儲存在使用者的本機應用程式資料區域，並避免不必要的網路傳輸。然而任何本機應用程式都無法保證絕對安全，也無法完全防範惡意軟體、帳戶遭入侵、管理員存取、磁碟遭竊或其他超出應用程式控制範圍的事件。

## 12. 使用者控制
使用者可以檢視資料儲存位置、匯出記錄、匯入記錄、建立支援的備份、刪除歷史記錄，或透過現有功能移除應用程式資料。對於提供相應控制的情況，使用者也可以停用或暫停記錄。

## 13. 兒童與受保護使用者
ScreenTime RS 是通用型本機工具，並非為收集兒童或其他受保護群體的資訊而設計。家長、監護人、組織及裝置管理員負責確保在其環境中使用監測軟體符合適用規則並具備必要權限。

## 14. 政策變更
當應用程式發生重大變更時，本隱私權政策可能進行修訂。新版本涉及資料處理方式變化時，應查閱相應版本號與最後更新日期。

## 15. 聯絡與署名
ScreenTime RS 由 ScreenTime RS 專案開發及維護。本隱私權政策描述的是 v0.9.0 實際實作的資料處理方式，不應被解釋為對無關第三方軟體或未來版本的保證。
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
