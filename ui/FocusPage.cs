using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;

namespace ScreenTimeRS.UI;

public sealed class FocusPage : Page
{
    // Focus is intentionally constructed lazily by MainWindow so a focus-page failure
    // cannot prevent the core ScreenTime UI from starting.

    private readonly Func<string[], Task<bool>> _runCommand;
    private readonly Func<int, int, Task<FocusSession[]?>> _loadHistory;
    private readonly Action _navigateToHistory;

    private readonly TextBlock _title = new();
    private readonly TextBlock _subtitle = new();
    private readonly TextBlock _durationLabel = new();
    private readonly TextBlock _customLabel = new();
    private readonly TextBlock _minutesUnit = new();
    private readonly Grid _presetRow = new() { ColumnSpacing = 6, RowSpacing = 8 };
    private readonly List<Button> _presetButtons = new();
    private readonly TextBox _customMinutes = new();
    private readonly TextBlock _timer = new();
    private readonly TextBlock _state = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _start = new();
    private readonly Button _pause = new();
    private readonly Button _resume = new();
    private readonly Button _stop = new();
    private readonly TextBlock _todayLabel = new();
    private readonly TextBlock _completedLabel = new();
    private readonly TextBlock _pausesLabel = new();
    private readonly TextBlock _todayValue = new();
    private readonly TextBlock _completedValue = new();
    private readonly TextBlock _pausesValue = new();
    private readonly TextBlock _historyTitle = new();
    private readonly Button _viewAllHistory = new();
    private readonly StackPanel _recentHistory = new();
    private readonly TextBlock _recentHistoryStatus = new();
    private readonly TextBlock _tips = new();
    private Color _accent = ThemeManager.DefaultAccent;
    private LanguageMode _language = LanguageMode.SimplifiedChinese;
    private int _lastCompletedCount = -1;
    private bool _lastFocusActive;
    private DateTime _completionMessageUntilUtc = DateTime.MinValue;
    private bool _historyLoadStarted;
    private bool _historyReloadPending;
    private bool _pageReady;
    private TextBlock? _fallbackTitle;
    private TextBlock? _fallbackBody;
    private TextBlock? _fallbackHint;
    // Keep the main card compact: only the latest ten records are shown here.
    // The full history is available on a dedicated page.
    private const int RecentHistoryLimit = 10;
    private StackPanel? _customEditorPanel;

    // A row of equally sized duration buttons replaces the old dropdown: the
    // options are visible at a glance, one click instead of two, and the card
    // no longer has a wide empty gap between its label and its control.
    static readonly int[] PresetMinutes = { 15, 25, 30, 45, 60, 90, 120 };
    static int CustomIndex => PresetMinutes.Length;
    int _selectedPreset = 1; // 25 minutes, matching the previous default.

    public FocusPage(Func<string[], Task<bool>> runCommand, Func<int, int, Task<FocusSession[]?>> loadHistory, Action navigateToHistory)
    {
        _runCommand = runCommand;
        _loadHistory = loadHistory;
        _navigateToHistory = navigateToHistory;
        try
        {
            BuildPage();
            _pageReady = true;
            ApplyLanguage(LanguageMode.SimplifiedChinese);
            ApplyAccent(_accent);
        }
        catch
        {
            _pageReady = false;
            BuildFallbackPage();
        }
    }

    private void BuildFallbackPage()
    {
        _fallbackTitle = new TextBlock { FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        _fallbackBody = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _fallbackHint = new TextBlock { Opacity = .68, TextWrapping = TextWrapping.Wrap };
        Content = new Border
        {
            Padding = new Thickness(28),
            Child = new StackPanel
            {
                Spacing = 12,
                Children = { _fallbackTitle, _fallbackBody, _fallbackHint }
            }
        };
    }

    private void BuildPage()
    {
        var root = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            Padding = new Thickness(28, 24, 28, 36)
        };
        var panel = new StackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };


        _title.FontSize = Ui.PageTitleSize;
        _title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        panel.Children.Add(_title);
        _subtitle.Opacity = .68;
        _subtitle.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_subtitle);

        var timerCard = Card();
        var timerPanel = new StackPanel { Spacing = 14 };
        _state.HorizontalAlignment = HorizontalAlignment.Center;
        _state.FontSize = 15;
        _state.Opacity = .72;
        timerPanel.Children.Add(_state);

        _timer.Text = "25:00";
        _timer.FontSize = 64;
        _timer.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        _timer.HorizontalAlignment = HorizontalAlignment.Center;
        _timer.TextAlignment = TextAlignment.Center;
        timerPanel.Children.Add(_timer);

        _progress.Height = 8;
        _progress.Maximum = 1;
        _progress.Value = 0;
        timerPanel.Children.Add(_progress);

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8
        };
        _start.Click += async (_, _) => await StartAsync();
        _pause.Click += async (_, _) => await TogglePauseAsync();
        _resume.Click += async (_, _) => await TogglePauseAsync();
        _stop.Click += async (_, _) => await StopAsync();
        controls.Children.Add(_start);
        controls.Children.Add(_pause);
        controls.Children.Add(_resume);
        controls.Children.Add(_stop);
        timerPanel.Children.Add(controls);
        timerCard.Child = timerPanel;
        panel.Children.Add(timerCard);

        var setupCard = Card();
        var setup = new StackPanel { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Stretch };

        _durationLabel.FontSize = 14;
        _durationLabel.Opacity = .72;
        setup.Children.Add(_durationLabel);

        for (var i = 0; i <= CustomIndex; i++)
            _presetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i <= CustomIndex; i++)
        {
            var index = i;
            var label = new TextBlock
            {
                FontSize = Ui.BodySize,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            var button = new Button
            {
                Content = label,
                Tag = label,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 9, 4, 9),
                CornerRadius = new CornerRadius(6)
            };
            button.Click += (_, _) => SelectPreset(index);
            Grid.SetColumn(button, i);
            _presetRow.Children.Add(button);
            _presetButtons.Add(button);
        }
        setup.Children.Add(_presetRow);

        _customEditorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        _customLabel.FontSize = Ui.BodySize;
        _customLabel.Opacity = .72;
        _customLabel.VerticalAlignment = VerticalAlignment.Center;
        _minutesUnit.FontSize = Ui.BodySize;
        _minutesUnit.Opacity = .72;
        _minutesUnit.VerticalAlignment = VerticalAlignment.Center;

        _customMinutes.PlaceholderText = "Minutes";
        _customMinutes.Width = 120;
        _customMinutes.VerticalAlignment = VerticalAlignment.Center;
        _customMinutes.TextChanged += (_, _) =>
        {
            if (IsCustomSelected())
            {
                UpdateCustomDurationPreview();
            }
        };
        _customMinutes.LostFocus += (_, _) => UpdateCustomDurationPreview();

        _customEditorPanel.Children.Add(_customLabel);
        _customEditorPanel.Children.Add(_customMinutes);
        _customEditorPanel.Children.Add(_minutesUnit);
        setup.Children.Add(_customEditorPanel);

        setupCard.Child = setup;
        panel.Children.Add(setupCard);
        var metrics = new Grid { ColumnSpacing = 12 };
        for (var i = 0; i < 3; i++)
            metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddMetric(metrics, 0, _todayLabel, _todayValue);
        AddMetric(metrics, 1, _completedLabel, _completedValue);
        AddMetric(metrics, 2, _pausesLabel, _pausesValue);
        panel.Children.Add(metrics);

        var historyCard = Card();
        var historyPanel = new StackPanel { Spacing = 10 };
        var historyHeader = new Grid { ColumnSpacing = 12 };
        historyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        historyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _historyTitle.FontSize = 18;
        _historyTitle.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        Grid.SetColumn(_historyTitle, 0);
        historyHeader.Children.Add(_historyTitle);

        var historyActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _viewAllHistory.Click += (_, _) => _navigateToHistory();
        historyActions.Children.Add(_viewAllHistory);
        Grid.SetColumn(historyActions, 1);
        historyHeader.Children.Add(historyActions);
        historyPanel.Children.Add(historyHeader);

        _recentHistoryStatus.Opacity = .58;
        _recentHistoryStatus.FontSize = 12;
        _recentHistoryStatus.TextWrapping = TextWrapping.Wrap;
        historyPanel.Children.Add(_recentHistory);
        historyPanel.Children.Add(_recentHistoryStatus);

        historyCard.Child = historyPanel;
        panel.Children.Add(historyCard);

        var tipsCard = Card();
        _tips.TextWrapping = TextWrapping.Wrap;
        _tips.Opacity = .68;
        tipsCard.Child = _tips;
        panel.Children.Add(tipsCard);

        root.Content = panel;
        Content = root;
    }

    private static Border Card()
    {
        return new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128)),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private static TextBlock TextBlockFor(string text, double size, double opacity)
        => new() { Text = text, FontSize = size, Opacity = opacity, VerticalAlignment = VerticalAlignment.Center };

    private static void AddMetric(Grid grid, int column, TextBlock label, TextBlock value)
    {
        var border = Card();
        var panel = new StackPanel { Spacing = 4 };
        label.Opacity = .68;
        panel.Children.Add(label);
        value.FontSize = 23;
        value.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        panel.Children.Add(value);
        border.Child = panel;
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    public void ApplyAccent(Color color)
    {
        _accent = color;
        if (!_pageReady) return;
        _progress.Foreground = new SolidColorBrush(color);
        _start.Background = new SolidColorBrush(color);
        // The dark accent is a light blue: white text on it is nearly unreadable.
        _start.Foreground = new SolidColorBrush(ThemeManager.OnAccent(color));
        RefreshPresetVisuals();
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = LanguageResolver.Resolve(language);
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;

        if (!_pageReady)
        {
            // The fallback page is built before the language is known, so it
            // follows later language switches instead of staying Chinese.
            if (_fallbackTitle is not null)
                _fallbackTitle.Text = en ? "Focus" : hant ? "專注" : "专注";
            if (_fallbackBody is not null)
                _fallbackBody.Text = en
                    ? "The Focus page could not be loaded."
                    : hant ? "專注頁面無法載入。" : "专注页面无法加载。";
            if (_fallbackHint is not null)
                _fallbackHint.Text = en
                    ? "Other features remain available."
                    : hant ? "其他功能仍可正常使用。" : "其他功能仍可正常使用。";
            return;
        }

        _title.Text = en ? "Focus" : hant ? "專注" : "专注";
        _subtitle.Text = en
            ? "Work in focused blocks without stopping the screen-time recorder. Switching apps does not end a focus session."
            : hant
                ? "以專注時段工作，不會停止螢幕使用時間記錄。切換應用程式不會結束專注時段。"
                : "以专注时段工作，不会停止屏幕使用时间记录。切换应用不会结束专注时段。";

        _start.Content = en ? "Start focus" : hant ? "開始專注" : "开始专注";
        _pause.Content = en ? "Pause" : hant ? "暫停" : "暂停";
        _resume.Content = en ? "Resume" : hant ? "繼續" : "继续";
        _stop.Content = en ? "End" : hant ? "結束" : "结束";

        _durationLabel.Text = en ? "Focus duration (minutes)" : hant ? "專注時長（分鐘）" : "专注时长（分钟）";
        _customLabel.Text = en ? "Custom" : hant ? "自訂" : "自定义";
        _minutesUnit.Text = en ? "minutes" : hant ? "分鐘" : "分钟";
        _customMinutes.PlaceholderText = en ? "1–1440" : hant ? "1–1440" : "1–1440";
        _historyTitle.Text = en ? "Recent focus history" : hant ? "最近專注歷史" : "最近专注历史";
        _viewAllHistory.Content = en ? "View all" : hant ? "查看全部" : "查看全部";
        _tips.Text = en
            ? "Focus time is recorded separately from screen time. Locking Windows, long inactivity, or the global usage-recording pause automatically stops adding focus time."
            : hant
                ? "專注時間與螢幕使用時間分開記錄。鎖定 Windows、長時間無操作或全域暫停使用時間記錄時，不會繼續累積專注時間。"
                : "专注时间与屏幕使用时间分开记录。锁定 Windows、长时间无操作或全局暂停使用时间记录时，不会继续累计专注时间。";

        SetMetricLabels(en, hant);
        RefreshPresetVisuals();
        UpdateCustomVisibility();
        UpdateStateText(_lastFocusActive, false, false);
    }

    private void SetMetricLabels(bool en, bool hant)
    {
        _todayLabel.Text = en ? "Focused today" : hant ? "今日專注" : "今日专注";
        _completedLabel.Text = en ? "Completed today" : hant ? "今日完成" : "今日完成";
        _pausesLabel.Text = en ? "Pauses today" : hant ? "今日暫停" : "今日暂停";
    }

    private void RefreshPresetVisuals()
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        var onAccent = ThemeManager.OnAccent(_accent);
        for (var i = 0; i < _presetButtons.Count; i++)
        {
            var button = _presetButtons[i];
            var label = (TextBlock)button.Tag!;
            label.Text = i == CustomIndex
                ? (en ? "Custom" : hant ? "自訂" : "自定义")
                : PresetMinutes[i].ToString();
            var selected = i == _selectedPreset;
            label.FontWeight = selected
                ? Microsoft.UI.Text.FontWeights.SemiBold
                : Microsoft.UI.Text.FontWeights.Normal;

            if (selected)
            {
                button.Background = new SolidColorBrush(_accent);
                button.Foreground = new SolidColorBrush(onAccent);
                // Keep the stock pointer states inside the same accent family:
                // otherwise hovering the selected chip flashes WinUI grey.
                button.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(_accent);
                button.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(_accent);
                button.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(onAccent);
                button.Resources["ButtonForegroundPressed"] = new SolidColorBrush(onAccent);
            }
            else
            {
                button.ClearValue(Control.BackgroundProperty);
                button.ClearValue(Control.ForegroundProperty);
                button.Resources = new ResourceDictionary();
            }
        }
    }

    private void SelectPreset(int index)
    {
        if (index == _selectedPreset) return;
        _selectedPreset = index;
        RefreshPresetVisuals();
        OnPresetChanged();
    }

    private bool IsCustomSelected() => _selectedPreset == CustomIndex;

    private int? TryGetCustomDurationSeconds()
    {
        if (!int.TryParse(_customMinutes.Text.Trim(), out var minutes)) return null;
        if (minutes < 1 || minutes > 1440) return null;
        return minutes * 60;
    }

    private int SelectedDurationSeconds()
    {
        if (_selectedPreset >= 0 && _selectedPreset < PresetMinutes.Length)
            return PresetMinutes[_selectedPreset] * 60;
        return TryGetCustomDurationSeconds() ?? 25 * 60;
    }

    private void OnPresetChanged()
    {
        UpdateCustomVisibility();
        if (IsCustomSelected() && string.IsNullOrWhiteSpace(_customMinutes.Text))
            _customMinutes.Text = "30";
        if (!(_latest?.focus_active == true || _latest?.focus_paused == true))
        {
            _timer.Text = FormatCountdown(SelectedDurationSeconds());
            _progress.Value = 0;
        }
    }

    private void UpdateCustomVisibility()
    {
        if (_customEditorPanel is not null)
            _customEditorPanel.Visibility = IsCustomSelected() ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateCustomDurationPreview()
    {
        if (!IsCustomSelected() || _latest?.focus_active == true || _latest?.focus_paused == true) return;
        var custom = TryGetCustomDurationSeconds();
        _timer.Text = custom.HasValue ? FormatCountdown(custom.Value) : FormatCountdown(25 * 60);
        _state.Text = custom.HasValue ? ReadyStateText() : InvalidCustomText();
        _start.IsEnabled = custom.HasValue;
    }

    private string ReadyStateText()
        => _language == LanguageMode.English ? "Ready to focus" : _language == LanguageMode.TraditionalChinese ? "準備開始專注" : "准备开始专注";

    private string InvalidCustomText()
        => _language == LanguageMode.English ? "Enter a duration from 1 to 1440 minutes" : _language == LanguageMode.TraditionalChinese ? "請輸入 1 至 1440 分鐘" : "请输入 1 至 1440 分钟";

    private async Task StartAsync()
    {
        int seconds;
        if (IsCustomSelected())
        {
            var custom = TryGetCustomDurationSeconds();
            if (!custom.HasValue)
            {
                _state.Text = InvalidCustomText();
                _customMinutes.Focus(FocusState.Programmatic);
                return;
            }
            seconds = custom.Value;
        }
        else
        {
            seconds = SelectedDurationSeconds();
        }

        _start.IsEnabled = false;
        var ok = await _runCommand(new[] { "--focus-start", seconds.ToString() });
        if (ok)
            await Task.Delay(100);
        await RefreshHistoryAsync(reset: true);
    }

    private async Task TogglePauseAsync()
    {
        var isPaused = _latest?.focus_paused == true;
        await _runCommand(new[] { isPaused ? "--focus-resume" : "--focus-pause" });
    }

    private async Task StopAsync()
    {
        await _runCommand(new[] { "--focus-stop" });
        await RefreshHistoryAsync(reset: true);
    }

    private Snapshot? _latest;

    public void UpdateSnapshot(Snapshot snapshot)
    {
        if (_lastCompletedCount < 0)
            _ = RefreshHistoryAsync(reset: true);
        _latest = snapshot;
        var justCompleted = _lastCompletedCount >= 0 && snapshot.focus_today_completed > _lastCompletedCount;
        _lastCompletedCount = (int)Math.Max(0, snapshot.focus_today_completed);

        var hasFocus = snapshot.focus_active || snapshot.focus_paused;
        var planned = Math.Max(1, snapshot.focus_planned_seconds > 0 ? snapshot.focus_planned_seconds : SelectedDurationSeconds());
        var remaining = hasFocus ? Math.Max(0, snapshot.focus_remaining_seconds) : planned;
        _timer.Text = FormatCountdown(remaining);
        var active = Math.Clamp(snapshot.focus_active_seconds / (double)planned, 0, 1);
        _progress.Value = hasFocus ? active : 0;
        _todayValue.Text = UiHelpers.Format(snapshot.focus_today_total, _language);
        _completedValue.Text = Math.Max(0, snapshot.focus_today_completed).ToString();
        _pausesValue.Text = Math.Max(0, snapshot.focus_today_pauses).ToString();

        var customValid = !IsCustomSelected() || TryGetCustomDurationSeconds().HasValue;
        _start.IsEnabled = !snapshot.focus_active && !snapshot.focus_paused && customValid;
        _pause.IsEnabled = snapshot.focus_active && !snapshot.focus_auto_paused;
        _resume.IsEnabled = snapshot.focus_paused && !snapshot.focus_auto_paused;
        _stop.IsEnabled = snapshot.focus_active || snapshot.focus_paused;

        if (justCompleted)
            _completionMessageUntilUtc = DateTime.UtcNow.AddSeconds(6);
        if (!hasFocus && IsCustomSelected() && !customValid)
            _state.Text = InvalidCustomText();
        else
            UpdateStateText(snapshot.focus_active, snapshot.focus_paused, snapshot.focus_auto_paused);

        if (_lastFocusActive && !snapshot.focus_active && !snapshot.focus_paused)
            _ = RefreshHistoryAsync(reset: true);
        _lastFocusActive = snapshot.focus_active || snapshot.focus_paused;
    }

    private void UpdateStateText(bool active, bool paused, bool autoPaused)
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        if (DateTime.UtcNow < _completionMessageUntilUtc)
        {
            _state.Text = en ? "Focus completed" : hant ? "專注已完成" : "专注已完成";
        }
        else if (active && autoPaused)
        {
            _state.Text = en ? "Auto-paused: computer inactive or locked" : hant ? "已自動暫停：電腦閒置或已鎖定" : "已自动暂停：电脑闲置或已锁定";
        }
        else if (paused)
        {
            _state.Text = en ? "Focus paused" : hant ? "專注已暫停" : "专注已暂停";
        }
        else if (active)
        {
            _state.Text = en ? "Focus in progress" : hant ? "專注進行中" : "专注进行中";
        }
        else
        {
            _state.Text = en ? "Ready to focus" : hant ? "準備開始專注" : "准备开始专注";
        }
    }

    public void RefreshHistory() => _ = RefreshHistoryAsync(reset: true);

    private async Task DeleteHistoryAsync(long id)
    {
        if (id <= 0) return;
        // The recent-list delete button is intentionally a direct action with a confirmation.
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot ?? Content?.XamlRoot,
            RequestedTheme = ActualTheme,
            Title = en ? "Delete focus record?" : hant ? "刪除專注記錄？" : "删除专注记录？",
            Content = en ? "This focus record will be permanently removed." : hant ? "此專注記錄將被永久刪除。" : "此专注记录将被永久删除。",
            PrimaryButtonText = en ? "Delete" : hant ? "刪除" : "删除",
            CloseButtonText = en ? "Cancel" : hant ? "取消" : "取消",
            DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var ok = await _runCommand(new[] { "--focus-delete", id.ToString() });
        if (ok)
            await RefreshHistoryAsync(reset: true);
    }

    private async Task RefreshHistoryAsync(bool reset = false)
    {
        _ = reset; // Main history is always reloaded as the latest 10 records.

        if (!_pageReady) return;
        if (_historyLoadStarted)
        {
            // Do not drop a refresh that arrives while a load is in flight;
            // run once more when the current load finishes.
            _historyReloadPending = true;
            return;
        }
        _historyLoadStarted = true;
        try
        {
            var sessions = await _loadHistory(RecentHistoryLimit, 0);
            var safe = sessions ?? Array.Empty<FocusSession>();
            _recentHistory.Children.Clear();
            RenderHistory(safe, _recentHistory, includeHeader: true);
            var en = _language == LanguageMode.English;
            var hant = _language == LanguageMode.TraditionalChinese;
            _recentHistoryStatus.Text = safe.Length == 0
                ? (en ? "No focus history yet." : hant ? "尚無專注歷史記錄。" : "还没有专注历史记录。")
                : (en ? $"Showing the latest {safe.Length} record(s). Older records remain available in Focus history." : hant ? $"目前顯示最近 {safe.Length} 筆記錄。較早記錄可在「專注歷史」頁面查看。" : $"当前显示最近 {safe.Length} 条记录。更早的记录可在“专注历史”页面查看。");
        }
        catch { }
        finally
        {
            _historyLoadStarted = false;
            if (_historyReloadPending)
            {
                _historyReloadPending = false;
                _ = RefreshHistoryAsync(reset: true);
            }
        }
    }

    private void RenderHistory(FocusSession[] sessions, StackPanel target, bool includeHeader)
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        if (includeHeader)
        {
            var header = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 0, 0, 4) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            AddHeaderCell(header, en ? "Started" : hant ? "開始時間" : "开始时间", 0);
            AddHeaderCell(header, en ? "Target" : hant ? "目標" : "目标", 1);
            AddHeaderCell(header, en ? "Focused" : hant ? "實際專注" : "实际专注", 2);
            AddHeaderCell(header, en ? "Status" : hant ? "狀態" : "状态", 3);
            AddHeaderCell(header, en ? "Action" : hant ? "操作" : "操作", 4);
            target.Children.Add(header);
        }

        foreach (var session in sessions)
        {
            var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(0, 7, 0, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            AddTextCell(row, FormatDateTime(session.start_at), 0);
            AddTextCell(row, FormatDuration(session.planned_seconds), 1);
            AddTextCell(row, FormatDuration(session.active_seconds), 2);
            AddTextCell(row, StatusText(session.status), 3);

            var delete = new Button
            {
                Content = en ? "Delete" : hant ? "刪除" : "删除",
                Padding = new Thickness(8, 3, 8, 3),
                IsEnabled = session.status is not ("active" or "paused"),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var id = session.id;
            delete.Click += async (_, _) => await DeleteHistoryAsync(id);
            Grid.SetColumn(delete, 4);
            row.Children.Add(delete);
            target.Children.Add(row);
        }
    }

    private static void AddHeaderCell(Grid grid, string text, int column)
    {
        var cell = new TextBlock { Text = text, Opacity = .58, FontSize = 12 };
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }

    private static void AddTextCell(Grid grid, string text, int column)
    {
        var cell = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }

    private string StatusText(string status)
    {
        return status switch
        {
            "completed" => _language == LanguageMode.English ? "Completed" : _language == LanguageMode.TraditionalChinese ? "已完成" : "已完成",
            "paused" => _language == LanguageMode.English ? "Paused" : _language == LanguageMode.TraditionalChinese ? "已暫停" : "已暂停",
            "active" => _language == LanguageMode.English ? "Active" : _language == LanguageMode.TraditionalChinese ? "進行中" : "进行中",
            _ => _language == LanguageMode.English ? "Ended" : _language == LanguageMode.TraditionalChinese ? "已結束" : "已结束"
        };
    }

    private string FormatDateTime(string text)
    {
        if (!DateTimeOffset.TryParse(text, out var value)) return text;
        value = value.ToLocalTime();
        return _language == LanguageMode.English
            ? value.ToString("MMM d, HH:mm")
            : value.ToString("MM-dd HH:mm");
    }

    private string FormatDuration(long seconds)
    {
        seconds = Math.Max(0, seconds);
        return UiHelpers.Format(seconds, _language);
    }

    private static string FormatCountdown(long seconds)
    {
        seconds = Math.Max(0, seconds);
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes:00}:{ts.Seconds:00}";
    }
}

public sealed class FocusSession
{
    public long id { get; set; }
    public string start_at { get; set; } = "";
    public string? end_at { get; set; }
    public long planned_seconds { get; set; }
    public long active_seconds { get; set; }
    public long pause_count { get; set; }
    public string status { get; set; } = "";
}
