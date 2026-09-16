using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIImage = Microsoft.UI.Xaml.Controls.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace ScreenTimeRS.UI;

public enum LanguageMode
{
    Chinese = 0,
    English = 1
}

public sealed class OverviewPage : Page
{
    readonly TextBlock[] values = new TextBlock[4];
    readonly TextBlock[] cardTitles = new TextBlock[4];
    readonly TextBlock[] cardSubs = new TextBlock[4];
    readonly InfoBar status;
    readonly TextBlock pageTitle;
    readonly TextBlock trendTitle;
    readonly TextBlock trendSummary;
    readonly Border[] trendBars = new Border[14];
    readonly TextBlock[] trendLabels = new TextBlock[14];
    readonly Border trendHoverCard;
    readonly TextBlock trendHoverText;
    int _hoveredTrendIndex = -1;
    LanguageMode _language = LanguageMode.Chinese;
    Snapshot? _lastSnapshot;
    bool statusClosed;

    public OverviewPage()
    {
        var panel = new StackPanel { Spacing = 18, Padding = new Thickness(28) };
        pageTitle = new TextBlock { FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        panel.Children.Add(pageTitle);

        var cards = new Grid { ColumnSpacing = 14, RowSpacing = 14 };
        for (int i = 0; i < 4; i++) cards.ColumnDefinitions.Add(new ColumnDefinition());
        AddCard(cards, 0, out values[0], out cardTitles[0], out cardSubs[0]);
        AddCard(cards, 1, out values[1], out cardTitles[1], out cardSubs[1]);
        AddCard(cards, 2, out values[2], out cardTitles[2], out cardSubs[2]);
        AddCard(cards, 3, out values[3], out cardTitles[3], out cardSubs[3]);
        cards.SizeChanged += (_, e) => UpdateCardLayout(cards, e.NewSize.Width);
        panel.Children.Add(cards);
        UpdateCardLayout(cards, 1000);

        status = new InfoBar { IsOpen = true, IsClosable = true };
        status.Closed += (_, _) => statusClosed = true;
        panel.Children.Add(status);

        var trendBorder = new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1)
        };
        var trendPanel = new StackPanel { Spacing = 10 };
        trendTitle = new TextBlock { FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        trendSummary = new TextBlock { FontSize = 12, Opacity = .65 };
        trendPanel.Children.Add(trendTitle);
        trendPanel.Children.Add(trendSummary);

        var chartScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollMode = ScrollMode.Disabled
        };

        var chartHost = new Grid { Height = 190, MinWidth = 784 };
        var chart = new Grid { Height = 190, ColumnSpacing = 8, MinWidth = 784 };
        var hoverCanvas = new Canvas { Height = 190, MinWidth = 784, IsHitTestVisible = false };

        for (int i = 0; i < 14; i++)
        {
            chart.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });

            var day = new Grid
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var dayContent = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Stretch, IsHitTestVisible = false };
            var barArea = new Grid { Height = 145, VerticalAlignment = VerticalAlignment.Bottom };
            var bar = new Border
            {
                Width = 24,
                Height = 4,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = .9
            };
            barArea.Children.Add(bar);

            var label = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 11,
                Opacity = .68
            };
            dayContent.Children.Add(barArea);
            dayContent.Children.Add(label);
            day.Children.Add(dayContent);

            int dayIndex = i;
            day.PointerEntered += (_, _) => ShowTrendHover(dayIndex);
            day.PointerMoved += (_, _) => ShowTrendHover(dayIndex);
            day.PointerExited += (_, _) => HideTrendHover(dayIndex);

            Grid.SetColumn(day, i);
            chart.Children.Add(day);
            trendBars[i] = bar;
            trendLabels[i] = label;
        }

        chartHost.Children.Add(chart);
        trendHoverText = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            TextWrapping = TextWrapping.NoWrap
        };
        trendHoverCard = new Border
        {
            Child = trendHoverText,
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(7),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        hoverCanvas.Children.Add(trendHoverCard);
        chartHost.Children.Add(hoverCanvas);
        chartScroll.Content = chartHost;
        trendPanel.Children.Add(chartScroll);
        trendBorder.Child = trendPanel;
        panel.Children.Add(trendBorder);

        Content = new ScrollViewer { Content = panel };
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        var en = language == LanguageMode.English;
        pageTitle.Text = en ? "Overview" : "概览";
        cardTitles[0].Text = en ? "Today" : "今天";
        cardTitles[1].Text = en ? "Yesterday" : "昨天";
        cardTitles[2].Text = en ? "This week" : "本周";
        cardTitles[3].Text = en ? "This month" : "本月";
        cardSubs[0].Text = en ? "Active time" : "活跃时间";
        cardSubs[1].Text = en ? "Active time" : "活跃时间";
        cardSubs[2].Text = en ? "Monday to today" : "周一至今天";
        cardSubs[3].Text = en ? "Accumulated this month" : "本月累计";
        trendTitle.Text = en ? "Last 14 days" : "最近 14 天";
        UpdateSnapshot(_lastSnapshot ?? new Snapshot());
    }

    public void UpdateSnapshot(Snapshot s)
    {
        _lastSnapshot = s;
        values[0].Text = UiHelpers.Format(s.today, _language);
        values[1].Text = UiHelpers.Format(s.yesterday, _language);
        values[2].Text = UiHelpers.Format(s.week, _language);
        values[3].Text = UiHelpers.Format(s.month, _language);

        var name = string.IsNullOrWhiteSpace(s.current_app) ? "—" :
            UiHelpers.FriendlyName(new AppStat { name = s.current_app });
        status.Severity = s.locked ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
        status.Title = s.locked
            ? (_language == LanguageMode.English ? "Currently locked" : "当前已锁定")
            : (_language == LanguageMode.English ? "Recording usage time" : "正在记录使用时间");
        status.Message = _language == LanguageMode.English
            ? $"Current app: {name}    CPU {s.cpu:F1}%    Memory {s.memory_mb} MB"
            : $"当前应用：{name}    CPU {s.cpu:F1}%    内存 {s.memory_mb} MB";
        if (!statusClosed) status.IsOpen = true;

        var days = s.daily.TakeLast(14).ToArray();
        var max = Math.Max(1, days.Select(x => x.seconds).DefaultIfEmpty(1).Max());
        var total = days.Sum(x => x.seconds);
        var average = days.Length == 0 ? 0 : total / days.Length;
        trendSummary.Text = _language == LanguageMode.English
            ? $"Total {UiHelpers.Format(total, _language)}  ·  Daily average {UiHelpers.Format(average, _language)}"
            : $"14 天累计 {UiHelpers.Format(total, _language)}  ·  日均 {UiHelpers.Format(average, _language)}";

        for (int i = 0; i < trendBars.Length; i++)
        {
            if (i < days.Length)
            {
                trendLabels[i].Text = days[i].label;
                var fraction = Math.Clamp(days[i].seconds / (double)max, 0.0, 1.0);
                trendBars[i].Height = Math.Max(4, 135 * fraction);
            }
            else
            {
                trendLabels[i].Text = "";
                trendBars[i].Height = 4;
            }
        }

        // The popup is a persistent in-page overlay rather than ToolTipService.
        // Re-apply it after each data refresh so a visible popup never blinks away.
        if (_hoveredTrendIndex >= 0)
            ShowTrendHover(_hoveredTrendIndex);
    }

    void ShowTrendHover(int index)
    {
        if (_lastSnapshot is null || index < 0 || index >= 14) return;

        _hoveredTrendIndex = index;
        var days = _lastSnapshot.daily.TakeLast(14).ToArray();
        trendHoverText.Text = index < days.Length
            ? (_language == LanguageMode.English
                ? $"{days[index].label}: {UiHelpers.Format(days[index].seconds, _language)}"
                : $"{days[index].label}：{UiHelpers.Format(days[index].seconds, _language)}")
            : (_language == LanguageMode.English ? "No usage record" : "暂无使用记录");

        trendHoverCard.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var popupWidth = trendHoverCard.DesiredSize.Width;
        var left = index * 56.0 + 24.0 - (popupWidth / 2.0);
        left = Math.Max(4, Math.Min(left, 784 - popupWidth - 4));
        Canvas.SetLeft(trendHoverCard, left);
        Canvas.SetTop(trendHoverCard, 4);
        trendHoverCard.Visibility = Visibility.Visible;
    }

    void HideTrendHover(int index)
    {
        if (_hoveredTrendIndex == index)
        {
            _hoveredTrendIndex = -1;
            trendHoverCard.Visibility = Visibility.Collapsed;
        }
    }

    static void AddCard(Grid g, int col, out TextBlock value, out TextBlock title, out TextBlock sub)
    {
        var b = new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1)
        };
        var p = new StackPanel { Spacing = 5 };
        title = new TextBlock { Opacity = .65 };
        p.Children.Add(title);
        value = new TextBlock
        {
            Text = "00小时 00分钟",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None,
            MinHeight = 34
        };
        p.Children.Add(value);
        sub = new TextBlock { Opacity = .6, FontSize = 12 };
        p.Children.Add(sub);
        b.Child = p; Grid.SetColumn(b, col); g.Children.Add(b);
    }

    static void UpdateCardLayout(Grid g, double width)
    {
        int columns = width >= 900 ? 4 : width >= 560 ? 2 : 1;
        int rows = (4 + columns - 1) / columns;
        while (g.RowDefinitions.Count < rows)
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        while (g.RowDefinitions.Count > rows)
            g.RowDefinitions.RemoveAt(g.RowDefinitions.Count - 1);
        for (int i = 0; i < g.Children.Count; i++)
        {
            if (g.Children[i] is FrameworkElement element)
            {
                Grid.SetColumn(element, i % columns);
                Grid.SetRow(element, i / columns);
            }
        }
    }
}

public sealed class AppsPage : Page
{
    readonly StackPanel list = new();
    readonly TextBox search = new();
    readonly ComboBox period = new();
    readonly TextBlock title = new();
    readonly Dictionary<string, AppRow> rows = new(StringComparer.OrdinalIgnoreCase);
    Snapshot snapshot = new();
    LanguageMode _language = LanguageMode.Chinese;

    public AppsPage()
    {
        // Use a Grid with a star-sized content row so the ScrollViewer receives
        // a finite viewport. A ScrollViewer inside a StackPanel can measure its
        // content at infinity and therefore fail to become scrollable.
        var root = new Grid { Padding = new Thickness(28), RowSpacing = 16 };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        title.FontSize = 30;
        title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var filters = new Grid { ColumnSpacing = 10 };
        filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
        filters.ColumnDefinitions.Add(new ColumnDefinition());
        period.Items.Add("今天");
        period.Items.Add("本周");
        period.Items.Add("本月");
        period.Items.Add("近半年");
        period.Items.Add("近一年");
        period.SelectedIndex = 0;
        period.SelectionChanged += (_, _) => RenderList();
        Grid.SetColumn(period, 0);
        filters.Children.Add(period);
        search.PlaceholderText = "搜索应用";
        search.TextChanged += (_, _) => RenderList();
        Grid.SetColumn(search, 1);
        filters.Children.Add(search);
        Grid.SetRow(filters, 1);
        root.Children.Add(filters);

        var scroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(scroll, 2);
        root.Children.Add(scroll);

        Content = root;
    }

    public void ApplyLanguage(LanguageMode language)
    {
        title.Text = language == LanguageMode.English ? "App usage" : "应用使用时间";
        search.PlaceholderText = language == LanguageMode.English ? "Search apps" : "搜索应用";
        var index = period.SelectedIndex;
        period.Items.Clear();
        var items = language == LanguageMode.English
            ? new[] { "Today", "This week", "This month", "Last 6 months", "Last year" }
            : new[] { "今天", "本周", "本月", "近半年", "近一年" };
        foreach (var item in items) period.Items.Add(item);
        period.SelectedIndex = Math.Clamp(index, 0, items.Length - 1);
        _language = language;
        RenderList();
    }

    public void UpdateSnapshot(Snapshot s) { snapshot = s; RenderList(); }

    void RenderList()
    {
        var source = period.SelectedIndex switch
        {
            1 => snapshot.apps_week,
            2 => snapshot.apps_month,
            3 => snapshot.apps_half_year,
            4 => snapshot.apps_year,
            _ => snapshot.apps
        };
        var apps = source.Where(x => UiHelpers.FriendlyName(x).Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase)).ToArray();
        var wanted = new HashSet<string>(apps.Select(Key), StringComparer.OrdinalIgnoreCase);

        foreach (var key in rows.Keys.Where(k => !wanted.Contains(k)).ToArray()) {
            list.Children.Remove(rows[key].Border); rows.Remove(key);
        }
        foreach (var app in apps) {
            var key = Key(app);
            if (!rows.TryGetValue(key, out var row)) { row = new AppRow(app, _language); rows[key] = row; }
            row.Update(app, source.Sum(x => x.seconds), _language);
        }

        var ordered = apps.Select(a => rows[Key(a)].Border).ToArray();
        bool same = ordered.Length == list.Children.Count &&
            ordered.Select((x, i) => ReferenceEquals(x, list.Children[i])).All(x => x);
        if (!same) {
            list.Children.Clear();
            foreach (var item in ordered) list.Children.Add(item);
        }
    }

    static string Key(AppStat a) => string.IsNullOrWhiteSpace(a.exe_path) ? a.name : a.exe_path;

    sealed class AppRow
    {
        public Border Border { get; }
        readonly TextBlock name, path, time;
        readonly ProgressBar bar;
        readonly WinUIImage icon;
        string iconKey = "";
        static readonly Dictionary<string, BitmapImage> IconCache = new(StringComparer.OrdinalIgnoreCase);

        public AppRow(AppStat app, LanguageMode language)
        {
            var grid = new Grid { Padding = new Thickness(10), ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            icon = new WinUIImage { Width = 36, Height = 36, Stretch = Stretch.Uniform };
            Grid.SetColumn(icon, 0); grid.Children.Add(icon);

            var info = new StackPanel();
            name = new TextBlock { FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            path = new TextBlock { Opacity = .55, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis };
            info.Children.Add(name); info.Children.Add(path);
            Grid.SetColumn(info, 1); grid.Children.Add(info);

            var right = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            time = new TextBlock { FontSize = 15 };
            bar = new ProgressBar { Minimum = 0, Maximum = 100, Width = 110 };
            right.Children.Add(time); right.Children.Add(bar);
            Grid.SetColumn(right, 2); grid.Children.Add(right);

            Border = new Border { Child = grid, BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 0, 1) };
            Update(app, 0, language);
        }

        public void Update(AppStat app, long today, LanguageMode language)
        {
            name.Text = UiHelpers.FriendlyName(app);
            path.Text = app.name + (string.IsNullOrWhiteSpace(app.exe_path) ? "" : " · " + app.exe_path);
            time.Text = UiHelpers.Format(app.seconds, language);
            bar.Value = Math.Min(100, app.seconds * 100.0 / Math.Max(1, today));
            if (!string.Equals(iconKey, app.exe_path, StringComparison.OrdinalIgnoreCase)) {
                iconKey = app.exe_path; SetIcon(app);
            }
        }

        void SetIcon(AppStat app)
        {
            try {
                if (string.IsNullOrWhiteSpace(app.exe_path) || !File.Exists(app.exe_path)) { icon.Source = null; return; }
                if (IconCache.TryGetValue(app.exe_path, out var cached)) { icon.Source = cached; return; }
                using var ico = Icon.ExtractAssociatedIcon(app.exe_path);
                if (ico == null) return;
                var dir = Path.Combine(Path.GetTempPath(), "ScreenTimeRS-icons");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(app.exe_path))) + ".png");
                if (!File.Exists(file)) using (var bmp = ico.ToBitmap()) bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                var bitmap = new BitmapImage(new Uri(file));
                IconCache[app.exe_path] = bitmap; icon.Source = bitmap;
            } catch { }
        }
    }
}

public sealed class StatsPage : Page
{
    readonly TextBlock[] summary = new TextBlock[4];
    readonly TextBlock[] summaryTitles = new TextBlock[4];
    readonly List<(TextBlock Label, ProgressBar Bar, TextBlock Value)> trend = new();
    readonly StackPanel top = new();
    readonly TextBlock title = new();
    readonly TextBlock description = new();
    readonly TextBlock trendTitle = new();
    readonly TextBlock topTitle = new();
    LanguageMode _language = LanguageMode.Chinese;
    Snapshot? _lastSnapshot;

    public StatsPage()
    {
        var p = new StackPanel { Spacing = 18, Padding = new Thickness(28) };
        title.FontSize = 30; title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; p.Children.Add(title);
        description.Opacity = .65; p.Children.Add(description);

        var grid = new Grid { ColumnSpacing = 12 };
        for (int i = 0; i < 4; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        AddSummary(grid, 0, out summary[0], out summaryTitles[0]); AddSummary(grid, 1, out summary[1], out summaryTitles[1]);
        AddSummary(grid, 2, out summary[2], out summaryTitles[2]); AddSummary(grid, 3, out summary[3], out summaryTitles[3]);
        p.Children.Add(grid);

        var trendBorder = new Border { Padding = new Thickness(20), CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var tp = new StackPanel { Spacing = 10 };
        trendTitle.FontSize = 20; trendTitle.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; tp.Children.Add(trendTitle);
        for (int i = 0; i < 14; i++) {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center }; row.Children.Add(label);
            var bar = new ProgressBar { Minimum = 0, Maximum = 1, Height = 16 }; Grid.SetColumn(bar, 1); row.Children.Add(bar);
            var value = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(value, 2); row.Children.Add(value); tp.Children.Add(row);
            trend.Add((label, bar, value));
        }
        trendBorder.Child = tp; p.Children.Add(trendBorder);

        var topBorder = new Border { Padding = new Thickness(20), CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        top.Spacing = 10;
        topTitle.FontSize = 20; topTitle.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; top.Children.Add(topTitle);
        topBorder.Child = top; p.Children.Add(topBorder);
        Content = new ScrollViewer { Content = p };
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        var en = language == LanguageMode.English;
        title.Text = en ? "Statistics" : "统计";
        description.Text = en ? "Live statistics from the background collector, updated every second." : "实时统计来自后台采集器，每秒更新一次。";
        summaryTitles[0].Text = en ? "Today" : "今天";
        summaryTitles[1].Text = en ? "Yesterday" : "昨天";
        summaryTitles[2].Text = en ? "This week" : "本周";
        summaryTitles[3].Text = en ? "This month" : "本月";
        trendTitle.Text = en ? "Last 14 days" : "最近 14 天";
        topTitle.Text = en ? "Today's top apps" : "今日应用排行";
        UpdateSnapshot(_lastSnapshot ?? new Snapshot());
    }

    public void UpdateSnapshot(Snapshot s)
    {
        _lastSnapshot = s;
        summary[0].Text = UiHelpers.Format(s.today, _language); summary[1].Text = UiHelpers.Format(s.yesterday, _language);
        summary[2].Text = UiHelpers.Format(s.week, _language); summary[3].Text = UiHelpers.Format(s.month, _language);
        var days = s.daily.TakeLast(14).ToArray();
        var max = Math.Max(1, days.Select(x => x.seconds).DefaultIfEmpty(1).Max());
        for (int i = 0; i < trend.Count; i++) {
            if (i < days.Length) {
                trend[i].Label.Text = days[i].label; trend[i].Bar.Maximum = max; trend[i].Bar.Value = days[i].seconds;
                trend[i].Value.Text = UiHelpers.Format(days[i].seconds, _language);
            } else { trend[i].Label.Text = ""; trend[i].Bar.Maximum = max; trend[i].Bar.Value = 0; trend[i].Value.Text = ""; }
        }
        while (top.Children.Count > 1) top.Children.RemoveAt(1);
        foreach (var app in s.apps.Take(10)) {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            var icon = new WinUIImage { Width = 28, Height = 28, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center };
            UiHelpers.SetAppIcon(icon, app);
            row.Children.Add(icon);
            var name = new TextBlock { Text = UiHelpers.FriendlyName(app), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(name, 1); row.Children.Add(name);
            var value = new TextBlock { Text = UiHelpers.Format(app.seconds, _language), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(value, 2); row.Children.Add(value); top.Children.Add(row);
        }
        if (s.apps.Length == 0) top.Children.Add(new TextBlock { Text = _language == LanguageMode.English ? "No app usage recorded today." : "今天还没有记录到应用使用时间。", Opacity = .65 });
    }

    static void AddSummary(Grid g, int col, out TextBlock value, out TextBlock title)
    {
        var b = new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var p = new StackPanel { Spacing = 5 };
        title = new TextBlock { Opacity = .65 };
        p.Children.Add(title);
        value = new TextBlock { Text = "00小时 00分钟", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        p.Children.Add(value); b.Child = p; Grid.SetColumn(b, col); g.Children.Add(b);
    }
}

public sealed class SettingsPage : Page
{
    readonly CheckBox startup, background;
    readonly ComboBox theme, language;
    readonly TextBlock pageTitle, generalTitle, appearanceTitle, aboutTitle, startupText, backgroundText, languageTitle, aboutText;
    bool updatingTheme;
    bool updatingLanguage;

    public event EventHandler<ThemeMode>? ThemeModeChanged;
    public event EventHandler<LanguageMode>? LanguageChanged;

    public SettingsPage(ThemeMode mode, LanguageMode languageMode)
    {
        var p = new StackPanel { Spacing = 18, Padding = new Thickness(28), MaxWidth = 720, HorizontalAlignment = HorizontalAlignment.Left };
        pageTitle = new TextBlock { FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        generalTitle = new TextBlock { FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        p.Children.Add(pageTitle);
        p.Children.Add(generalTitle);

        startup = new CheckBox { IsChecked = StartupEnabled() };
        startup.Checked += (_, _) => SetStartup(true);
        startup.Unchecked += (_, _) => SetStartup(false);
        startupText = new TextBlock();
        startup.Content = startupText;
        background = new CheckBox { IsChecked = true };
        backgroundText = new TextBlock();
        background.Content = backgroundText;
        p.Children.Add(startup); p.Children.Add(background);

        appearanceTitle = new TextBlock { Margin = new Thickness(0, 15, 0, 0), FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        p.Children.Add(appearanceTitle);
        theme = new ComboBox { Width = 240 };
        theme.SelectionChanged += Theme_SelectionChanged;
        p.Children.Add(theme);

        languageTitle = new TextBlock { Margin = new Thickness(0, 15, 0, 0), FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        p.Children.Add(languageTitle);
        language = new ComboBox { Width = 240 };
        language.SelectionChanged += Language_SelectionChanged;
        p.Children.Add(language);

        aboutTitle = new TextBlock { Margin = new Thickness(0, 15, 0, 0), FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        p.Children.Add(aboutTitle);
        aboutText = new TextBlock();
        p.Children.Add(aboutText);
        Content = new ScrollViewer { Content = p };

        SetLanguage(languageMode);
        SetThemeMode(mode);
    }

    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updatingTheme || theme.SelectedIndex < 0) return;
        ThemeModeChanged?.Invoke(this, (ThemeMode)theme.SelectedIndex);
    }

    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updatingLanguage || language.SelectedIndex < 0) return;
        LanguageChanged?.Invoke(this, (LanguageMode)language.SelectedIndex);
    }

    public void SetThemeMode(ThemeMode mode)
    {
        if (theme.SelectedIndex == (int)mode) return;
        updatingTheme = true;
        theme.SelectedIndex = (int)mode;
        updatingTheme = false;
    }

    public void SetLanguage(LanguageMode mode)
    {
        var en = mode == LanguageMode.English;
        updatingLanguage = true;
        language.Items.Clear();
        language.Items.Add("中文");
        language.Items.Add("English");
        language.SelectedIndex = (int)mode;
        updatingLanguage = false;

        pageTitle.Text = en ? "Settings" : "设置";
        generalTitle.Text = en ? "General" : "常规";
        startupText.Text = en ? "Start ScreenTime RS silently in the background when I sign in to Windows" : "登录 Windows 后在后台静默启动";
        backgroundText.Text = en ? "Continue recording usage time in the background" : "后台继续记录使用时间";
        appearanceTitle.Text = en ? "Appearance" : "外观";
        languageTitle.Text = en ? "Language" : "语言";
        aboutTitle.Text = en ? "About" : "关于";
        aboutText.Text = en
            ? "ScreenTime RS\nVersion 0.4.1\nRust monitoring core + WinUI 3 / Fluent UI"
            : "ScreenTime RS\n版本 0.4.1\nRust monitoring core + WinUI 3 / Fluent UI";

        var themeIndex = theme.SelectedIndex;
        updatingTheme = true;
        theme.Items.Clear();
        if (en)
        {
            theme.Items.Add("Follow system");
            theme.Items.Add("Light");
            theme.Items.Add("Dark");
        }
        else
        {
            theme.Items.Add("跟随系统");
            theme.Items.Add("浅色");
            theme.Items.Add("深色");
        }
        if (themeIndex >= 0 && themeIndex < theme.Items.Count)
            theme.SelectedIndex = themeIndex;
        updatingTheme = false;
    }

    static bool StartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            return key?.GetValue("ScreenTimeRS") is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch { return false; }
    }

    static void SetStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (key == null) return;
            if (enabled)
            {
                var exe = Path.Combine(AppContext.BaseDirectory, "screentime-rs.exe");
                key.SetValue("ScreenTimeRS", $"\"{exe}\" --background");
            }
            else key.DeleteValue("ScreenTimeRS", false);
        }
        catch { }
    }
}
