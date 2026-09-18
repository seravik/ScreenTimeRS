#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use anyhow::Result;
use chrono::{Datelike, Duration as ChronoDuration, Local, Months, NaiveDate};
use directories::ProjectDirs;
use rusqlite::{params, Connection};
use std::{sync::{Arc, Mutex}, thread, time::{Duration, Instant}};
use sysinfo::{Pid, ProcessesToUpdate, System};
use tray_icon::{menu::{Menu, MenuEvent, MenuItem, PredefinedMenuItem}, Icon, TrayIconBuilder};

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
        let c = Connection::open(&db.path)?;
        c.execute_batch(
            "CREATE TABLE IF NOT EXISTS app_usage(
               date TEXT NOT NULL, app TEXT NOT NULL, seconds INTEGER NOT NULL DEFAULT 0,
               exe_path TEXT NOT NULL DEFAULT '',
               PRIMARY KEY(date,app));
             CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY,value TEXT NOT NULL);"
        )?;
        let _ = c.execute("ALTER TABLE app_usage ADD COLUMN exe_path TEXT NOT NULL DEFAULT ''", []);
        Ok(db)
    }
    fn conn(&self) -> Result<Connection> { Ok(Connection::open(&self.path)?) }
    fn add(&self, date: &str, app: &str, seconds: i64, exe_path: &str) -> Result<()> {
        if seconds <= 0 || app.is_empty() { return Ok(()); }
        let c = self.conn()?;
        c.execute(
            "INSERT INTO app_usage(date,app,seconds,exe_path) VALUES(?1,?2,?3,?4)
             ON CONFLICT(date,app) DO UPDATE SET seconds=seconds+excluded.seconds, exe_path=CASE WHEN excluded.exe_path<>'' THEN excluded.exe_path ELSE app_usage.exe_path END",
            params![date, app, seconds, exe_path])?;
        Ok(())
    }
    fn range_total(&self, start: NaiveDate, end: NaiveDate) -> Result<i64> {
        let c = self.conn()?;
        Ok(c.query_row(
            "SELECT COALESCE(SUM(seconds),0) FROM app_usage WHERE date>=?1 AND date<=?2",
            params![start.to_string(), end.to_string()], |r| r.get(0))?)
    }
    fn apps_today(&self) -> Result<Vec<AppStat>> {
        let today = Local::now().date_naive();
        self.apps_range(today, today)
    }

    fn apps_range(&self, start: NaiveDate, end: NaiveDate) -> Result<Vec<AppStat>> {
        let c = self.conn()?;
        let mut st = c.prepare(
            "SELECT app,SUM(seconds),MAX(exe_path) FROM app_usage
             WHERE date>=?1 AND date<=?2
             GROUP BY app ORDER BY SUM(seconds) DESC")?;
        let rows = st.query_map(params![start.to_string(), end.to_string()], |r| Ok(AppStat {
            name: r.get(0)?, seconds: r.get(1)?, exe_path: r.get(2).unwrap_or_default()
        }))?;
        Ok(rows.filter_map(Result::ok).collect())
    }

    fn apps_all(&self) -> Result<Vec<AppStat>> {
        let c = self.conn()?;
        let mut st = c.prepare(
            "SELECT app,SUM(seconds),MAX(exe_path) FROM app_usage
             GROUP BY app ORDER BY SUM(seconds) DESC")?;
        let rows = st.query_map([], |r| Ok(AppStat {
            name: r.get(0)?, seconds: r.get(1)?, exe_path: r.get(2).unwrap_or_default()
        }))?;
        Ok(rows.filter_map(Result::ok).collect())
    }
    fn daily(&self, days: i64) -> Result<Vec<(String,i64)>> {
        let today = Local::now().date_naive();
        let start = today - ChronoDuration::days(days.saturating_sub(1));
        self.daily_range(start, today)
    }

    fn daily_range(&self, start: NaiveDate, end: NaiveDate) -> Result<Vec<(String,i64)>> {
        if start > end { return Ok(Vec::new()); }
        let c = self.conn()?;
        let mut totals = std::collections::HashMap::<NaiveDate, i64>::new();
        let mut st = c.prepare(
            "SELECT date, COALESCE(SUM(seconds),0) FROM app_usage WHERE date>=?1 AND date<=?2 GROUP BY date ORDER BY date"
        )?;
        let rows = st.query_map(params![start.to_string(), end.to_string()], |r| {
            let date: String = r.get(0)?;
            let seconds: i64 = r.get(1)?;
            Ok((date, seconds))
        })?;
        for row in rows.flatten() {
            if let Ok(date) = NaiveDate::parse_from_str(&row.0, "%Y-%m-%d") {
                totals.insert(date, row.1);
            }
        }

        let span = (end - start).num_days().max(0) + 1;
        let mut out = Vec::with_capacity(span as usize);
        for i in 0..span {
            let d = start + ChronoDuration::days(i);
            out.push((d.format("%m-%d").to_string(), *totals.get(&d).unwrap_or(&0)));
        }
        Ok(out)
    }

    fn daily_all(&self) -> Result<Vec<(String,i64)>> {
        let today = Local::now().date_naive();
        let c = self.conn()?;
        let min_date: Option<String> = c.query_row(
            "SELECT MIN(date) FROM app_usage",
            [],
            |r| r.get(0)
        )?;
        let Some(start_text) = min_date else { return Ok(Vec::new()); };
        let Ok(start) = NaiveDate::parse_from_str(&start_text, "%Y-%m-%d") else {
            return Ok(Vec::new());
        };
        if start > today { return Ok(Vec::new()); }

        let mut totals = std::collections::HashMap::<NaiveDate, i64>::new();
        let mut st = c.prepare(
            "SELECT date, COALESCE(SUM(seconds),0) FROM app_usage WHERE date>=?1 AND date<=?2 GROUP BY date ORDER BY date"
        )?;
        let rows = st.query_map(params![start.to_string(), today.to_string()], |r| {
            let date: String = r.get(0)?;
            let seconds: i64 = r.get(1)?;
            Ok((date, seconds))
        })?;
        for row in rows.flatten() {
            if let Ok(date) = NaiveDate::parse_from_str(&row.0, "%Y-%m-%d") {
                totals.insert(date, row.1);
            }
        }

        let span = (today - start).num_days().max(0) + 1;
        let mut out = Vec::with_capacity(span as usize);
        for i in 0..span {
            let d = start + ChronoDuration::days(i);
            out.push((d.format("%Y-%m-%d").to_string(), *totals.get(&d).unwrap_or(&0)));
        }
        Ok(out)
    }

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
    unsafe {
        let hwnd = windows::Win32::UI::WindowsAndMessaging::GetForegroundWindow();
        hwnd.0.is_null()
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

#[derive(serde::Serialize)]
struct JsonSnapshot {
    current_app: String, current_window: String, today: i64, yesterday: i64, week: i64, month: i64,
    apps: Vec<JsonApp>, apps_week: Vec<JsonApp>, apps_month: Vec<JsonApp>,
    apps_half_year: Vec<JsonApp>, apps_year: Vec<JsonApp>, apps_90_days: Vec<JsonApp>, apps_30_days: Vec<JsonApp>, apps_all: Vec<JsonApp>,
    daily: Vec<JsonDaily>, daily_year: Vec<JsonDaily>, daily_all: Vec<JsonDaily>, locked: bool, monitor_on: bool, active_now: bool, idle_seconds: u64, cpu: f32, memory_mb: u64,
}

fn data_dir() -> Option<std::path::PathBuf> {
    ProjectDirs::from("com", "ScreenTimeRS", APP_NAME).map(|d| d.data_local_dir().to_path_buf())
}

fn snapshot_path() -> Option<std::path::PathBuf> {
    data_dir().map(|d| d.join("snapshot.json"))
}

fn shutdown_path() -> Option<std::path::PathBuf> {
    data_dir().map(|d| d.join(SHUTDOWN_FILE))
}

fn request_shutdown() {
    if let Some(path) = shutdown_path() {
        if let Some(parent) = path.parent() {
            let _ = std::fs::create_dir_all(parent);
        }
        let _ = std::fs::write(path, b"shutdown");
    }
}

fn write_snapshot(s: &Snapshot) {
    if let Some(path) = snapshot_path() {
        if let Some(parent) = path.parent() { let _ = std::fs::create_dir_all(parent); }
        let data = JsonSnapshot {
            current_app: s.current_app.clone(), current_window: s.current_window.clone(),
            today:s.today, yesterday:s.yesterday, week:s.week, month:s.month,
            apps:s.apps.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            apps_week:s.apps_week.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            apps_month:s.apps_month.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            apps_half_year:s.apps_half_year.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            apps_year:s.apps_year.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            apps_90_days:s.apps_90_days.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            apps_30_days:s.apps_30_days.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            apps_all:s.apps_all.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            daily:s.daily.iter().map(|(label,seconds)| JsonDaily{label:label.clone(),seconds:*seconds}).collect(),
            daily_year:s.daily_year.iter().map(|(label,seconds)| JsonDaily{label:label.clone(),seconds:*seconds}).collect(),
            daily_all:s.daily_all.iter().map(|(label,seconds)| JsonDaily{label:label.clone(),seconds:*seconds}).collect(), locked:s.locked, monitor_on:s.monitor_on, active_now:s.active_now, idle_seconds:s.idle_seconds, cpu:s.cpu, memory_mb:s.memory_mb,
        };
        let tmp = path.with_extension("json.tmp");
        if let Ok(text) = serde_json::to_string(&data) {
            if std::fs::write(&tmp, text).is_ok() {
                // Windows does not replace an existing destination with rename().
                // Remove the previous snapshot first so the collector can keep
                // updating the file after the initial snapshot has been created.
                let _ = std::fs::remove_file(&path);
                let _ = std::fs::rename(&tmp, &path);
            }
        }
    }
}

struct Collector;
impl Collector {
    fn start(db: Arc<Database>, snap: Arc<Mutex<Snapshot>>) {
        thread::spawn(move || {
            let mut sys = System::new();
            let mut last = Instant::now();
            let mut refresh_counter = 0u8;
            let mut cached_week = Vec::new();
            let mut cached_month = Vec::new();
            let mut cached_half_year = Vec::new();
            let mut cached_year = Vec::new();
            let mut cached_90_days = Vec::new();
            let mut cached_30_days = Vec::new();
            let mut cached_all = Vec::new();
            let mut cached_daily_year = Vec::new();
            let mut cached_daily_all = Vec::new();

            loop {
                thread::sleep(Duration::from_secs(1));
                if shutdown_path().map(|p| p.exists()).unwrap_or(false) {
                    return;
                }

                let sec = last.elapsed().as_secs().clamp(1, 2) as i64;
                last = Instant::now();
                let locked = is_locked();
                let idle = idle_seconds();
                let (app, title, exe_path) = foreground(&mut sys);
                let active = active_now(locked, idle, &app);
                if active {
                    let _ = db.add(&Local::now().date_naive().to_string(), &app, sec, &exe_path);
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

                refresh_counter = refresh_counter.wrapping_add(1);
                // Historical application lists are comparatively expensive; refresh them
                // every five seconds while keeping today's data and system metrics live.
                if refresh_counter % 5 == 0 || (cached_week.is_empty() && cached_all.is_empty()) {
                    cached_week = db.apps_range(week_start, today).unwrap_or_default();
                    cached_month = db.apps_range(month_start, today).unwrap_or_default();
                    cached_half_year = db.apps_range(six_months_ago, today).unwrap_or_default();
                    cached_year = db.apps_range(year_ago, today).unwrap_or_default();
                    cached_90_days = db.apps_range(ninety_start, today).unwrap_or_default();
                    cached_30_days = db.apps_range(thirty_start, today).unwrap_or_default();
                    cached_all = db.apps_all().unwrap_or_default();
                    cached_daily_year = db.daily(365).unwrap_or_default();
                    cached_daily_all = db.daily_all().unwrap_or_default();
                }

                let apps = db.apps_today().unwrap_or_default();
                let daily = db.daily(30).unwrap_or_default();
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
                    apps_all: cached_all.clone(),
                    daily,
                    daily_year: cached_daily_year.clone(),
                    daily_all: cached_daily_all.clone(),
                    locked,
                    monitor_on: monitor_on(locked, idle),
                    active_now: active,
                    idle_seconds: idle,
                    cpu: sys.global_cpu_usage(),
                    memory_mb: sys.used_memory() / 1024 / 1024,
                };

                if let Ok(mut current) = snap.lock() {
                    *current = snapshot;
                    write_snapshot(&current);
                }
            }
        });
    }
}

fn tray_icon()->Icon {
    let mut rgba=Vec::with_capacity(32*32*4);
    for y in 0..32 { for x in 0..32 {
        let dx=x as i32-16; let dy=y as i32-16;
        let on=dx*dx+dy*dy<13*13;
        rgba.extend_from_slice(if on { &[45,145,220,255] } else { &[0,0,0,0] });
    }}
    Icon::from_rgba(rgba,32,32).unwrap()
}

fn launch_ui() {
    if let Ok(exe) = std::env::current_exe() {
        if let Some(dir) = exe.parent() {
            let ui = dir.join("ScreenTimeRS.UI.exe");
            if ui.exists() { let _ = std::process::Command::new(ui).spawn(); }
        }
    }
}

fn main()->Result<()> {
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
            apps_all:db.apps_all().unwrap_or_default(),
            daily:db.daily(30).unwrap_or_default(),
            daily_year:db.daily(365).unwrap_or_default(),
            daily_all:db.daily_all().unwrap_or_default(),
            ..Snapshot::default()
        };
        if let Ok(mut current)=snap.lock() {
            *current=initial;
            write_snapshot(&current);
        }
    }

    let background = std::env::args().any(|a| a == "--background");

    // The Rust process owns both the collector and the tray. This keeps the
    // lifecycle single-owner: a normal launch starts the collector and UI,
    // while --background starts only the collector/tray for Windows startup.
    Collector::start(db.clone(), snap.clone());

    let menu=Menu::new();
    let show=MenuItem::new("打开 ScreenTime RS",true,None);
    let quit=MenuItem::new("退出 ScreenTime RS",true,None);
    menu.append(&show)?;
    menu.append(&PredefinedMenuItem::separator())?;
    menu.append(&quit)?;

    // Keep the tray object alive for the entire process lifetime.
    // Explicitly disable left-click menu behavior so the standard Windows
    // right-click context menu always contains the two actions above.
    let _tray=TrayIconBuilder::new()
        .with_icon(tray_icon())
        .with_tooltip(APP_NAME)
        .with_menu(Box::new(menu))
        .with_menu_on_left_click(false)
        .build()?;
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
