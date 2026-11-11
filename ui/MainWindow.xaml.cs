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
using System.Linq;
using System.Reflection;
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

public sealed partial class MainWindow : Window
{
    private readonly string _snapshotPath;
    private readonly string _analyticsPath;
    private readonly string _shutdownPath;
    private Process? _collector;
    private ThemeMode _themeMode;
    private LanguageMode _languageMode;

    private const string UserSettingsKey = @"Software\ScreenTimeRS";
    private const string NavigationPaneValue = "NavigationPaneOpen";
    private const string ThemeModeValue = "ThemeMode";
    private const string LanguageModeValue = "LanguageMode";
    private const string NavigationPaneLengthValue = "NavigationPaneLength";
    private const string TermsAcceptedValue = "TermsAccepted_v0_9_2";
    private double _savedPaneLength = 320;
    private bool _resizingPane;
    private Snapshot _latestSnapshot = new();
    private uint _resizePointerId;
    private int _lastFocusCompletedCount = -1;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = false)]
    private static extern bool MessageBeep(uint uType);

    private readonly OverviewPage _overviewPage;
    private readonly AppsPage _appsPage;
    private readonly StatsPage _statsPage;
    private FocusPage? _focusPage;
    private UIElement? _focusContent;
    private readonly FocusHistoryPage _focusHistoryPage;
    private readonly SettingsPage _settingsPage;
    private readonly UsageShellPage _usageShell;
    private readonly FocusShellPage _focusShell;

    private LanguageMode UiLanguage => LanguageResolver.Resolve(_languageMode);

    public MainWindow()
    {
        InitializeComponent();
        SetResizeCursor(NavResizeHandle);

        // ProjectDirs::data_local_dir() on Windows resolves to:
        // %LOCALAPPDATA%\ScreenTimeRS\ScreenTime RS\data
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenTimeRS", "ScreenTime RS", "data");

        _snapshotPath = Path.Combine(dataDir, "snapshot.json");
        _analyticsPath = Path.Combine(dataDir, "analytics.json");
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
        var accent = CurrentAccent();
        ThemeManager.ApplyAccent(accent);

        _overviewPage = new OverviewPage();
        _overviewPage.PauseToggleRequested += TogglePause;
        _appsPage = new AppsPage(LoadDayUsageAsync, ShowAppDetailsAsync, LoadAppsAllTimeAsync);
        _statsPage = new StatsPage(LoadStatsPeriodAsync);
        _focusHistoryPage = new FocusHistoryPage(LoadFocusHistoryAsync, RunCollectorCommandAsync, () => { }, RefreshRecentFocusHistory);
        _focusHistoryPage.ApplyAccent(accent);
        _focusHistoryPage.ApplyLanguage(UiLanguage);
        _focusHistoryPage.SetEmbedded(true);
        _settingsPage = new SettingsPage(_themeMode, _languageMode, accent);
        _settingsPage.ThemeModeChanged += SettingsPage_ThemeModeChanged;
        _settingsPage.LanguageChanged += SettingsPage_LanguageChanged;
        _settingsPage.ExportDataRequested += SettingsPage_ExportDataRequested;
        _settingsPage.ImportDataRequested += SettingsPage_ImportDataRequested;
        _settingsPage.DeleteAllDataRequested += SettingsPage_DeleteAllDataRequested;
        _settingsPage.PauseRequested += SettingsPage_PauseRequested;
        _settingsPage.BackupRequested += SettingsPage_BackupRequested;
        _settingsPage.DiagnosticsRequested += SettingsPage_DiagnosticsRequested;
        _usageShell = new UsageShellPage(_statsPage, _appsPage);
        _focusShell = new FocusShellPage(GetFocusContent, _focusHistoryPage);
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


    private static void SetResizeCursor(UIElement element)
    {
        try
        {
            var cursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            typeof(UIElement).InvokeMember(
                "ProtectedCursor",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                binder: null,
                target: element,
                args: new object?[] { cursor });
        }
        catch { }
    }

    private void RootGrid_ActualThemeChanged(FrameworkElement sender, object args)
    {
        ApplyAccent(CurrentAccent());
        ApplyTitleBarTheme();
    }

    private Color CurrentAccent() =>
        RootGrid.ActualTheme == ElementTheme.Dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black;

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
        ApplyAccent(CurrentAccent());
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

    private void SettingsPage_PauseRequested(object? sender, EventArgs e) => TogglePause();

    private void TogglePause()
    {
        try
        {
            var pausePath = Path.Combine(Path.GetDirectoryName(_shutdownPath)!, "pause.flag");
            if (File.Exists(pausePath)) File.Delete(pausePath); else File.WriteAllText(pausePath, "paused");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(pausePath)!, "refresh.flag"), "refresh");
        }
        catch { }
    }

    private async void SettingsPage_BackupRequested(object? sender, EventArgs e)
    {
        try
        {
            var exe = Path.Combine(AppContext.BaseDirectory, "screentime-rs.exe");
            var psi = new ProcessStartInfo(exe, "--backup") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            using var process = Process.Start(psi);
            var path = process == null ? "" : await process.StandardOutput.ReadToEndAsync();
            if (process != null) await process.WaitForExitAsync();
            var en = UiLanguage == LanguageMode.English; var hant = UiLanguage == LanguageMode.TraditionalChinese;
            var dialog = new ContentDialog { Title = en ? "Backup created" : hant ? "備份已建立" : "备份已创建", Content = path.Trim(), CloseButtonText = en ? "OK" : hant ? "確定" : "确定", XamlRoot = ContentFrame.XamlRoot };
            await dialog.ShowAsync();
        } catch { }
    }

    private async void SettingsPage_DiagnosticsRequested(object? sender, EventArgs e)
    {
        try
        {
            var exe = Path.Combine(AppContext.BaseDirectory, "screentime-rs.exe");
            var psi = new ProcessStartInfo(exe, "--diagnostics") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            using var process = Process.Start(psi); var text = process == null ? "" : await process.StandardOutput.ReadToEndAsync();
            if (process != null) await process.WaitForExitAsync();
            var en = UiLanguage == LanguageMode.English; var hant = UiLanguage == LanguageMode.TraditionalChinese;
            var dialog = new ContentDialog { Title = en ? "Diagnostics" : hant ? "診斷信息" : "诊断信息", Content = new ScrollViewer { MaxHeight = 420, Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap } }, CloseButtonText = en ? "Close" : hant ? "關閉" : "关闭", XamlRoot = ContentFrame.XamlRoot };
            await dialog.ShowAsync();
        } catch { }
    }

    private void ApplyAccent(Color color)
    {
        ThemeManager.ApplyAccent(color);

        _overviewPage?.ApplyAccent(color);
        _usageShell?.ApplyAccent(color);
        _focusShell?.ApplyAccent(color);
        _settingsPage?.ApplyAccent(color);
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
                NavUsage.Content = "Usage";
                NavFocus.Content = "Focus";
                NavSettings.Content = "Settings";
                break;
            case LanguageMode.TraditionalChinese:
                NavOverview.Content = "概覽";
                NavUsage.Content = "使用";
                NavFocus.Content = "專注";
                NavSettings.Content = "設定";
                break;
            default:
                NavOverview.Content = "概览";
                NavUsage.Content = "使用";
                NavFocus.Content = "专注";
                NavSettings.Content = "设置";
                break;
        }
        _overviewPage.ApplyLanguage(effective);
        _usageShell.ApplyLanguage(effective);
        _focusShell.ApplyLanguage(effective);
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
        var fullText = LegalDocuments.Combined(uiLanguage);

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
            // The collector writes once per second, but the overview does not need
            // to repaint that often. Two seconds keeps the UI calm and cuts the
            // per-tick JSON parse in half.
            await Task.Delay(2000);

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

            var snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(_snapshotPath)) ?? new Snapshot();
            if (File.Exists(_analyticsPath))
            {
                var analytics = JsonSerializer.Deserialize<AnalyticsSnapshot>(File.ReadAllText(_analyticsPath));
                if (analytics is not null)
                {
                    snapshot.apps_week = analytics.apps_week;
                    snapshot.apps_month = analytics.apps_month;
                    snapshot.apps_half_year = analytics.apps_half_year;
                    snapshot.apps_year = analytics.apps_year;
                    snapshot.apps_90_days = analytics.apps_90_days;
                    snapshot.apps_30_days = analytics.apps_30_days;
                    snapshot.daily = analytics.daily;
                    snapshot.daily_year = analytics.daily_year;
                }
            }
            return snapshot;
        }
        catch { return null; }
    }

    private UIElement GetFocusContent()
    {
        if (_focusContent is not null)
            return _focusContent;

        try
        {
            _focusPage = new FocusPage(RunCollectorCommandAsync, LoadFocusHistoryAsync, NavigateToFocusHistory);
            _focusPage.ApplyAccent(CurrentAccent());
            _focusPage.ApplyLanguage(UiLanguage);
            _focusContent = _focusPage;
            return _focusContent;
        }
        catch (Exception ex)
        {
            LogFocusUiError(ex);
            var en = UiLanguage == LanguageMode.English;
            var hant = UiLanguage == LanguageMode.TraditionalChinese;
            var page = new Page
            {
                Content = new Border
                {
                    Padding = new Thickness(28),
                    Child = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = en ? "Focus" : hant ? "專注" : "专注",
                                FontSize = 30,
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                            },
                            new TextBlock
                            {
                                Text = en ? "The Focus page could not be loaded." : hant ? "專注頁面無法載入。" : "专注页面无法加载。",
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock
                            {
                                Text = en ? "The app has kept the rest of the interface available. Check ui-crash.log for details." : hant ? "其餘功能仍可正常使用。詳細錯誤請查看 ui-crash.log。" : "其余功能仍可正常使用。详细错误请查看 ui-crash.log。",
                                Opacity = .68,
                                TextWrapping = TextWrapping.Wrap
                            }
                        }
                    }
                }
            };
            _focusContent = page;
            return page;
        }
    }

    private static void LogFocusUiError(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScreenTimeRS", "ScreenTime RS", "data");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "ui-crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Focus UI error\n{ex}\n\n");
        }
        catch { }
    }

    private void ShowSelectedPage()
    {
        if (Nav.SelectedItem is not NavigationViewItem item) return;

        ContentFrame.Content = item.Tag?.ToString() switch
        {
            "overview" => _overviewPage,
            "usage" => _usageShell,
            "focus" => ActivateFocusShell(),
            "settings" => _settingsPage,
            _ => _overviewPage
        };
    }

    private UIElement ActivateFocusShell()
    {
        _focusShell.Activate();
        return _focusShell;
    }

    private void UpdatePages(Snapshot s)
    {
        var wasFocusRunning = _latestSnapshot.focus_active || _latestSnapshot.focus_paused;
        var justCompleted = _lastFocusCompletedCount >= 0
            && s.focus_today_completed > _lastFocusCompletedCount
            && wasFocusRunning
            && !s.focus_active
            && !s.focus_paused;

        _lastFocusCompletedCount = (int)Math.Max(0, s.focus_today_completed);
        _latestSnapshot = s;
        _settingsPage.SetMonitoringState(s.paused);

        if (justCompleted)
            PlayFocusCompletionSound();

        UpdateVisiblePage();
    }

    private static void PlayFocusCompletionSound()
    {
        try
        {
            // Use the Windows information notification sound so the app does not
            // need a bundled audio file or an additional media dependency.
            MessageBeep(0x00000040); // MB_ICONINFORMATION
        }
        catch { }
    }

    private void NavigateToFocusHistory()
    {
        try
        {
            // History is a tab inside the focus shell now; keep Focus selected in
            // the navigation pane and switch the shell tab instead of the frame.
            _focusShell.SelectHistory();
        }
        catch (Exception ex)
        {
            LogFocusUiError(ex);
        }
    }

    private void RefreshRecentFocusHistory()
    {
        try
        {
            _focusPage?.RefreshHistory();
        }
        catch (Exception ex)
        {
            LogFocusUiError(ex);
        }
    }

    private void UpdateVisiblePage()
    {
        switch (Nav.SelectedItem is NavigationViewItem item ? item.Tag?.ToString() : "overview")
        {
            case "usage":
                _usageShell.UpdateSnapshot(_latestSnapshot);
                break;
            case "focus":
                try
                {
                    _focusShell.UpdateSnapshot(_latestSnapshot);
                }
                catch (Exception ex)
                {
                    LogFocusUiError(ex);
                }
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
            // Keep the resize hit target deliberately narrow: only a small strip
            // directly on the pane boundary should start resizing, similar to
            // common desktop navigation panes.
            Canvas.SetLeft(NavResizeHandle, Math.Max(0, Nav.OpenPaneLength - 3));
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
        try
        {
            ShowSelectedPage();
            UpdateVisiblePage();
        }
        catch (Exception ex)
        {
            LogFocusUiError(ex);
        }
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

    private string CollectorExecutablePath()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "screentime-rs.exe");
        if (!File.Exists(exe))
            exe = Path.Combine(AppContext.BaseDirectory, "..", "screentime-rs.exe");
        return exe;
    }

    private async Task<bool> RunCollectorCommandAsync(params string[] arguments)
    {
        try
        {
            var exe = CollectorExecutablePath();
            if (!File.Exists(exe)) return false;

            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
            if (arguments is null || arguments.Length == 0) return false;
            foreach (var argument in arguments)
                psi.ArgumentList.Add(argument);
            using var process = Process.Start(psi);
            if (process is null) return false;
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    private async Task<string?> RunCollectorQueryAsync(params string[] arguments)
    {
        try
        {
            var exe = CollectorExecutablePath();
            if (!File.Exists(exe)) return null;

            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in arguments)
                psi.ArgumentList.Add(argument);

            using var process = Process.Start(psi);
            if (process is null) return null;
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();
            return process.ExitCode == 0 ? await outputTask : null;
        }
        catch { return null; }
    }

    private async Task LoadDayUsageAsync(DateTime date)
    {
        var text = await RunCollectorQueryAsync("--query-day", date.ToString("yyyy-MM-dd"));
        if (text is null) return;
        try
        {
            var data = JsonSerializer.Deserialize<DayUsageResponse>(text);
            if (data is not null)
                _appsPage.SetSelectedDay(data);
        }
        catch { }
    }

    private async Task LoadAppsAllTimeAsync()
    {
        var text = await RunCollectorQueryAsync("--query-all");
        if (text is null) return;
        try
        {
            var data = JsonSerializer.Deserialize<RangeUsageResponse>(text);
            if (data is not null)
                _appsPage.SetAllTimeData(data);
        }
        catch { }
    }

    private async Task LoadStatsPeriodAsync(int periodIndex)
    {
        if (periodIndex != 4)
        {
            _statsPage.SetAllTimeData(null);
            return;
        }

        var text = await RunCollectorQueryAsync("--query-all");
        if (text is null) return;
        try
        {
            var data = JsonSerializer.Deserialize<RangeUsageResponse>(text);
            if (data is not null)
                _statsPage.SetAllTimeData(data);
        }
        catch { }
    }

    private async Task<FocusSession[]?> LoadFocusHistoryAsync(int limit, int offset)
    {
        var text = await RunCollectorQueryAsync("--focus-history", limit.ToString(), offset.ToString());
        if (text is null) return null;
        try
        {
            var data = JsonSerializer.Deserialize<FocusHistoryResponse>(text);
            return data?.sessions ?? Array.Empty<FocusSession>();
        }
        catch { return null; }
    }

    private async Task ShowAppDetailsAsync(AppStat app)
    {
        try
        {
            var index = _appsPage.SelectedPeriodIndex;
            var args = new List<string> { "--query-app", app.name };
            if (index == 6 && _appsPage.SelectedDay is DateTime selectedDay)
            {
                var date = selectedDay.ToString("yyyy-MM-dd");
                args.Add(date);
                args.Add(date);
            }
            else if (index != 5)
            {
                var today = DateTime.Today;
                var start = index switch
                {
                    1 => today.AddDays(-(int)today.DayOfWeek + (today.DayOfWeek == DayOfWeek.Sunday ? -6 : 1)),
                    2 => new DateTime(today.Year, today.Month, 1),
                    3 => today.AddMonths(-6),
                    4 => today.AddMonths(-12),
                    _ => today
                };
                args.Add(start.ToString("yyyy-MM-dd"));
                args.Add(today.ToString("yyyy-MM-dd"));
            }

            var text = await RunCollectorQueryAsync(args.ToArray());
            if (text is null)
            {
                _appsPage.ShowDetailError();
                return;
            }

            AppHistoryResponse? history = null;
            try { history = JsonSerializer.Deserialize<AppHistoryResponse>(text); } catch { }
            if (history is null)
            {
                _appsPage.ShowDetailError();
                return;
            }

            // Share is relative to the whole selected range, not to this app's
            // own total. AppsPage caches the range denominator while rendering.
            var total = _appsPage.RangeTotal;
            if (total <= 0)
                total = history.daily.Sum(x => Math.Max(0, x.seconds));

            _appsPage.ShowDetails(history, app, total);
        }
        catch
        {
            _appsPage.ShowDetailError();
        }
    }

    private async Task RefreshAfterDataChangeAsync()
    {
        for (int i = 0; i < 6; i++)
        {
            await Task.Delay(300);
            UpdatePages(LoadSnapshot() ?? new Snapshot());
        }

        if (_appsPage.SelectedPeriodIndex == 5)
            await LoadAppsAllTimeAsync();
        else if (_appsPage.SelectedPeriodIndex == 6 && _appsPage.SelectedDay is DateTime selectedDay)
            await LoadDayUsageAsync(selectedDay);

        if (_statsPage.AllTimeTotal is not null)
            await LoadStatsPeriodAsync(4);
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
    public long current_session_seconds { get; set; }
    public long longest_session_today { get; set; }
    public long unlocks_today { get; set; }
    public bool paused { get; set; }
    public JsonDaily[] hourly_today { get; set; } = Array.Empty<JsonDaily>();
    public bool focus_active { get; set; }
    public bool focus_paused { get; set; }
    public bool focus_auto_paused { get; set; }
    public long focus_planned_seconds { get; set; }
    public long focus_active_seconds { get; set; }
    public long focus_remaining_seconds { get; set; }
    public long focus_today_total { get; set; }
    public long focus_today_completed { get; set; }
    public long focus_today_pauses { get; set; }
}

public sealed class FocusHistoryResponse
{
    public FocusSession[] sessions { get; set; } = Array.Empty<FocusSession>();
}

public sealed class AnalyticsSnapshot
{
    public AppStat[] apps_week { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_month { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_half_year { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_year { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_90_days { get; set; } = Array.Empty<AppStat>();
    public AppStat[] apps_30_days { get; set; } = Array.Empty<AppStat>();
    public JsonDaily[] daily { get; set; } = Array.Empty<JsonDaily>();
    public JsonDaily[] daily_year { get; set; } = Array.Empty<JsonDaily>();
}

public sealed class DayUsageResponse
{
    public string date { get; set; } = "";
    public long total_seconds { get; set; }
    public AppStat[] apps { get; set; } = Array.Empty<AppStat>();
}

public sealed class RangeUsageResponse
{
    public string start_date { get; set; } = "";
    public string end_date { get; set; } = "";
    public long total_seconds { get; set; }
    public AppStat[] apps { get; set; } = Array.Empty<AppStat>();
    public JsonDaily[] daily { get; set; } = Array.Empty<JsonDaily>();
}

public sealed class AppHistoryResponse
{
    public string app { get; set; } = "";
    public long total_seconds { get; set; }
    public JsonDaily[] daily { get; set; } = Array.Empty<JsonDaily>();
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
            "cursor" => "Cursor",
            "devenv" => "Microsoft Visual Studio",
            "discord" => "Discord",
            "slack" => "Slack",
            "telegram" => "Telegram",
            "steam" => "Steam",
            "steamwebhelper" => "Steam",
            "msteams" => "Microsoft Teams",
            "ms-teams" => "Microsoft Teams",
            "teams" => "Microsoft Teams",
            "winword" => "Microsoft Word",
            "excel" => "Microsoft Excel",
            "powerpnt" => "Microsoft PowerPoint",
            "outlook" => "Microsoft Outlook",
            "notepad" => "Notepad",
            "mspaint" => "Paint",
            "windowsterminal" => "Windows Terminal",
            "wt" => "Windows Terminal",
            "taskmgr" => "Task Manager",
            "snippingtool" => "Snipping Tool",
            "wezterm" => "WezTerm",
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
