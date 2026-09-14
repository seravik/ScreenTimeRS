fn main() {
    #[cfg(windows)]
    {
        let mut res = winresource::WindowsResource::new();
        res.set("ProductName", "ScreenTime RS");
        res.set("FileDescription", "Windows screen and application time tracker");
        res.set("CompanyName", "ScreenTime RS");
        res.set_icon("assets/ScreenTimeRS.ico");
        let _ = res.compile();
    }
}
