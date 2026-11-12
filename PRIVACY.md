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
