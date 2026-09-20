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
