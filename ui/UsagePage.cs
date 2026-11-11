using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UIColor = Windows.UI.Color;

namespace ScreenTimeRS.UI;

/// <summary>
/// Usage: one page, two tabs. Trends answers "how did the period go", Apps
/// answers "where did the time go". Both share the same range control pattern
/// and the same card shape.
/// </summary>
public sealed class UsagePage : Page
{
    const double ChartHeight = 140;

    readonly Func<string[], Task<string?>> _query;
    readonly Button _trendTab, _appsTab;
    readonly TextBlock _trendTabText, _appsTabText;
    Border _trendUnderline, _appsUnderline;
    readonly ScrollViewer _trendScroll, _appsScroll;
    bool _showTrends = true;

    readonly ComboBox _trendRange;
    readonly TextBlock[] _metricLabels = new TextBlock[4];
    readonly TextBlock[] _metricValues = new TextBlock[4];
    readonly TextBlock _trendHint;
    readonly Grid _chart;
    readonly Canvas _chartLayer;
    readonly Border _chartTip;
    readonly TextBlock _chartTipText;
    readonly TextBlock _axisStart;
    readonly TextBlock _axisEnd;
    JsonDaily[] _trendDays = Array.Empty<JsonDaily>();
    int _trendHover = -1;
    string _trendSignature = "";

    readonly ComboBox _appRange;
    readonly CalendarDatePicker _appDate;
    readonly TextBox _search;
    readonly StackPanel _list;
    readonly TextBlock _listEmpty;
    readonly Border _detailHost;
    readonly StackPanel _detail;
    readonly TextBlock _detailEmpty;

    LanguageMode _language = LanguageMode.SimplifiedChinese;
    UIColor _accent = UIColor.FromArgb(255, 0, 120, 212);
    Snapshot _snapshot = new();

    JsonDaily[] _dailyAll = Array.Empty<JsonDaily>();
    bool _dailyAllLoaded;
    RangeUsageResponse? _allRange;
    DayUsageResponse? _dayRange;
    string _listSignature = "";
    string _detailKey = "";
    bool _updatingRange;

    public UsagePage(Func<string[], Task<string?>> query)
    {
        _query = query;

        _trendRange = new ComboBox { Width = 200 };
        Ui.ApplyComboAccent(_trendRange, _accent);
        _trendRange.SelectionChanged += async (_, _) => await OnTrendRangeChanged();
        _trendHint = Ui.Caption("");

        for (int i = 0; i < 4; i++)
        {
            _metricLabels[i] = Ui.Caption("");
            _metricValues[i] = new TextBlock
            {
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
        }

        var metrics = new Grid { ColumnSpacing = 16 };
        for (int i = 0; i < 4; i++)
        {
            metrics.ColumnDefinitions.Add(new ColumnDefinition());
            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(_metricLabels[i]);
            stack.Children.Add(_metricValues[i]);
            Grid.SetColumn(stack, i);
            metrics.Children.Add(stack);
        }

        _chart = new Grid { Height = ChartHeight, ColumnSpacing = 3 };
        _chartTipText = new TextBlock
        {
            FontSize = Ui.CaptionSize,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        _chartTip = new Border
        {
            Child = _chartTipText,
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(UIColor.FromArgb(235, 24, 24, 24)),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _chartLayer = new Canvas { IsHitTestVisible = false };
        _chartLayer.Children.Add(_chartTip);

        var chartHost = new Grid();
        chartHost.Children.Add(_chart);
        chartHost.Children.Add(_chartLayer);

        _axisStart = new TextBlock { FontSize = Ui.CaptionSize, Opacity = .5 };
        _axisEnd = new TextBlock { FontSize = Ui.CaptionSize, Opacity = .5, HorizontalAlignment = HorizontalAlignment.Right };
        var axis = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        axis.ColumnDefinitions.Add(new ColumnDefinition());
        axis.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(_axisEnd, 1);
        axis.Children.Add(_axisStart);
        axis.Children.Add(_axisEnd);

        var chartStack = new StackPanel { Spacing = 0 };
        chartStack.Children.Add(chartHost);
        chartStack.Children.Add(axis);

        var trendControls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        trendControls.Children.Add(_trendRange);
        trendControls.Children.Add(_trendHint);

        var trendPanel = new StackPanel { Spacing = 16, Padding = Ui.PagePadding };
        trendPanel.Children.Add(trendControls);
        trendPanel.Children.Add(Ui.Card(metrics));
        trendPanel.Children.Add(Ui.Card(chartStack));

        var trendScroll = new ScrollViewer
        {
            Content = trendPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        _appRange = new ComboBox { Width = 200 };
        Ui.ApplyComboAccent(_appRange, _accent);
        _appRange.SelectionChanged += (_, _) => OnAppRangeChanged();
        _appDate = new CalendarDatePicker { Width = 180, DateFormat = "{year.full}-{month.integer}/{day.integer}" };
        _appDate.DateChanged += (_, _) => OnAppRangeChanged();
        _appDate.Visibility = Visibility.Collapsed;
        _search = new TextBox { PlaceholderText = "", Width = 200 };
        _search.TextChanged += (_, _) => RenderAppList();

        var appControls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        appControls.Children.Add(_appRange);
        appControls.Children.Add(_appDate);
        appControls.Children.Add(_search);

        _list = new StackPanel { Spacing = 2 };
        _listEmpty = Ui.Caption("");
        _listEmpty.Visibility = Visibility.Collapsed;

        var listScroll = new ScrollViewer
        {
            Content = new StackPanel { Children = { _listEmpty, _list } },
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        _detail = new StackPanel { Spacing = 12 };
        _detailEmpty = Ui.Caption("");
        _detailHost = new Border
        {
            BorderBrush = new SolidColorBrush(UIColor.FromArgb(30, 128, 128, 128)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20),
            Width = 320,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = new ScrollViewer
            {
                Content = new StackPanel { Children = { _detailEmpty, _detail } },
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        };

        var masterDetail = new Grid { ColumnSpacing = 16 };
        masterDetail.ColumnDefinitions.Add(new ColumnDefinition());
        masterDetail.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(listScroll, 0);
        Grid.SetColumn(_detailHost, 1);
        masterDetail.Children.Add(listScroll);
        masterDetail.Children.Add(_detailHost);

        var appsPanel = new StackPanel { Spacing = 16, Padding = Ui.PagePadding };
        appsPanel.Children.Add(appControls);
        appsPanel.Children.Add(masterDetail);

        var appsScroll = new ScrollViewer
        {
            Content = appsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        _trendScroll = trendScroll;
        _appsScroll = appsScroll;

        _trendTabText = new TextBlock { Text = "", FontSize = Ui.BodySize, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        _appsTabText = new TextBlock { Text = "", FontSize = Ui.BodySize, Opacity = .6 };

        _trendTab = TabButton(_trendTabText, out _trendUnderline);
        _appsTab = TabButton(_appsTabText, out _appsUnderline);
        _trendTab.Click += (_, _) => SetTab(true);
        _appsTab.Click += (_, _) => SetTab(false);

        var tabBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 24,
            Padding = new Thickness(28, 14, 28, 0)
        };
        tabBar.Children.Add(_trendTab);
        tabBar.Children.Add(_appsTab);

        _appsScroll.Visibility = Visibility.Collapsed;
        var host = new Grid();
        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(tabBar, 0);
        Grid.SetRow(_trendScroll, 1);
        Grid.SetRow(_appsScroll, 1);
        host.Children.Add(tabBar);
        host.Children.Add(_trendScroll);
        host.Children.Add(_appsScroll);

        Content = host;
        ApplyTabVisuals();

        Loaded += async (_, _) =>
        {
            await EnsureDailyAll();
            UpdateTrends();
            await EnsureAllRange();
            RenderAppList();
        };
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        _trendHint.Text = Str.Usage.ShareHint(language);
        _search.PlaceholderText = Str.Usage.SearchPlaceholder(language);
        _listEmpty.Text = Str.Usage.NoUsage(language);
        _detailEmpty.Text = Str.Usage.NoSelection(language);
        for (int i = 0; i < _metricLabels.Length; i++)
            _metricLabels[i].Text = Str.Usage.MetricLabel(i, language);

        _updatingRange = true;
        var trendIndex = _trendRange.SelectedIndex;
        _trendRange.Items.Clear();
        for (int i = 0; i < 5; i++)
            _trendRange.Items.Add(Str.Usage.TrendRange(i, language));
        _trendRange.SelectedIndex = Math.Clamp(trendIndex, 0, 4);

        var appIndex = _appRange.SelectedIndex;
        _appRange.Items.Clear();
        for (int i = 0; i < 7; i++)
            _appRange.Items.Add(Str.Usage.AppRange(i, language));
        _appRange.SelectedIndex = Math.Clamp(appIndex, 0, 6);
        _updatingRange = false;

        _trendTabText.Text = Str.Usage.TrendsTab(language);
        _appsTabText.Text = Str.Usage.AppsTab(language);
        ApplyTabVisuals();

        _listSignature = "";
        RenderTrendChart();
        RenderAppList();
    }

    public void ApplyAccent(UIColor color)
    {
        _accent = color;
        Ui.ApplyComboAccent(_trendRange, color);
        Ui.ApplyComboAccent(_appRange, color);
        RenderTrendChart();
        _listSignature = "";
        RenderAppList();
    }

    public void UpdateSnapshot(Snapshot snapshot)
    {
        _snapshot = snapshot;
        if (_showTrends)
            UpdateTrends();
        else
            RenderAppList();
    }

    static Button TabButton(TextBlock label, out Border underline)
    {
        underline = new Border
        {
            Height = 2,
            CornerRadius = new CornerRadius(1),
            Background = new SolidColorBrush(UIColor.FromArgb(0, 0, 0, 0)),
            Margin = new Thickness(0, 6, 0, 0)
        };
        var stack = new StackPanel { Spacing = 0 };
        stack.Children.Add(label);
        stack.Children.Add(underline);
        return new Button
        {
            Content = stack,
            Padding = new Thickness(0, 4, 0, 6),
            Background = new SolidColorBrush(UIColor.FromArgb(0, 0, 0, 0)),
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
    }

    void SetTab(bool trends)
    {
        if (_showTrends == trends)
            return;
        _showTrends = trends;
        _trendScroll.Visibility = trends ? Visibility.Visible : Visibility.Collapsed;
        _appsScroll.Visibility = trends ? Visibility.Collapsed : Visibility.Visible;
        ApplyTabVisuals();
        _ = trends ? LoadTrendsAsync() : LoadAppsAsync();
    }

    void ApplyTabVisuals()
    {
        _trendTabText.FontWeight = _showTrends ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
        _appsTabText.FontWeight = _showTrends ? Microsoft.UI.Text.FontWeights.Normal : Microsoft.UI.Text.FontWeights.SemiBold;
        _trendTabText.Opacity = _showTrends ? 1 : .6;
        _appsTabText.Opacity = _showTrends ? .6 : 1;
        _trendUnderline.Background = new SolidColorBrush(_showTrends ? _accent : UIColor.FromArgb(0, 0, 0, 0));
        _appsUnderline.Background = new SolidColorBrush(_showTrends ? UIColor.FromArgb(0, 0, 0, 0) : _accent);
    }

    async Task LoadTrendsAsync()
    {
        await EnsureDailyAll();
        UpdateTrends();
    }

    async Task LoadAppsAsync()
    {
        await EnsureAllRange();
        RenderAppList();
    }

    async Task EnsureDailyAll()
    {
        if (_dailyAllLoaded)
            return;
        var text = await _query(new[] { "--query-all" });
        if (text is null)
            return;
        try
        {
            var data = JsonSerializer.Deserialize<RangeUsageResponse>(text);
            if (data is null)
                return;
            _dailyAll = data.daily ?? Array.Empty<JsonDaily>();
            _allRange = data;
            _dailyAllLoaded = true;
        }
        catch { }
    }

    async Task EnsureAllRange()
    {
        if (_allRange is not null)
            return;
        var text = await _query(new[] { "--query-all" });
        if (text is null)
            return;
        try { _allRange = JsonSerializer.Deserialize<RangeUsageResponse>(text); } catch { }
    }

    async Task OnTrendRangeChanged()
    {
        if (_updatingRange)
            return;
        await EnsureDailyAll();
        UpdateTrends();
    }

    void UpdateTrends()
    {
        var days = SliceDays();
        var total = days.Sum(d => Math.Max(0, d.seconds));
        var active = days.Count(d => d.seconds > 0);
        var average = days.Length == 0 ? 0 : total / days.Length;
        var peak = days.OrderByDescending(d => d.seconds).FirstOrDefault();

        _metricValues[0].Text = UiHelpers.Format(total, _language);
        _metricValues[1].Text = UiHelpers.Format(average, _language);
        _metricValues[2].Text = active.ToString();
        _metricValues[3].Text = peak is null || peak.seconds <= 0
            ? "—"
            : UiHelpers.Format(peak.seconds, _language);
        _metricLabels[3].Text = peak is null || peak.seconds <= 0
            ? Str.Usage.MetricLabel(3, _language)
            : $"{Str.Usage.MetricLabel(3, _language)} · {ShortDate(peak.label)}";

        var peakSeconds = peak is null ? 0 : Math.Max(0, peak.seconds);
        var signature = days.Length + "|" + total + "|" + peakSeconds;
        if (!string.Equals(signature, _trendSignature, StringComparison.Ordinal))
        {
            _trendSignature = signature;
            _trendDays = days;
            RenderTrendChart();
        }
    }

    JsonDaily[] SliceDays()
    {
        var all = _dailyAll;
        if (all.Length == 0)
            return Array.Empty<JsonDaily>();

        var take = _trendRange.SelectedIndex switch
        {
            0 => 30,
            1 => 90,
            2 => 180,
            3 => 365,
            _ => 0
        };
        if (take == 0)
            return all;
        // Fill the window so the chart always has one column per day, including
        // days without any record.
        var map = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var day in all)
            map[day.label] = Math.Max(0, day.seconds);

        var today = DateTime.Today;
        var list = new List<JsonDaily>(take);
        for (int i = take - 1; i >= 0; i--)
        {
            var key = today.AddDays(-i).ToString("yyyy-MM-dd");
            list.Add(new JsonDaily { label = key, seconds = map.TryGetValue(key, out var seconds) ? seconds : 0 });
        }
        return list.ToArray();
    }

    string ShortDate(string raw) => DateTime.TryParse(raw, out var date)
        ? date.ToString("MM-dd")
        : raw;

    void RenderTrendChart()
    {
        _chart.Children.Clear();
        _chart.ColumnDefinitions.Clear();
        _trendHover = -1;
        _chartTip.Visibility = Visibility.Collapsed;

        var days = _trendDays;
        if (days.Length == 0)
            return;

        _axisStart.Text = ShortDate(days[0].label);
        _axisEnd.Text = ShortDate(days[^1].label);

        var peak = Math.Max(1, days.Max(d => Math.Max(0, d.seconds)));
        for (int i = 0; i < days.Length; i++)
        {
            _chart.ColumnDefinitions.Add(new ColumnDefinition());
            var cell = new Grid();
            var gap = days.Length <= 60 ? 3 : 0.5;
            var bar = new Border
            {
                Height = days[i].seconds <= 0 ? 0 : Math.Max(4, ChartHeight * days[i].seconds / (double)peak),
                CornerRadius = new CornerRadius(2, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(gap, 0, gap, 0),
                Background = Ui.Brush(_accent),
                Opacity = .9
            };
            cell.Children.Add(bar);
            int index = i;
            cell.PointerEntered += (_, _) => ShowChartTip(index);
            cell.PointerMoved += (_, _) => ShowChartTip(index);
            cell.PointerExited += (_, _) => HideChartTip(index);
            Grid.SetColumn(cell, i);
            _chart.Children.Add(cell);
        }
    }

    void ShowChartTip(int index)
    {
        if (index < 0 || index >= _trendDays.Length || _chart.ActualWidth <= 0)
            return;

        _trendHover = index;
        var day = _trendDays[index];
        _chartTipText.Text = $"{day.label}  ·  {UiHelpers.Format(Math.Max(0, day.seconds), _language)}";
        _chartTip.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

        var column = _chart.ActualWidth / _trendDays.Length;
        var left = column * index + (column - _chartTip.DesiredSize.Width) / 2;
        left = Math.Clamp(left, 0, Math.Max(0, _chart.ActualWidth - _chartTip.DesiredSize.Width));
        Canvas.SetLeft(_chartTip, left);
        Canvas.SetTop(_chartTip, 0);
        _chartTip.Visibility = Visibility.Visible;
    }

    void HideChartTip(int index)
    {
        if (_trendHover != index)
            return;
        _trendHover = -1;
        _chartTip.Visibility = Visibility.Collapsed;
    }

    void OnAppRangeChanged()
    {
        if (_updatingRange)
            return;
        _appDate.Visibility = _appRange.SelectedIndex == 6 ? Visibility.Visible : Visibility.Collapsed;
        if (_appRange.SelectedIndex == 6 && _appDate.Date is null)
            _appDate.Date = DateTimeOffset.Now.Date;
        _listSignature = "";
        _ = RefreshAppDataAsync();
    }

    async Task RefreshAppDataAsync()
    {
        if (_appRange.SelectedIndex == 6)
        {
            var date = (_appDate.Date ?? DateTimeOffset.Now).Date;
            var text = await _query(new[] { "--query-day", date.ToString("yyyy-MM-dd") });
            if (text is not null)
            {
                try { _dayRange = JsonSerializer.Deserialize<DayUsageResponse>(text); } catch { }
            }
        }
        else if (_appRange.SelectedIndex == 5)
        {
            await EnsureAllRange();
        }

        _listSignature = "";
        RenderAppList();
    }

    AppStat[] CurrentApps()
    {
        return _appRange.SelectedIndex switch
        {
            0 => _snapshot.apps,
            1 => _snapshot.apps_week,
            2 => _snapshot.apps_month,
            3 => _snapshot.apps_half_year,
            4 => _snapshot.apps_year,
            5 => _allRange?.apps ?? Array.Empty<AppStat>(),
            6 => _dayRange?.apps ?? Array.Empty<AppStat>(),
            _ => Array.Empty<AppStat>()
        };
    }

    long CurrentTotal(AppStat[] apps)
    {
        if (_appRange.SelectedIndex == 6 && _dayRange is not null)
            return _dayRange.total_seconds;
        return apps.Sum(a => Math.Max(0, a.seconds));
    }

    void RenderAppList()
    {
        var apps = CurrentApps();
        var total = CurrentTotal(apps);
        var filter = _search.Text?.Trim() ?? "";
        var filtered = apps
            .Where(a => a.seconds >= 60)
            .Where(a => filter.Length == 0 || UiHelpers.FriendlyName(a, _language).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.seconds)
            .ToArray();

        var signature = _appRange.SelectedIndex + "|" + (_search.Text ?? "") + "|" + string.Join(';', filtered.Select(a => a.name + ":" + a.seconds));
        if (string.Equals(signature, _listSignature, StringComparison.Ordinal) && _list.Children.Count > 0)
            return;
        _listSignature = signature;

        _list.Children.Clear();
        _listEmpty.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (filtered.Length == 0)
            return;

        var max = Math.Max(1, filtered[0].seconds);
        foreach (var app in filtered)
        {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new Microsoft.UI.Xaml.Controls.Image { Width = 20, Height = 20, Stretch = Stretch.Uniform };
            UiHelpers.SetAppIcon(icon, app);
            var name = new TextBlock
            {
                Text = UiHelpers.FriendlyName(app, _language),
                FontSize = Ui.BodySize,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            var time = new TextBlock
            {
                Text = UiHelpers.Format(app.seconds, _language),
                FontSize = Ui.BodySize,
                Opacity = .62,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            Grid.SetColumn(name, 1);
            Grid.SetColumn(time, 2);
            row.Children.Add(icon);
            row.Children.Add(name);
            row.Children.Add(time);

            var track = new Border
            {
                Height = 3,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(UIColor.FromArgb(24, 128, 128, 128))
            };
            var fill = new Border
            {
                Height = 3,
                CornerRadius = new CornerRadius(2),
                Background = Ui.Brush(_accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var barHost = new Grid();
            barHost.Children.Add(track);
            barHost.Children.Add(fill);
            barHost.SizeChanged += (_, _) => fill.Width = Math.Max(0, barHost.ActualWidth * app.seconds / (double)max);

            var stack = new StackPanel { Spacing = 5, Padding = new Thickness(6, 6, 6, 6) };
            stack.Children.Add(row);
            stack.Children.Add(barHost);

            var button = new Button
            {
                Content = stack,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(6),
                Background = new SolidColorBrush(UIColor.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0)
            };
            button.Click += async (_, _) => await ShowDetail(app, total);
            _list.Children.Add(button);
        }
    }

    async Task ShowDetail(AppStat app, long total)
    {
        _detailKey = app.name;
        _detail.Children.Clear();
        _detailEmpty.Visibility = Visibility.Collapsed;

        var header = new Grid { ColumnSpacing = 10 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        header.ColumnDefinitions.Add(new ColumnDefinition());
        var icon = new Microsoft.UI.Xaml.Controls.Image { Width = 28, Height = 28, Stretch = Stretch.Uniform };
        UiHelpers.SetAppIcon(icon, app);
        var title = new StackPanel { Spacing = 2 };
        title.Children.Add(new TextBlock
        {
            Text = UiHelpers.FriendlyName(app, _language),
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var share = total > 0 ? app.seconds * 100.0 / total : 0;
        title.Children.Add(Ui.Caption($"{UiHelpers.Format(app.seconds, _language)}  ·  {share:F0}%"));
        Grid.SetColumn(icon, 0);
        Grid.SetColumn(title, 1);
        header.Children.Add(icon);
        header.Children.Add(title);
        _detail.Children.Add(header);

        var history = await LoadHistory(app);
        if (!string.Equals(_detailKey, app.name, StringComparison.Ordinal))
            return;
        if (history is null)
        {
            _detail.Children.Add(Ui.Caption(Str.Usage.LoadFailed(_language)));
            return;
        }

        var list = new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
        foreach (var day in history.daily.Where(d => d.seconds > 0).Reverse())
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = day.label, FontSize = Ui.CaptionSize, Opacity = .7 });
            var value = new TextBlock
            {
                Text = UiHelpers.Format(day.seconds, _language),
                FontSize = Ui.CaptionSize,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(value, 1);
            row.Children.Add(value);
            list.Children.Add(row);
        }
        if (list.Children.Count == 0)
            list.Children.Add(Ui.Caption(Str.Usage.NoUsage(_language)));
        _detail.Children.Add(list);
    }

    async Task<AppHistoryResponse?> LoadHistory(AppStat app)
    {
        var index = _appRange.SelectedIndex;
        var args = new List<string> { "--query-app", app.name };
        if (index == 6)
        {
            var date = (_appDate.Date ?? DateTimeOffset.Now).Date.ToString("yyyy-MM-dd");
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

        var text = await _query(args.ToArray());
        if (text is null)
            return null;
        try { return JsonSerializer.Deserialize<AppHistoryResponse>(text); } catch { return null; }
    }
}
