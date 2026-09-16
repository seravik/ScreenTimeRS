using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
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
    private const string AccentColorValue = "AccentColor";
    private const string TermsAcceptedValue = "TermsAccepted_v0_5_0";
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
        var accent = LoadAccentColor();
        ThemeManager.ApplyAccent(accent);

        _overviewPage = new OverviewPage();
        _appsPage = new AppsPage();
        _statsPage = new StatsPage();
        _settingsPage = new SettingsPage(_themeMode, _languageMode, accent);
        _settingsPage.ThemeModeChanged += SettingsPage_ThemeModeChanged;
        _settingsPage.LanguageChanged += SettingsPage_LanguageChanged;
        _settingsPage.AccentColorChanged += SettingsPage_AccentColorChanged;
        ApplyAccent(accent);

        ApplyLanguage(_languageMode);
        ApplyThemeMode(_themeMode, save: false);
        RootGrid.ActualThemeChanged += RootGrid_ActualThemeChanged;

        Nav.SelectedItem = Nav.MenuItems[0];
        DispatcherQueue.TryEnqueue(() =>
        {
            if (Nav.SelectedItem is null && Nav.MenuItems.Count > 0)
                Nav.SelectedItem = Nav.MenuItems[0];

            ShowSelectedPage();
            UpdatePages(LoadSnapshot() ?? new Snapshot());
        });

        _ = InitializeAfterConsentAsync();
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
        ApplyAccent(LoadAccentColor());
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
        ApplyAccent(LoadAccentColor());
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

    private void SettingsPage_AccentColorChanged(object? sender, Color color)
    {
        ApplyAccent(color);
        SaveAccentColor(color);
        ApplyTitleBarTheme();
    }

    private void ApplyAccent(Color color)
    {
        ThemeManager.ApplyAccent(color);
        ApplyNavigationAccent(color);

        ThemeButton.Background = new SolidColorBrush(color);
        ThemeButton.BorderBrush = new SolidColorBrush(color);
        ThemeButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

        _overviewPage?.ApplyAccent(color);
        _settingsPage?.ApplyAccent(color);
    }

    private bool _refreshingNavigationTheme;

    private void ApplyNavigationAccent(Color color)
    {
        var accentBrush = new SolidColorBrush(color);
        var selectedBackground = new SolidColorBrush(Color.FromArgb(36, color.R, color.G, color.B));
        var hoverBackground = new SolidColorBrush(Color.FromArgb(52, color.R, color.G, color.B));
        var pressedBackground = new SolidColorBrush(Color.FromArgb(72, color.R, color.G, color.B));
        var lowBrush = new SolidColorBrush(Color.FromArgb(54, color.R, color.G, color.B));
        var mediumBrush = new SolidColorBrush(Color.FromArgb(90, color.R, color.G, color.B));
        var highBrush = new SolidColorBrush(Color.FromArgb(145, color.R, color.G, color.B));

        SetNavigationResources(Nav.Resources, accentBrush, selectedBackground, hoverBackground, pressedBackground, lowBrush, mediumBrush, highBrush);
        ApplyNavigationItemResources(NavOverview, accentBrush, selectedBackground, hoverBackground, pressedBackground, lowBrush, mediumBrush, highBrush);
        ApplyNavigationItemResources(NavApps, accentBrush, selectedBackground, hoverBackground, pressedBackground, lowBrush, mediumBrush, highBrush);
        ApplyNavigationItemResources(NavStats, accentBrush, selectedBackground, hoverBackground, pressedBackground, lowBrush, mediumBrush, highBrush);
        ApplyNavigationItemResources(NavSettings, accentBrush, selectedBackground, hoverBackground, pressedBackground, lowBrush, mediumBrush, highBrush);

        // NavigationViewItem keeps its materialized template/resources. Rebuilding
        // the four existing items is the reliable way to make the selection
        // indicator and selected foreground pick up the new accent immediately,
        // without closing the window and without toggling pane state.
        if (Nav.XamlRoot != null && !_refreshingNavigationTheme)
        {
            var selectedTag = (Nav.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            var paneOpen = Nav.IsPaneOpen;
            _savedPaneLength = Nav.OpenPaneLength;
            _refreshingNavigationTheme = true;
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    Nav.MenuItems.Clear();
                    Nav.MenuItems.Add(NavOverview);
                    Nav.MenuItems.Add(NavApps);
                    Nav.MenuItems.Add(NavStats);
                    Nav.MenuItems.Add(NavSettings);
                    Nav.OpenPaneLength = _savedPaneLength;
                    if (!string.IsNullOrWhiteSpace(selectedTag))
                    {
                        foreach (var item in Nav.MenuItems)
                        {
                            if (item is NavigationViewItem nvi && string.Equals(nvi.Tag?.ToString(), selectedTag, StringComparison.Ordinal))
                            {
                                Nav.SelectedItem = nvi;
                                break;
                            }
                        }
                    }
                    Nav.IsPaneOpen = paneOpen;
                    Nav.UpdateLayout();
                }
                finally
                {
                    _refreshingNavigationTheme = false;
                }
            });
        }
    }

    private static void ApplyNavigationItemResources(
        NavigationViewItem item,
        SolidColorBrush accentBrush,
        SolidColorBrush selectedBackground,
        SolidColorBrush hoverBackground,
        SolidColorBrush pressedBackground,
        SolidColorBrush lowBrush,
        SolidColorBrush mediumBrush,
        SolidColorBrush highBrush)
    {
        SetNavigationResources(item.Resources, accentBrush, selectedBackground, hoverBackground, pressedBackground, lowBrush, mediumBrush, highBrush);
    }

    private static void SetNavigationResources(ResourceDictionary resources, SolidColorBrush accentBrush, SolidColorBrush selectedBackground, SolidColorBrush hoverBackground, SolidColorBrush pressedBackground, SolidColorBrush lowBrush, SolidColorBrush mediumBrush, SolidColorBrush highBrush)
    {
        resources["NavigationViewSelectionIndicatorForeground"] = accentBrush;
        resources["NavigationViewItemForegroundSelected"] = accentBrush;
        resources["NavigationViewItemIconForegroundSelected"] = accentBrush;
        resources["NavigationViewItemBackgroundSelected"] = selectedBackground;
        resources["NavigationViewItemBackgroundSelectedPointerOver"] = hoverBackground;
        resources["NavigationViewItemBackgroundSelectedPressed"] = pressedBackground;
        resources["SystemControlHighlightListAccentLowBrush"] = lowBrush;
        resources["SystemControlHighlightListAccentMediumBrush"] = mediumBrush;
        resources["SystemControlHighlightListAccentHighBrush"] = highBrush;
    }

    private Color LoadAccentColor()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserSettingsKey);
            var value = key?.GetValue(AccentColorValue) as string;
            if (ThemeManager.TryParseHex(value, out var color)) return color;
        }
        catch { }
        return ThemeManager.DefaultAccent;
    }

    private static void SaveAccentColor(Color color)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UserSettingsKey);
            key?.SetValue(AccentColorValue, ThemeManager.ToHex(color));
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

    private bool HasAcceptedTerms()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserSettingsKey);
            return key?.GetValue(TermsAcceptedValue) is int value && value == 1;
        }
        catch { return false; }
    }

    private void SaveTermsAccepted()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UserSettingsKey);
            key?.SetValue(TermsAcceptedValue, 1, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch { }
    }

    private async Task InitializeAfterConsentAsync()
    {
        await Task.Delay(120);
        if (HasAcceptedTerms())
        {
            StartCollector();
            return;
        }

        var en = _languageMode == LanguageMode.English;
        var fullText = en
            ? "TERMS OF USE\n\nVersion: v0.5.0\n\n1. ScreenTime RS records Windows application and screen usage time locally for personal management and reference.\n2. The software is provided as implemented and may not work identically on every Windows environment, third-party application, or future system update.\n3. Users are responsible for reviewing the recorded scope and for decisions made based on the statistics.\n4. Do not use the software for activities that violate applicable laws or the legitimate rights of others.\n\nPRIVACY POLICY\n\nVersion: v0.5.0\n\n1. ScreenTime RS stores its core statistics locally and does not actively upload them to a remote server.\n2. To provide usage statistics, the software may store application names, executable paths, usage durations, and necessary local runtime state.\n3. Data is stored by default under the current Windows user's LocalAppData directory. Uninstalling the program does not automatically delete these statistics.\n4. The software does not collect personal information for advertising tracking and does not actively sell or share usage statistics with third parties.\n5. Windows, antivirus software, or other system components may have independent system-level access to data; those third-party practices are outside this policy."
            : "使用条款\n\n版本：v0.5.0\n\n1. ScreenTime RS 用于在本机统计 Windows 应用与屏幕使用时间，统计结果仅供个人管理和参考。\n2. 软件按现有功能提供，不保证在所有 Windows 环境、第三方应用或未来系统更新中始终正常工作。\n3. 用户应自行确认软件记录范围，并对基于统计结果作出的决定负责。\n4. 不得利用本软件进行违反适用法律法规或侵犯他人合法权益的活动。\n\n隐私政策\n\n版本：v0.5.0\n\n1. ScreenTime RS 的核心统计数据保存在本机，不由软件主动上传到远程服务器。\n2. 为完成统计，软件可能保存应用名称、可执行文件路径、使用时长以及必要的本机运行状态。\n3. 数据默认存储在当前 Windows 用户的 LocalAppData 目录中。卸载程序不会自动删除这些统计数据。\n4. 软件不以广告追踪为目的收集个人信息，也不会主动将统计数据出售或共享给第三方。\n5. Windows、杀毒软件或其他系统组件可能拥有独立的系统级数据访问能力，本政策不涵盖这些第三方行为。";

        var content = new ScrollViewer
        {
            MaxHeight = 500,
            Content = new TextBlock
            {
                Text = fullText,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            }
        };

        var dialog = new ContentDialog
        {
            Title = en ? "Terms of Use & Privacy Policy" : "用户条款与隐私政策",
            Content = content,
            PrimaryButtonText = en ? "Accept and Continue" : "同意并继续",
            CloseButtonText = en ? "Decline and Exit" : "不同意并退出",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = RootGrid.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            SaveTermsAccepted();
            StartCollector();
        }
        else
        {
            Close();
        }
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
        if (_refreshingNavigationTheme) return;
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

    public static SolidColorBrush AccentBrush()
    {
        if (Application.Current.Resources.TryGetValue("ScreenTimeAccentBrush", out var value) && value is SolidColorBrush brush)
            return brush;
        return new SolidColorBrush(ThemeManager.DefaultAccent);
    }

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
