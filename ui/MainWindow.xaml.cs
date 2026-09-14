using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.UI;

namespace ScreenTimeRS.UI;

public sealed partial class MainWindow : Window
{
    private readonly string _snapshotPath;
    private Process? _collector;
    private bool _dark;

    public MainWindow()
    {
        InitializeComponent();
        _snapshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreenTimeRS", "ScreenTime RS", "data", "snapshot.json");
        StartCollector();
        Nav.SelectedItem = Nav.MenuItems[0];
        DispatcherQueue.TryEnqueue(() =>
        {
            if (Nav.SelectedItem is null && Nav.MenuItems.Count > 0)
                Nav.SelectedItem = Nav.MenuItems[0];
            RenderCurrent();
        });
        _ = RefreshLoop();
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

            // Do not merely check the process name: another installation or an
            // older build can also be named screentime-rs.exe. We must launch the
            // collector that belongs to this exact UI build.
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
            DispatcherQueue.TryEnqueue(RenderCurrent);
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

    private void RenderCurrent()
    {
        if (Nav.SelectedItem is not NavigationViewItem item) return;

        // The collector writes snapshot.json asynchronously. Never leave the UI blank
        // while the first snapshot is being created.
        var s = LoadSnapshot() ?? new Snapshot();
        switch (item.Tag?.ToString())
        {
            case "overview": ContentFrame.Content = new OverviewPage(s); break;
            case "apps": ContentFrame.Content = new AppsPage(s); break;
            case "stats": ContentFrame.Content = new StatsPage(s); break;
            case "settings": ContentFrame.Content = new SettingsPage(_dark); break;
        }
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        if (Nav.MenuItems.Count > 0)
            Nav.SelectedItem = Nav.MenuItems[0];
        RenderCurrent();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args) => RenderCurrent();
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RenderCurrent();
    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _dark = !_dark;
        if (Content is FrameworkElement root) root.RequestedTheme = _dark ? ElementTheme.Dark : ElementTheme.Light;
        RenderCurrent();
    }
}

public sealed class Snapshot
{
    public string current_app { get; set; } = ""; public string current_window { get; set; } = "";
    public long today { get; set; } public long yesterday { get; set; } public long week { get; set; } public long month { get; set; }
    public AppStat[] apps { get; set; } = Array.Empty<AppStat>(); public JsonDaily[] daily { get; set; } = Array.Empty<JsonDaily>();
    public bool locked { get; set; } public bool monitor_on { get; set; } public float cpu { get; set; } public ulong memory_mb { get; set; }
}
public sealed class AppStat { public string name { get; set; } = ""; public long seconds { get; set; } public string exe_path { get; set; } = ""; }
public sealed class JsonDaily { public string label { get; set; } = ""; public long seconds { get; set; } }

public static class UiHelpers
{
    public static string Format(long s) => $"{s / 3600:00}小时 {(s % 3600) / 60:00}分钟";
    public static string FriendlyName(AppStat app)
    {
        var n = Path.GetFileNameWithoutExtension(app.name);
        var special = n.ToLowerInvariant() switch { "explorer" => "文件资源管理器", "firefox" => "Firefox", "chrome" => "Google Chrome", "msedge" => "Microsoft Edge", "code" => "Visual Studio Code", "discord" => "Discord", _ => null };
        if (special != null) return special;
        try
        {
            if (File.Exists(app.exe_path))
            {
                var info = FileVersionInfo.GetVersionInfo(app.exe_path);
                return string.IsNullOrWhiteSpace(info.FileDescription) ? (string.IsNullOrWhiteSpace(info.ProductName) ? n : info.ProductName) : info.FileDescription;
            }
        } catch { }
        return n;
    }
}
