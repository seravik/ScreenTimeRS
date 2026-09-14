#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use anyhow::Result;
use chrono::{Datelike, Duration as ChronoDuration, Local, NaiveDate};
use directories::ProjectDirs;
use rusqlite::{params, Connection};
use std::{sync::{Arc, Mutex}, thread, time::{Duration, Instant}};
use sysinfo::{Pid, ProcessesToUpdate, System};
use tray_icon::{menu::{Menu, MenuEvent, MenuItem, PredefinedMenuItem}, Icon, TrayIconBuilder};
use winreg::{enums::HKEY_CURRENT_USER, RegKey};

#[cfg(windows)]
use windows::Win32::{
    Foundation::{CloseHandle, HANDLE},
    System::Threading::{OpenProcess, PROCESS_QUERY_LIMITED_INFORMATION, QueryFullProcessImageNameW},
    UI::WindowsAndMessaging::{GetForegroundWindow, GetWindowTextW, GetWindowThreadProcessId},
};

const APP_NAME: &str = "ScreenTime RS";
const RUN_KEY: &str = r"Software\Microsoft\Windows\CurrentVersion\Run";

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
    daily: Vec<(String, i64)>,
    locked: bool,
    monitor_on: bool,
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
        let d = Local::now().date_naive().to_string();
        let c = self.conn()?;
        let mut st = c.prepare(
            "SELECT app,MAX(seconds),MAX(exe_path) FROM app_usage WHERE date=?1 GROUP BY app ORDER BY MAX(seconds) DESC")?;
        let rows = st.query_map(params![d], |r| Ok(AppStat {
            name: r.get(0)?, seconds: r.get(1)?, exe_path: r.get(2).unwrap_or_default()
        }))?;
        Ok(rows.filter_map(Result::ok).collect())
    }
    fn daily(&self, days: i64) -> Result<Vec<(String,i64)>> {
        let today = Local::now().date_naive();
        let mut out = Vec::new();
        for i in (0..days).rev() {
            let d = today - ChronoDuration::days(i);
            out.push((d.format("%m-%d").to_string(), self.range_total(d,d)?));
        }
        Ok(out)
    }
    fn get(&self, key: &str) -> Option<String> {
        self.conn().ok()?.query_row(
            "SELECT value FROM settings WHERE key=?1", params![key], |r| r.get(0)
        ).ok()
    }
    fn set(&self, key: &str, value: &str) -> Result<()> {
        let c = self.conn()?;
        c.execute(
            "INSERT INTO settings(key,value) VALUES(?1,?2)
             ON CONFLICT(key) DO UPDATE SET value=excluded.value",
            params![key,value])?;
        Ok(())
    }
}

fn fmt(s: i64) -> String { format!("{:02}h {:02}m", s/3600, (s%3600)/60) }

fn startup_enabled() -> bool {
    #[cfg(windows)] {
        let k = RegKey::predef(HKEY_CURRENT_USER);
        if let Ok(key) = k.open_subkey(RUN_KEY) {
            return key.get_value::<String,_>("ScreenTimeRS").is_ok();
        }
    }
    false
}

fn set_startup(on: bool) -> Result<()> {
    #[cfg(windows)] {
        let k = RegKey::predef(HKEY_CURRENT_USER);
        let (key, _) = k.create_subkey(RUN_KEY)?;
        if on {
            let exe = std::env::current_exe()?;
            let cmd = format!(r#""{}" --background"#, exe.display());
            key.set_value("ScreenTimeRS", &cmd)?;
        } else {
            let _ = key.delete_value("ScreenTimeRS");
        }
    }
    Ok(())
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
fn is_locked() -> bool {
    unsafe {
        let hwnd = windows::Win32::UI::WindowsAndMessaging::GetForegroundWindow();
        hwnd.0.is_null()
    }
}
#[cfg(not(windows))]
fn is_locked()->bool { false }

// WM_SYSCOMMAND/monitor power state integration can be expanded with a hidden window.
// For v0.2.0, the foreground-window signal is used as a conservative monitor-off/locked indicator.
fn monitor_on(locked: bool) -> bool { !locked }

#[derive(serde::Serialize, Clone)]
struct JsonApp { name: String, seconds: i64, exe_path: String }

#[derive(serde::Serialize)]
struct JsonDaily { label: String, seconds: i64 }

#[derive(serde::Serialize)]
struct JsonSnapshot {
    current_app: String, current_window: String, today: i64, yesterday: i64, week: i64, month: i64,
    apps: Vec<JsonApp>, daily: Vec<JsonDaily>, locked: bool, monitor_on: bool, cpu: f32, memory_mb: u64,
}

fn snapshot_path() -> Option<std::path::PathBuf> {
    ProjectDirs::from("com", "ScreenTimeRS", APP_NAME).map(|d| d.data_local_dir().join("snapshot.json"))
}

fn write_snapshot(s: &Snapshot) {
    if let Some(path) = snapshot_path() {
        if let Some(parent) = path.parent() { let _ = std::fs::create_dir_all(parent); }
        let data = JsonSnapshot {
            current_app: s.current_app.clone(), current_window: s.current_window.clone(),
            today:s.today, yesterday:s.yesterday, week:s.week, month:s.month,
            apps:s.apps.iter().map(|a| JsonApp{name:a.name.clone(),seconds:a.seconds,exe_path:a.exe_path.clone()}).collect(),
            daily:s.daily.iter().map(|(label,seconds)| JsonDaily{label:label.clone(),seconds:*seconds}).collect(), locked:s.locked, monitor_on:s.monitor_on, cpu:s.cpu, memory_mb:s.memory_mb,
        };
        let tmp = path.with_extension("json.tmp");
        if let Ok(text) = serde_json::to_string(&data) { let _ = std::fs::write(&tmp, text); let _ = std::fs::rename(&tmp, &path); }
    }
}

struct Collector;
impl Collector {
    fn start(db: Arc<Database>, snap: Arc<Mutex<Snapshot>>) {
        thread::spawn(move || {
            let mut sys=System::new_all();
            let mut last=Instant::now();
            loop {
                thread::sleep(Duration::from_secs(1));
                let sec=last.elapsed().as_secs().clamp(1,2) as i64;
                last=Instant::now();
                let locked=is_locked();
                let (app,title,exe_path)=foreground(&mut sys);
                if !locked && !app.is_empty() {
                    let _=db.add(&Local::now().date_naive().to_string(),&app,sec,&exe_path);
                }
                sys.refresh_cpu_usage();
                sys.refresh_memory();
                let today=Local::now().date_naive();
                let yesterday=today-ChronoDuration::days(1);
                let week_start = today - ChronoDuration::days(today.weekday().num_days_from_monday() as i64);
                let month_start = today.with_day(1).unwrap();
                let apps=db.apps_today().unwrap_or_default();
                let daily=db.daily(14).unwrap_or_default();
                let mut s=snap.lock().unwrap();
                *s=Snapshot {
                    current_app:app,current_window:title,
                    today:db.range_total(today,today).unwrap_or(0),
                    yesterday:db.range_total(yesterday,yesterday).unwrap_or(0),
                    week:db.range_total(week_start,today).unwrap_or(0),
                    month:db.range_total(month_start,today).unwrap_or(0),
                    apps,daily,locked,monitor_on:monitor_on(locked),
                    cpu:sys.global_cpu_usage(),memory_mb:sys.used_memory()/1024/1024
                };
                write_snapshot(&s);
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
        let initial=Snapshot {
            today:db.range_total(today,today).unwrap_or(0),
            yesterday:db.range_total(yesterday,yesterday).unwrap_or(0),
            week:db.range_total(week_start,today).unwrap_or(0),
            month:db.range_total(month_start,today).unwrap_or(0),
            apps:db.apps_today().unwrap_or_default(),
            daily:db.daily(14).unwrap_or_default(),
            ..Snapshot::default()
        };
        if let Ok(mut current)=snap.lock() {
            *current=initial;
            write_snapshot(&current);
        }
    }

    let background=std::env::args().any(|a|a=="--background" || a=="--collector");
    if background {
        Collector::start(db.clone(),snap.clone());
    }

    let menu=Menu::new();
    let show=MenuItem::new("打开 ScreenTime RS",true,None);
    let quit=MenuItem::new("退出",true,None);
    menu.append(&show)?; menu.append(&PredefinedMenuItem::separator())?; menu.append(&quit)?;
    let _tray=TrayIconBuilder::new().with_icon(tray_icon()).with_tooltip(APP_NAME)
        .with_menu(Box::new(menu)).build()?;
    let show_id=show.id().clone(); let quit_id=quit.id().clone();

    if !background { launch_ui(); }
    loop {
        if let Ok(e)=MenuEvent::receiver().try_recv() {
            if e.id==show_id { launch_ui(); }
            if e.id==quit_id { return Ok(()); }
        }
        thread::sleep(Duration::from_millis(250));
    }
}
