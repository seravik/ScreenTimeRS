using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

public sealed class OverviewPage : Page
{
    readonly TextBlock[] values = new TextBlock[4];
    readonly InfoBar status;
    readonly List<(TextBlock Label, ProgressBar Bar)> trend = new();
    bool statusClosed;

    public OverviewPage()
    {
        var panel = new StackPanel { Spacing = 18, Padding = new Thickness(28) };
        panel.Children.Add(new TextBlock { Text = "概览", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        var cards = new Grid { ColumnSpacing = 14, RowSpacing = 14 };
        for (int i = 0; i < 4; i++) cards.ColumnDefinitions.Add(new ColumnDefinition());
        AddCard(cards, 0, "今天", "活跃时间", out values[0]);
        AddCard(cards, 1, "昨天", "活跃时间", out values[1]);
        AddCard(cards, 2, "本周", "周一至今天", out values[2]);
        AddCard(cards, 3, "本月", "本月累计", out values[3]);
        cards.SizeChanged += (_, e) => UpdateCardLayout(cards, e.NewSize.Width);
        panel.Children.Add(cards);
        UpdateCardLayout(cards, 1000);

        status = new InfoBar { IsOpen = true, IsClosable = true, Title = "正在记录使用时间" };
        status.Closed += (_, _) => statusClosed = true;
        panel.Children.Add(status);

        var border = new Border { Padding = new Thickness(20), CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var tp = new StackPanel { Spacing = 8 };
        tp.Children.Add(new TextBlock { Text = "最近 14 天", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        for (int i = 0; i < 14; i++)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var label = new TextBlock { Width = 55 };
            var bar = new ProgressBar { Minimum = 0, Maximum = 1, Width = 420, Height = 18 };
            row.Children.Add(label); row.Children.Add(bar); tp.Children.Add(row);
            trend.Add((label, bar));
        }
        border.Child = tp; panel.Children.Add(border);
        Content = new ScrollViewer { Content = panel };
    }

    public void UpdateSnapshot(Snapshot s)
    {
        values[0].Text = UiHelpers.Format(s.today);
        values[1].Text = UiHelpers.Format(s.yesterday);
        values[2].Text = UiHelpers.Format(s.week);
        values[3].Text = UiHelpers.Format(s.month);
        var name = string.IsNullOrWhiteSpace(s.current_app) ? "—" :
            UiHelpers.FriendlyName(new AppStat { name = s.current_app });
        status.Severity = s.locked ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
        status.Title = s.locked ? "当前已锁定" : "正在记录使用时间";
        status.Message = $"当前应用：{name}    CPU {s.cpu:F1}%    内存 {s.memory_mb} MB";
        if (!statusClosed) status.IsOpen = true;

        var days = s.daily.TakeLast(14).ToArray();
        var max = Math.Max(1, days.Select(x => x.seconds).DefaultIfEmpty(1).Max());
        for (int i = 0; i < trend.Count; i++)
        {
            if (i < days.Length) {
                trend[i].Label.Text = days[i].label;
                trend[i].Bar.Maximum = max;
                trend[i].Bar.Value = days[i].seconds;
            } else {
                trend[i].Label.Text = "";
                trend[i].Bar.Maximum = max;
                trend[i].Bar.Value = 0;
            }
        }
    }

    static void AddCard(Grid g, int col, string title, string sub, out TextBlock value)
    {
        var b = new Border { Padding = new Thickness(18), CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var p = new StackPanel { Spacing = 5 };
        p.Children.Add(new TextBlock { Text = title, Opacity = .65 });
        value = new TextBlock
        {
            Text = "00小时 00分钟",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None,
            MinHeight = 34
        };
        p.Children.Add(value); p.Children.Add(new TextBlock { Text = sub, Opacity = .6, FontSize = 12 });
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
            // Grid.SetColumn/SetRow require a FrameworkElement in this WinUI target.
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
    readonly Dictionary<string, AppRow> rows = new(StringComparer.OrdinalIgnoreCase);
    Snapshot snapshot = new();

    public AppsPage()
    {
        // Use a Grid with a star-sized content row so the ScrollViewer receives
        // a finite viewport. A ScrollViewer inside a StackPanel can measure its
        // content at infinity and therefore fail to become scrollable.
        var root = new Grid { Padding = new Thickness(28), RowSpacing = 16 };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = new TextBlock { Text = "应用使用时间", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        search.PlaceholderText = "搜索应用";
        search.TextChanged += (_, _) => RenderList();
        Grid.SetRow(search, 1);
        root.Children.Add(search);

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

    public void UpdateSnapshot(Snapshot s) { snapshot = s; RenderList(); }

    void RenderList()
    {
        var apps = snapshot.apps.Where(x => UiHelpers.FriendlyName(x).Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase)).ToArray();
        var wanted = new HashSet<string>(apps.Select(Key), StringComparer.OrdinalIgnoreCase);

        foreach (var key in rows.Keys.Where(k => !wanted.Contains(k)).ToArray()) {
            list.Children.Remove(rows[key].Border); rows.Remove(key);
        }
        foreach (var app in apps) {
            var key = Key(app);
            if (!rows.TryGetValue(key, out var row)) { row = new AppRow(app); rows[key] = row; }
            row.Update(app, snapshot.today);
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

        public AppRow(AppStat app)
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
            Update(app, 0);
        }

        public void Update(AppStat app, long today)
        {
            name.Text = UiHelpers.FriendlyName(app);
            path.Text = app.name + (string.IsNullOrWhiteSpace(app.exe_path) ? "" : " · " + app.exe_path);
            time.Text = UiHelpers.Format(app.seconds);
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
    readonly List<(TextBlock Label, ProgressBar Bar, TextBlock Value)> trend = new();
    readonly StackPanel top = new();

    public StatsPage()
    {
        var p = new StackPanel { Spacing = 18, Padding = new Thickness(28) };
        p.Children.Add(new TextBlock { Text = "统计", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        p.Children.Add(new TextBlock { Text = "实时统计来自后台采集器，每秒更新一次。", Opacity = .65 });

        var grid = new Grid { ColumnSpacing = 12 };
        for (int i = 0; i < 4; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        AddSummary(grid, 0, "今天", out summary[0]); AddSummary(grid, 1, "昨天", out summary[1]);
        AddSummary(grid, 2, "本周", out summary[2]); AddSummary(grid, 3, "本月", out summary[3]);
        p.Children.Add(grid);

        var trendBorder = new Border { Padding = new Thickness(20), CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var tp = new StackPanel { Spacing = 10 };
        tp.Children.Add(new TextBlock { Text = "最近 14 天", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
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
        top.Children.Add(new TextBlock { Text = "今日应用排行", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        topBorder.Child = top; p.Children.Add(topBorder);
        Content = new ScrollViewer { Content = p };
    }

    public void UpdateSnapshot(Snapshot s)
    {
        summary[0].Text = UiHelpers.Format(s.today); summary[1].Text = UiHelpers.Format(s.yesterday);
        summary[2].Text = UiHelpers.Format(s.week); summary[3].Text = UiHelpers.Format(s.month);
        var days = s.daily.TakeLast(14).ToArray();
        var max = Math.Max(1, days.Select(x => x.seconds).DefaultIfEmpty(1).Max());
        for (int i = 0; i < trend.Count; i++) {
            if (i < days.Length) {
                trend[i].Label.Text = days[i].label; trend[i].Bar.Maximum = max; trend[i].Bar.Value = days[i].seconds;
                trend[i].Value.Text = UiHelpers.Format(days[i].seconds);
            } else { trend[i].Label.Text = ""; trend[i].Bar.Maximum = max; trend[i].Bar.Value = 0; trend[i].Value.Text = ""; }
        }
        while (top.Children.Count > 1) top.Children.RemoveAt(1);
        foreach (var app in s.apps.Take(10)) {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            row.Children.Add(new TextBlock { Text = UiHelpers.FriendlyName(app), TextTrimming = TextTrimming.CharacterEllipsis });
            var value = new TextBlock { Text = UiHelpers.Format(app.seconds), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(value, 1); row.Children.Add(value); top.Children.Add(row);
        }
        if (s.apps.Length == 0) top.Children.Add(new TextBlock { Text = "今天还没有记录到应用使用时间。", Opacity = .65 });
    }

    static void AddSummary(Grid g, int col, string title, out TextBlock value)
    {
        var b = new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var p = new StackPanel { Spacing = 5 };
        p.Children.Add(new TextBlock { Text = title, Opacity = .65 });
        value = new TextBlock { Text = "00小时 00分钟", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        p.Children.Add(value); b.Child = p; Grid.SetColumn(b, col); g.Children.Add(b);
    }
}

public sealed class SettingsPage : Page
{
    readonly CheckBox startup, background;
    readonly ComboBox theme;

    public SettingsPage(bool dark)
    {
        var p = new StackPanel { Spacing = 18, Padding = new Thickness(28), MaxWidth = 720, HorizontalAlignment = HorizontalAlignment.Left };
        p.Children.Add(new TextBlock { Text = "设置", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        p.Children.Add(new TextBlock { Text = "常规", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        startup = new CheckBox { Content = "登录 Windows 后自动启动", IsChecked = StartupEnabled() };
        startup.Checked += (_, _) => SetStartup(true);
        startup.Unchecked += (_, _) => SetStartup(false);
        background = new CheckBox { Content = "后台继续记录使用时间", IsChecked = true };
        p.Children.Add(startup); p.Children.Add(background);
        p.Children.Add(new TextBlock { Text = "外观", FontSize = 20, Margin = new Thickness(0, 15, 0, 0), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        theme = new ComboBox { Width = 240 };
        theme.Items.Add("跟随系统"); theme.Items.Add("浅色"); theme.Items.Add("深色"); p.Children.Add(theme);
        p.Children.Add(new TextBlock { Text = "关于", FontSize = 20, Margin = new Thickness(0, 15, 0, 0), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        p.Children.Add(new TextBlock { Text = "ScreenTime RS\n版本 0.2.4\nRust monitoring core + WinUI 3 / Fluent UI" });
        Content = new ScrollViewer { Content = p };
        SetDark(dark);
    }

    public void SetDark(bool dark) { theme.SelectedIndex = dark ? 2 : 0; }

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
                var exe = Path.Combine(AppContext.BaseDirectory, "ScreenTimeRS.UI.exe");
                key.SetValue("ScreenTimeRS", $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue("ScreenTimeRS", false);
            }
        }
        catch { }
    }
}
