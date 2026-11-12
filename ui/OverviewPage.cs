using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using UIColor = Windows.UI.Color;

namespace ScreenTimeRS.UI;

/// <summary>
/// Overview: one day at a glance. Everything on this page answers "how much did
/// I use today", so there is no range picker and no second chart.
/// </summary>
public sealed class OverviewPage : Page
{
    const double ChartHeight = 120;

    readonly TextBlock title, todayLabel, todayValue;
    readonly TextBlock currentLabel, currentValue, longestLabel, longestValue, unlockLabel, unlockValue;
    readonly TextBlock hourlyTitle, topTitle, topEmpty;
    readonly Border collectorBanner;
    readonly TextBlock collectorBannerText;
    readonly Border[] hourBars = new Border[24];
    readonly Grid[] hourCells = new Grid[24];
    readonly double[] hourHeights = new double[24];
    readonly Canvas hoverLayer;
    readonly Border hoverCard;
    readonly TextBlock hoverText;
    readonly StackPanel topList;
    readonly Grid hourGrid;

    LanguageMode _language = LanguageMode.SimplifiedChinese;
    UIColor _accent = UIColor.FromArgb(255, 0, 0, 0);
    Snapshot? _snapshot;
    string _topSignature = "";
    int _hoverIndex = -1;
    readonly List<TopRow> _topRows = new();

    sealed class TopRow
    {
        public AppStat App = new();
        public long Max = 1;
        public TextBlock Time = null!;
        public Border Fill = null!;
        public Grid Host = null!;
    }

    public OverviewPage()
    {
        title = Ui.PageTitle("");

        var header = new Grid { ColumnSpacing = 12 };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.Children.Add(title);

        todayLabel = Ui.Caption("");
        todayValue = Ui.Value("");
        var todayStack = new StackPanel { Spacing = 6 };
        todayStack.Children.Add(todayLabel);
        todayStack.Children.Add(todayValue);

        currentLabel = Ui.Caption(""); currentValue = MetricValue();
        longestLabel = Ui.Caption(""); longestValue = MetricValue();
        unlockLabel = Ui.Caption(""); unlockValue = MetricValue();

        var metrics = new Grid { ColumnSpacing = 32, VerticalAlignment = VerticalAlignment.Bottom };
        for (int i = 0; i < 3; i++)
            metrics.ColumnDefinitions.Add(new ColumnDefinition());
        metrics.Children.Add(Metric(currentLabel, currentValue, 0));
        metrics.Children.Add(Metric(longestLabel, longestValue, 1));
        metrics.Children.Add(Metric(unlockLabel, unlockValue, 2));

        var todayCard = new Grid { ColumnSpacing = 48 };
        todayCard.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        todayCard.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(todayStack, 0);
        Grid.SetColumn(metrics, 1);
        todayCard.Children.Add(todayStack);
        todayCard.Children.Add(metrics);

        hourlyTitle = SectionTitle("");
        hourGrid = new Grid { Height = ChartHeight, ColumnSpacing = 6 };
        for (int i = 0; i < 24; i++)
        {
            hourGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var cell = new Grid();
            var bar = new Border
            {
                Width = 10,
                Height = 0,
                CornerRadius = new CornerRadius(2, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Ui.Brush(_accent),
                Opacity = .85
            };
            cell.Children.Add(bar);
            int index = i;
            cell.PointerEntered += (_, _) => ShowHover(index);
            cell.PointerMoved += (_, _) => ShowHover(index);
            cell.PointerExited += (_, _) => HideHover(index);
            Grid.SetColumn(cell, i);
            hourGrid.Children.Add(cell);
            hourCells[i] = cell;
            hourBars[i] = bar;
        }

        hoverText = new TextBlock
        {
            FontSize = Ui.CaptionSize,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        hoverCard = new Border
        {
            Child = hoverText,
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(UIColor.FromArgb(235, 24, 24, 24)),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        hoverLayer = new Canvas { IsHitTestVisible = false };
        hoverLayer.Children.Add(hoverCard);

        var hourHost = new Grid();
        hourHost.Children.Add(hourGrid);
        hourHost.Children.Add(hoverLayer);

        var axis = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        for (int i = 0; i < 24; i++)
            axis.ColumnDefinitions.Add(new ColumnDefinition());
        foreach (var mark in new[] { 0, 6, 12, 18, 23 })
        {
            var label = new TextBlock
            {
                Text = mark.ToString("00"),
                FontSize = Ui.CaptionSize,
                Opacity = .5,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(label, mark);
            axis.Children.Add(label);
        }

        var hourStack = new StackPanel { Spacing = 14 };
        hourStack.Children.Add(hourlyTitle);
        hourStack.Children.Add(hourHost);
        hourStack.Children.Add(axis);

        topTitle = SectionTitle("");
        topEmpty = Ui.Caption("");
        topEmpty.Visibility = Visibility.Collapsed;
        topList = new StackPanel { Spacing = 10 };

        var topStack = new StackPanel { Spacing = 12 };
        topStack.Children.Add(topTitle);
        topStack.Children.Add(topEmpty);
        topStack.Children.Add(topList);

        collectorBannerText = new TextBlock { FontSize = Ui.CaptionSize, TextWrapping = TextWrapping.Wrap };
        collectorBanner = new Border
        {
            Child = collectorBannerText,
            Padding = new Thickness(12, 10, 12, 10),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(UIColor.FromArgb(20, 196, 43, 28)),
            BorderBrush = new SolidColorBrush(UIColor.FromArgb(70, 196, 43, 28)),
            BorderThickness = new Thickness(1),
            Visibility = Visibility.Collapsed
        };

        var root = new StackPanel { Spacing = Ui.CardGap, Padding = Ui.PagePadding };
        root.Children.Add(header);
        root.Children.Add(collectorBanner);
        root.Children.Add(Ui.Card(todayCard));
        root.Children.Add(Ui.Card(hourStack));
        root.Children.Add(Ui.Card(topStack));

        Content = new ScrollViewer
        {
            Content = root,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    /// <summary>Shows a warning when no fresh snapshot is being written, which
    /// means the collector is not recording (missing, crashed or never started).</summary>
    public void SetCollectorRunning(bool running)
    {
        collectorBanner.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        if (!running)
            collectorBannerText.Text = CollectorWarningText();
    }

    string CollectorWarningText() => _language switch
    {
        LanguageMode.English => "Tracking is not running, so screen time is not being recorded. Try restarting the app.",
        LanguageMode.TraditionalChinese => "資料擷取未執行，目前不會記錄螢幕使用時間。請嘗試重新啟動應用程式。",
        _ => "数据采集未运行，当前不会记录屏幕时间。请尝试重启应用。"
    };

    static TextBlock MetricValue() => new()
    {
        FontSize = 15,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    };

    static StackPanel Metric(TextBlock label, TextBlock value, int column)
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(label);
        stack.Children.Add(value);
        Grid.SetColumn(stack, column);
        return stack;
    }

    static TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        FontSize = Ui.BodySize,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    };

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        title.Text = Str.Overview.Title(language);
        todayLabel.Text = Str.Overview.Today(language);
        currentLabel.Text = Str.Overview.CurrentSession(language);
        longestLabel.Text = Str.Overview.LongestSession(language);
        unlockLabel.Text = Str.Overview.Unlocks(language);
        hourlyTitle.Text = Str.Overview.ByHour(language);
        topTitle.Text = Str.Overview.TopApps(language);
        topEmpty.Text = Str.Overview.NoApps(language);
        collectorBannerText.Text = CollectorWarningText();
        _topSignature = "";
        UpdateSnapshot(_snapshot ?? new Snapshot());
    }

    public void ApplyAccent(UIColor color)
    {
        _accent = color;
        var brush = Ui.Brush(color);
        foreach (var bar in hourBars)
            bar.Background = brush;
        // Top-app bars are only rebuilt when the app set changes, so repaint
        // their fill here or they keep the previous theme's accent.
        foreach (var row in _topRows)
            row.Fill.Background = brush;
    }

    public void UpdateSnapshot(Snapshot s)
    {
        _snapshot = s;
        SetText(todayValue, UiHelpers.Format(s.today, _language));
        SetText(currentValue, UiHelpers.Format(Math.Max(0, s.current_session_seconds), _language));
        SetText(longestValue, UiHelpers.Format(Math.Max(0, s.longest_session_today), _language));
        SetText(unlockValue, s.unlocks_today.ToString());

        UpdateHourly(s);
        UpdateTopApps(s);

        if (_hoverIndex >= 0)
            ShowHover(_hoverIndex);
    }

    static void SetText(TextBlock block, string text)
    {
        if (!string.Equals(block.Text, text, StringComparison.Ordinal))
            block.Text = text;
    }

    void UpdateHourly(Snapshot s)
    {
        var peak = 1L;
        for (int i = 0; i < hourBars.Length; i++)
        {
            var seconds = i < s.hourly_today.Length ? Math.Max(0, s.hourly_today[i].seconds) : 0;
            if (seconds > peak) peak = seconds;
        }

        for (int i = 0; i < hourBars.Length; i++)
        {
            var seconds = i < s.hourly_today.Length ? Math.Max(0, s.hourly_today[i].seconds) : 0;
            var height = seconds <= 0 ? 0 : Math.Max(4, ChartHeight * seconds / (double)peak);
            if (Math.Abs(height - hourHeights[i]) < 0.5)
                continue;
            hourHeights[i] = height;
            hourBars[i].Height = height;
        }
    }

    void UpdateTopApps(Snapshot s)
    {
        var top = s.apps.Where(a => a.seconds > 0).OrderByDescending(a => a.seconds).Take(5).ToArray();
        // Only the app set and its order force a rebuild. Seconds change every
        // snapshot, so including them rebuilt the whole list every two seconds.
        var signature = string.Join('|', top.Select(a => a.name));
        if (string.Equals(signature, _topSignature, StringComparison.Ordinal) && _topRows.Count == top.Length)
        {
            UpdateTopAppValues(top);
            return;
        }
        _topSignature = signature;
        RebuildTopApps(top);
    }

    void RebuildTopApps(AppStat[] top)
    {
        topList.Children.Clear();
        _topRows.Clear();
        topEmpty.Visibility = top.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (top.Length == 0)
            return;

        var max = Math.Max(1, top[0].seconds);
        foreach (var app in top)
        {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var name = new TextBlock
            {
                Text = UiHelpers.FriendlyName(app, _language),
                FontSize = Ui.BodySize,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var time = new TextBlock
            {
                Text = UiHelpers.Format(app.seconds, _language),
                FontSize = Ui.BodySize,
                Opacity = .62
            };
            Grid.SetColumn(time, 1);
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
                Width = 0,
                CornerRadius = new CornerRadius(2),
                Background = Ui.Brush(_accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var barHost = new Grid();
            barHost.Children.Add(track);
            barHost.Children.Add(fill);

            var stack = new StackPanel { Spacing = 5 };
            stack.Children.Add(row);
            stack.Children.Add(barHost);
            topList.Children.Add(stack);

            var topRow = new TopRow { App = app, Max = max, Time = time, Fill = fill, Host = barHost };
            barHost.SizeChanged += (_, _) => UpdateTopBar(topRow);
            _topRows.Add(topRow);
        }
        foreach (var topRow in _topRows)
            UpdateTopBar(topRow);
    }

    void UpdateTopAppValues(AppStat[] top)
    {
        topEmpty.Visibility = top.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (top.Length == 0)
            return;

        var max = Math.Max(1, top[0].seconds);
        for (int i = 0; i < _topRows.Count && i < top.Length; i++)
        {
            var topRow = _topRows[i];
            topRow.App = top[i];
            topRow.Max = max;
            topRow.Time.Text = UiHelpers.Format(top[i].seconds, _language);
            UpdateTopBar(topRow);
        }
    }

    static void UpdateTopBar(TopRow row)
        => row.Fill.Width = Math.Max(0, row.Host.ActualWidth * row.App.seconds / (double)Math.Max(1, row.Max));

    void ShowHover(int index)
    {
        if (_snapshot is null || index < 0 || index >= 24)
            return;

        _hoverIndex = index;
        var seconds = index < _snapshot.hourly_today.Length ? Math.Max(0, _snapshot.hourly_today[index].seconds) : 0;
        var label = index < _snapshot.hourly_today.Length && !string.IsNullOrWhiteSpace(_snapshot.hourly_today[index].label)
            ? _snapshot.hourly_today[index].label
            : index.ToString("00");
        hoverText.Text = $"{label}  ·  {UiHelpers.FormatIdle((ulong)seconds, _language)}";

        hoverCard.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = hourGrid.ActualWidth <= 0 ? 0 : hourGrid.ActualWidth;
        var column = width <= 0 ? 0 : width / 24.0;
        var left = column * index + (column - hoverCard.DesiredSize.Width) / 2;
        left = Math.Clamp(left, 0, Math.Max(0, width - hoverCard.DesiredSize.Width));
        Canvas.SetLeft(hoverCard, left);
        Canvas.SetTop(hoverCard, 0);
        hoverCard.Visibility = Visibility.Visible;
    }

    void HideHover(int index)
    {
        if (_hoverIndex != index)
            return;
        _hoverIndex = -1;
        hoverCard.Visibility = Visibility.Collapsed;
    }
}
