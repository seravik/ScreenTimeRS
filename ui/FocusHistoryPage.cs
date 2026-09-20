using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.UI;

namespace ScreenTimeRS.UI;

/// <summary>
/// Dedicated full focus-history page. The Focus page only shows the latest ten
/// sessions; this page keeps the full history out of the main focus workflow.
/// </summary>
public sealed class FocusHistoryPage : Page
{
    private readonly Func<int, int, Task<FocusSession[]?>> _loadHistory;
    private readonly Func<string[], Task<bool>> _runCommand;
    private readonly Action _navigateBack;
    private readonly Action? _historyChanged;

    private readonly TextBlock _title = new();
    private readonly TextBlock _subtitle = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _empty = new();
    private readonly TextBlock _count = new();
    private readonly StackPanel _rows = new();
    private readonly Button _back = new();
    private readonly Button _loadMore = new();
    private readonly Button _clear = new();
    private readonly ProgressRing _loading = new() { IsActive = false, Width = 18, Height = 18 };

    private Color _accent = ThemeManager.DefaultAccent;
    private LanguageMode _language = LanguageMode.SimplifiedChinese;
    private bool _loadedOnce;
    private bool _loadingMore;
    private bool _hasMore;
    private int _offset;

    private const int PageSize = 50;

    public FocusHistoryPage(
        Func<int, int, Task<FocusSession[]?>> loadHistory,
        Func<string[], Task<bool>> runCommand,
        Action navigateBack,
        Action? historyChanged = null)
    {
        _loadHistory = loadHistory;
        _runCommand = runCommand;
        _navigateBack = navigateBack;
        _historyChanged = historyChanged;
        BuildPage();
        ApplyLanguage(LanguageMode.SimplifiedChinese);
        ApplyAccent(_accent);
        Loaded += (_, _) =>
        {
            if (!_loadedOnce)
                Refresh();
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

        root.SizeChanged += (_, e) =>
        {
            var available = e.NewSize.Width - root.Padding.Left - root.Padding.Right;
            if (available > 0)
                panel.Width = available;
        };

        _title.FontSize = 30;
        _title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        panel.Children.Add(_title);

        _subtitle.Opacity = .68;
        _subtitle.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_subtitle);

        var toolbar = new Grid { ColumnSpacing = 12 };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _back.HorizontalAlignment = HorizontalAlignment.Left;
        _back.Click += (_, _) => _navigateBack();
        Grid.SetColumn(_back, 0);
        toolbar.Children.Add(_back);

        _count.VerticalAlignment = VerticalAlignment.Center;
        _count.Opacity = .62;
        Grid.SetColumn(_count, 1);
        toolbar.Children.Add(_count);

        _loading.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_loading, 2);
        toolbar.Children.Add(_loading);

        _loadMore.HorizontalAlignment = HorizontalAlignment.Right;
        _loadMore.Click += async (_, _) => await LoadMoreAsync();
        Grid.SetColumn(_loadMore, 3);
        toolbar.Children.Add(_loadMore);

        _clear.HorizontalAlignment = HorizontalAlignment.Right;
        _clear.Click += async (_, _) => await ClearHistoryAsync();
        Grid.SetColumn(_clear, 4);
        toolbar.Children.Add(_clear);

        panel.Children.Add(toolbar);

        var historyCard = Card();
        var historyPanel = new StackPanel { Spacing = 0 };
        historyPanel.Children.Add(BuildHeader());
        historyPanel.Children.Add(_rows);
        _empty.Margin = new Thickness(0, 18, 0, 18);
        _empty.Opacity = .65;
        historyPanel.Children.Add(_empty);
        historyCard.Child = historyPanel;
        panel.Children.Add(historyCard);

        _status.Opacity = .62;
        _status.FontSize = 12;
        _status.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_status);

        Content = root;
        root.Content = panel;
    }

    private Border BuildHeader()
    {
        var header = new Border
        {
            Padding = new Thickness(0, 0, 0, 8),
            BorderBrush = new SolidColorBrush(Color.FromArgb(55, 128, 128, 128)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
        header.Child = grid;
        _headerGrid = grid;
        return header;
    }

    private Grid? _headerGrid;

    public void ApplyAccent(Color color)
    {
        _accent = color;
        _loadMore.Background = new SolidColorBrush(color);
        _loadMore.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = LanguageResolver.Resolve(language);
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;

        _title.Text = en ? "Focus history" : hant ? "專注歷史" : "专注历史";
        _subtitle.Text = en
            ? "Browse all saved focus sessions. The main Focus page keeps only the latest ten records for a compact layout."
            : hant
                ? "瀏覽所有已儲存的專注時段。主專注頁面僅顯示最近十筆記錄，以保持介面簡潔。"
                : "查看全部已保存的专注记录。专注页面仅显示最近 10 条，以保持界面简洁。";
        _back.Content = en ? "Back to Focus" : hant ? "返回專注" : "返回专注";
        _loadMore.Content = en ? "Load more" : hant ? "載入更多" : "加载更多";
        _clear.Content = en ? "Clear history" : hant ? "清理歷史" : "清理历史";
        UpdateCountText();
        UpdateStatusText();
        UpdateHeaderLabels();
    }

    private void UpdateHeaderLabels()
    {
        if (_headerGrid is null) return;
        for (var i = _headerGrid.Children.Count - 1; i >= 0; i--)
            _headerGrid.Children.RemoveAt(i);
        AddHeaderCell(_headerGrid, _language == LanguageMode.English ? "Started" : _language == LanguageMode.TraditionalChinese ? "開始時間" : "开始时间", 0);
        AddHeaderCell(_headerGrid, _language == LanguageMode.English ? "Target" : _language == LanguageMode.TraditionalChinese ? "目標" : "目标", 1);
        AddHeaderCell(_headerGrid, _language == LanguageMode.English ? "Focused" : _language == LanguageMode.TraditionalChinese ? "實際專注" : "实际专注", 2);
        AddHeaderCell(_headerGrid, _language == LanguageMode.English ? "Status" : _language == LanguageMode.TraditionalChinese ? "狀態" : "状态", 3);
        AddHeaderCell(_headerGrid, _language == LanguageMode.English ? "Action" : _language == LanguageMode.TraditionalChinese ? "操作" : "操作", 4);
    }

    private void UpdateCountText()
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        _count.Text = _offset <= 0
            ? (en ? "" : "")
            : (en ? $"Loaded {_offset} record(s)." : hant ? $"已載入 {_offset} 筆記錄。" : $"已加载 {_offset} 条记录。");
    }

    private void UpdateStatusText()
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        _status.Text = _hasMore
            ? (en ? "More history is available." : hant ? "還有更多歷史記錄。" : "还有更多历史记录。")
            : (_loadedOnce ? (en ? "All available focus history is shown." : hant ? "已顯示全部可用專注歷史。" : "已显示全部可用专注历史。") : "");
        _loadMore.Visibility = _hasMore ? Visibility.Visible : Visibility.Collapsed;
        _loadMore.IsEnabled = !_loadingMore && _hasMore;
    }

    public void Refresh()
    {
        _ = RefreshAsync(reset: true);
    }

    private async Task RefreshAsync(bool reset)
    {
        if (_loadingMore) return;
        _loadingMore = true;
        _loading.IsActive = true;
        try
        {
            if (reset)
            {
                _offset = 0;
                _rows.Children.Clear();
                _empty.Text = "";
            }

            var sessions = await _loadHistory(PageSize, _offset);
            var safe = sessions ?? Array.Empty<FocusSession>();
            if (_offset == 0 && safe.Length == 0)
            {
                var en = _language == LanguageMode.English;
                var hant = _language == LanguageMode.TraditionalChinese;
                _empty.Text = en ? "No focus history yet." : hant ? "尚無專注歷史記錄。" : "还没有专注历史记录。";
            }
            else
            {
                RenderRows(safe);
            }

            _offset += safe.Length;
            _hasMore = safe.Length >= PageSize;
            _loadedOnce = true;
            UpdateCountText();
            UpdateStatusText();
        }
        catch (Exception ex)
        {
            _status.Text = _language == LanguageMode.English
                ? $"Failed to load focus history: {ex.Message}"
                : _language == LanguageMode.TraditionalChinese
                    ? $"載入專注歷史失敗：{ex.Message}"
                    : $"加载专注历史失败：{ex.Message}";
        }
        finally
        {
            _loading.IsActive = false;
            _loadingMore = false;
            UpdateStatusText();
        }
    }

    private Task LoadMoreAsync() => RefreshAsync(reset: false);

    private void RenderRows(FocusSession[] sessions)
    {
        foreach (var session in sessions)
        {
            var row = new Grid
            {
                ColumnSpacing = 12,
                Padding = new Thickness(0, 9, 0, 9)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });

            AddTextCell(row, FormatDateTime(session.start_at), 0);
            AddTextCell(row, FormatDuration(session.planned_seconds), 1);
            AddTextCell(row, FormatDuration(session.active_seconds), 2);
            AddTextCell(row, StatusText(session.status), 3);

            var delete = new Button
            {
                Content = _language == LanguageMode.English ? "Delete" : _language == LanguageMode.TraditionalChinese ? "刪除" : "删除",
                Padding = new Thickness(10, 3, 10, 3),
                IsEnabled = session.status is not ("active" or "paused"),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var id = session.id;
            delete.Click += async (_, _) => await DeleteHistoryAsync(id);
            Grid.SetColumn(delete, 4);
            row.Children.Add(delete);

            _rows.Children.Add(row);
        }
    }

    private async Task DeleteHistoryAsync(long id)
    {
        if (id <= 0) return;

        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot ?? Content?.XamlRoot,
            RequestedTheme = ActualTheme,
            Title = en ? "Delete focus record?" : hant ? "刪除專注記錄？" : "删除专注记录？",
            Content = en
                ? "This focus record will be permanently deleted. This action cannot be undone."
                : hant
                    ? "此專注記錄將被永久刪除。此操作無法復原。"
                    : "此专注记录将被永久删除。此操作无法撤销。",
            PrimaryButtonText = en ? "Delete" : hant ? "刪除" : "删除",
            CloseButtonText = en ? "Cancel" : hant ? "取消" : "取消",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var ok = await _runCommand(new[] { "--focus-delete", id.ToString() });
        if (ok)
        {
            await RefreshAsync(reset: true);
            _historyChanged?.Invoke();
        }
    }

    private async Task ClearHistoryAsync()
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot ?? Content?.XamlRoot,
            RequestedTheme = ActualTheme,
            Title = en ? "Clear focus history?" : hant ? "清理專注歷史？" : "清理专注历史？",
            Content = en
                ? "Completed and ended focus records will be removed. Active or paused sessions will not be removed."
                : hant
                    ? "將刪除已完成及已結束的專注記錄。正在進行或暫停中的專注不會被刪除。"
                    : "将删除已完成和已结束的专注记录。正在进行或暂停中的专注不会被删除。",
            PrimaryButtonText = en ? "Clear" : hant ? "清理" : "清理",
            CloseButtonText = en ? "Cancel" : hant ? "取消" : "取消",
            DefaultButton = ContentDialogButton.Close
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        var ok = await _runCommand(new[] { "--focus-delete-all" });
        if (ok)
        {
            await RefreshAsync(reset: true);
            _historyChanged?.Invoke();
        }
    }

    private string StatusText(string status)
        => status switch
        {
            "completed" => _language == LanguageMode.English ? "Completed" : "已完成",
            "paused" => _language == LanguageMode.English ? "Paused" : _language == LanguageMode.TraditionalChinese ? "已暫停" : "已暂停",
            "active" => _language == LanguageMode.English ? "Active" : _language == LanguageMode.TraditionalChinese ? "進行中" : "进行中",
            _ => _language == LanguageMode.English ? "Ended" : _language == LanguageMode.TraditionalChinese ? "已結束" : "已结束"
        };

    private string FormatDateTime(string text)
    {
        if (!DateTimeOffset.TryParse(text, out var value)) return text;
        value = value.ToLocalTime();
        return _language == LanguageMode.English ? value.ToString("MMM d, HH:mm") : value.ToString("MM-dd HH:mm");
    }

    private string FormatDuration(long seconds)
        => UiHelpers.Format(Math.Max(0, seconds), _language);

    private static Border Card() => new()
    {
        Padding = new Thickness(20),
        CornerRadius = new CornerRadius(12),
        BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
        BorderThickness = new Thickness(1)
    };

    private static void AddHeaderCell(Grid grid, string text, int column)
    {
        var cell = new TextBlock { Text = text, Opacity = .58, FontSize = 12 };
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }

    private static void AddTextCell(Grid grid, string text, int column)
    {
        var cell = new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }
}
