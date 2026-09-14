#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use anyhow::Result;
use chrono::{Datelike, Duration as ChronoDuration, Local, NaiveDate};
use directories::ProjectDirs;
use eframe::egui::{self, Color32, RichText};
use rusqlite::{params, Connection};
use std::{
    sync::{Arc, Mutex},
    thread,
    time::{Duration, Instant},
};
use sysinfo::{Pid, ProcessesToUpdate, System};
use tray_icon::{
    menu::{Menu, MenuEvent, MenuItem, PredefinedMenuItem},
    Icon, TrayIconBuilder,
};
use winreg::{enums::HKEY_CURRENT_USER, RegKey};

#[cfg(windows)]
use windows::Win32::{
    Foundation::{CloseHandle, HANDLE},
    System::{
        Threading::{OpenProcess, PROCESS_QUERY_LIMITED_INFORMATION, QueryFullProcessImageNameW},
    },
    UI::WindowsAndMessaging::{
        GetForegroundWindow, GetWindowTextW, GetWindowThreadProcessId,
    },
};

const APP_NAME: &str = "ScreenTime RS";
const RUN_KEY: &str = r"Software\Microsoft\Windows\CurrentVersion\Run";

// Bundled Simplified Chinese font. egui's built-in fonts do not contain CJK glyphs,
// so Chinese text would otherwise render as empty square boxes on Windows.
fn configure_fonts(ctx: &egui::Context) {
    let mut fonts = egui::FontDefinitions::default();
    fonts.font_data.insert(
        "noto_sans_sc".to_owned(),
        egui::FontData::from_static(include_bytes!("../assets/NotoSansCJKSC-Regular.ttf")).into(),
    );
    fonts
        .families
        .entry(egui::FontFamily::Proportional)
        .or_default()
        .insert(0, "noto_sans_sc".to_owned());
    fonts
        .families
        .entry(egui::FontFamily::Monospace)
        .or_default()
        .insert(0, "noto_sans_sc".to_owned());
    ctx.set_fonts(fonts);
}

#[derive(Clone, Default)]
struct AppStat { name: String, seconds: i64 }

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
        std::fs::create_dir_all(dirs.data_dir())?;
        let db = Self { path: dirs.data_dir().join("screentime.db") };
        let c = Connection::open(&db.path)?;
        c.execute_batch(
            "CREATE TABLE IF NOT EXISTS app_usage(
               date TEXT NOT NULL, app TEXT NOT NULL, seconds INTEGER NOT NULL DEFAULT 0,
               PRIMARY KEY(date,app));
             CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY,value TEXT NOT NULL);"
        )?;
        Ok(db)
    }
    fn conn(&self) -> Result<Connection> { Ok(Connection::open(&self.path)?) }
    fn add(&self, date: &str, app: &str, seconds: i64) -> Result<()> {
        if seconds <= 0 || app.is_empty() { return Ok(()); }
        let c = self.conn()?;
        c.execute(
            "INSERT INTO app_usage(date,app,seconds) VALUES(?1,?2,?3)
             ON CONFLICT(date,app) DO UPDATE SET seconds=seconds+excluded.seconds",
            params![date, app, seconds])?;
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
            "SELECT app,seconds FROM app_usage WHERE date=?1 ORDER BY seconds DESC")?;
        let rows = st.query_map(params![d], |r| Ok(AppStat {
            name: r.get(0)?, seconds: r.get(1)?
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
fn foreground(sys: &mut System) -> (String,String) {
    unsafe {
        let hwnd = GetForegroundWindow();
        if hwnd.0.is_null() { return (String::new(), String::new()); }
        let mut pid=0u32;
        GetWindowThreadProcessId(hwnd, Some(&mut pid));
        if pid==0 { return (String::new(), String::new()); }
        sys.refresh_processes(ProcessesToUpdate::Some(&[Pid::from_u32(pid)]), true);
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
                    }
                }
            }
            let _=CloseHandle(h);
        }
        let mut b=[0u16;512];
        let len=GetWindowTextW(hwnd,&mut b);
        (name,String::from_utf16_lossy(&b[..len as usize]))
    }
}
#[cfg(not(windows))]
fn foreground(_: &mut System)->(String,String){("Unsupported".into(),"".into())}

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
// For v0.1.0, the foreground-window signal is used as a conservative monitor-off/locked indicator.
fn monitor_on(locked: bool) -> bool { !locked }

struct Collector;
impl Collector {
    fn start(db: Arc<Database>, snap: Arc<Mutex<Snapshot>>) {
        thread::spawn(move || {
            let mut sys=System::new_all();
            let mut last=Instant::now();
            loop {
                thread::sleep(Duration::from_secs(2));
                let sec=last.elapsed().as_secs().clamp(1,3) as i64;
                last=Instant::now();
                let locked=is_locked();
                let (app,title)=foreground(&mut sys);
                if !locked && !app.is_empty() {
                    let _=db.add(&Local::now().date_naive().to_string(),&app,sec);
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

struct App {
    snap:Arc<Mutex<Snapshot>>, db:Arc<Database>, page:usize, dark:bool, startup:bool
}
impl App {
    fn card(ui:&mut egui::Ui,t:&str,v:&str,s:&str) {
        egui::Frame::group(ui.style()).inner_margin(16.0).corner_radius(14.0).show(ui,|ui|{
            ui.label(RichText::new(t).size(12.0).color(Color32::GRAY));
            ui.add_space(5.0); ui.label(RichText::new(v).size(25.0).strong());
            ui.label(RichText::new(s).size(11.0).color(Color32::GRAY));
        });
    }
    fn overview(&self,ui:&mut egui::Ui,s:&Snapshot) {
        ui.heading("概览"); ui.add_space(12.0);
        ui.horizontal_wrapped(|ui|{
            Self::card(ui,"今天",&fmt(s.today),"活跃时间");
            Self::card(ui,"昨天",&fmt(s.yesterday),"活跃时间");
            Self::card(ui,"本周",&fmt(s.week),"周一至今天");
            Self::card(ui,"本月",&fmt(s.month),"本月累计");
        });
        ui.add_space(16.0);
        ui.horizontal(|ui|{
            ui.label(RichText::new("状态").strong());
            ui.label(if s.locked { "已锁定" } else { "正常运行" });
            ui.separator();
            ui.label(if s.monitor_on { "显示器：开启 / 活动" } else { "显示器：关闭 / 锁定" });
            ui.separator();
            ui.label(format!("CPU {:.1}%",s.cpu));
            ui.label(format!("内存 {} MB",s.memory_mb));
        });
        ui.add_space(16.0);
        egui::Frame::group(ui.style()).inner_margin(16.0).corner_radius(14.0).show(ui,|ui|{
            ui.label(RichText::new("最近 14 天").size(17.0).strong());
            ui.add_space(10.0);
            let max=s.daily.iter().map(|x|x.1).max().unwrap_or(1).max(1);
            for (d,v) in &s.daily {
                ui.horizontal(|ui|{
                    ui.add_sized([45.0,20.0],egui::Label::new(d));
                    let w=430.0*(*v as f32/max as f32);
                    ui.add_sized([w.max(2.0),18.0],egui::Label::new(
                        RichText::new(" ").background_color(Color32::from_rgb(45,145,220))));
                    ui.label(fmt(*v));
                });
            }
        });
    }
    fn apps(&self,ui:&mut egui::Ui,s:&Snapshot) {
        ui.heading("应用使用时间"); ui.add_space(12.0);
        let total=s.today.max(1);
        for (i,a) in s.apps.iter().enumerate() {
            let p=a.seconds as f32/total as f32;
            ui.horizontal(|ui|{
                ui.label(RichText::new(format!("{:02}",i+1)).color(Color32::GRAY));
                ui.add_sized([230.0,22.0],egui::Label::new(RichText::new(&a.name).strong()));
                ui.add(egui::ProgressBar::new(p).desired_width(280.0));
                ui.label(fmt(a.seconds));
            });
            ui.add_space(6.0);
        }
    }
    fn settings(&mut self,ui:&mut egui::Ui) {
        ui.heading("设置"); ui.add_space(15.0);
        let old=self.startup; ui.checkbox(&mut self.startup,"登录 Windows 后自动启动");
        if old!=self.startup { let _=set_startup(self.startup); }
        let od=self.dark; ui.checkbox(&mut self.dark,"深色模式");
        if od!=self.dark { let _=self.db.set("dark",if self.dark{"true"}else{"false"}); }
        ui.add_space(18.0);
        ui.label(RichText::new("应用图标识别").strong());
        ui.label("应用按 Windows 进程可执行文件名归类；后续版本可增加从 EXE 提取真实图标缓存。");
        ui.add_space(12.0);
        ui.label(RichText::new("数据库").strong());
        if let Some(d)=ProjectDirs::from("com","ScreenTimeRS",APP_NAME) {
            ui.monospace(d.data_dir().join("screentime.db").display().to_string());
        }
    }
}
impl eframe::App for App {
    fn update(&mut self,ctx:&egui::Context,_:&mut eframe::Frame) {
        if self.dark {ctx.set_visuals(egui::Visuals::dark())} else {ctx.set_visuals(egui::Visuals::light())}
        let s=self.snap.lock().unwrap().clone();
        egui::SidePanel::left("nav").exact_width(190.0).show(ctx,|ui|{
            ui.add_space(16.0);
            ui.label(RichText::new("ScreenTime RS").size(21.0).strong());
            ui.label(RichText::new("Windows 时间统计").small().color(Color32::GRAY));
            ui.add_space(22.0);
            for (i,x) in ["概览","应用使用时间","设置"].iter().enumerate() {
                if ui.selectable_label(self.page==i,*x).clicked(){self.page=i}
                ui.add_space(4.0);
            }
            ui.separator();
            ui.label(RichText::new("当前应用").color(Color32::GRAY));
            ui.label(RichText::new(if s.current_app.is_empty(){"—"}else{&s.current_app}).strong());
            if !s.current_window.is_empty(){ui.label(RichText::new(&s.current_window).small().color(Color32::GRAY));}
            ui.with_layout(egui::Layout::bottom_up(egui::Align::LEFT),|ui|{
                ui.label(RichText::new("v0.1.0").small().color(Color32::GRAY));
            });
        });
        egui::CentralPanel::default().show(ctx,|ui|{
            match self.page {0=>self.overview(ui,&s),1=>self.apps(ui,&s),_=>
                self.settings(ui)}
        });
        ctx.request_repaint_after(Duration::from_secs(1));
    }
}

fn main()->Result<()> {
    let db=Arc::new(Database::new()?);
    let snap=Arc::new(Mutex::new(Snapshot::default()));
    Collector::start(db.clone(),snap.clone());

    let menu=Menu::new();
    let show=MenuItem::new("打开 ScreenTime RS",true,None);
    let quit=MenuItem::new("退出",true,None);
    menu.append(&show)?; menu.append(&PredefinedMenuItem::separator())?; menu.append(&quit)?;
    let _tray=TrayIconBuilder::new().with_icon(tray_icon()).with_tooltip(APP_NAME)
        .with_menu(Box::new(menu)).build()?;
    let show_id=show.id().clone(); let quit_id=quit.id().clone();

    let background=std::env::args().any(|a|a=="--background");
    if background {
        loop {
            if let Ok(e)=MenuEvent::receiver().try_recv() {
                if e.id==show_id {break}
                if e.id==quit_id {return Ok(())}
            }
            thread::sleep(Duration::from_millis(250));
        }
    }
    let db2=db.clone(); let snap2=snap.clone();
    let dark=db.get("dark").as_deref()!=Some("false");
    let startup=startup_enabled();
    let options=eframe::NativeOptions{viewport:egui::ViewportBuilder::default()
        .with_inner_size([1080.0,720.0]).with_min_inner_size([900.0,600.0])
        .with_title(APP_NAME),..Default::default()};
    if let Err(e) = eframe::run_native(APP_NAME,options,Box::new(move|cc|{
        configure_fonts(&cc.egui_ctx);
        Ok(Box::new(App{
            snap:snap2,db:db2,page:0,dark,startup
        }))
    })) {
        eprintln!("Failed to start GUI: {e:?}");
        return Err(anyhow::anyhow!("GUI failed to start"));
    }
    Ok(())
}
