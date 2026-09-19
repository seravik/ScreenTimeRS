# ScreenTime RS v0.8.0 — Unified usage data model

This revision promotes usage timing to a canonical SQLite fact table: `usage_records`.

Each canonical record has:

- `precision = precise`: a real runtime session with `start_at`, `end_at`, app, executable path, and tracked seconds.
- `precision = daily`: a legacy/import aggregate for a calendar day when exact clock time is not available.

All major statistics are derived from this one source:

- Today / yesterday / week / month totals
- App usage and All Time app usage
- Daily trend data
- Per-app daily history
- Session totals and longest session
- The 24-hour chart

The old `app_usage`, `sessions`, and `hourly_usage` tables remain only for compatibility and one-time migration. New runtime tracking writes precise records to `usage_records`; the old hourly table is no longer appended to.

## Migration behavior

On first start after this revision:

1. Existing `sessions` are copied into `usage_records` as precise records.
2. Existing `app_usage` totals are compared with the migrated precise time for the same app/day.
3. Only the remaining unassigned time is copied as a daily-precision record.
4. Existing `hourly_usage` is not copied because that table has no app dimension and duplicates totals already represented by `app_usage`/sessions. Assigning it to an hour+app would invent information.
5. A schema marker `schema:unified-v1` makes the migration idempotent.

Historical daily-only time is therefore preserved in totals and app statistics but is deliberately not fabricated into specific hours.


## v0.8.0 unified continuous-session model

Precise runtime records are app segments linked by `usage_records.session_id`.
A continuous computer-activity session may contain multiple foreground apps. Switching
from Firefox to VS Code closes only the Firefox app segment and starts a VS Code segment
with the same `session_id`; it does not end the continuous session.

A continuous session ends only when tracking becomes inactive (lock, pause, or the
configured idle threshold). The dashboard metrics use this model:

- Current continuous use: elapsed time of the current active session.
- Longest continuous use today: sum of all precise segments sharing one `session_id`
  for the portion that overlaps today.
- App usage and hourly usage: derived from the precise app segments.
- Daily totals: derived from both precise records and legacy/import daily-precision
  records, without inventing historical hour assignments.

Schema marker `schema:unified-v2` indicates this `session_id` model has been applied.
Existing unified-v1 precise records are conservatively treated as individual sessions,
because their historical continuity across app switches was not recorded.
