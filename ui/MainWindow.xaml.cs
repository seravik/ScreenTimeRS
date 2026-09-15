using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScreenTimeRS.UI;

public sealed partial class MainWindow : Window
{
    private readonly string _snapshotPath;
    private readonly string _shutdownPath;
    private Process? _collector;
    private bool _dark;

    private const string UserSettingsKey = @"Software\ScreenTimeRS";
    private const string NavigationPaneValue = "NavigationPaneOpen";

    private readonly OverviewPage _overviewPage;
    private readonly AppsPage _appsPage;
    private readonly StatsPage _statsPage;
    private readonly SettingsPage _settingsPage;

    public MainWindow()
    {
        InitializeComponent();

        // ProjectDirs::data_local_dir() on Windows resolves to:
        // %LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data
        // Keep the WinUI reader exactly aligned with ProjectDirs::data_local_dir().
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenTimeRS", "ScreenTime RS", "data");

        _snapshotPath = Path.Combine(dataDir, "snapshot.json");
        _shutdownPath = Path.Combine(dataDir, "shutdown.flag");

        SetWindowIdentity();

        // Persist the NavigationView pane state immediately when the actual
        // IsPaneOpen dependency property changes. This avoids relying only on
        // the end-of-animation PaneClosed/PaneOpened events, which can be missed
        // when the window is closed quickly with the X button.
        Nav.RegisterPropertyChangedCallback(NavigationView.IsPaneOpenProperty, (_, _) =>
        {
            SaveNavigationPaneState(Nav.IsPaneOpen);
        });

        // Also write the latest value when the window closes so the saved state
        // is correct even if the pane is being animated during shutdown.
        Closed += (_, _) => SaveNavigationPaneState(Nav.IsPaneOpen);

        Nav.IsPaneOpen = LoadNavigationPaneState();

        // A previous tray exit request must never affect a new launch.
        try { if (File.Exists(_shutdownPath)) File.Delete(_shutdownPath); } catch { }

        _overviewPage = new OverviewPage();
        _appsPage = new AppsPage();
        _statsPage = new StatsPage();
        _settingsPage = new SettingsPage(_dark);

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
            // WinUI 3 unpackaged apps otherwise commonly show the generic
            // title "WinUI Desktop". Explicitly set both title and .ico.
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
        _overviewPage.UpdateSnapshot(s);
        _appsPage.UpdateSnapshot(s);
        _statsPage.UpdateSnapshot(s);
        _settingsPage.SetDark(_dark);
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

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        if (Nav.MenuItems.Count > 0)
            Nav.SelectedItem = Nav.MenuItems[0];

        ShowSelectedPage();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        ShowSelectedPage();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        UpdatePages(LoadSnapshot() ?? new Snapshot());
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _dark = !_dark;

        if (Content is FrameworkElement root)
            root.RequestedTheme = _dark ? ElementTheme.Dark : ElementTheme.Light;

        _settingsPage.SetDark(_dark);
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
    public static string Format(long s) => $"{s / 3600:00}小时 {(s % 3600) / 60:00}分钟";

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
