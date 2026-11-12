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
