using Microsoft.UI.Windowing;
using Microsoft.UI.Input;
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
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Windows.UI;

namespace ScreenTimeRS.UI;

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

public sealed class NavigationResizeHandle : Grid
{

    public NavigationResizeHandle()
    {
    }

    public void SetResizeCursor()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
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
    private const string TermsAcceptedValue = "TermsAccepted_v0_7_0";
    private double _savedPaneLength = 320;
    private bool _resizingPane;
    private Snapshot _latestSnapshot = new();
    private uint _resizePointerId;

    private readonly OverviewPage _overviewPage;
    private readonly AppsPage _appsPage;
    private readonly StatsPage _statsPage;
    private readonly SettingsPage _settingsPage;

    private LanguageMode UiLanguage => LanguageResolver.Resolve(_languageMode);

    public MainWindow()
    {
        InitializeComponent();
        NavResizeHandle.SetResizeCursor();

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
        SetTitleBar(AppTitleBarDragRegion);

        Nav.RegisterPropertyChangedCallback(NavigationView.IsPaneOpenProperty, (_, _) =>
        {
            SaveNavigationPaneState(Nav.IsPaneOpen);
            if (Nav.IsPaneOpen)
                Nav.OpenPaneLength = _savedPaneLength;
            UpdateNavigationResizeHandle();
        });
        Closed += (_, _) =>
        {
            SaveNavigationPaneState(Nav.IsPaneOpen);
            SaveNavigationPaneLength(Nav.OpenPaneLength);
        };
        _savedPaneLength = LoadNavigationPaneLength();
        Nav.OpenPaneLength = _savedPaneLength;
        Nav.IsPaneOpen = LoadNavigationPaneState();
        Nav.SizeChanged += (_, _) => UpdateNavigationResizeHandle();
        RootGrid.SizeChanged += (_, _) => UpdateNavigationResizeHandle();
        UpdateNavigationResizeHandle();

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
        _settingsPage.ExportDataRequested += SettingsPage_ExportDataRequested;
        _settingsPage.ImportDataRequested += SettingsPage_ImportDataRequested;
        _settingsPage.DeleteAllDataRequested += SettingsPage_DeleteAllDataRequested;
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
        UpdateNavigationResizeHandle();
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


        _overviewPage?.ApplyAccent(color);
        _appsPage?.ApplyAccent(color);
        _statsPage?.ApplyAccent(color);
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
        var effective = LanguageResolver.Resolve(language);

        switch (effective)
        {
            case LanguageMode.English:
                NavOverview.Content = "Overview";
                NavApps.Content = "App usage";
                NavStats.Content = "Statistics";
                NavSettings.Content = "Settings";
                break;
            case LanguageMode.TraditionalChinese:
                NavOverview.Content = "概覽";
                NavApps.Content = "應用程式使用時間";
                NavStats.Content = "統計";
                NavSettings.Content = "設定";
                break;
            default:
                NavOverview.Content = "概览";
                NavApps.Content = "应用使用时间";
                NavStats.Content = "统计";
                NavSettings.Content = "设置";
                break;
        }
        _overviewPage.ApplyLanguage(effective);
        _appsPage.ApplyLanguage(effective);
        _statsPage.ApplyLanguage(effective);
        _settingsPage.SetLanguage(language, effective);
    }


    private LanguageMode LoadLanguageMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserSettingsKey);
            var value = key?.GetValue(LanguageModeValue);
            return value switch
            {
                string text when string.Equals(text, "system", StringComparison.OrdinalIgnoreCase) => LanguageMode.System,
                string text when string.Equals(text, "en", StringComparison.OrdinalIgnoreCase) => LanguageMode.English,
                string text when string.Equals(text, "zh-TW", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "zh-Hant", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "zh-HK", StringComparison.OrdinalIgnoreCase) => LanguageMode.TraditionalChinese,
                _ => LanguageMode.SimplifiedChinese
            };
        }
        catch
        {
            return LanguageMode.SimplifiedChinese;
        }
    }

    private static void SaveLanguageMode(LanguageMode mode)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UserSettingsKey);
            key?.SetValue(LanguageModeValue, mode switch
            {
                LanguageMode.System => "system",
                LanguageMode.English => "en",
                LanguageMode.TraditionalChinese => "zh-TW",
                _ => "zh-CN"
            });
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

        var uiLanguage = UiLanguage;
        var en = uiLanguage == LanguageMode.English;
        var hant = uiLanguage == LanguageMode.TraditionalChinese;
        var fullText = en
            ? "TERMS OF USE\n\nVersion: v0.7.4\n\n1. ScreenTime RS records Windows application and screen usage time locally for personal management and reference.\n2. The software is provided as implemented and may not work identically on every Windows environment, third-party application, or future system update.\n3. Users are responsible for reviewing the recorded scope and for decisions made based on the statistics.\n4. Do not use the software for activities that violate applicable laws or the legitimate rights of others.\n\nPRIVACY POLICY\n\nVersion: v0.7.4\n\n1. ScreenTime RS stores its core statistics locally and does not actively upload them to a remote server.\n2. To provide usage statistics, the software may store application names, executable paths, usage durations, and necessary local runtime state.\n3. Data is stored by default under the current Windows user's LocalAppData directory. Uninstalling the program does not automatically delete these statistics.\n4. The software does not collect personal information for advertising tracking and does not actively sell or share usage statistics with third parties.\n5. Windows, antivirus software, or other system components may have independent system-level access to data; those third-party practices are outside this policy."
            : hant
                ? "使用條款\n\n版本：v0.7.4\n\n1. ScreenTime RS 用於在本機統計 Windows 應用程式與螢幕使用時間，統計結果僅供個人管理與參考。\n2. 軟體依現有功能提供，不保證在所有 Windows 環境、第三方應用程式或未來系統更新中始終正常運作。\n3. 使用者應自行確認軟體記錄範圍，並對根據統計結果作出的決定負責。\n4. 不得利用本軟體進行違反適用法律法規或侵犯他人合法權益的活動。\n\n隱私權政策\n\n版本：v0.7.4\n\n1. ScreenTime RS 的核心統計資料儲存在本機，軟體不會主動上傳至遠端伺服器。\n2. 為提供統計功能，軟體可能儲存應用程式名稱、可執行檔路徑、使用時間以及必要的本機執行狀態。\n3. 資料預設儲存在目前 Windows 使用者的 LocalAppData 目錄中。解除安裝程式不會自動刪除這些統計資料。\n4. 軟體不會以廣告追蹤為目的收集個人資訊，也不會主動出售或分享統計資料給第三方。\n5. Windows、防毒軟體或其他系統元件可能具有獨立的系統層級資料存取能力；這些第三方行為不在本政策範圍內。"
                : "使用条款\n\n版本：v0.7.4\n\n1. ScreenTime RS 用于在本机统计 Windows 应用与屏幕使用时间，统计结果仅供个人管理和参考。\n2. 软件按现有功能提供，不保证在所有 Windows 环境、第三方应用或未来系统更新中始终正常工作。\n3. 用户应自行确认软件记录范围，并对基于统计结果作出的决定负责。\n4. 不得利用本软件进行违反适用法律法规或侵犯他人合法权益的活动。\n\n隐私政策\n\n版本：v0.7.4\n\n1. ScreenTime RS 的核心统计数据保存在本机，不由软件主动上传到远程服务器。\n2. 为完成统计，软件可能保存应用名称、可执行文件路径、使用时长以及必要的本机运行状态。\n3. 数据默认存储在当前 Windows 用户的 LocalAppData 目录中。卸载程序不会自动删除这些统计数据。\n4. 软件不以广告追踪为目的收集个人信息，也不会主动将统计数据出售或共享给第三方。\n5. Windows、杀毒软件或其他系统组件可能拥有独立的系统级数据访问能力，本政策不涵盖这些第三方行为。";

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
            RequestedTheme = RootGrid.ActualTheme,
            Title = en ? "Terms of Use & Privacy Policy" : hant ? "使用條款與隱私權政策" : "用户条款与隐私政策",
            Content = content,
            PrimaryButtonText = en ? "Accept and Continue" : hant ? "同意並繼續" : "同意并继续",
            CloseButtonText = en ? "Decline and Exit" : hant ? "不同意並退出" : "不同意并退出",
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



    private void NavResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!Nav.IsPaneOpen) return;
        var point = e.GetCurrentPoint(NavResizeCanvas);
        if (!point.Properties.IsLeftButtonPressed) return;

        _resizingPane = true;
        _resizePointerId = point.PointerId;
        NavResizeHandle.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void NavResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizingPane || e.Pointer.PointerId != _resizePointerId) return;

        var point = e.GetCurrentPoint(NavResizeCanvas);
        var width = Math.Clamp(point.Position.X, 220, 480);
        Nav.OpenPaneLength = width;
        _savedPaneLength = width;

        // Keep the transparent hit zone stationary during a captured drag.
        // Repositioning it on every move can make the pointer repeatedly
        // enter/leave the boundary and cause cursor flicker.
        e.Handled = true;
    }

    private void NavResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizingPane || e.Pointer.PointerId != _resizePointerId) return;

        _resizingPane = false;
        _savedPaneLength = Nav.OpenPaneLength;
        SaveNavigationPaneLength(_savedPaneLength);
        try { NavResizeHandle.ReleasePointerCapture(e.Pointer); } catch { }
        UpdateNavigationResizeHandle();
        e.Handled = true;
    }

    private void NavResizeHandle_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        FinishNavigationResize();
    }

    private void NavResizeHandle_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (_resizingPane)
            FinishNavigationResize();
    }

    private void FinishNavigationResize()
    {
        if (!_resizingPane) return;
        _resizingPane = false;
        _savedPaneLength = Nav.OpenPaneLength;
        SaveNavigationPaneLength(_savedPaneLength);
        try { NavResizeHandle.ReleasePointerCaptures(); } catch { }
        UpdateNavigationResizeHandle();
    }

    private void UpdateNavigationResizeHandle()
    {
        if (Nav.IsPaneOpen)
        {
            NavResizeHandle.Visibility = Visibility.Visible;
            NavResizeHandle.Height = Math.Max(1, RootGrid.ActualHeight - 42);
            Canvas.SetLeft(NavResizeHandle, Math.Max(0, Nav.OpenPaneLength - 11));
            Canvas.SetTop(NavResizeHandle, 0);
        }
        else
        {
            NavResizeHandle.Visibility = Visibility.Collapsed;
        }
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
        UpdateNavigationResizeHandle();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_refreshingNavigationTheme) return;
        ShowSelectedPage();
        UpdateVisiblePage();
    }

    private async void SettingsPage_ExportDataRequested(object? sender, EventArgs e)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"ScreenTimeRS-data-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };
        picker.FileTypeChoices.Add(UiLanguage == LanguageMode.English ? "JSON data" : UiLanguage == LanguageMode.TraditionalChinese ? "JSON 資料" : "JSON 数据", new List<string> { ".json" });
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        var ok = await RunCollectorCommandAsync("--export-data", file.Path);
        await ShowDataOperationResultAsync(ok,
            UiText.ExportSuccess(UiLanguage),
            UiText.ExportFailed(UiLanguage));
    }

    private async void SettingsPage_ImportDataRequested(object? sender, EventArgs e)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(".json");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var confirmed = await ShowConfirmationAsync(
            UiText.ImportTitle(UiLanguage),
            UiText.ImportConfirm(UiLanguage),
            UiText.ImportAction(UiLanguage),
            UiText.Cancel(UiLanguage));
        if (!confirmed) return;

        var ok = await RunCollectorCommandAsync("--import-data", file.Path);
        await ShowDataOperationResultAsync(ok,
            UiText.ImportSuccess(UiLanguage),
            UiText.ImportFailed(UiLanguage));
        if (ok)
            await RefreshAfterDataChangeAsync();
    }

    private async void SettingsPage_DeleteAllDataRequested(object? sender, EventArgs e)
    {
        var code = DataConfirmationCode.Generate();
        var dialogContent = new StackPanel { Spacing = 12 };
        dialogContent.Children.Add(new TextBlock
        {
            Text = UiText.DeleteInstruction(UiLanguage),
            TextWrapping = TextWrapping.Wrap
        });
        dialogContent.Children.Add(new TextBlock
        {
            // Keep the confirmation code visually spaced without relying on
            // TextBlock.LetterSpacing, which is not exposed by WinUI 3.
            Text = string.Join(" ", code.ToCharArray()),
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        var input = new TextBox
        {
            PlaceholderText = UiText.DeletePlaceholder(UiLanguage),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        dialogContent.Children.Add(input);

        // Preserve the original v0.7.0 ContentDialog layout and change only
        // the destructive button color.
        var dialog = new ContentDialog
        {
            RequestedTheme = RootGrid.ActualTheme,
            Title = UiText.DeleteTitle(UiLanguage),
            Content = dialogContent,
            PrimaryButtonText = UiText.DeleteAction(UiLanguage),
            CloseButtonText = UiText.Cancel(UiLanguage),
            PrimaryButtonStyle = null,
            IsPrimaryButtonEnabled = false,
            XamlRoot = ContentFrame.XamlRoot
        };

        void UpdateDeleteButtonState()
        {
            var valid = string.Equals(input.Text.Trim(), code, StringComparison.Ordinal);
            dialog.IsPrimaryButtonEnabled = valid;
            dialog.PrimaryButtonStyle = valid
                ? CreateDangerButtonStyle()
                : null;
        }

        input.TextChanged += (_, _) => UpdateDeleteButtonState();
        UpdateDeleteButtonState();

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var finalDialog = new ContentDialog
        {
            RequestedTheme = RootGrid.ActualTheme,
            Title = UiText.DeleteFinalTitle(UiLanguage),
            Content = new TextBlock
            {
                Text = UiText.DeleteFinalInstruction(UiLanguage),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 620
            },
            PrimaryButtonText = UiText.DeleteAction(UiLanguage),
            CloseButtonText = UiText.Cancel(UiLanguage),
            PrimaryButtonStyle = CreateDangerButtonStyle(),
            XamlRoot = ContentFrame.XamlRoot
        };

        var finalResult = await finalDialog.ShowAsync();
        if (finalResult != ContentDialogResult.Primary) return;

        var ok = await RunCollectorCommandAsync("--clear-data", code);
        await ShowDataOperationResultAsync(ok,
            UiText.DeleteSuccess(UiLanguage),
            UiText.DeleteFailed(UiLanguage));
        if (ok)
            await RefreshAfterDataChangeAsync();
    }

    private static Style CreateDangerButtonStyle()
    {
        var style = new Style(typeof(Button));
        if (Application.Current.Resources.TryGetValue("DefaultButtonStyle", out var baseStyle) && baseStyle is Style defaultStyle)
            style.BasedOn = defaultStyle;

        var danger = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
        style.Setters.Add(new Setter(Button.BackgroundProperty, danger));
        style.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Microsoft.UI.Colors.White)));
        style.Setters.Add(new Setter(Button.BorderBrushProperty, danger));
        return style;
    }

    private async Task<bool> RunCollectorCommandAsync(string command, string argument)
    {
        try
        {
            var exe = Path.Combine(AppContext.BaseDirectory, "screentime-rs.exe");
            if (!File.Exists(exe))
                exe = Path.Combine(AppContext.BaseDirectory, "..", "screentime-rs.exe");
            if (!File.Exists(exe)) return false;

            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
            psi.ArgumentList.Add(command);
            psi.ArgumentList.Add(argument);
            using var process = Process.Start(psi);
            if (process is null) return false;
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    private async Task RefreshAfterDataChangeAsync()
    {
        for (int i = 0; i < 6; i++)
        {
            await Task.Delay(300);
            UpdatePages(LoadSnapshot() ?? new Snapshot());
        }
    }

    private async Task ShowDataOperationResultAsync(bool success, string successText, string failedText)
    {
        var dialog = new ContentDialog
        {
            RequestedTheme = RootGrid.ActualTheme,
            Title = success ? UiText.Done(UiLanguage) : UiText.Error(UiLanguage),
            Content = success ? successText : failedText,
            CloseButtonText = UiText.Close(UiLanguage),
            XamlRoot = ContentFrame.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task<bool> ShowConfirmationAsync(string title, string content, string primary, string close)
    {
        var dialog = new ContentDialog
        {
            RequestedTheme = RootGrid.ActualTheme,
            Title = title,
            Content = content,
            PrimaryButtonText = primary,
            CloseButtonText = close,
            XamlRoot = ContentFrame.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
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
    public AppStat[] apps_90_days { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_30_days { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_all { get; set; } = Array.Empty<AppStat>();
    public JsonDaily[] daily { get; set; } = Array.Empty<JsonDaily>();
    public JsonDaily[] daily_year { get; set; } = Array.Empty<JsonDaily>();
    public JsonDaily[] daily_all { get; set; } = Array.Empty<JsonDaily>();
    public bool locked { get; set; }
    public bool monitor_on { get; set; }
    public bool active_now { get; set; }
    public ulong idle_seconds { get; set; }
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

    public static string FormatIdle(ulong seconds, LanguageMode language)
    {
        var hant = language == LanguageMode.TraditionalChinese;
        if (seconds < 60)
            return language == LanguageMode.English ? $"{seconds}s" : $"{seconds}秒";
        var minutes = seconds / 60;
        if (minutes < 60)
            return language == LanguageMode.English ? $"{minutes}m" : hant ? $"{minutes}分鐘" : $"{minutes}分钟";
        var hours = minutes / 60;
        return language == LanguageMode.English ? $"{hours}h {minutes % 60}m" : hant ? $"{hours}小時 {minutes % 60}分鐘" : $"{hours}小时 {minutes % 60}分钟";
    }

    public static string Format(long s) => Format(s, LanguageMode.SimplifiedChinese);

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
        return language switch
        {
            LanguageMode.English => $"{hours:00}h {minutes:00}m",
            LanguageMode.TraditionalChinese => $"{hours:00}小時 {minutes:00}分鐘",
            _ => $"{hours:00}小时 {minutes:00}分钟"
        };
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

    public static string FriendlyName(AppStat app, LanguageMode language = LanguageMode.SimplifiedChinese)
    {
        var n = Path.GetFileNameWithoutExtension(app.name);
        var special = n.ToLowerInvariant() switch
        {
            "explorer" => language == LanguageMode.TraditionalChinese ? "檔案總管" : language == LanguageMode.English ? "File Explorer" : "文件资源管理器",
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
