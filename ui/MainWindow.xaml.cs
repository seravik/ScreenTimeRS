using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.UI;

namespace ScreenTimeRS.UI;

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

public sealed partial class MainWindow : Window
{
    private readonly string _snapshotPath;
    private readonly string _shutdownPath;
    private Process? _collector;
    private ThemeMode _themeMode;
    private LanguageMode _languageMode;

    private const string UserSettingsKey = @"Software\ScreenTimeRS";
    private const string NavigationPaneValue = "NavigationPaneOpen";
    private const string ThemeModeValue = "ThemeMode";
    private const string LanguageModeValue = "LanguageMode";
    private const string NavigationPaneLengthValue = "NavigationPaneLength";
    private double _savedPaneLength = 320;
    private bool _resizingPane;
    private Snapshot _latestSnapshot = new();
    private uint _resizePointerId;

    private readonly OverviewPage _overviewPage;
    private readonly AppsPage _appsPage;
    private readonly StatsPage _statsPage;
    private readonly SettingsPage _settingsPage;

    public MainWindow()
    {
        InitializeComponent();

        // ProjectDirs::data_local_dir() on Windows resolves to:
        // %LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenTimeRS", "ScreenTime RS", "data");

        _snapshotPath = Path.Combine(dataDir, "snapshot.json");
        _shutdownPath = Path.Combine(dataDir, "shutdown.flag");

        SetWindowIdentity();

        // Use the custom title bar as the actual window title bar so the
        // system title text/icon are not shown a second time.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        Nav.RegisterPropertyChangedCallback(NavigationView.IsPaneOpenProperty, (_, _) =>
        {
            SaveNavigationPaneState(Nav.IsPaneOpen);
            if (Nav.IsPaneOpen)
                Nav.OpenPaneLength = _savedPaneLength;
        });
        Closed += (_, _) =>
        {
            SaveNavigationPaneState(Nav.IsPaneOpen);
            SaveNavigationPaneLength(Nav.OpenPaneLength);
        };
        _savedPaneLength = LoadNavigationPaneLength();
        Nav.OpenPaneLength = _savedPaneLength;
        Nav.PointerPressed += Nav_PointerPressed;
        Nav.PointerMoved += Nav_PointerMoved;
        Nav.PointerReleased += Nav_PointerReleased;
        Nav.PointerCanceled += Nav_PointerCanceled;
        Nav.IsPaneOpen = LoadNavigationPaneState();

        try { if (File.Exists(_shutdownPath)) File.Delete(_shutdownPath); } catch { }

        _themeMode = LoadThemeMode();
        _languageMode = LoadLanguageMode();

        _overviewPage = new OverviewPage();
        _appsPage = new AppsPage();
        _statsPage = new StatsPage();
        _settingsPage = new SettingsPage(_themeMode, _languageMode);
        _settingsPage.ThemeModeChanged += SettingsPage_ThemeModeChanged;
        _settingsPage.LanguageChanged += SettingsPage_LanguageChanged;

        ApplyLanguage(_languageMode);
        ApplyThemeMode(_themeMode, save: false);
        RootGrid.ActualThemeChanged += RootGrid_ActualThemeChanged;

        StartCollector();

        Nav.SelectedItem = Nav.MenuItems[0];
        DispatcherQueue.TryEnqueue(() =>
        {
            if (Nav.SelectedItem is null && Nav.MenuItems.Count > 0)
                Nav.SelectedItem = Nav.MenuItems[0];

            ShowSelectedPage();
            UpdatePages(LoadSnapshot() ?? new Snapshot());
        });

        _ = RefreshLoop();
    }

    private void SetWindowIdentity()
    {
        try
        {
            AppWindow.Title = "ScreenTime RS";
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ScreenTimeRS.ico");
            if (!File.Exists(iconPath))
                iconPath = Path.Combine(AppContext.BaseDirectory, "ScreenTimeRS.ico");
            if (File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
                AppWindow.SetTaskbarIcon(iconPath);
            }
        }
        catch { }
    }

    private void RootGrid_ActualThemeChanged(FrameworkElement sender, object args)
    {
        ApplyTitleBarTheme();
    }

    private void ApplyTitleBarTheme()
    {
        try
        {
            var dark = RootGrid.ActualTheme == ElementTheme.Dark;
            var background = Color.FromArgb(255,
                dark ? (byte)32 : (byte)245,
                dark ? (byte)32 : (byte)245,
                dark ? (byte)32 : (byte)245);
            var foreground = dark
                ? Color.FromArgb(255, 245, 245, 245)
                : Color.FromArgb(255, 32, 32, 32);
            var hoverBackground = dark
                ? Color.FromArgb(255, 52, 52, 52)
                : Color.FromArgb(255, 232, 232, 232);
            var pressedBackground = dark
                ? Color.FromArgb(255, 64, 64, 64)
                : Color.FromArgb(255, 218, 218, 218);
            var inactiveBackground = background;
            var inactiveForeground = dark
                ? Color.FromArgb(255, 170, 170, 170)
                : Color.FromArgb(255, 110, 110, 110);

            var titleBar = AppWindow.TitleBar;
            titleBar.BackgroundColor = background;
            titleBar.ForegroundColor = foreground;
            titleBar.InactiveBackgroundColor = inactiveBackground;
            titleBar.InactiveForegroundColor = inactiveForeground;
            titleBar.ButtonBackgroundColor = background;
            titleBar.ButtonForegroundColor = foreground;
            titleBar.ButtonHoverBackgroundColor = hoverBackground;
            titleBar.ButtonHoverForegroundColor = foreground;
            titleBar.ButtonPressedBackgroundColor = pressedBackground;
            titleBar.ButtonPressedForegroundColor = foreground;
            titleBar.ButtonInactiveBackgroundColor = inactiveBackground;
            titleBar.ButtonInactiveForegroundColor = inactiveForeground;
        }
        catch { }
    }

    private void ApplyThemeMode(ThemeMode mode, bool save)
    {
        _themeMode = mode;
        if (save)
            SaveThemeMode(mode);

        RootGrid.RequestedTheme = mode switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        _settingsPage?.SetThemeMode(mode);
        ApplyTitleBarTheme();
    }

    private void SettingsPage_ThemeModeChanged(object? sender, ThemeMode mode)
    {
        ApplyThemeMode(mode, save: true);
    }

    private ThemeMode LoadThemeMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserSettingsKey);
            var value = key?.GetValue(ThemeModeValue);
            return value switch
            {
                string text when string.Equals(text, "light", StringComparison.OrdinalIgnoreCase) => ThemeMode.Light,
                string text when string.Equals(text, "dark", StringComparison.OrdinalIgnoreCase) => ThemeMode.Dark,
                _ => ThemeMode.System
            };
        }
        catch
        {
            return ThemeMode.System;
        }
    }

    private static void SaveThemeMode(ThemeMode mode)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UserSettingsKey);
            key?.SetValue(ThemeModeValue, mode switch
            {
                ThemeMode.Light => "light",
                ThemeMode.Dark => "dark",
                _ => "system"
            });
        }
        catch { }
    }

    private void SettingsPage_LanguageChanged(object? sender, LanguageMode language)
    {
        _languageMode = language;
        SaveLanguageMode(language);
        ApplyLanguage(language);
    }

    private void ApplyLanguage(LanguageMode language)
    {
        _languageMode = language;
        var en = language == LanguageMode.English;
        AppHeaderTitle.Text = "ScreenTime RS";
        AppHeaderSubtitle.Text = en ? "Windows screen time and app usage statistics" : "Windows 使用时间与应用统计";
        ThemeButton.Content = en ? "Theme" : "主题";
        RefreshButton.Content = en ? "Refresh" : "刷新";
        NavOverview.Content = en ? "Overview" : "概览";
        NavApps.Content = en ? "App usage" : "应用使用时间";
        NavStats.Content = en ? "Statistics" : "统计";
        NavSettings.Content = en ? "Settings" : "设置";
        _overviewPage.ApplyLanguage(language);
        _appsPage.ApplyLanguage(language);
        _statsPage.ApplyLanguage(language);
        _settingsPage.SetLanguage(language);
    }

    private LanguageMode LoadLanguageMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserSettingsKey);
            var value = key?.GetValue(LanguageModeValue);
            return value switch
            {
                string text when string.Equals(text, "en", StringComparison.OrdinalIgnoreCase) => LanguageMode.English,
                _ => LanguageMode.Chinese
            };
        }
        catch
        {
            return LanguageMode.Chinese;
        }
    }

    private static void SaveLanguageMode(LanguageMode mode)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UserSettingsKey);
            key?.SetValue(LanguageModeValue, mode == LanguageMode.English ? "en" : "zh-CN");
        }
        catch { }
    }

    private void StartCollector()
    {
        try
        {
            var exe = Path.Combine(AppContext.BaseDirectory, "screentime-rs.exe");
            if (!File.Exists(exe)) return;

            var fullExe = Path.GetFullPath(exe);
            foreach (var p in Process.GetProcessesByName("screentime-rs"))
            {
                try
                {
                    var running = Path.GetFullPath(p.MainModule?.FileName ?? "");
                    if (string.Equals(running, fullExe, StringComparison.OrdinalIgnoreCase))
                    {
                        _collector = p;
                        return;
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }

            _collector = Process.Start(new ProcessStartInfo(fullExe, "--background")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppContext.BaseDirectory
            });
        }
        catch { }
    }

    private async Task RefreshLoop()
    {
        while (true)
        {
            await Task.Delay(1000);

            if (File.Exists(_shutdownPath))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    try { Close(); } catch { }
                });
                return;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                var snapshot = LoadSnapshot() ?? new Snapshot();
                UpdatePages(snapshot);
            });
        }
    }

    private Snapshot? LoadSnapshot()
    {
        try
        {
            if (!File.Exists(_snapshotPath)) return null;
            return JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(_snapshotPath));
        }
        catch { return null; }
    }

    private void ShowSelectedPage()
    {
        if (Nav.SelectedItem is not NavigationViewItem item) return;

        ContentFrame.Content = item.Tag?.ToString() switch
        {
            "overview" => _overviewPage,
            "apps" => _appsPage,
            "stats" => _statsPage,
            "settings" => _settingsPage,
            _ => _overviewPage
        };
    }

    private void UpdatePages(Snapshot s)
    {
        _latestSnapshot = s;
        UpdateVisiblePage();
    }

    private void UpdateVisiblePage()
    {
        switch (Nav.SelectedItem is NavigationViewItem item ? item.Tag?.ToString() : "overview")
        {
            case "apps":
                _appsPage.UpdateSnapshot(_latestSnapshot);
                break;
            case "stats":
                _statsPage.UpdateSnapshot(_latestSnapshot);
                break;
            case "settings":
                break;
            default:
                _overviewPage.UpdateSnapshot(_latestSnapshot);
                break;
        }
    }

    private static bool LoadNavigationPaneState()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserSettingsKey);
            var value = key?.GetValue(NavigationPaneValue);
            return value is not int intValue || intValue != 0;
        }
        catch
        {
            return true;
        }
    }

    private static void SaveNavigationPaneState(bool isOpen)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UserSettingsKey);
            key?.SetValue(NavigationPaneValue, isOpen ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch { }
    }

    private void Nav_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!Nav.IsPaneOpen) return;
        var point = e.GetCurrentPoint(Nav);
        var edge = Nav.OpenPaneLength;
        if (point.Position.X >= edge - 10 && point.Position.X <= edge + 10)
        {
            _resizingPane = true;
            _resizePointerId = point.PointerId;
            Nav.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void Nav_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizingPane || e.Pointer.PointerId != _resizePointerId) return;
        var point = e.GetCurrentPoint(Nav);
        var width = Math.Clamp(point.Position.X, 220, 480);
        Nav.OpenPaneLength = width;
        _savedPaneLength = width;
        e.Handled = true;
    }

    private void Nav_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizingPane || e.Pointer.PointerId != _resizePointerId) return;
        _resizingPane = false;
        _savedPaneLength = Nav.OpenPaneLength;
        SaveNavigationPaneLength(_savedPaneLength);
        Nav.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void Nav_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizingPane || e.Pointer.PointerId != _resizePointerId) return;
        _resizingPane = false;
        _savedPaneLength = Nav.OpenPaneLength;
        SaveNavigationPaneLength(_savedPaneLength);
        try { Nav.ReleasePointerCapture(e.Pointer); } catch { }
    }

    private static double LoadNavigationPaneLength()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserSettingsKey);
            var value = key?.GetValue(NavigationPaneLengthValue);
            if (value is int i) return Math.Clamp(i, 220, 480);
            if (value is long l) return Math.Clamp((double)l, 220, 480);
            if (value is string s && double.TryParse(s, out var d)) return Math.Clamp(d, 220, 480);
        }
        catch { }
        return 320;
    }

    private static void SaveNavigationPaneLength(double length)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UserSettingsKey);
            key?.SetValue(NavigationPaneLengthValue, (int)Math.Round(Math.Clamp(length, 220, 480)), Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch { }
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        if (Nav.MenuItems.Count > 0)
            Nav.SelectedItem = Nav.MenuItems[0];
        ShowSelectedPage();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        ShowSelectedPage();
        UpdateVisiblePage();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        UpdatePages(LoadSnapshot() ?? new Snapshot());
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var next = RootGrid.ActualTheme == ElementTheme.Dark ? ThemeMode.Light : ThemeMode.Dark;
        ApplyThemeMode(next, save: true);
    }
}

public sealed class Snapshot
{
    public string current_app { get; set; } = "";
    public string current_window { get; set; } = "";
    public long today { get; set; }
    public long yesterday { get; set; }
    public long week { get; set; }
    public long month { get; set; }
    public AppStat[] apps { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_week { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_month { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_half_year { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_year { get; set; } = Array.Empty<AppStat>();
    public JsonDaily[] daily { get; set; } = Array.Empty<JsonDaily>();
    public bool locked { get; set; }
    public bool monitor_on { get; set; }
    public float cpu { get; set; }
    public ulong memory_mb { get; set; }
}

public sealed class AppStat
{
    public string name { get; set; } = "";
    public long seconds { get; set; }
    public string exe_path { get; set; } = "";
}

public sealed class JsonDaily
{
    public string label { get; set; } = "";
    public long seconds { get; set; }
}

public static class UiHelpers
{
    static readonly Dictionary<string, BitmapImage> AppIconCache = new(StringComparer.OrdinalIgnoreCase);

    public static string Format(long s) => Format(s, LanguageMode.Chinese);

    public static string Format(long s, LanguageMode language)
    {
        var hours = s / 3600;
        var minutes = (s % 3600) / 60;
        return language == LanguageMode.English
            ? $"{hours:00}h {minutes:00}m"
            : $"{hours:00}小时 {minutes:00}分钟";
    }

    public static void SetAppIcon(Microsoft.UI.Xaml.Controls.Image image, AppStat app)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(app.exe_path) || !File.Exists(app.exe_path)) return;
            if (AppIconCache.TryGetValue(app.exe_path, out var cached))
            {
                image.Source = cached;
                return;
            }

            using var ico = System.Drawing.Icon.ExtractAssociatedIcon(app.exe_path);
            if (ico == null) return;
            var dir = Path.Combine(Path.GetTempPath(), "ScreenTimeRS-icons");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(app.exe_path))) + ".png");
            if (!File.Exists(file))
                using (var bmp = ico.ToBitmap()) bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);

            var bitmap = new BitmapImage(new Uri(file));
            AppIconCache[app.exe_path] = bitmap;
            image.Source = bitmap;
        }
        catch { }
    }

    public static string FriendlyName(AppStat app)
    {
        var n = Path.GetFileNameWithoutExtension(app.name);
        var special = n.ToLowerInvariant() switch
        {
            "explorer" => "文件资源管理器",
            "firefox" => "Firefox",
            "chrome" => "Google Chrome",
            "msedge" => "Microsoft Edge",
            "code" => "Visual Studio Code",
            "discord" => "Discord",
            _ => null
        };

        if (special != null) return special;

        try
        {
            if (File.Exists(app.exe_path))
            {
                var info = FileVersionInfo.GetVersionInfo(app.exe_path);
                return string.IsNullOrWhiteSpace(info.FileDescription)
                    ? (string.IsNullOrWhiteSpace(info.ProductName) ? n : info.ProductName)
                    : info.FileDescription;
            }
        }
        catch { }

        return n;
    }
}
