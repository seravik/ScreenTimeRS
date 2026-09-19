#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use anyhow::Result;
use serde::{Deserialize, Serialize};
use chrono::{Datelike, Duration as ChronoDuration, Local, Months, NaiveDate, DateTime, FixedOffset, TimeZone};
use directories::ProjectDirs;
use rusqlite::{params, Connection, OptionalExtension};
use std::{sync::{Arc, Mutex}, thread, time::{Duration, Instant}};
use sysinfo::{Pid, ProcessesToUpdate, System};
use tray_icon::{menu::{Icon as MenuIcon, IconMenuItem, Menu, MenuEvent, PredefinedMenuItem}, Icon, TrayIconBuilder, TrayIconEvent, MouseButton};

#[cfg(windows)]
use windows::Win32::{
    Foundation::{CloseHandle, HANDLE},
    System::Threading::{OpenProcess, PROCESS_QUERY_LIMITED_INFORMATION, QueryFullProcessImageNameW},
    UI::Input::KeyboardAndMouse::{GetLastInputInfo, LASTINPUTINFO},
    System::SystemInformation::GetTickCount,
    UI::WindowsAndMessaging::{
        DispatchMessageW, GetForegroundWindow, GetWindowTextW, GetWindowThreadProcessId,
        PeekMessageW, TranslateMessage, MSG, PM_REMOVE,
    },
};

const APP_NAME: &str = "ScreenTime RS";
const SHUTDOWN_FILE: &str = "shutdown.flag";
const REFRESH_FILE: &str = "refresh.flag";
const EXPORT_FORMAT_VERSION: u32 = 2;
const PAUSE_FILE: &str = "pause.flag";
const ANALYTICS_REFRESH_INTERVAL: u8 = 5;
#[derive(Clone, Default)]
struct AppStat {
    name: String,
    seconds: i64,
    exe_path: String,
}

#[derive(Clone, Default)]
struct Snapshot {
    current_app: String,
    current_window: String,
    today: i64,
    yesterday: i64,
    week: i64,
    month: i64,
    apps: Vec<AppStat>,
    apps_week: Vec<AppStat>,
    apps_month: Vec<AppStat>,
    apps_half_year: Vec<AppStat>,
    apps_year: Vec<AppStat>,
    apps_90_days: Vec<AppStat>,
    apps_30_days: Vec<AppStat>,
    apps_all: Vec<AppStat>,
    daily: Vec<(String, i64)>,
    daily_year: Vec<(String, i64)>,
    daily_all: Vec<(String, i64)>,
    locked: bool,
    monitor_on: bool,
    active_now: bool,
    idle_seconds: u64,
    cpu: f32,
    memory_mb: u64,
    current_session_seconds: i64,
    longest_session_today: i64,
    unlocks_today: i64,
    paused: bool,
    hourly_today: Vec<(String, i64)>,
}

struct Database { path: std::path::PathBuf }

impl Database {
    fn new() -> Result<Self> {
        let dirs = ProjectDirs::from("com", "ScreenTimeRS", APP_NAME)
            .ok_or_else(|| anyhow::anyhow!("cannot resolve local data directory"))?;
        std::fs::create_dir_all(dirs.data_local_dir())?;
        let db_path = dirs.data_local_dir().join("screentime.db");
        let old_db = dirs.data_dir().join("screentime.db");
        if !db_path.exists() && old_db.exists() {
            let _ = std::fs::copy(&old_db, &db_path);
        }
        let db = Self { path: db_path };
        {
            let c = db.conn()?;
            c.execute_batch(
                "CREATE TABLE IF NOT EXISTS app_usage(
                   date TEXT NOT NULL, app TEXT NOT NULL, seconds INTEGER NOT NULL DEFAULT 0,
                   exe_path TEXT NOT NULL DEFAULT '', PRIMARY KEY(date,app));
                 CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY,value TEXT NOT NULL);
                 CREATE TABLE IF NOT EXISTS sessions(
                   id INTEGER PRIMARY KEY AUTOINCREMENT, start_at TEXT NOT NULL, end_at TEXT,
                   app TEXT NOT NULL, exe_path TEXT NOT NULL DEFAULT '', seconds INTEGER NOT NULL DEFAULT 0);
                 CREATE TABLE IF NOT EXISTS hourly_usage(
                   date TEXT NOT NULL, hour INTEGER NOT NULL, seconds INTEGER NOT NULL DEFAULT 0,
                   PRIMARY KEY(date,hour));
                 CREATE TABLE IF NOT EXISTS app_categories(
                   app TEXT PRIMARY KEY, category TEXT NOT NULL);
                 -- Canonical usage model: precise runtime sessions and day-level
                 -- legacy/import records share one table. Hourly/daily statistics
                 -- are derived from this canonical source rather than duplicated tables.
                 CREATE TABLE IF NOT EXISTS usage_records(
                   id INTEGER PRIMARY KEY AUTOINCREMENT,
                   start_at TEXT NOT NULL,
                   end_at TEXT,
                   record_date TEXT NOT NULL,
                   app TEXT NOT NULL,
                   exe_path TEXT NOT NULL DEFAULT '',
                   seconds INTEGER NOT NULL DEFAULT 0,
                   precision TEXT NOT NULL DEFAULT 'precise',
                   source TEXT NOT NULL,
                   source_key TEXT NOT NULL
                 );
                 CREATE UNIQUE INDEX IF NOT EXISTS idx_usage_records_source
                   ON usage_records(source, source_key);
                 CREATE INDEX IF NOT EXISTS idx_usage_records_date
                   ON usage_records(record_date);
                 CREATE INDEX IF NOT EXISTS idx_usage_records_app
                   ON usage_records(app);
                 CREATE INDEX IF NOT EXISTS idx_usage_records_start_end
                   ON usage_records(start_at, end_at);"
            )?;
            let _ = c.execute("ALTER TABLE app_usage ADD COLUMN exe_path TEXT NOT NULL DEFAULT ''", []);
            // A continuous computer-activity session can contain multiple app
            // segments. session_id links those precise segments.
            let _ = c.execute("ALTER TABLE usage_records ADD COLUMN session_id INTEGER", []);
            let _ = c.execute("CREATE INDEX IF NOT EXISTS idx_usage_records_session ON usage_records(session_id)", []);
        }
        db.migrate_to_unified()?;
        Ok(db)
    }

    fn conn(&self) -> Result<Connection> {
        let c = Connection::open(&self.path)?;
        c.busy_timeout(Duration::from_secs(5))?;
        Ok(c)
    }

    fn migrate_to_unified(&self) -> Result<()> {
        if self.setting("schema:unified-v2")?.is_some() { return Ok(()); }
        let mut c = self.conn()?;
        let tx = c.transaction()?;

        // 1) Preserve old precise session data. Existing sessions are already the
        // most accurate timing source available in pre-unified releases.
        {
            let existing_sessions: Vec<(i64,String,Option<String>,String,String,i64)> = {
                let mut st = tx.prepare("SELECT id,start_at,end_at,app,exe_path,seconds FROM sessions WHERE seconds>0 ORDER BY id")?;
                let rows = st.query_map([], |r| Ok((
                    r.get::<_, i64>(0)?,
                    r.get::<_, String>(1)?,
                    r.get::<_, Option<String>>(2)?,
                    r.get::<_, String>(3)?,
                    r.get::<_, String>(4).unwrap_or_default(),
                    r.get::<_, i64>(5)?,
                )))?;
                rows.flatten().collect()
            };
            for (id, start_at, end_at, app, exe_path, seconds) in existing_sessions {
                if app.trim().is_empty() || seconds <= 0 { continue; }
                let record_date = parse_record_date(&start_at).unwrap_or_else(|| Local::now().date_naive());
                let effective_end = end_at.or_else(|| parse_precise(&start_at).map(|t| (t + ChronoDuration::seconds(seconds)).to_rfc3339()));
                tx.execute(
                    "INSERT OR IGNORE INTO usage_records(start_at,end_at,record_date,app,exe_path,seconds,precision,source,source_key,session_id)
                     VALUES(?1,?2,?3,?4,?5,?6,'precise','legacy_session',?7,?8)",
                    params![start_at, effective_end, record_date.to_string(), app, exe_path, seconds, format!("session:{id}"), id]
                )?;
            }
        }

        // 2) Preserve old day-level aggregates without pretending we know their hour.
        // Subtract precise session time already represented above; only the remainder
        // becomes a daily-precision record. This prevents duplicate totals.
        let legacy_rows: Vec<(String,String,i64,String)> = {
            let mut st = tx.prepare("SELECT date,app,seconds,exe_path FROM app_usage ORDER BY date,app")?;
            let rows = st.query_map([], |r| Ok((r.get(0)?, r.get(1)?, r.get(2)?, r.get(3).unwrap_or_default())))?;
            rows.flatten().collect()
        };
        for (date_text, app, total_seconds, exe_path) in legacy_rows {
            if total_seconds <= 0 || app.trim().is_empty() { continue; }
            let Some(date) = NaiveDate::parse_from_str(&date_text, "%Y-%m-%d").ok() else { continue; };
            let precise = precise_seconds_for_day_tx(&tx, &app, date)?;
            let remainder = (total_seconds - precise).max(0);
            if remainder == 0 { continue; }
            let start_at = format!("{date}T00:00:00");
            let end_at = format!("{}T00:00:00", date + ChronoDuration::days(1));
            tx.execute(
                "INSERT OR IGNORE INTO usage_records(start_at,end_at,record_date,app,exe_path,seconds,precision,source,source_key,session_id)
                 VALUES(?1,?2,?3,?4,?5,?6,'daily','legacy_daily',?7,NULL)",
                params![start_at, end_at, date_text, app, exe_path, remainder, format!("daily:{date_text}:{app}")]
            )?;
        }

        // Existing unified-v1 precise records have no session_id. Each historical
        // precise segment is treated as its own session; no continuity is invented.
        tx.execute("UPDATE usage_records SET session_id=id WHERE precision='precise' AND session_id IS NULL", [])?;
        tx.execute("INSERT INTO settings(key,value) VALUES('schema:unified-v1','1') ON CONFLICT(key) DO UPDATE SET value='1'", [])?;
        tx.execute("INSERT INTO settings(key,value) VALUES('schema:unified-v2','1') ON CONFLICT(key) DO UPDATE SET value='1'", [])?;
        tx.commit()?;
        Ok(())
    }

    fn fetch_records(&self, start: NaiveDate, end: NaiveDate) -> Result<Vec<UsageRecord>> {
        if start > end { return Ok(Vec::new()); }
        // A precise session can cross midnight, so include one preceding start-day
        // bucket and let Rust calculate exact overlap from timestamps.
        let query_start = start - ChronoDuration::days(1);
        let c = self.conn()?;
        let mut st = c.prepare(
            "SELECT id,start_at,end_at,record_date,app,exe_path,seconds,precision,source,session_id
             FROM usage_records
             WHERE record_date>=?1 AND record_date<=?2"
        )?;
        let rows = st.query_map(params![query_start.to_string(), end.to_string()], |r| Ok(UsageRecord {
            id: r.get(0)?,
            start_at: r.get(1)?,
            end_at: r.get(2)?,
            record_date: r.get(3)?,
            app: r.get(4)?,
            exe_path: r.get(5).unwrap_or_default(),
            seconds: r.get(6)?,
            precision: r.get(7)?,
            source: r.get(8)?,
            session_id: r.get(9)?,
        }))?;
        Ok(rows.filter_map(Result::ok).collect())
    }

    fn range_total(&self, start: NaiveDate, end: NaiveDate) -> Result<i64> {
        Ok(self.fetch_records(start, end)?.into_iter().map(|r| r.overlap_seconds(start, end)).sum())
    }

    fn apps_today(&self) -> Result<Vec<AppStat>> {
        let today = Local::now().date_naive();
        self.apps_range(today, today)
    }

    fn apps_range(&self, start: NaiveDate, end: NaiveDate) -> Result<Vec<AppStat>> {
        let mut map = std::collections::HashMap::<String, AppStat>::new();
        for r in self.fetch_records(start, end)? {
            let seconds = r.overlap_seconds(start, end);
            if seconds <= 0 { continue; }
            let entry = map.entry(r.app.clone()).or_insert_with(|| AppStat { name: r.app.clone(), seconds: 0, exe_path: String::new() });
            entry.seconds += seconds;
            if entry.exe_path.is_empty() && !r.exe_path.is_empty() { entry.exe_path = r.exe_path.clone(); }
        }
        let mut out: Vec<_> = map.into_values().collect();
        out.sort_by(|a,b| b.seconds.cmp(&a.seconds).then_with(|| a.name.cmp(&b.name)));
        Ok(out)
    }

    fn apps_for_day(&self, day: NaiveDate) -> Result<Vec<AppStat>> { self.apps_range(day, day) }

    fn all_time_start(&self) -> Result<Option<NaiveDate>> {
        let c = self.conn()?;
        let date: Option<String> = c.query_row("SELECT MIN(record_date) FROM usage_records", [], |r| r.get(0))?;
        Ok(date.and_then(|x| NaiveDate::parse_from_str(&x, "%Y-%m-%d").ok()))
    }

    fn daily_full_range(&self, start: NaiveDate, end: NaiveDate) -> Result<Vec<(String,i64)>> {
        if start > end { return Ok(Vec::new()); }
        let records = self.fetch_records(start, end)?;
        let mut totals = std::collections::HashMap::<NaiveDate, i64>::new();
        for r in records {
            let first = start.max(r.start_date());
            let last = end.min(r.end_date().unwrap_or(first));
            let mut day = first;
            while day <= last {
                let sec = r.overlap_seconds(day, day);
                if sec > 0 { *totals.entry(day).or_default() += sec; }
                day += ChronoDuration::days(1);
            }
        }
        let span = (end - start).num_days() + 1;
        Ok((0..span).map(|i| {
            let d = start + ChronoDuration::days(i);
            (d.to_string(), *totals.get(&d).unwrap_or(&0))
        }).collect())
    }

    fn range_usage(&self, start: NaiveDate, end: NaiveDate) -> Result<(Vec<AppStat>, Vec<(String,i64)>)> {
        Ok((self.apps_range(start, end)?, self.daily_full_range(start, end)?))
    }

    fn app_history(&self, app: &str, start: NaiveDate, end: NaiveDate) -> Result<Vec<(String,i64)>> {
        let records = self.fetch_records(start, end)?;
        let mut totals = std::collections::HashMap::<NaiveDate, i64>::new();
        for r in records.into_iter().filter(|r| r.app == app) {
            let first = start.max(r.start_date());
            let last = end.min(r.end_date().unwrap_or(first));
            let mut day = first;
            while day <= last {
                let sec = r.overlap_seconds(day, day);
                if sec > 0 { *totals.entry(day).or_default() += sec; }
                day += ChronoDuration::days(1);
            }
        }
        let span = (end - start).num_days().max(0) + 1;
        Ok((0..span).map(|i| {
            let d = start + ChronoDuration::days(i);
            (d.format("%m-%d").to_string(), *totals.get(&d).unwrap_or(&0))
        }).collect())
    }

    fn backup_before_import(&self) -> Result<std::path::PathBuf> {
        let dirs = ProjectDirs::from("com", "ScreenTimeRS", APP_NAME)
            .ok_or_else(|| anyhow::anyhow!("cannot resolve local data directory"))?;
        let backup_dir = dirs.data_local_dir().join("backups");
        std::fs::create_dir_all(&backup_dir)?;
        let filename = format!("ScreenTimeRS-pre-import-{}.db", Local::now().format("%Y%m%d-%H%M%S-%f"));
        let target = backup_dir.join(filename);
        let c = self.conn()?;
        c.execute("VACUUM INTO ?1", params![target.to_string_lossy().to_string()])?;
        drop(c);
        let mut backups: Vec<_> = std::fs::read_dir(&backup_dir)?.filter_map(Result::ok).map(|e| e.path())
            .filter(|p| p.file_name().and_then(|n| n.to_str()).map(|n| n.starts_with("ScreenTimeRS-pre-import-") && n.ends_with(".db")).unwrap_or(false)).collect();
        backups.sort();
        while backups.len() > 5 { let _ = std::fs::remove_file(backups.remove(0)); }
        Ok(target)
    }

    fn validate_records(records: &[ExportRecord]) -> Result<()> {
        for record in records {
            if record.app.trim().is_empty() || record.app.len() > 512 || record.exe_path.len() > 4096 || record.seconds < 0 {
                anyhow::bail!("invalid usage record");
            }
            NaiveDate::parse_from_str(&record.date, "%Y-%m-%d")
                .map_err(|_| anyhow::anyhow!("invalid usage record date"))?;
            if let Some(precision) = &record.precision {
                if precision != "daily" && precision != "precise" {
                    anyhow::bail!("invalid usage record precision");
                }
                if precision == "precise" {
                    let start = record.start_at.as_deref().and_then(parse_precise)
                        .ok_or_else(|| anyhow::anyhow!("precise record missing start_at"))?;
                    if let Some(end) = record.end_at.as_deref().and_then(parse_precise) {
                        if end < start { anyhow::bail!("invalid usage record time range"); }
                    }
                }
            }
        }
        Ok(())
    }

    fn daily(&self, days: i64) -> Result<Vec<(String,i64)>> {
        let today = Local::now().date_naive();
        let start = today - ChronoDuration::days(days.saturating_sub(1));
        self.daily_range(start, today)
    }

    fn daily_range(&self, start: NaiveDate, end: NaiveDate) -> Result<Vec<(String,i64)>> {
        self.daily_full_range(start, end).map(|v| v.into_iter().map(|(d,s)| {
            let date = NaiveDate::parse_from_str(&d, "%Y-%m-%d").unwrap_or(start);
            (date.format("%m-%d").to_string(), s)
        }).collect())
    }

    fn daily_all(&self) -> Result<Vec<(String,i64)>> {
        let today = Local::now().date_naive();
        let Some(start) = self.all_time_start()? else { return Ok(Vec::new()); };
        self.daily_full_range(start, today)
    }

    fn export_records(&self) -> Result<Vec<ExportRecord>> {
        let c = self.conn()?;
        let mut st = c.prepare(
            "SELECT record_date,app,seconds,exe_path,start_at,end_at,precision,session_id
             FROM usage_records ORDER BY record_date,app,start_at,id"
        )?;
        let rows = st.query_map([], |r| Ok(ExportRecord {
            date: r.get(0)?,
            app: r.get(1)?,
            seconds: r.get(2)?,
            exe_path: r.get(3).unwrap_or_default(),
            start_at: r.get(4).ok(),
            end_at: r.get(5).ok(),
            precision: r.get(6).ok(),
            session_id: r.get(7).ok(),
        }))?;
        Ok(rows.filter_map(Result::ok).collect())
    }

    fn replace_all_records(&self, records: &[ExportRecord]) -> Result<()> {
        Self::validate_records(records)?;
        let mut c = self.conn()?;
        let tx = c.transaction()?;
        tx.execute("DELETE FROM usage_records", [])?;
        tx.execute("DELETE FROM app_usage", [])?;
        tx.execute("DELETE FROM sessions", [])?;
        tx.execute("DELETE FROM hourly_usage", [])?;

        // Import both legacy daily records and precision-preserving records produced
        // by the unified exporter. app_usage remains a compatibility mirror only.
        let mut daily_mirror = std::collections::HashMap::<(String,String),(i64,String)>::new();
        {
            let mut st = tx.prepare(
                "INSERT INTO usage_records(start_at,end_at,record_date,app,exe_path,seconds,precision,source,source_key,session_id)
                 VALUES(?1,?2,?3,?4,?5,?6,?7,'import',?8,?9)"
            )?;
            for (i, record) in records.iter().enumerate() {
                let is_precise = record.precision.as_deref() == Some("precise") && record.start_at.is_some();
                let (start_at, end_at, precision, session_id) = if is_precise {
                    let start = record.start_at.clone().unwrap();
                    (start, record.end_at.clone(), "precise", record.session_id)
                } else {
                    let date = NaiveDate::parse_from_str(&record.date, "%Y-%m-%d")?;
                    (format!("{date}T00:00:00"), Some(format!("{}T00:00:00", date + ChronoDuration::days(1))), "daily", None)
                };
                st.execute(params![start_at,end_at,record.date,record.app,record.exe_path,record.seconds,precision,format!("{}:{}:{i}",record.date,record.app),session_id])?;
                let key=(record.date.clone(),record.app.clone());
                let entry=daily_mirror.entry(key).or_insert((0,record.exe_path.clone()));
                entry.0 += record.seconds;
                if entry.1.is_empty() && !record.exe_path.is_empty() { entry.1=record.exe_path.clone(); }
            }
        }
        {
            let mut st = tx.prepare("INSERT INTO app_usage(date,app,seconds,exe_path) VALUES(?1,?2,?3,?4)")?;
            for ((date,app),(seconds,exe_path)) in daily_mirror {
                st.execute(params![date,app,seconds,exe_path])?;
            }
        }
        tx.commit()?;
        Ok(())
    }

    fn setting(&self, key: &str) -> Result<Option<String>> {
        let c = self.conn()?;
        Ok(c.query_row("SELECT value FROM settings WHERE key=?1", params![key], |r| r.get(0)).optional()?)
    }
    fn set_setting(&self, key: &str, value: &str) -> Result<()> {
        let c = self.conn()?;
        c.execute("INSERT INTO settings(key,value) VALUES(?1,?2) ON CONFLICT(key) DO UPDATE SET value=excluded.value", params![key,value])?;
        Ok(())
    }
    fn start_activity_session(&self, app: &str, exe_path: &str, start: &str) -> Result<(i64,i64)> {
        let date = parse_record_date(start).unwrap_or_else(|| Local::now().date_naive());
        let c = self.conn()?;
        c.execute(
            "INSERT INTO usage_records(start_at,end_at,record_date,app,exe_path,seconds,precision,source,source_key,session_id)
             VALUES(?1,?1,?2,?3,?4,0,'precise','runtime_session',?5,NULL)",
            params![start,date.to_string(),app,exe_path,format!("pending:{start}")]
        )?;
        let segment_id = c.last_insert_rowid();
        c.execute(
            "UPDATE usage_records SET source_key=?2,session_id=?3 WHERE id=?1",
            params![segment_id, format!("runtime:{segment_id}"), segment_id]
        )?;
        Ok((segment_id, segment_id))
    }

    fn start_activity_segment(&self, session_id: i64, app: &str, exe_path: &str, start: &str) -> Result<i64> {
        let date = parse_record_date(start).unwrap_or_else(|| Local::now().date_naive());
        let c = self.conn()?;
        c.execute(
            "INSERT INTO usage_records(start_at,end_at,record_date,app,exe_path,seconds,precision,source,source_key,session_id)
             VALUES(?1,?1,?2,?3,?4,0,'precise','runtime_session',?5,?6)",
            params![start,date.to_string(),app,exe_path,format!("pending:{start}:{session_id}"),session_id]
        )?;
        let segment_id = c.last_insert_rowid();
        c.execute(
            "UPDATE usage_records SET source_key=?2 WHERE id=?1",
            params![segment_id, format!("runtime:{segment_id}")]
        )?;
        Ok(segment_id)
    }

    fn update_session(&self, id: i64, seconds: i64, end_at: Option<&str>) -> Result<()> {
        if seconds <= 0 { return Ok(()); }
        let c = self.conn()?;
        c.execute("UPDATE usage_records SET seconds=?2,end_at=COALESCE(?3,end_at) WHERE id=?1 AND precision='precise' AND source='runtime_session'", params![id,seconds,end_at])?;
        Ok(())
    }
    fn session_stats_today(&self, today: NaiveDate) -> Result<(i64,i64)> {
        let mut total = 0i64;
        let mut by_session = std::collections::HashMap::<i64,i64>::new();
        for r in self.fetch_records(today,today)?.into_iter().filter(|r| r.precision=="precise") {
            let sec = r.overlap_seconds(today,today);
            if sec <= 0 { continue; }
            total += sec;
            let session_id = r.session_id.unwrap_or(r.id);
            *by_session.entry(session_id).or_default() += sec;
        }
        let longest = by_session.values().copied().max().unwrap_or(0);
        Ok((total,longest))
    }
    fn hourly_today(&self, today: NaiveDate) -> Result<Vec<(String,i64)>> {
        let mut out=vec![(String::new(),0i64);24];
        for h in 0u32..24 { out[h as usize].0=format!("{:02}:00",h); }
        for r in self.fetch_records(today,today)?.into_iter().filter(|r| r.precision=="precise") {
            let Some(start)=r.start_datetime() else { continue; };
            let Some(end)=r.end_datetime() else { continue; };
            let start_ts=Local.from_local_datetime(&today.and_hms_opt(0,0,0).unwrap()).earliest().unwrap().timestamp();
            let end_day=today+ChronoDuration::days(1);
            let end_ts=Local.from_local_datetime(&end_day.and_hms_opt(0,0,0).unwrap()).latest().unwrap().timestamp();
            let rec_start=start.timestamp(); let rec_end=end.timestamp();
            let total_span=(rec_end-rec_start).max(1);
            let mut allocated=0i64;
            let mut last_hour: Option<usize>=None;
            for h in 0u32..24 {
                let hs=Local.from_local_datetime(&(today.and_hms_opt(h,0,0).unwrap())).earliest().unwrap().timestamp();
                let he=if h==23 { end_ts } else { Local.from_local_datetime(&(today.and_hms_opt(h+1,0,0).unwrap())).latest().unwrap().timestamp() };
                let os=rec_start.max(hs).max(start_ts);
                let oe=rec_end.min(he).min(end_ts);
                if oe>os {
                    let piece=((r.seconds as i128)*(oe-os) as i128/total_span as i128) as i64;
                    out[h as usize].1 += piece;
                    allocated += piece;
                    last_hour=Some(h as usize);
                }
            }
            if let Some(h)=last_hour {
                out[h].1 += (r.seconds-allocated).max(0);
            }
        }
        Ok(out)
    }
    fn unlocks_today(&self, today: NaiveDate) -> Result<i64> {
        Ok(self.setting(&format!("unlocks:{today}"))?.and_then(|v|v.parse().ok()).unwrap_or(0))
    }
    fn increment_unlocks(&self, today: NaiveDate) -> Result<i64> {
        let n=self.unlocks_today(today)?+1; self.set_setting(&format!("unlocks:{today}"),&n.to_string())?; Ok(n)
    }
    fn backup_now(&self) -> Result<std::path::PathBuf> {
        let dirs=ProjectDirs::from("com","ScreenTimeRS",APP_NAME).ok_or_else(||anyhow::anyhow!("cannot resolve local data directory"))?;
        let dir=dirs.data_local_dir().join("backups"); std::fs::create_dir_all(&dir)?;
        let target=dir.join(format!("ScreenTimeRS-auto-{}.db",Local::now().format("%Y%m%d-%H%M%S")));
        let c=self.conn()?; c.execute("VACUUM INTO ?1",params![target.to_string_lossy().to_string()])?;
        let mut files:Vec<_>=std::fs::read_dir(&dir)?.filter_map(Result::ok).map(|e|e.path()).filter(|x|x.file_name().and_then(|n|n.to_str()).map(|n|n.starts_with("ScreenTimeRS-auto-")).unwrap_or(false)).collect();
        files.sort(); while files.len()>7 { let _=std::fs::remove_file(files.remove(0)); }
        Ok(target)
    }
    fn clear_all_records(&self) -> Result<()> {
        let c=self.conn()?;
        c.execute("DELETE FROM usage_records",[])?;
        c.execute("DELETE FROM app_usage",[])?;
        c.execute("DELETE FROM sessions",[])?;
        c.execute("DELETE FROM hourly_usage",[])?;
        c.execute("DELETE FROM settings WHERE key LIKE 'unlocks:%'",[])?;
        Ok(())
    }
}

#[derive(Clone)]
struct UsageRecord {
    id: i64,
    start_at: String,
    end_at: Option<String>,
    record_date: String,
    app: String,
    exe_path: String,
    seconds: i64,
    precision: String,
    source: String,
    session_id: Option<i64>,
}

impl UsageRecord {
    fn start_datetime(&self) -> Option<DateTime<FixedOffset>> { parse_precise(&self.start_at) }
    fn end_datetime(&self) -> Option<DateTime<FixedOffset>> { self.end_at.as_deref().and_then(parse_precise) }
    fn start_date(&self) -> NaiveDate { NaiveDate::parse_from_str(&self.record_date,"%Y-%m-%d").unwrap_or_else(|_| self.start_datetime().map(|d| d.date_naive()).unwrap_or_else(|| Local::now().date_naive())) }
    fn end_date(&self) -> Option<NaiveDate> { if self.precision=="daily" { return Some(self.start_date()); } self.end_datetime().map(|x| x.date_naive()) }
    fn overlap_seconds(&self, start: NaiveDate, end: NaiveDate) -> i64 {
        if self.seconds<=0 || start>end { return 0; }
        if self.precision=="daily" {
            return if self.start_date()>=start && self.start_date()<=end { self.seconds } else { 0 };
        }
        let Some(rs)=self.start_datetime() else { return 0; };
        let re=self.end_datetime().unwrap_or_else(|| rs + ChronoDuration::seconds(self.seconds));
        let day_start=Local.from_local_datetime(&start.and_hms_opt(0,0,0).unwrap()).earliest().unwrap();
        let day_end=Local.from_local_datetime(&(end+ChronoDuration::days(1)).and_hms_opt(0,0,0).unwrap()).latest().unwrap();
        let os=rs.timestamp().max(day_start.timestamp());
        let oe=re.timestamp().min(day_end.timestamp());
        if oe<=os { return 0; }
        let span=(re.timestamp()-rs.timestamp()).max(1);
        ((self.seconds as i128)*(oe-os) as i128/span as i128) as i64
    }
}

fn parse_precise(text: &str) -> Option<DateTime<FixedOffset>> {
    DateTime::parse_from_rfc3339(text).ok()
}
fn parse_record_date(text: &str) -> Option<NaiveDate> {
    parse_precise(text).map(|d| d.date_naive()).or_else(|| NaiveDate::parse_from_str(text,"%Y-%m-%d").ok())
}
fn precise_seconds_for_day_tx(tx: &rusqlite::Transaction<'_>, app: &str, day: NaiveDate) -> Result<i64> {
    let mut st=tx.prepare("SELECT start_at,end_at,seconds FROM usage_records WHERE precision='precise' AND app=?1 AND start_at < ?2 AND (end_at IS NULL OR end_at > ?3)")?;
    let day_start=Local.from_local_datetime(&day.and_hms_opt(0,0,0).unwrap()).earliest().unwrap();
    let day_end=Local.from_local_datetime(&(day+ChronoDuration::days(1)).and_hms_opt(0,0,0).unwrap()).latest().unwrap();
    let start_rfc=day_start.to_rfc3339(); let end_rfc=day_end.to_rfc3339();
    let rows=st.query_map(params![app,end_rfc,start_rfc],|r|Ok((r.get::<_,String>(0)?,r.get::<_,Option<String>>(1)?,r.get::<_,i64>(2)?)))?;
    let mut total=0i64;
    for row in rows.flatten() {
        let (s,e,secs)=row; let Some(rs)=parse_precise(&s) else { continue; }; let re=e.as_deref().and_then(parse_precise).unwrap_or_else(||rs+ChronoDuration::seconds(secs));
        let os=rs.timestamp().max(day_start.timestamp()); let oe=re.timestamp().min(day_end.timestamp());
        if oe>os { let span=(re.timestamp()-rs.timestamp()).max(1); total += ((secs as i128)*(oe-os) as i128/span as i128) as i64; }
    }
    Ok(total)
}

#[cfg(windows)]
fn foreground(sys: &mut System) -> (String,String,String) {
    unsafe {
        let hwnd = GetForegroundWindow();
        if hwnd.0.is_null() { return (String::new(), String::new(), String::new()); }
        let mut pid=0u32;
        GetWindowThreadProcessId(hwnd, Some(&mut pid));
        if pid==0 { return (String::new(), String::new(), String::new()); }
        sys.refresh_processes(ProcessesToUpdate::Some(&[Pid::from_u32(pid)]), true);
        let mut exe_path = String::new();
        let mut name = sys.process(Pid::from_u32(pid))
            .map(|p| p.name().to_string_lossy().into_owned())
            .unwrap_or_else(|| "Unknown".into());

        // Prefer executable filename from the process image for stable app grouping.
        let h: HANDLE = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid).unwrap_or(HANDLE(std::ptr::null_mut()));
        if !h.0.is_null() {
            let mut buf=[0u16;1024]; let mut n=buf.len() as u32;
            if QueryFullProcessImageNameW(h, windows::Win32::System::Threading::PROCESS_NAME_FORMAT(0), windows::core::PWSTR(buf.as_mut_ptr()), &mut n).is_ok() {
                if let Some(path)=String::from_utf16(&buf[..n as usize]).ok() {
                    if let Some(file)=std::path::Path::new(&path).file_name() {
                        name=file.to_string_lossy().into_owned();
                        exe_path=path.clone();
                    }
                }
            }
            let _=CloseHandle(h);
        }
        let mut b=[0u16;512];
        let len=GetWindowTextW(hwnd,&mut b);
        (name,String::from_utf16_lossy(&b[..len as usize]),exe_path)
    }
}
#[cfg(not(windows))]
fn foreground(_: &mut System)->(String,String,String){("Unsupported".into(),"".into(),"".into())}

#[cfg(windows)]
fn idle_seconds() -> u64 {
    unsafe {
        let mut input = LASTINPUTINFO {
            cbSize: std::mem::size_of::<LASTINPUTINFO>() as u32,
            dwTime: 0,
        };
        if !GetLastInputInfo(&mut input).as_bool() {
            return 0;
        }
        let now = GetTickCount();
        now.wrapping_sub(input.dwTime) as u64 / 1000
    }
}
#[cfg(not(windows))]
fn idle_seconds() -> u64 { 0 }

#[cfg(windows)]
fn is_locked() -> bool {
    use std::ffi::c_void;

    // Windows switches the input desktop to a secure desktop while the
    // workstation is locked. This is more reliable than treating an empty
    // foreground window as proof of a lock.
    #[link(name = "user32")]
    extern "system" {
        fn OpenInputDesktop(dwFlags: u32, fInherit: i32, dwDesiredAccess: u32) -> *mut c_void;
        fn GetUserObjectInformationW(
            hObj: *mut c_void,
            nIndex: u32,
            pvInfo: *mut c_void,
            nLength: u32,
            lpnLen: *mut u32,
        ) -> i32;
        fn CloseDesktop(hDesktop: *mut c_void) -> i32;
    }

    const DESKTOP_READOBJECTS: u32 = 0x0001;
    const UOI_NAME: u32 = 2;

    unsafe {
        let desktop = OpenInputDesktop(0, 0, DESKTOP_READOBJECTS);
        if desktop.is_null() {
            let hwnd = GetForegroundWindow();
            return hwnd.0.is_null();
        }

        let mut buffer = [0u16; 128];
        let mut needed = 0u32;
        let ok = GetUserObjectInformationW(
            desktop,
            UOI_NAME,
            buffer.as_mut_ptr() as *mut c_void,
            (buffer.len() * std::mem::size_of::<u16>()) as u32,
            &mut needed,
        );
        let _ = CloseDesktop(desktop);

        if ok == 0 {
            let hwnd = GetForegroundWindow();
            return hwnd.0.is_null();
        }

        let units = (needed as usize / std::mem::size_of::<u16>()).min(buffer.len());
        let name = String::from_utf16_lossy(&buffer[..units]).trim_end_matches('\0').to_string();
        !name.eq_ignore_ascii_case("Default")
    }
}
#[cfg(not(windows))]
fn is_locked() -> bool { false }

const ACTIVE_IDLE_LIMIT_SECS: u64 = 300;
fn monitor_on(locked: bool, idle: u64) -> bool {
    !locked && idle < 60
}
fn active_now(locked: bool, idle: u64, app: &str) -> bool {
    !locked && idle < ACTIVE_IDLE_LIMIT_SECS && !app.is_empty()
}

#[derive(serde::Serialize, Clone)]
struct JsonApp { name: String, seconds: i64, exe_path: String }

#[derive(serde::Serialize)]
struct JsonDaily { label: String, seconds: i64 }

#[derive(Serialize, Deserialize)]
struct ExportRecord {
    date: String,
    app: String,
    seconds: i64,
    #[serde(default)]
    exe_path: String,
    // v0.8.0 unified exports can preserve precise session boundaries. Older
    // exports omit these fields and are imported as daily-precision records.
    #[serde(default)]
    start_at: Option<String>,
    #[serde(default)]
    end_at: Option<String>,
    #[serde(default)]
    precision: Option<String>,
    #[serde(default)]
    session_id: Option<i64>,
}

#[derive(Serialize, Deserialize)]
struct ExportFile {
    format_version: u32,
    application: String,
    exported_at: String,
    records: Vec<ExportRecord>,
}

#[derive(serde::Serialize)]
struct JsonSnapshot {
    current_app: String,
    current_window: String,
    today: i64,
    yesterday: i64,
    week: i64,
    month: i64,
    apps: Vec<JsonApp>,
    locked: bool,
    monitor_on: bool,
    active_now: bool,
    idle_seconds: u64,
    cpu: f32,
    memory_mb: u64,
    current_session_seconds: i64,
    longest_session_today: i64,
    unlocks_today: i64,
    paused: bool,
    hourly_today: Vec<JsonDaily>,
}

#[derive(serde::Serialize)]
struct JsonAnalytics {
    apps_week: Vec<JsonApp>,
    apps_month: Vec<JsonApp>,
    apps_half_year: Vec<JsonApp>,
    apps_year: Vec<JsonApp>,
    apps_90_days: Vec<JsonApp>,
    apps_30_days: Vec<JsonApp>,
    daily: Vec<JsonDaily>,
    daily_year: Vec<JsonDaily>,
}

#[derive(serde::Serialize)]
struct JsonDayUsage {
    date: String,
    total_seconds: i64,
    apps: Vec<JsonApp>,
}

#[derive(serde::Serialize)]
struct JsonRangeUsage {
    start_date: String,
    end_date: String,
    total_seconds: i64,
    apps: Vec<JsonApp>,
    daily: Vec<JsonDaily>,
}

#[derive(serde::Serialize)]
struct JsonAppHistory {
    app: String,
    total_seconds: i64,
    daily: Vec<JsonDaily>,
}

fn data_dir() -> Option<std::path::PathBuf> {
    ProjectDirs::from("com", "ScreenTimeRS", APP_NAME).map(|d| d.data_local_dir().to_path_buf())
}

fn snapshot_path() -> Option<std::path::PathBuf> {
    data_dir().map(|d| d.join("snapshot.json"))
}

fn analytics_path() -> Option<std::path::PathBuf> {
    data_dir().map(|d| d.join("analytics.json"))
}

fn shutdown_path() -> Option<std::path::PathBuf> {
    data_dir().map(|d| d.join(SHUTDOWN_FILE))
}

fn refresh_path() -> Option<std::path::PathBuf> {
    data_dir().map(|d| d.join(REFRESH_FILE))
}

fn pause_path() -> Option<std::path::PathBuf> { data_dir().map(|d| d.join(PAUSE_FILE)) }
fn is_paused() -> bool { pause_path().map(|p| p.exists()).unwrap_or(false) }
fn set_paused(paused: bool) { if let Some(p)=pause_path() { if paused { let _=std::fs::write(p,b"paused"); } else { let _=std::fs::remove_file(p); } } }

fn request_refresh() {
    if let Some(path) = refresh_path() {
        if let Some(parent) = path.parent() { let _ = std::fs::create_dir_all(parent); }
        let _ = std::fs::write(path, b"refresh");
    }
}

fn request_shutdown() {
    if let Some(path) = shutdown_path() {
        if let Some(parent) = path.parent() {
            let _ = std::fs::create_dir_all(parent);
        }
        let _ = std::fs::write(path, b"shutdown");
    }
}

fn write_json_atomic(path: std::path::PathBuf, text: String) {
    if let Some(parent) = path.parent() { let _ = std::fs::create_dir_all(parent); }
    let tmp = path.with_extension("tmp");
    if std::fs::write(&tmp, text).is_ok() {
        let _ = std::fs::remove_file(&path);
        let _ = std::fs::rename(&tmp, &path);
    }
}

fn write_snapshot(s: &Snapshot) {
    if let Some(path) = snapshot_path() {
        let data = JsonSnapshot {
            current_app: s.current_app.clone(),
            current_window: s.current_window.clone(),
            today: s.today,
            yesterday: s.yesterday,
            week: s.week,
            month: s.month,
            apps: s.apps.iter().map(|a| JsonApp { name: a.name.clone(), seconds: a.seconds, exe_path: a.exe_path.clone() }).collect(),
            locked: s.locked,
            monitor_on: s.monitor_on,
            active_now: s.active_now,
            idle_seconds: s.idle_seconds,
            cpu: s.cpu,
            memory_mb: s.memory_mb,
            current_session_seconds: s.current_session_seconds,
            longest_session_today: s.longest_session_today,
            unlocks_today: s.unlocks_today,
            paused: s.paused,
            hourly_today: s.hourly_today.iter().map(|(label,seconds)| JsonDaily { label: label.clone(), seconds: *seconds }).collect(),
        };
        if let Ok(text) = serde_json::to_string(&data) {
            write_json_atomic(path, text);
        }
    }
}

fn write_analytics(s: &Snapshot) {
    if let Some(path) = analytics_path() {
        let data = JsonAnalytics {
            apps_week: s.apps_week.iter().map(|a| JsonApp { name: a.name.clone(), seconds: a.seconds, exe_path: a.exe_path.clone() }).collect(),
            apps_month: s.apps_month.iter().map(|a| JsonApp { name: a.name.clone(), seconds: a.seconds, exe_path: a.exe_path.clone() }).collect(),
            apps_half_year: s.apps_half_year.iter().map(|a| JsonApp { name: a.name.clone(), seconds: a.seconds, exe_path: a.exe_path.clone() }).collect(),
            apps_year: s.apps_year.iter().map(|a| JsonApp { name: a.name.clone(), seconds: a.seconds, exe_path: a.exe_path.clone() }).collect(),
            apps_90_days: s.apps_90_days.iter().map(|a| JsonApp { name: a.name.clone(), seconds: a.seconds, exe_path: a.exe_path.clone() }).collect(),
            apps_30_days: s.apps_30_days.iter().map(|a| JsonApp { name: a.name.clone(), seconds: a.seconds, exe_path: a.exe_path.clone() }).collect(),
            daily: s.daily.iter().map(|(label, seconds)| JsonDaily { label: label.clone(), seconds: *seconds }).collect(),
            daily_year: s.daily_year.iter().map(|(label, seconds)| JsonDaily { label: label.clone(), seconds: *seconds }).collect(),
        };
        if let Ok(text) = serde_json::to_string(&data) {
            write_json_atomic(path, text);
        }
    }
}

struct Collector;
impl Collector {
    fn start(db: Arc<Database>, snap: Arc<Mutex<Snapshot>>) {
        thread::spawn(move || {
            let mut sys = System::new();
            let mut last = Instant::now();
            let mut refresh_counter = ANALYTICS_REFRESH_INTERVAL;
            let mut cached_week = Vec::new();
            let mut cached_month = Vec::new();
            let mut cached_half_year = Vec::new();
            let mut cached_year = Vec::new();
            let mut cached_90_days = Vec::new();
            let mut cached_30_days = Vec::new();
            let mut cached_daily = Vec::new();
            let mut cached_daily_year = Vec::new();
            let mut analytics_initialized = false;
            // One continuous computer-activity session may contain multiple app
            // segments. Switching apps no longer resets session_seconds.
            let mut activity_session_id: Option<i64> = None;
            let mut app_segment_id: Option<i64> = None;
            let mut session_app = String::new();
            let mut session_exe = String::new();
            let mut session_seconds: i64 = 0;
            let mut segment_seconds: i64 = 0;
            let mut was_locked = false;
            let mut last_backup_day: Option<NaiveDate> = None;

            loop {
                thread::sleep(Duration::from_secs(1));
                if shutdown_path().map(|p| p.exists()).unwrap_or(false) {
                    return;
                }
                let force_refresh = refresh_path().map(|p| {
                    if p.exists() { let _ = std::fs::remove_file(&p); true } else { false }
                }).unwrap_or(false);

                let elapsed = last.elapsed();
                last = Instant::now();
                // Count only elapsed sampling time, capped to one extra second so
                // suspend/resume gaps never turn into huge usage blocks.
                let sec = elapsed.as_secs().clamp(1, 2) as i64;
                let locked = is_locked();
                let idle = idle_seconds();
                let (app, title, exe_path) = foreground(&mut sys);
                let paused = is_paused();
                let active = !paused && active_now(locked, idle, &app);
                let now_local = Local::now();
                let today_date = now_local.date_naive();
                if !was_locked && locked { let _ = db.set_setting("last_lock", &now_local.to_rfc3339()); }
                if was_locked && !locked { let _ = db.increment_unlocks(today_date); }
                was_locked = locked;
                if active {
                    if activity_session_id.is_none() {
                        session_seconds = 0;
                        segment_seconds = 0;
                        session_app = app.clone();
                        session_exe = exe_path.clone();
                        match db.start_activity_session(&app, &exe_path, &now_local.to_rfc3339()) {
                            Ok((session_id, segment_id)) => {
                                activity_session_id = Some(session_id);
                                app_segment_id = Some(segment_id);
                            }
                            Err(_) => {
                                activity_session_id = None;
                                app_segment_id = None;
                            }
                        }
                    } else if session_app != app || session_exe != exe_path {
                        // Switching foreground applications does not end the
                        // continuous computer-activity session.
                        if let Some(id) = app_segment_id.take() {
                            let _ = db.update_session(id, segment_seconds, Some(&now_local.to_rfc3339()));
                        }
                        if let Some(session_id) = activity_session_id {
                            session_app = app.clone();
                            session_exe = exe_path.clone();
                            segment_seconds = 0;
                            app_segment_id = db.start_activity_segment(
                                session_id, &app, &exe_path, &now_local.to_rfc3339()
                            ).ok();
                        }
                    }

                    session_seconds += sec;
                    segment_seconds += sec;
                    if let Some(id) = app_segment_id {
                        let _ = db.update_session(id, segment_seconds, Some(&now_local.to_rfc3339()));
                    }
                } else if activity_session_id.take().is_some() {
                    if let Some(id) = app_segment_id.take() {
                        let _ = db.update_session(id, segment_seconds, Some(&now_local.to_rfc3339()));
                    }
                    session_seconds = 0;
                    segment_seconds = 0;
                    session_app.clear();
                    session_exe.clear();
                }
                if last_backup_day != Some(today_date) && !paused {
                    if db.backup_now().is_ok() { last_backup_day=Some(today_date); }
                }

                sys.refresh_cpu_usage();
                sys.refresh_memory();

                let now = Local::now();
                let today = now.date_naive();
                let yesterday = today - ChronoDuration::days(1);
                let month_start = today.with_day(1).unwrap();
                let week_start = today - ChronoDuration::days(today.weekday().num_days_from_monday() as i64);
                let six_months_ago = today.checked_sub_months(Months::new(6)).unwrap_or(today);
                let ninety_start = today - ChronoDuration::days(89);
                let thirty_start = today - ChronoDuration::days(29);
                let year_ago = today.checked_sub_months(Months::new(12)).unwrap_or(today);

                refresh_counter = refresh_counter.saturating_sub(1);
                if force_refresh || refresh_counter == 0 || !analytics_initialized {
                    cached_week = db.apps_range(week_start, today).unwrap_or_default();
                    cached_month = db.apps_range(month_start, today).unwrap_or_default();
                    cached_half_year = db.apps_range(six_months_ago, today).unwrap_or_default();
                    cached_year = db.apps_range(year_ago, today).unwrap_or_default();
                    cached_90_days = db.apps_range(ninety_start, today).unwrap_or_default();
                    cached_30_days = db.apps_range(thirty_start, today).unwrap_or_default();
                    cached_daily = db.daily(30).unwrap_or_default();
                    cached_daily_year = db.daily(365).unwrap_or_default();
                    refresh_counter = ANALYTICS_REFRESH_INTERVAL;
                    analytics_initialized = true;
                }

                let apps = db.apps_today().unwrap_or_default();
                let snapshot = Snapshot {
                    current_app: app,
                    current_window: title,
                    today: db.range_total(today, today).unwrap_or(0),
                    yesterday: db.range_total(yesterday, yesterday).unwrap_or(0),
                    week: db.range_total(week_start, today).unwrap_or(0),
                    month: db.range_total(month_start, today).unwrap_or(0),
                    apps,
                    apps_week: cached_week.clone(),
                    apps_month: cached_month.clone(),
                    apps_half_year: cached_half_year.clone(),
                    apps_year: cached_year.clone(),
                    apps_90_days: cached_90_days.clone(),
                    apps_30_days: cached_30_days.clone(),
                    apps_all: Vec::new(),
                    daily: cached_daily.clone(),
                    daily_year: cached_daily_year.clone(),
                    daily_all: Vec::new(),
                    locked,
                    monitor_on: !paused && monitor_on(locked, idle),
                    active_now: active,
                    idle_seconds: idle,
                    cpu: sys.global_cpu_usage(),
                    memory_mb: sys.used_memory() / 1024 / 1024,
                    current_session_seconds: session_seconds,
                    longest_session_today: db.session_stats_today(today).unwrap_or((0,0)).1,
                    unlocks_today: db.unlocks_today(today).unwrap_or(0),
                    paused,
                    hourly_today: db.hourly_today(today).unwrap_or_default(),
                };

                if let Ok(mut current) = snap.lock() {
                    *current = snapshot;
                    write_snapshot(&current);
                    if force_refresh || refresh_counter == ANALYTICS_REFRESH_INTERVAL {
                        write_analytics(&current);
                    }
                }
            }
        });
    }
}

fn tray_icon_rgba() -> Vec<u8> {
    // Compact monitor glyph matching the ScreenTime RS application identity.
    let mut rgba=vec![0u8; 32*32*4];
    let set = |rgba: &mut Vec<u8>, x: usize, y: usize, color: [u8;4]| {
        if x < 32 && y < 32 {
            let i = (y*32 + x)*4;
            rgba[i..i+4].copy_from_slice(&color);
        }
    };

    let blue = [31, 149, 229, 255];
    let dark = [34, 42, 52, 255];

    // Rounded monitor frame.
    for y in 5..23 {
        for x in 5..27 {
            let edge = y == 5 || y == 22 || x == 5 || x == 26;
            let corner = (x == 5 || x == 26) && (y == 5 || y == 6 || y == 21 || y == 22);
            if edge && !corner {
                set(&mut rgba, x, y, blue);
            }
        }
    }
    for y in 7..21 {
        for x in 7..25 {
            set(&mut rgba, x, y, dark);
        }
    }

    // Stand and base.
    for y in 23..27 {
        for x in 14..18 {
            set(&mut rgba, x, y, blue);
        }
    }
    for y in 27..29 {
        for x in 11..21 {
            set(&mut rgba, x, y, blue);
        }
    }

    rgba
}

fn tray_icon() -> Icon {
    Icon::from_rgba(tray_icon_rgba(), 32, 32).unwrap()
}

fn tray_menu_icon() -> MenuIcon {
    MenuIcon::from_rgba(tray_icon_rgba(), 32, 32).unwrap()
}

fn launch_ui() {
    if let Ok(exe) = std::env::current_exe() {
        if let Some(dir) = exe.parent() {
            let ui = dir.join("ScreenTimeRS.UI.exe");
            if ui.exists() { let _ = std::process::Command::new(ui).spawn(); }
        }
    }
}


fn print_json<T: Serialize>(value: &T) -> Result<()> {
    println!("{}", serde_json::to_string(value)?);
    Ok(())
}

fn parse_date_arg(text: &str) -> Result<NaiveDate> {
    NaiveDate::parse_from_str(text, "%Y-%m-%d")
        .map_err(|_| anyhow::anyhow!("invalid date"))
}

fn handle_cli(args: &[String]) -> Result<bool> {
    match args.get(1).map(String::as_str) {
        Some("--export-data") => {
            let path = args.get(2).ok_or_else(|| anyhow::anyhow!("missing export path"))?;
            let db = Database::new()?;
            let export = ExportFile {
                format_version: EXPORT_FORMAT_VERSION,
                application: APP_NAME.to_string(),
                exported_at: Local::now().to_rfc3339(),
                records: db.export_records()?,
            };
            let target = std::path::PathBuf::from(path);
            if let Some(parent) = target.parent() {
                if !parent.as_os_str().is_empty() { std::fs::create_dir_all(parent)?; }
            }
            let text = serde_json::to_string_pretty(&export)?;
            std::fs::write(target, text)?;
            Ok(true)
        }
        Some("--import-data") => {
            let path = args.get(2).ok_or_else(|| anyhow::anyhow!("missing import path"))?;
            let text = std::fs::read_to_string(path)?;
            let export: ExportFile = serde_json::from_str(&text)?;
            if export.format_version != EXPORT_FORMAT_VERSION {
                anyhow::bail!("unsupported usage data format");
            }
            if !export.application.is_empty() && export.application != APP_NAME {
                anyhow::bail!("unsupported application data");
            }
            Database::validate_records(&export.records)?;
            let db = Database::new()?;
            let _backup = db.backup_before_import()?;
            db.replace_all_records(&export.records)?;
            request_refresh();
            Ok(true)
        }
        Some("--clear-data") => {
            let token = args.get(2).map(String::as_str).unwrap_or("");
            if token.len() < 6 || token.len() > 32 {
                anyhow::bail!("invalid confirmation token");
            }
            let db = Database::new()?;
            db.clear_all_records()?;
            request_refresh();
            Ok(true)
        }
        Some("--query-day") => {
            let date = parse_date_arg(args.get(2).ok_or_else(|| anyhow::anyhow!("missing date"))?)?;
            let db = Database::new()?;
            let apps = db.apps_for_day(date)?;
            let total_seconds = apps.iter().map(|app| app.seconds).sum();
            let response = JsonDayUsage {
                date: date.to_string(),
                total_seconds,
                apps: apps.into_iter().map(|a| JsonApp { name: a.name, seconds: a.seconds, exe_path: a.exe_path }).collect(),
            };
            print_json(&response)?;
            Ok(true)
        }
        Some("--query-range") => {
            let start = parse_date_arg(args.get(2).ok_or_else(|| anyhow::anyhow!("missing start date"))?)?;
            let end = parse_date_arg(args.get(3).ok_or_else(|| anyhow::anyhow!("missing end date"))?)?;
            if start > end { anyhow::bail!("invalid date range"); }
            let db = Database::new()?;
            let (apps, daily) = db.range_usage(start, end)?;
            let total_seconds = daily.iter().map(|(_, seconds)| *seconds).sum();
            let response = JsonRangeUsage {
                start_date: start.to_string(),
                end_date: end.to_string(),
                total_seconds,
                apps: apps.into_iter().map(|a| JsonApp { name: a.name, seconds: a.seconds, exe_path: a.exe_path }).collect(),
                daily: daily.into_iter().map(|(label, seconds)| JsonDaily { label, seconds }).collect(),
            };
            print_json(&response)?;
            Ok(true)
        }
        Some("--query-all") => {
            let db = Database::new()?;
            let today = Local::now().date_naive();
            let start = db.all_time_start()?.unwrap_or(today);
            let (apps, daily) = db.range_usage(start, today)?;
            let total_seconds = daily.iter().map(|(_, seconds)| *seconds).sum();
            let response = JsonRangeUsage {
                start_date: start.to_string(),
                end_date: today.to_string(),
                total_seconds,
                apps: apps.into_iter().map(|a| JsonApp { name: a.name, seconds: a.seconds, exe_path: a.exe_path }).collect(),
                daily: daily.into_iter().map(|(label, seconds)| JsonDaily { label, seconds }).collect(),
            };
            print_json(&response)?;
            Ok(true)
        }
        Some("--query-app") => {
            let app = args.get(2).ok_or_else(|| anyhow::anyhow!("missing app name"))?;
            let db = Database::new()?;
            let (start, end) = match (args.get(3), args.get(4)) {
                (Some(start), Some(end)) => (parse_date_arg(start)?, parse_date_arg(end)?),
                (None, None) => (db.all_time_start()?.unwrap_or_else(|| Local::now().date_naive()), Local::now().date_naive()),
                _ => anyhow::bail!("invalid app date range"),
            };
            let daily = db.app_history(app, start, end)?;
            let total_seconds = daily.iter().map(|(_, seconds)| *seconds).sum();
            let response = JsonAppHistory {
                app: app.clone(),
                total_seconds,
                daily: daily.into_iter().map(|(label, seconds)| JsonDaily { label, seconds }).collect(),
            };
            print_json(&response)?;
            Ok(true)
        }
        Some("--pause") => { set_paused(true); Ok(true) }
        Some("--resume") => { set_paused(false); request_refresh(); Ok(true) }
        Some("--backup") => { let db=Database::new()?; println!("{}", db.backup_now()?.display()); Ok(true) }
        Some("--data-dir") => { if let Some(p)=data_dir(){ println!("{}",p.display()); } Ok(true) }
        Some("--diagnostics") => { let db=Database::new()?; let today=Local::now().date_naive(); let (active,longest)=db.session_stats_today(today)?; let records=db.export_records()?.len(); let size=std::fs::metadata(&db.path).map(|m|m.len()).unwrap_or(0); println!("{{\"version\":\"0.8.0\",\"data_dir\":\"{}\",\"database_bytes\":{},\"records\":{},\"active_session_seconds_today\":{},\"longest_session_seconds_today\":{},\"paused\":{}}}", data_dir().map(|p|p.display().to_string()).unwrap_or_default().replace('\\', "/"),size,records,active,longest,is_paused()); Ok(true) }
        _ => Ok(false),
    }
}

fn main()->Result<()> {
    let args: Vec<String> = std::env::args().collect();
    if handle_cli(&args)? {
        return Ok(());
    }

    let db=Arc::new(Database::new()?);
    let snap=Arc::new(Mutex::new(Snapshot::default()));

    // Create an initial snapshot immediately so the WinUI frontend never starts blank
    // while the background collector is waiting for its first sampling interval.
    {
        let today=Local::now().date_naive();
        let yesterday=today-ChronoDuration::days(1);
        let week_start=today-ChronoDuration::days(today.weekday().num_days_from_monday() as i64);
        let month_start=today.with_day(1).unwrap();
        let six_months_ago=today.checked_sub_months(Months::new(6)).unwrap_or(today);
        let year_ago=today.checked_sub_months(Months::new(12)).unwrap_or(today);
        let initial=Snapshot {
            today:db.range_total(today,today).unwrap_or(0),
            yesterday:db.range_total(yesterday,yesterday).unwrap_or(0),
            week:db.range_total(week_start,today).unwrap_or(0),
            month:db.range_total(month_start,today).unwrap_or(0),
            apps:db.apps_today().unwrap_or_default(),
            apps_week:db.apps_range(week_start,today).unwrap_or_default(),
            apps_month:db.apps_range(month_start,today).unwrap_or_default(),
            apps_half_year:db.apps_range(six_months_ago,today).unwrap_or_default(),
            apps_year:db.apps_range(year_ago,today).unwrap_or_default(),
            apps_90_days:db.apps_range(today-ChronoDuration::days(89),today).unwrap_or_default(),
            apps_30_days:db.apps_range(today-ChronoDuration::days(29),today).unwrap_or_default(),
            apps_all:Vec::new(),
            daily:db.daily(30).unwrap_or_default(),
            daily_year:db.daily(365).unwrap_or_default(),
            daily_all:Vec::new(),
            hourly_today:db.hourly_today(today).unwrap_or_default(),
            longest_session_today:db.session_stats_today(today).unwrap_or((0,0)).1,
            unlocks_today:db.unlocks_today(today).unwrap_or(0),
            paused:is_paused(),
            ..Snapshot::default()
        };
        if let Ok(mut current)=snap.lock() {
            *current=initial;
            write_snapshot(&current);
            write_analytics(&current);
        }
    }

    let background = std::env::args().any(|a| a == "--background");

    // The Rust process owns both the collector and the tray. This keeps the
    // lifecycle single-owner: a normal launch starts the collector and UI,
    // while --background starts only the collector/tray for Windows startup.
    Collector::start(db.clone(), snap.clone());

    let menu=Menu::new();
    let show=IconMenuItem::new("ScreenTime RS", true, Some(tray_menu_icon()), None);
    let quit=tray_icon::menu::MenuItem::new("退出",true,None);
    menu.append(&show)?;
    menu.append(&PredefinedMenuItem::separator())?;
    menu.append(&quit)?;

    // Keep the tray object alive for the entire process lifetime.
    // Explicitly disable left-click menu behavior so the standard Windows
    // right-click context menu always contains the two actions above.
    let tray=TrayIconBuilder::new()
        .with_icon(tray_icon())
        .with_tooltip(APP_NAME)
        .with_menu(Box::new(menu))
        .with_menu_on_left_click(false)
        .build()?;
    let tray_id=tray.id().clone();
    let show_id=show.id().clone();
    let quit_id=quit.id().clone();

    if !background { launch_ui(); }

    // Windows tray-icon requires a native Windows message loop on the same
    // thread that owns the TrayIcon. Without pumping messages, the icon can
    // appear but its context menu and menu clicks are not delivered.
    loop {
        #[cfg(windows)]
        unsafe {
            let mut msg = MSG::default();
            while PeekMessageW(&mut msg, None, 0, 0, PM_REMOVE).as_bool() {
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
        }

        while let Ok(e)=TrayIconEvent::receiver().try_recv() {
            #[cfg(windows)]
            if let TrayIconEvent::DoubleClick { id, button: MouseButton::Left, .. } = e {
                if id == tray_id {
                    launch_ui();
                }
            }
        }

        while let Ok(e)=MenuEvent::receiver().try_recv() {
            if e.id==show_id {
                launch_ui();
            } else if e.id==quit_id {
                request_shutdown();
                return Ok(());
            }
        }

        thread::sleep(Duration::from_millis(20));
    }
}
