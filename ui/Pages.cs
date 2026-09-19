using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIImage = Microsoft.UI.Xaml.Controls.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;
using System.Security.Cryptography;
using UIColor = Windows.UI.Color;

namespace ScreenTimeRS.UI;

public enum LanguageMode
{
    System = 0,
    SimplifiedChinese = 1,
    TraditionalChinese = 2,
    English = 3
}

internal static class LanguageResolver
{
    public static LanguageMode Resolve(LanguageMode preference)
    {
        if (preference != LanguageMode.System)
            return preference;

        var culture = CultureInfo.CurrentUICulture;
        var name = culture.Name;

        if (name.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("zh-MO", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Hant", StringComparison.OrdinalIgnoreCase))
            return LanguageMode.TraditionalChinese;

        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return LanguageMode.SimplifiedChinese;

        return LanguageMode.English;
    }
}

internal static class UiText
{
    public static bool IsEnglish(LanguageMode language) => language == LanguageMode.English;
    public static bool IsTraditional(LanguageMode language) => language == LanguageMode.TraditionalChinese;

    public static string NavOverview(LanguageMode l) => l switch { LanguageMode.English => "Overview", LanguageMode.TraditionalChinese => "概覽", _ => "概览" };
    public static string NavApps(LanguageMode l) => l switch { LanguageMode.English => "App usage", LanguageMode.TraditionalChinese => "應用程式使用時間", _ => "应用使用时间" };
    public static string NavStats(LanguageMode l) => l switch { LanguageMode.English => "Statistics", LanguageMode.TraditionalChinese => "統計", _ => "统计" };
    public static string NavSettings(LanguageMode l) => l switch { LanguageMode.English => "Settings", LanguageMode.TraditionalChinese => "設定", _ => "设置" };

    public static string ExportSuccess(LanguageMode l) => l switch { LanguageMode.English => "Usage data exported successfully.", LanguageMode.TraditionalChinese => "使用資料已成功匯出。", _ => "使用数据已成功导出。" };
    public static string ExportFailed(LanguageMode l) => l switch { LanguageMode.English => "The usage data could not be exported.", LanguageMode.TraditionalChinese => "無法匯出使用資料。", _ => "无法导出使用数据。" };
    public static string ImportTitle(LanguageMode l) => l switch { LanguageMode.English => "Import usage data", LanguageMode.TraditionalChinese => "匯入使用資料", _ => "导入使用数据" };
    public static string ImportConfirm(LanguageMode l) => l switch { LanguageMode.English => "Importing will replace the current usage history with the selected backup. Continue?", LanguageMode.TraditionalChinese => "匯入將以所選備份取代目前的使用歷史記錄。要繼續嗎？", _ => "导入将使用所选备份替换当前使用历史记录。是否继续？" };
    public static string ImportAction(LanguageMode l) => l switch { LanguageMode.English => "Import", LanguageMode.TraditionalChinese => "匯入", _ => "导入" };
    public static string DeleteTitle(LanguageMode l) => l switch { LanguageMode.English => "Delete all usage data", LanguageMode.TraditionalChinese => "刪除所有使用資料", _ => "删除所有使用数据" };
    public static string DeleteInstruction(LanguageMode l) => l switch { LanguageMode.English => "This action permanently deletes all recorded usage data. Type the code below exactly to confirm.", LanguageMode.TraditionalChinese => "此操作將永久刪除所有已記錄的使用資料。請準確輸入下方代碼以確認。", _ => "此操作将永久删除所有已记录的使用数据。请准确输入下方验证码确认。" };
    public static string DeletePlaceholder(LanguageMode l) => l switch { LanguageMode.English => "Enter confirmation code", LanguageMode.TraditionalChinese => "輸入確認代碼", _ => "输入确认验证码" };
    public static string DeleteAction(LanguageMode l) => l switch { LanguageMode.English => "Delete all data", LanguageMode.TraditionalChinese => "刪除所有資料", _ => "删除全部数据" };
    public static string DeleteFinalTitle(LanguageMode l) => l switch { LanguageMode.English => "Final confirmation", LanguageMode.TraditionalChinese => "最終確認", _ => "最终确认" };
    public static string DeleteFinalInstruction(LanguageMode l) => l switch { LanguageMode.English => "The confirmation code is correct. All recorded usage data will be permanently deleted and cannot be recovered. Continue?", LanguageMode.TraditionalChinese => "驗證碼正確。所有已記錄的使用資料將永久刪除且無法復原。是否繼續？", _ => "验证码正确。所有已记录的使用数据将永久删除且无法恢复。是否继续？" };
    public static string DeleteSuccess(LanguageMode l) => l switch { LanguageMode.English => "All usage data has been deleted.", LanguageMode.TraditionalChinese => "所有使用資料已刪除。", _ => "所有使用数据已删除。" };
    public static string DeleteFailed(LanguageMode l) => l switch { LanguageMode.English => "The usage data could not be deleted.", LanguageMode.TraditionalChinese => "無法刪除使用資料。", _ => "无法删除使用数据。" };
    public static string ImportSuccess(LanguageMode l) => l switch { LanguageMode.English => "Usage data imported successfully.", LanguageMode.TraditionalChinese => "使用資料已成功匯入。", _ => "使用数据已成功导入。" };
    public static string ImportFailed(LanguageMode l) => l switch { LanguageMode.English => "The selected backup could not be imported.", LanguageMode.TraditionalChinese => "無法匯入所選備份。", _ => "无法导入所选备份。" };
    public static string Done(LanguageMode l) => l switch { LanguageMode.English => "Done", LanguageMode.TraditionalChinese => "完成", _ => "完成" };
    public static string Error(LanguageMode l) => l switch { LanguageMode.English => "Error", LanguageMode.TraditionalChinese => "錯誤", _ => "错误" };
    public static string Close(LanguageMode l) => l switch { LanguageMode.English => "Close", LanguageMode.TraditionalChinese => "關閉", _ => "关闭" };
    public static string Cancel(LanguageMode l) => l switch { LanguageMode.English => "Cancel", LanguageMode.TraditionalChinese => "取消", _ => "取消" };

    public static string LanguageName(LanguageMode l) => l switch { LanguageMode.System => "Follow system", LanguageMode.English => "English", LanguageMode.TraditionalChinese => "繁體中文", _ => "简体中文" };
    public static string LocalizedLanguageName(LanguageMode l, LanguageMode ui) => l switch
    {
        LanguageMode.System => ui == LanguageMode.TraditionalChinese ? "跟隨系統" : ui == LanguageMode.English ? "Follow system" : "跟随系统",
        LanguageMode.English => "English",
        LanguageMode.TraditionalChinese => "繁體中文",
        _ => "简体中文"
    };
}

internal static class AppCategoryResolver
{
    public static string Get(string app, LanguageMode language)
    {
        var n = (app ?? "").ToLowerInvariant();
        var key = n.Contains("chrome") || n.Contains("firefox") || n.Contains("edge") || n.Contains("opera") || n.Contains("brave") ? 0
            : n.Contains("code") || n.Contains("studio") || n.Contains("devenv") || n.Contains("idea") || n.Contains("rider") || n.Contains("terminal") || n.Contains("powershell") ? 1
            : n.Contains("steam") || n.Contains("epic") || n.Contains("game") || n.Contains("discord") ? 2
            : n.Contains("word") || n.Contains("excel") || n.Contains("powerpnt") || n.Contains("outlook") ? 3 : 4;
        if (language == LanguageMode.English) return key switch { 0 => "Browser", 1 => "Development", 2 => "Entertainment", 3 => "Productivity", _ => "System / Other" };
        if (language == LanguageMode.TraditionalChinese) return key switch { 0 => "瀏覽器", 1 => "開發", 2 => "娛樂", 3 => "生產力", _ => "系統 / 其他" };
        return key switch { 0 => "浏览器", 1 => "开发", 2 => "娱乐", 3 => "生产力", _ => "系统 / 其他" };
    }
}

internal static class DataConfirmationCode
{
    const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        Span<char> buffer = stackalloc char[6];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(buffer);
    }
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
    readonly TextBlock sessionValue, longestValue, unlockValue, hourlyTitle, hourlyHint;
    readonly TextBlock[] metricTitles = new TextBlock[3];
    readonly Border[] hourlyBars = new Border[24];
    readonly Border[] trendBars = new Border[30];
    readonly TextBlock[] trendLabels = new TextBlock[30];
    readonly Border hourlyHoverCard;
    readonly TextBlock hourlyHoverText;
    readonly Border trendHoverCard;
    readonly TextBlock trendHoverText;
    int _hoveredHourlyIndex = -1;
    int _hoveredTrendIndex = -1;
    LanguageMode _language = LanguageMode.SimplifiedChinese;
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
        panel.Children.Add(cards);

        var metrics = new Grid { ColumnSpacing = 14 };
        for (int i = 0; i < 3; i++) metrics.ColumnDefinitions.Add(new ColumnDefinition());
        AddMetricCard(metrics, 0, out sessionValue, out metricTitles[0], "当前连续使用");
        AddMetricCard(metrics, 1, out longestValue, out metricTitles[1], "今日最长连续使用");
        AddMetricCard(metrics, 2, out unlockValue, out metricTitles[2], "今日解锁次数");
        panel.Children.Add(metrics);

        var hourlyBorder = new Border { Padding = new Thickness(20), CornerRadius = new CornerRadius(12), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var hourlyPanel = new StackPanel { Spacing = 10 };
        hourlyTitle = new TextBlock { FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        hourlyPanel.Children.Add(hourlyTitle);
        hourlyHint = new TextBlock { FontSize = 12, Opacity = .65, TextWrapping = TextWrapping.Wrap };
        hourlyPanel.Children.Add(hourlyHint);
        var hourlyScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var hourlyHost = new Grid { Height = 150, MinWidth = 960 };
        var hourlyGrid = new Grid { Height = 150, MinWidth = 960, ColumnSpacing = 6 };
        for (int i = 0; i < 24; i++) {
            hourlyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            var bar = new Border { Width = 18, Height = 4, CornerRadius = new CornerRadius(5), Background = UiHelpers.AccentBrush(), VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Center };
            var col = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Stretch };
            var area = new Grid { Height = 115, VerticalAlignment = VerticalAlignment.Bottom };
            area.Children.Add(bar);
            col.Children.Add(area);
            var label = new TextBlock { Text = i.ToString("00"), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, Opacity = .65 };
            col.Children.Add(label);

            int hourIndex = i;
            col.PointerEntered += (_, _) => ShowHourlyHover(hourIndex);
            col.PointerMoved += (_, _) => ShowHourlyHover(hourIndex);
            col.PointerExited += (_, _) => HideHourlyHover(hourIndex);

            Grid.SetColumn(col, i);
            hourlyGrid.Children.Add(col);
            hourlyBars[i] = bar;
        }
        hourlyHost.Children.Add(hourlyGrid);

        hourlyHoverText = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.NoWrap
        };
        hourlyHoverCard = new Border
        {
            Child = hourlyHoverText,
            Padding = new Thickness(10, 7, 10, 7),
            CornerRadius = new CornerRadius(7),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        var hourlyHoverCanvas = new Canvas
        {
            Height = 150,
            MinWidth = 960,
            IsHitTestVisible = false
        };
        hourlyHoverCanvas.Children.Add(hourlyHoverCard);
        hourlyHost.Children.Add(hourlyHoverCanvas);

        hourlyScroll.Content = hourlyHost;
        hourlyPanel.Children.Add(hourlyScroll);
        hourlyBorder.Child = hourlyPanel;
        panel.Children.Add(hourlyBorder);

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

        var chartHost = new Grid { Height = 190, MinWidth = 1680 };
        var chart = new Grid { Height = 190, ColumnSpacing = 8, MinWidth = 1680 };
        var hoverCanvas = new Canvas { Height = 190, MinWidth = 1680, IsHitTestVisible = false };

        for (int i = 0; i < 30; i++)
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
                Background = UiHelpers.AccentBrush(),
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

    static void AddMetricCard(Grid grid, int column, out TextBlock value, out TextBlock titleBlock, string title)
    {
        var border = new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(10), BorderBrush = new SolidColorBrush(UIColor.FromArgb(45,128,128,128)), BorderThickness = new Thickness(1) };
        var stack = new StackPanel { Spacing = 5 };
        value = new TextBlock
        {
            FontSize = 23,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(value);
        titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 12,
            Opacity = .65,
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(titleBlock);
        border.Child = stack;
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    public void ApplyAccent(UIColor color)
    {
        var brush = new SolidColorBrush(color);
        foreach (var bar in trendBars)
            bar.Background = brush;
        foreach (var bar in hourlyBars)
            bar.Background = brush;

        // InfoBar uses dedicated severity theme resources rather than the
        // generic accent resources. Override the success-state resources
        // locally so the "Recording usage time" status follows the selected
        // application accent instead of staying green.
        var subtle = new SolidColorBrush(ColorWithAlpha(color, 32));
        status.Background = subtle;
        status.Resources["InfoBarSuccessSeverityBackgroundBrush"] = subtle;
        status.Resources["InfoBarSuccessSeverityIconForeground"] =
            new SolidColorBrush(Microsoft.UI.Colors.White);
        status.Resources["SystemFillColorSuccessBrush"] = brush;
        status.Resources["SystemFillColorSuccessBackgroundBrush"] = subtle;
        status.Resources["SystemFillColorSolidSuccessBackgroundBrush"] = brush;
    }

    static UIColor ColorWithAlpha(UIColor color, byte alpha) =>
        UIColor.FromArgb(alpha, color.R, color.G, color.B);

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        var en = language == LanguageMode.English;
        var hant = language == LanguageMode.TraditionalChinese;
        pageTitle.Text = en ? "Overview" : hant ? "概覽" : "概览";
        cardTitles[0].Text = en ? "Today" : hant ? "今天" : "今天";
        cardTitles[1].Text = en ? "Yesterday" : hant ? "昨天" : "昨天";
        cardTitles[2].Text = en ? "This week" : hant ? "本週" : "本周";
        cardTitles[3].Text = en ? "This month" : hant ? "本月" : "本月";
        cardSubs[0].Text = en ? "Active time" : hant ? "活躍時間" : "活跃时间";
        cardSubs[1].Text = en ? "Active time" : hant ? "活躍時間" : "活跃时间";
        cardSubs[2].Text = en ? "Monday to today" : hant ? "週一至今天" : "周一至今天";
        cardSubs[3].Text = en ? "Accumulated this month" : hant ? "本月累計" : "本月累计";
        metricTitles[0].Text = en ? "Current continuous use" : hant ? "目前連續使用" : "当前连续使用";
        metricTitles[1].Text = en ? "Longest continuous use today" : hant ? "今日最長連續使用" : "今日最长连续使用";
        metricTitles[2].Text = en ? "Unlocks today" : hant ? "今日解鎖次數" : "今日解锁次数";
        trendTitle.Text = en ? "Last 30 days" : hant ? "最近 30 天" : "最近 30 天";
        hourlyTitle.Text = en ? "Today by hour" : hant ? "今天每小時" : "今日按小时";
        hourlyHint.Text = "";
        UpdateSnapshot(_lastSnapshot ?? new Snapshot());
        if (_hoveredHourlyIndex >= 0)
            ShowHourlyHover(_hoveredHourlyIndex);
    }
    public void UpdateSnapshot(Snapshot s)
    {
        _lastSnapshot = s;
        values[0].Text = UiHelpers.Format(s.today, _language);
        values[1].Text = UiHelpers.Format(s.yesterday, _language);
        values[2].Text = UiHelpers.Format(s.week, _language);
        values[3].Text = UiHelpers.Format(s.month, _language);
        sessionValue.Text = UiHelpers.Format(s.current_session_seconds, _language);
        longestValue.Text = UiHelpers.Format(s.longest_session_today, _language);
        unlockValue.Text = s.unlocks_today.ToString();
        var hourlyMax = Math.Max(1, s.hourly_today.Select(x => Math.Max(0, x.seconds)).DefaultIfEmpty(0).Max());
        var hourlyRecordedTotal = s.hourly_today.Sum(x => Math.Max(0, x.seconds));
        for (int i = 0; i < hourlyBars.Length; i++) {
            var seconds = i < s.hourly_today.Length ? Math.Max(0, s.hourly_today[i].seconds) : 0;
            hourlyBars[i].Height = seconds <= 0 ? 0 : Math.Max(6, 105 * seconds / (double)hourlyMax);
        }
        var dayTotalForHint = Math.Max(0, s.today);
        var missingHourly = Math.Max(0, dayTotalForHint - hourlyRecordedTotal);
        if (missingHourly > 0)
        {
            var recordedText = UiHelpers.Format(hourlyRecordedTotal, _language);
            var totalText = UiHelpers.Format(dayTotalForHint, _language);
            var missingText = UiHelpers.Format(missingHourly, _language);
            hourlyHint.Text = _language == LanguageMode.English
                ? $"Hourly recorded {recordedText} / today's total {totalText}. {missingText} of today's total cannot be assigned to an exact hour because it comes from older daily aggregate records."
                : _language == LanguageMode.TraditionalChinese
                    ? $"每小時已記錄 {recordedText}／今日總計 {totalText}。其中 {missingText} 來自較早的每日彙總資料，無法準確分配到具體小時。"
                    : $"按小时已记录 {recordedText}／今日总计 {totalText}。其中 {missingText} 来自较早的每日汇总数据，无法准确分配到具体小时。";
        }
        else
        {
            hourlyHint.Text = _language == LanguageMode.English ? "All of today's recorded usage is represented in the hourly chart." : _language == LanguageMode.TraditionalChinese ? "今日已記錄的使用時間均已顯示在每小時圖表中。" : "今天已记录的使用时间均已显示在按小时图表中。";
        }

        var name = string.IsNullOrWhiteSpace(s.current_app) ? "—" :
            UiHelpers.FriendlyName(new AppStat { name = s.current_app }, _language);
        if (s.paused)
        {
            status.Severity = InfoBarSeverity.Warning;
            status.Title = _language == LanguageMode.English ? "Recording paused" : _language == LanguageMode.TraditionalChinese ? "已暫停記錄" : "已暂停记录";
            status.Message = _language == LanguageMode.English ? "Usage tracking is paused from Advanced settings." : _language == LanguageMode.TraditionalChinese ? "使用時間記錄已在進階設定中暫停。" : "使用时间记录已在高级设置中暂停。";
        }
        else if (s.locked)
        {
            status.Severity = InfoBarSeverity.Warning;
            status.Title = _language == LanguageMode.English ? "Currently locked" : _language == LanguageMode.TraditionalChinese ? "目前已鎖定" : "当前已锁定";
            status.Message = _language == LanguageMode.English
                ? $"Usage tracking paused    CPU {s.cpu:F1}%    Memory {s.memory_mb} MB"
                : _language == LanguageMode.TraditionalChinese
                    ? $"已暫停記錄使用時間    CPU {s.cpu:F1}%    記憶體 {s.memory_mb} MB"
                    : $"已暂停记录使用时间    CPU {s.cpu:F1}%    内存 {s.memory_mb} MB";
        }
        else if (!s.active_now)
        {
            status.Severity = InfoBarSeverity.Informational;
            status.Title = _language == LanguageMode.English ? "Inactive" : _language == LanguageMode.TraditionalChinese ? "尚未偵測到活動" : "暂未检测到活动";
            var idle = s.idle_seconds;
            status.Message = _language == LanguageMode.English
                ? $"No active input for {UiHelpers.FormatIdle(idle, _language)}    CPU {s.cpu:F1}%    Memory {s.memory_mb} MB"
                : _language == LanguageMode.TraditionalChinese
                    ? $"連續 {UiHelpers.FormatIdle(idle, _language)} 無活動輸入    CPU {s.cpu:F1}%    記憶體 {s.memory_mb} MB"
                    : $"连续 {UiHelpers.FormatIdle(idle, _language)} 无活动输入    CPU {s.cpu:F1}%    内存 {s.memory_mb} MB";
        }
        else
        {
            status.Severity = InfoBarSeverity.Success;
            status.Title = _language == LanguageMode.English ? "Recording usage time" : _language == LanguageMode.TraditionalChinese ? "正在記錄使用時間" : "正在记录使用时间";
            status.Message = _language == LanguageMode.English
                ? $"Current app: {name}    CPU {s.cpu:F1}%    Memory {s.memory_mb} MB"
                : _language == LanguageMode.TraditionalChinese
                    ? $"目前應用程式：{name}    CPU {s.cpu:F1}%    記憶體 {s.memory_mb} MB"
                    : $"当前应用：{name}    CPU {s.cpu:F1}%    内存 {s.memory_mb} MB";
        }
        if (!statusClosed) status.IsOpen = true;

        var days = s.daily.TakeLast(30).ToArray();
        var max = Math.Max(1, days.Select(x => x.seconds).DefaultIfEmpty(1).Max());
        var total = days.Sum(x => x.seconds);
        var average = days.Length == 0 ? 0 : total / days.Length;
        trendSummary.Text = _language == LanguageMode.English
            ? $"Total {UiHelpers.Format(total, _language)}  ·  Daily average {UiHelpers.Format(average, _language)}"
            : _language == LanguageMode.TraditionalChinese ? $"30 天累計 {UiHelpers.Format(total, _language)}  ·  日均 {UiHelpers.Format(average, _language)}" : $"30 天累计 {UiHelpers.Format(total, _language)}  ·  日均 {UiHelpers.Format(average, _language)}";

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

        // Re-apply the in-page overlay after each refresh so a visible popup never blinks away.
        if (_hoveredTrendIndex >= 0)
            ShowTrendHover(_hoveredTrendIndex);
    }

    void ShowHourlyHover(int index)
    {
        if (_lastSnapshot is null || index < 0 || index >= 24) return;

        _hoveredHourlyIndex = index;
        var hours = _lastSnapshot.hourly_today ?? Array.Empty<JsonDaily>();
        var seconds = index < hours.Length ? Math.Max(0, hours[index].seconds) : 0;
        var dayTotal = Math.Max(0, _lastSnapshot.today);
        var percent = dayTotal > 0 ? seconds * 100.0 / dayTotal : 0.0;

        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        var start = index.ToString("00");
        var end = ((index + 1) % 24).ToString("00");
        var duration = UiHelpers.Format(seconds, _language);

        hourlyHoverText.Text = en
            ? $"{start}:00–{end}:00\nUsage: {duration}\nToday: {percent:F1}%"
            : hant
                ? $"{start}:00–{end}:00\n使用時間：{duration}\n今日佔比：{percent:F1}%"
                : $"{start}:00–{end}:00\n使用时间：{duration}\n今日占比：{percent:F1}%";

        var dark = ActualTheme == ElementTheme.Dark;
        hourlyHoverCard.Background = new SolidColorBrush(
            dark ? UIColor.FromArgb(244, 45, 45, 45) : UIColor.FromArgb(248, 255, 255, 255));
        hourlyHoverCard.BorderBrush = new SolidColorBrush(
            dark ? UIColor.FromArgb(95, 255, 255, 255) : UIColor.FromArgb(80, 120, 120, 120));
        hourlyHoverText.Foreground = new SolidColorBrush(
            dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black);

        hourlyHoverCard.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var popupWidth = hourlyHoverCard.DesiredSize.Width;
        var step = 42.0;
        var center = index * step + 18.0;
        var left = Math.Clamp(center - popupWidth / 2.0, 4.0, 960.0 - popupWidth - 4.0);
        Canvas.SetLeft(hourlyHoverCard, left);
        Canvas.SetTop(hourlyHoverCard, 4);
        hourlyHoverCard.Visibility = Visibility.Visible;
    }

    void HideHourlyHover(int index)
    {
        if (_hoveredHourlyIndex == index)
        {
            _hoveredHourlyIndex = -1;
            hourlyHoverCard.Visibility = Visibility.Collapsed;
        }
    }

    void ShowTrendHover(int index)
    {
        if (_lastSnapshot is null || index < 0 || index >= 30) return;

        _hoveredTrendIndex = index;
        var days = _lastSnapshot.daily.TakeLast(30).ToArray();
        trendHoverText.Text = index < days.Length
            ? (_language == LanguageMode.English
                ? $"{days[index].label}: {UiHelpers.Format(days[index].seconds, _language)}"
                : $"{days[index].label}：{UiHelpers.Format(days[index].seconds, _language)}")
            : (_language == LanguageMode.English ? "No usage record" : _language == LanguageMode.TraditionalChinese ? "暫無使用記錄" : "暂无使用记录");

        trendHoverCard.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var popupWidth = trendHoverCard.DesiredSize.Width;
        var left = index * 56.0 + 24.0 - (popupWidth / 2.0);
        left = Math.Max(4, Math.Min(left, 1680 - popupWidth - 4));
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
        title = new TextBlock
        {
            Opacity = .65,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None
        };
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
        sub = new TextBlock
        {
            Opacity = .6,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None
        };
        p.Children.Add(sub);
        b.Child = p; Grid.SetColumn(b, col); g.Children.Add(b);
    }

}

public sealed class AppsPage : Page
{
    readonly StackPanel list = new();
    readonly TextBox search = new();
    readonly ComboBox period = new();
    readonly CalendarDatePicker datePicker = new();
    readonly TextBlock daySummary = new();
    readonly TextBlock emptyState = new();
    readonly TextBlock title = new();
    readonly Dictionary<string, AppRow> rows = new(StringComparer.OrdinalIgnoreCase);
    readonly Func<DateTime, Task>? _loadDayUsageAsync;
    readonly Func<AppStat, Task>? _showAppDetailsAsync;
    readonly Func<Task>? _loadAllTimeAsync;
    Snapshot snapshot = new();
    DayUsageResponse? _selectedDayData;
    AppStat[] _allTimeApps = Array.Empty<AppStat>();
    bool _allTimeLoaded;
    bool _allTimeLoading;
    bool _updatingPeriod;
    LanguageMode _language = LanguageMode.SimplifiedChinese;
    UIColor _accentColor = ThemeManager.DefaultAccent;

    public AppsPage(
        Func<DateTime, Task>? loadDayUsageAsync = null,
        Func<AppStat, Task>? showAppDetailsAsync = null,
        Func<Task>? loadAllTimeAsync = null)
    {
        _loadDayUsageAsync = loadDayUsageAsync;
        _showAppDetailsAsync = showAppDetailsAsync;
        _loadAllTimeAsync = loadAllTimeAsync;

        var root = new Grid { Padding = new Thickness(28), RowSpacing = 16 };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        title.FontSize = 30;
        title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var filters = new Grid { ColumnSpacing = 10, RowSpacing = 6 };
        filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        filters.ColumnDefinitions.Add(new ColumnDefinition());
        filters.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        filters.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        period.Items.Add("今天");
        period.Items.Add("本周");
        period.Items.Add("本月");
        period.Items.Add("近半年");
        period.Items.Add("近一年");
        period.Items.Add("全部时间");
        period.Items.Add("指定日期");
        period.SelectedIndex = 0;
        period.SelectionChanged += Period_SelectionChanged;
        ApplyComboBoxAccent(period, _accentColor);
        Grid.SetColumn(period, 0);
        Grid.SetRow(period, 0);
        filters.Children.Add(period);

        datePicker.Width = 220;
        datePicker.MaxDate = DateTimeOffset.Now.Date;
        datePicker.Date = DateTimeOffset.Now.Date;
        datePicker.DateChanged += DatePicker_DateChanged;
        Grid.SetColumn(datePicker, 1);
        Grid.SetRow(datePicker, 0);
        filters.Children.Add(datePicker);

        search.PlaceholderText = "搜索应用";
        search.TextChanged += (_, _) => RenderList();
        Grid.SetColumn(search, 1);
        Grid.SetColumnSpan(search, 2);
        Grid.SetRow(search, 0);
        filters.Children.Add(search);

        daySummary.FontSize = 12;
        daySummary.Opacity = .65;
        daySummary.TextWrapping = TextWrapping.Wrap;
        Grid.SetColumn(daySummary, 0);
        Grid.SetColumnSpan(daySummary, 3);
        Grid.SetRow(daySummary, 1);
        filters.Children.Add(daySummary);

        root.Children.Add(filters);
        Grid.SetRow(filters, 1);

        emptyState.FontSize = 15;
        emptyState.Opacity = .62;
        emptyState.TextAlignment = TextAlignment.Center;
        emptyState.VerticalAlignment = VerticalAlignment.Center;
        emptyState.Visibility = Visibility.Collapsed;
        Grid.SetRow(emptyState, 2);
        root.Children.Add(emptyState);

        var scroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(scroll, 2);
        root.Children.Add(scroll);

        Content = root;
        UpdateDatePickerState();
        UpdateDaySummary();
    }

    public int SelectedPeriodIndex => period.SelectedIndex;
    public DateTime? SelectedDay => datePicker.Date?.DateTime.Date;
    public long? SelectedDayTotal => _selectedDayData?.total_seconds;
    public long? AllTimeAppsTotal => period.SelectedIndex == 5 && _allTimeLoaded
        ? _allTimeApps.Sum(x => Math.Max(0, x.seconds))
        : null;

    async void Period_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingPeriod || period.SelectedIndex < 0) return;
        UpdateDatePickerState();
        if (period.SelectedIndex == 5)
        {
            _selectedDayData = null;
            _allTimeLoading = true;
            RenderList();
            try
            {
                if (_loadAllTimeAsync is not null)
                    await _loadAllTimeAsync();
            }
            finally
            {
                _allTimeLoading = false;
                RenderList();
            }
        }
        else if (period.SelectedIndex == 6)
        {
            await LoadSelectedDayAsync();
        }
        else
        {
            _selectedDayData = null;
        }
        UpdateDaySummary();
        RenderList();
    }

    async void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (_updatingPeriod || !args.NewDate.HasValue) return;
        _updatingPeriod = true;
        period.SelectedIndex = 6;
        _updatingPeriod = false;
        _selectedDayData = null;
        await LoadSelectedDayAsync();
        UpdateDatePickerState();
        UpdateDaySummary();
        RenderList();
    }

    async Task LoadSelectedDayAsync()
    {
        var selected = datePicker.Date?.DateTime.Date;
        if (!selected.HasValue)
        {
            _selectedDayData = null;
            return;
        }
        if (_loadDayUsageAsync is not null)
            await _loadDayUsageAsync(selected.Value);
    }

    public void SetSelectedDay(DayUsageResponse data)
    {
        if (!DateTime.TryParseExact(data.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return;
        if (datePicker.Date?.DateTime.Date != parsed.Date) return;
        _selectedDayData = data;
        if (period.SelectedIndex == 6)
        {
            UpdateDaySummary();
            RenderList();
        }
    }

    public void SetAllTimeData(RangeUsageResponse data)
    {
        _allTimeApps = data?.apps ?? Array.Empty<AppStat>();
        _allTimeLoaded = true;
        snapshot.apps_all = _allTimeApps;
        if (period.SelectedIndex == 5)
            RenderList();
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        var en = language == LanguageMode.English;
        var hant = language == LanguageMode.TraditionalChinese;
        title.Text = en ? "App usage" : hant ? "應用程式使用時間" : "应用使用时间";
        search.PlaceholderText = en ? "Search apps" : hant ? "搜尋應用程式" : "搜索应用";
        var index = Math.Clamp(period.SelectedIndex < 0 ? 0 : period.SelectedIndex, 0, 6);
        _updatingPeriod = true;
        period.Items.Clear();
        var items = en
            ? new[] { "Today", "This week", "This month", "Last 6 months", "Last year", "All time", "Selected day" }
            : hant
                ? new[] { "今天", "本週", "本月", "近半年", "近一年", "全部時間", "指定日期" }
                : new[] { "今天", "本周", "本月", "近半年", "近一年", "全部时间", "指定日期" };
        foreach (var item in items) period.Items.Add(item);
        period.SelectedIndex = index;
        _updatingPeriod = false;
        UpdateDatePickerState();
        UpdateDaySummary();
        RenderList();
    }

    public void ApplyAccent(UIColor color)
    {
        _accentColor = color;
        ApplyComboBoxAccent(period, color);
        foreach (var row in rows.Values)
            row.ApplyAccent(color);
    }

    public void UpdateSnapshot(Snapshot s)
    {
        snapshot = s;
        // analytics.json does not contain the on-demand all-time range.
        // Preserve the separately loaded all-time data across the 1-second snapshot refresh loop.
        snapshot.apps_all = _allTimeApps;
        RenderList();
    }

    void UpdateDatePickerState()
    {
        var selectedDayMode = period.SelectedIndex == 6;

        datePicker.Visibility = selectedDayMode ? Visibility.Visible : Visibility.Collapsed;
        datePicker.IsEnabled = selectedDayMode;

        if (selectedDayMode)
        {
            Grid.SetColumn(datePicker, 1);
            Grid.SetColumnSpan(datePicker, 1);
            Grid.SetColumn(search, 2);
            Grid.SetColumnSpan(search, 1);
        }
        else
        {
            // For all range presets, keep the search box in the date-picker
            // position so the filter bar stays compact and consistent.
            Grid.SetColumn(search, 1);
            Grid.SetColumnSpan(search, 2);
        }
    }

    void UpdateDaySummary()
    {
        if (period.SelectedIndex != 6)
        {
            daySummary.Text = "";
            return;
        }
        var selected = SelectedDay;
        if (!selected.HasValue)
        {
            daySummary.Text = _language == LanguageMode.English ? "Select a date to view recorded apps." : _language == LanguageMode.TraditionalChinese ? "選擇日期以查看已記錄的應用程式。" : "选择日期以查看已记录的应用。";
            return;
        }
        var total = _selectedDayData?.total_seconds ?? 0;
        var dateText = selected.Value.ToString(_language == LanguageMode.English ? "MMM d, yyyy" : "yyyy年M月d日");
        daySummary.Text = _language == LanguageMode.English
            ? $"{dateText} · {UiHelpers.Format(total, _language)} recorded"
            : _language == LanguageMode.TraditionalChinese
                ? $"{dateText} · 已記錄 {UiHelpers.Format(total, _language)}"
                : $"{dateText} · 已记录 {UiHelpers.Format(total, _language)}";
    }

    void RenderList()
    {
        AppStat[] source;
        long denominator;
        switch (period.SelectedIndex)
        {
            case 1: source = snapshot.apps_week; denominator = source.Sum(x => Math.Max(0, x.seconds)); break;
            case 2: source = snapshot.apps_month; denominator = source.Sum(x => Math.Max(0, x.seconds)); break;
            case 3: source = snapshot.apps_half_year; denominator = source.Sum(x => Math.Max(0, x.seconds)); break;
            case 4: source = snapshot.apps_year; denominator = source.Sum(x => Math.Max(0, x.seconds)); break;
            case 5: source = _allTimeApps; denominator = source.Sum(x => Math.Max(0, x.seconds)); break;
            case 6: source = _selectedDayData?.apps ?? Array.Empty<AppStat>(); denominator = _selectedDayData?.total_seconds ?? source.Sum(x => Math.Max(0, x.seconds)); break;
            default: source = snapshot.apps; denominator = source.Sum(x => Math.Max(0, x.seconds)); break;
        }

        var apps = source
            .Where(x => UiHelpers.FriendlyName(x, _language).Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var wanted = new HashSet<string>(apps.Select(Key), StringComparer.OrdinalIgnoreCase);

        foreach (var key in rows.Keys.Where(k => !wanted.Contains(k)).ToArray())
        {
            list.Children.Remove(rows[key].Border);
            rows.Remove(key);
        }

        foreach (var app in apps)
        {
            var key = Key(app);
            if (!rows.TryGetValue(key, out var row))
            {
                row = new AppRow(app, _language, _accentColor, _showAppDetailsAsync);
                rows[key] = row;
            }
            row.ApplyAccent(_accentColor);
            row.Update(app, denominator, _language);
        }

        var ordered = apps.Select(a => rows[Key(a)].Border).ToArray();
        bool same = ordered.Length == list.Children.Count &&
            ordered.Select((x, i) => ReferenceEquals(x, list.Children[i])).All(x => x);
        if (!same)
        {
            list.Children.Clear();
            foreach (var item in ordered) list.Children.Add(item);
        }

        bool isAllTime = period.SelectedIndex == 5;
        if (isAllTime && _allTimeLoading)
        {
            emptyState.Text = _language == LanguageMode.English
                ? "Loading all-time usage…"
                : _language == LanguageMode.TraditionalChinese
                    ? "正在載入全部時間使用資料…"
                    : "正在加载全部时间使用数据…";
            emptyState.Visibility = Visibility.Visible;
            list.Visibility = Visibility.Collapsed;
        }
        else if (isAllTime && !_allTimeLoaded)
        {
            emptyState.Text = _language == LanguageMode.English
                ? "Loading all-time usage…"
                : _language == LanguageMode.TraditionalChinese
                    ? "正在載入全部時間使用資料…"
                    : "正在加载全部时间使用数据…";
            emptyState.Visibility = Visibility.Visible;
            list.Visibility = Visibility.Collapsed;
        }
        else if (apps.Length == 0)
        {
            emptyState.Text = _language == LanguageMode.English
                ? "No recorded app usage for this period."
                : _language == LanguageMode.TraditionalChinese
                    ? "此期間沒有已記錄的應用程式使用資料。"
                    : "此时间段没有已记录的应用使用数据。";
            emptyState.Visibility = Visibility.Visible;
            list.Visibility = Visibility.Collapsed;
        }
        else
        {
            emptyState.Visibility = Visibility.Collapsed;
            list.Visibility = Visibility.Visible;
        }
    }

    static void ApplyComboBoxAccent(ComboBox combo, UIColor color)
    {
        var accent = new SolidColorBrush(color);
        combo.Resources["ComboBoxItemPillFillBrush"] = accent;
        combo.Resources["ComboBoxItemBackgroundSelected"] = new SolidColorBrush(UIColor.FromArgb(40, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemBackgroundSelectedUnfocused"] = new SolidColorBrush(UIColor.FromArgb(34, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemBackgroundSelectedPointerOver"] = new SolidColorBrush(UIColor.FromArgb(56, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemBackgroundSelectedPressed"] = new SolidColorBrush(UIColor.FromArgb(72, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemForegroundSelected"] = accent;
        combo.Resources["ComboBoxItemForegroundSelectedPointerOver"] = accent;
        combo.Resources["ComboBoxSelectedBackground"] = new SolidColorBrush(UIColor.FromArgb(40, color.R, color.G, color.B));
        combo.Resources["ComboBoxSelectedPointerOverBackground"] = new SolidColorBrush(UIColor.FromArgb(56, color.R, color.G, color.B));
    }

    static string Key(AppStat a) => string.IsNullOrWhiteSpace(a.exe_path) ? a.name : a.exe_path;

    sealed class AppRow
    {
        public Border Border { get; }
        readonly TextBlock name, path, time;
        readonly ProgressBar bar;
        readonly WinUIImage icon;
        readonly Func<AppStat, Task>? onDetails;
        string iconKey = "";
        AppStat _app = new();
        static readonly Dictionary<string, BitmapImage> IconCache = new(StringComparer.OrdinalIgnoreCase);

        public AppRow(AppStat app, LanguageMode language, UIColor accentColor, Func<AppStat, Task>? onDetails)
        {
            this.onDetails = onDetails;
            var grid = new Grid { Padding = new Thickness(10), ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

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

            Border = new Border
            {
                Child = grid,
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 0, 1),
                IsHitTestVisible = true
            };
            Border.Tapped += async (_, _) =>
            {
                if (this.onDetails is not null)
                    await this.onDetails(_app);
            };
            Update(app, 0, language);
        }

        public void Update(AppStat app, long total, LanguageMode language)
        {
            _app = app;
            name.Text = UiHelpers.FriendlyName(app, language);
            path.Text = AppCategoryResolver.Get(app.name, language) + " · " + app.name + (string.IsNullOrWhiteSpace(app.exe_path) ? "" : " · " + app.exe_path);
            time.Text = UiHelpers.Format(app.seconds, language);
            bar.Value = Math.Min(100, app.seconds * 100.0 / Math.Max(1, total));
            if (!string.Equals(iconKey, app.exe_path, StringComparison.OrdinalIgnoreCase))
            {
                iconKey = app.exe_path;
                SetIcon(app);
            }
        }

        public void ApplyAccent(UIColor color)
        {
            var accent = new SolidColorBrush(color);
            var track = new SolidColorBrush(UIColor.FromArgb(40, color.R, color.G, color.B));
            bar.Foreground = accent;
            bar.Background = track;
            bar.Resources["ProgressBarIndicatorForeground"] = accent;
            bar.Resources["ProgressBarIndicatorForegroundPointerOver"] = accent;
            bar.Resources["ProgressBarTrackFill"] = track;
        }

        void SetIcon(AppStat app)
        {
            try
            {
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
            }
            catch { }
        }
    }
}

public sealed class StatsPage : Page
{
    readonly TextBlock[] summary = new TextBlock[4];
    readonly TextBlock[] summaryTitles = new TextBlock[4];
    readonly List<TrendColumn> trend = new();
    readonly List<ColumnDefinition> trendColumns = new();
    readonly Border trendHoverCard;
    readonly TextBlock trendHoverText;
    readonly StackPanel top = new();
    readonly List<TopAppRow> topRows = new();
    readonly TextBlock topEmpty = new();
    readonly TextBlock title = new();
    readonly TextBlock description = new();
    readonly TextBlock trendTitle = new();
    readonly TextBlock topTitle = new();
    readonly ComboBox period = new();
    readonly TextBlock periodHint = new();
    readonly Func<int, Task>? _loadPeriodAsync;
    RangeUsageResponse? _allTimeData;
    LanguageMode _language = LanguageMode.SimplifiedChinese;
    Snapshot? _lastSnapshot;
    UIColor _accentColor = ThemeManager.DefaultAccent;
    bool _updatingPeriod;
    const double TrendColumnWidth = 60.0;

    public StatsPage(Func<int, Task>? loadPeriodAsync = null)
    {
        _loadPeriodAsync = loadPeriodAsync;
        var root = new Grid { Padding = new Thickness(28), RowSpacing = 14 };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        title.FontSize = 30;
        title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        description.Opacity = .65;
        description.TextWrapping = TextWrapping.Wrap;
        Grid.SetRow(description, 1);
        root.Children.Add(description);

        var filterRow = new Grid { ColumnSpacing = 12 };
        filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        filterRow.ColumnDefinitions.Add(new ColumnDefinition());
        period.Width = 220;
        period.Items.Add("最近 30 天");
        period.Items.Add("最近 90 天");
        period.Items.Add("近半年");
        period.Items.Add("近一年");
        period.Items.Add("全部时间");
        period.SelectedIndex = 0;
        period.SelectionChanged += Period_SelectionChanged;
        Grid.SetColumn(period, 0);
        filterRow.Children.Add(period);
        periodHint.VerticalAlignment = VerticalAlignment.Center;
        periodHint.Opacity = .62;
        Grid.SetColumn(periodHint, 1);
        filterRow.Children.Add(periodHint);
        Grid.SetRow(filterRow, 2);
        root.Children.Add(filterRow);

        var summaryGrid = new Grid { ColumnSpacing = 12 };
        for (int i = 0; i < 4; i++) summaryGrid.ColumnDefinitions.Add(new ColumnDefinition());
        AddSummary(summaryGrid, 0, out summary[0], out summaryTitles[0]);
        AddSummary(summaryGrid, 1, out summary[1], out summaryTitles[1]);
        AddSummary(summaryGrid, 2, out summary[2], out summaryTitles[2]);
        AddSummary(summaryGrid, 3, out summary[3], out summaryTitles[3]);
        root.Children.Add(summaryGrid);
        Grid.SetRow(summaryGrid, 3);

        var content = new StackPanel { Spacing = 14, Margin = new Thickness(0, 14, 0, 0) };
        var trendBorder = new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1)
        };
        var trendPanel = new StackPanel { Spacing = 8 };
        trendTitle.FontSize = 20;
        trendTitle.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        trendPanel.Children.Add(trendTitle);

        var chartScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollMode = ScrollMode.Disabled
        };

        const int maxTrendColumns = 365;
        var chartHost = new Grid { Height = 250, MinWidth = 420 };
        var chart = new Grid { Height = 250, ColumnSpacing = 0, MinWidth = 420 };
        var hoverCanvas = new Canvas { Height = 250, MinWidth = 420, IsHitTestVisible = false };
        for (int i = 0; i < maxTrendColumns; i++)
        {
            var column = new ColumnDefinition { Width = new GridLength(0) };
            trendColumns.Add(column);
            chart.ColumnDefinitions.Add(column);
        }

        for (int i = 0; i < maxTrendColumns; i++)
        {
            var columnDay = new Grid { VerticalAlignment = VerticalAlignment.Stretch, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
            columnDay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(222) });
            columnDay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

            var barArea = new Grid { Margin = new Thickness(5, 0, 5, 0), VerticalAlignment = VerticalAlignment.Bottom };
            var baseline = new Border
            {
                Height = 1,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(UIColor.FromArgb(45, _accentColor.R, _accentColor.G, _accentColor.B))
            };
            barArea.Children.Add(baseline);
            var bar = new Border
            {
                Width = 24,
                Height = 4,
                CornerRadius = new CornerRadius(7),
                Background = UiHelpers.AccentBrush(),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = .92
            };
            barArea.Children.Add(bar);
            Grid.SetRow(barArea, 0);
            columnDay.Children.Add(barArea);

            var label = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                Opacity = .7
            };
            Grid.SetRow(label, 1);
            columnDay.Children.Add(label);

            int dayIndex = i;
            columnDay.PointerEntered += (_, _) => ShowTrendHover(dayIndex);
            columnDay.PointerMoved += (_, _) => ShowTrendHover(dayIndex);
            columnDay.PointerExited += (_, _) => HideTrendHover(dayIndex);
            Grid.SetColumn(columnDay, i);
            chart.Children.Add(columnDay);
            trend.Add(new TrendColumn(label, bar, baseline));
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

        var topBorder = new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1)
        };
        top.Spacing = 9;
        topTitle.FontSize = 20;
        topTitle.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        top.Children.Add(topTitle);
        topEmpty.Opacity = .65;
        top.Children.Add(topEmpty);
        topBorder.Child = top;

        content.Children.Add(trendBorder);
        content.Children.Add(topBorder);
        var contentScroll = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(contentScroll, 4);
        root.Children.Add(contentScroll);
        Content = root;
    }

    async void Period_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingPeriod || period.SelectedIndex < 0) return;
        if (period.SelectedIndex == 4 && _loadPeriodAsync is not null)
            await _loadPeriodAsync(4);
        else if (period.SelectedIndex != 4)
            _allTimeData = null;
        RenderCurrentSnapshot();
    }

    public long? AllTimeTotal => _allTimeData?.total_seconds;

    public void SetAllTimeData(RangeUsageResponse? data)
    {
        _allTimeData = data;
        if (period.SelectedIndex == 4)
            RenderCurrentSnapshot();
    }

    public void ApplyAccent(UIColor color)
    {
        _accentColor = color;
        ApplyComboBoxAccent(period, color);
        var accent = new SolidColorBrush(color);
        foreach (var item in trend)
        {
            item.Bar.Background = accent;
            item.Baseline.Background = new SolidColorBrush(UIColor.FromArgb(45, color.R, color.G, color.B));
        }
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        var en = language == LanguageMode.English;
        var hant = language == LanguageMode.TraditionalChinese;
        title.Text = en ? "Statistics" : hant ? "統計" : "统计";
        description.Text = en
            ? "Review screen-time trends and application usage from the local history."
            : hant ? "查看本機歷史記錄中的使用時間趨勢與應用程式使用情況。"
            : "查看本机历史记录中的使用时长趋势与应用使用情况。";
        periodHint.Text = en
            ? "Percentages show each app's share of the selected period."
            : hant ? "百分比表示各應用程式在所選期間的使用時間佔比。"
            : "百分比表示各应用在所选时间段中的使用时间占比。";
        var selectedPeriod = Math.Clamp(period.SelectedIndex < 0 ? 0 : period.SelectedIndex, 0, 4);
        _updatingPeriod = true;
        period.Items.Clear();
        if (en)
        {
            period.Items.Add("Last 30 days");
            period.Items.Add("Last 90 days");
            period.Items.Add("Last 6 months");
            period.Items.Add("Last year");
            period.Items.Add("All time");
        }
        else if (hant)
        {
            period.Items.Add("最近 30 天");
            period.Items.Add("最近 90 天");
            period.Items.Add("近半年");
            period.Items.Add("近一年");
            period.Items.Add("全部時間");
        }
        else
        {
            period.Items.Add("最近 30 天");
            period.Items.Add("最近 90 天");
            period.Items.Add("近半年");
            period.Items.Add("近一年");
            period.Items.Add("全部时间");
        }
        period.SelectedIndex = selectedPeriod;
        _updatingPeriod = false;
        trendHoverCard.Visibility = Visibility.Collapsed;
        trendTitle.Text = en ? "Daily usage trend" : hant ? "每日使用趨勢" : "每日使用趋势";
        topTitle.Text = en ? "Top apps · usage share" : hant ? "應用程式排行 · 使用時間佔比" : "应用排行 · 使用时间占比";
        summaryTitles[0].Text = en ? "Period total" : hant ? "期間總計" : "时间段总计";
        summaryTitles[1].Text = en ? "Daily average" : hant ? "日均使用" : "日均使用";
        summaryTitles[2].Text = en ? "Active days" : hant ? "活躍天數" : "活跃天数";
        summaryTitles[3].Text = en ? "Peak day" : hant ? "最高單日" : "最高单日";
        RenderCurrentSnapshot();
    }

    public void UpdateSnapshot(Snapshot s)
    {
        _lastSnapshot = s;
        RenderCurrentSnapshot();
    }

    void RenderCurrentSnapshot()
    {
        if (_lastSnapshot is null) return;

        var rawDays = GetSelectedDailyData();
        var total = period.SelectedIndex == 4 && _allTimeData is not null
            ? _allTimeData.total_seconds
            : rawDays.Sum(x => Math.Max(0, x.seconds));
        var average = rawDays.Length == 0 ? 0 : (long)Math.Round(total / (double)rawDays.Length);
        var activeDays = rawDays.Count(x => x.seconds > 0);
        var peak = rawDays.OrderByDescending(x => x.seconds).FirstOrDefault();

        summary[0].Text = UiHelpers.Format(total, _language);
        summary[1].Text = UiHelpers.Format(average, _language);
        summary[2].Text = activeDays.ToString();
        summary[3].Text = peak is null ? "—" : $"{peak.label}\n{UiHelpers.Format(peak.seconds, _language)}";

        var displayDays = BuildTrendPoints(rawDays);
        for (int i = 0; i < trendColumns.Count; i++)
            trendColumns[i].Width = new GridLength(i < displayDays.Length ? TrendColumnWidth : 0);

        var max = Math.Max(1, displayDays.Select(x => x.seconds).DefaultIfEmpty(1).Max());
        const double chartHeight = 222;
        for (int i = 0; i < trend.Count; i++)
        {
            if (i < displayDays.Length)
            {
                var item = displayDays[i];
                trend[i].Label.Text = FormatTrendLabel(item.label);
                trend[i].Bar.Height = item.seconds <= 0 ? 3 : Math.Max(6, chartHeight * item.seconds / max);
                trend[i].Label.Visibility = Visibility.Visible;
                trend[i].Bar.Visibility = Visibility.Visible;
                trend[i].Baseline.Visibility = Visibility.Visible;
            }
            else
            {
                trend[i].Label.Text = "";
                trend[i].Bar.Height = 3;
                trend[i].Label.Visibility = Visibility.Collapsed;
                trend[i].Bar.Visibility = Visibility.Collapsed;
                trend[i].Baseline.Visibility = Visibility.Collapsed;
            }
        }

        var apps = period.SelectedIndex switch
        {
            0 => _lastSnapshot.apps_30_days,
            1 => _lastSnapshot.apps_90_days,
            2 => _lastSnapshot.apps_half_year,
            3 => _lastSnapshot.apps_year,
            4 => _allTimeData?.apps ?? Array.Empty<AppStat>(),
            _ => _lastSnapshot.apps_30_days
        };
        var topApps = apps.Take(10).ToArray();
        topEmpty.Text = _language == LanguageMode.English ? "No app usage recorded in this period." : _language == LanguageMode.TraditionalChinese ? "此期間沒有應用程式使用記錄。" : "该时间段暂无应用使用记录。";
        topEmpty.Visibility = topApps.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

        while (topRows.Count < topApps.Length)
        {
            var row = new TopAppRow();
            topRows.Add(row);
            top.Children.Add(row.Root);
        }
        while (topRows.Count > topApps.Length)
        {
            var last = topRows[^1];
            top.Children.Remove(last.Root);
            topRows.RemoveAt(topRows.Count - 1);
        }

        var denominator = Math.Max(1, total);
        for (int i = 0; i < topApps.Length; i++)
        {
            var app = topApps[i];
            var row = topRows[i];
            row.Name.Text = UiHelpers.FriendlyName(app, _language);
            var share = app.seconds * 100.0 / denominator;
            row.Value.Text = $"{UiHelpers.Format(app.seconds, _language)}  ·  {share:F1}%";
            UiHelpers.SetAppIcon(row.Icon, app);
        }
    }

    JsonDaily[] GetSelectedDailyData()
    {
        if (_lastSnapshot is null) return Array.Empty<JsonDaily>();
        return period.SelectedIndex switch
        {
            1 => _lastSnapshot.daily_year.TakeLast(Math.Min(90, _lastSnapshot.daily_year.Length)).ToArray(),
            2 => _lastSnapshot.daily_year.TakeLast(Math.Min(183, _lastSnapshot.daily_year.Length)).ToArray(),
            3 => _lastSnapshot.daily_year.TakeLast(Math.Min(365, _lastSnapshot.daily_year.Length)).ToArray(),
            4 => _allTimeData?.daily ?? Array.Empty<JsonDaily>(),
            _ => _lastSnapshot.daily.TakeLast(Math.Min(30, _lastSnapshot.daily.Length)).ToArray()
        };
    }

    (string label, long seconds)[] BuildTrendPoints(JsonDaily[] raw)
    {
        if (raw.Length <= 365)
            return raw.Select(x => (x.label, x.seconds)).ToArray();
        return raw
            .GroupBy(x => x.label.Length >= 7 ? x.label[..7] : x.label)
            .Select(g => (g.Key, g.Sum(x => Math.Max(0, x.seconds))))
            .ToArray();
    }

    static string FormatTrendLabel(string raw)
    {
        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var day))
            return day.ToString("MM-dd");
        if (DateTime.TryParseExact(raw, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var month))
            return month.ToString("yy-MM");
        return raw;
    }

    void ShowTrendHover(int index)
    {
        if (_lastSnapshot is null || index < 0 || index >= trend.Count) return;
        var points = BuildTrendPoints(GetSelectedDailyData());
        if (index >= points.Length)
        {
            trendHoverCard.Visibility = Visibility.Collapsed;
            return;
        }
        var item = points[index];
        trendHoverText.Text = _language == LanguageMode.English
            ? $"{item.label}: {UiHelpers.Format(item.seconds, _language)}"
            : $"{item.label}：{UiHelpers.Format(item.seconds, _language)}";
        trendHoverCard.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var popupWidth = trendHoverCard.DesiredSize.Width;
        var left = index * TrendColumnWidth + (TrendColumnWidth / 2.0) - (popupWidth / 2.0);
        var chartWidth = Math.Max(420, points.Length * TrendColumnWidth);
        left = Math.Max(4, Math.Min(left, chartWidth - popupWidth - 4));
        Canvas.SetLeft(trendHoverCard, left);
        Canvas.SetTop(trendHoverCard, 6);
        trendHoverCard.Visibility = Visibility.Visible;
    }

    void HideTrendHover(int index) => trendHoverCard.Visibility = Visibility.Collapsed;

    sealed class TrendColumn
    {
        public TextBlock Label { get; }
        public Border Bar { get; }
        public Border Baseline { get; }
        public TrendColumn(TextBlock label, Border bar, Border baseline) { Label = label; Bar = bar; Baseline = baseline; }
    }

    sealed class TopAppRow
    {
        public Grid Root { get; }
        public WinUIImage Icon { get; }
        public TextBlock Name { get; }
        public TextBlock Value { get; }
        public TopAppRow()
        {
            Root = new Grid { ColumnSpacing = 12, Height = 36 };
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            Root.ColumnDefinitions.Add(new ColumnDefinition());
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            Icon = new WinUIImage { Width = 28, Height = 28, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center };
            Root.Children.Add(Icon);
            Name = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(Name, 1); Root.Children.Add(Name);
            Value = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(Value, 2); Root.Children.Add(Value);
        }
    }

    static void AddSummary(Grid g, int col, out TextBlock value, out TextBlock title)
    {
        var b = new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1)
        };
        var p = new StackPanel { Spacing = 5 };
        title = new TextBlock { Opacity = .65, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.None };
        p.Children.Add(title);
        value = new TextBlock { Text = "—", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, MaxLines = 2 };
        p.Children.Add(value);
        b.Child = p;
        Grid.SetColumn(b, col);
        g.Children.Add(b);
    }

    static void ApplyComboBoxAccent(ComboBox combo, UIColor color)
    {
        var accent = new SolidColorBrush(color);
        combo.Resources["ComboBoxItemPillFillBrush"] = accent;
        combo.Resources["ComboBoxItemBackgroundSelected"] = new SolidColorBrush(UIColor.FromArgb(40, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemBackgroundSelectedUnfocused"] = new SolidColorBrush(UIColor.FromArgb(34, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemBackgroundSelectedPointerOver"] = new SolidColorBrush(UIColor.FromArgb(56, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemBackgroundSelectedPressed"] = new SolidColorBrush(UIColor.FromArgb(72, color.R, color.G, color.B));
        combo.Resources["ComboBoxItemForegroundSelected"] = accent;
        combo.Resources["ComboBoxItemForegroundSelectedPointerOver"] = accent;
        combo.Resources["ComboBoxSelectedBackground"] = new SolidColorBrush(UIColor.FromArgb(40, color.R, color.G, color.B));
        combo.Resources["ComboBoxSelectedPointerOverBackground"] = new SolidColorBrush(UIColor.FromArgb(56, color.R, color.G, color.B));
    }
}

public sealed class SettingsPage : Page
{
    readonly Border startupBox;
    readonly Grid startupToggle;
    bool startupEnabled;
    readonly ComboBox theme, language;
    ColorPicker colorPicker = null!;
    readonly TextBlock pageTitle, generalTitle, appearanceTitle, startupText, languageTitle, aboutText;
    readonly TextBlock dataSectionTitle, legalSectionTitle, advancedTitle, aboutTitle;
    readonly TextBlock themeModeLabel, languageModeLabel, legalHint, dataCardTitle;
    readonly TextBlock paletteTitle, customColorTitle, accentHint, dataHint;
    readonly TextBlock dataLocationTitle, dataLocationText, dataLocationHint;
    readonly Button exportDataButton, importDataButton, deleteAllDataButton, openDataFolderButton, pauseButton, backupButton, diagnosticsButton;
    readonly Expander themeColorsExpander;
    readonly Border dataCard, legalCard, advancedCard;
    readonly StackPanel themeContent;
    readonly Grid colorPickerHost;
    readonly Border colorSpectrumInputSurface;
    readonly Border colorSpectrumTooltip;
    readonly TextBlock colorSpectrumTooltipText;
    readonly Button termsButton;
    readonly Button[] paletteButtons = new Button[7];
    ColorSpectrum? colorSpectrum;
    bool colorSpectrumPointerDown;
    bool updatingTheme;
    bool updatingLanguage;
    bool updatingColor;
    LanguageMode _language;
    LanguageMode _languagePreference;
    UIColor _accentColor;
    bool _monitorPaused;

    public event EventHandler<ThemeMode>? ThemeModeChanged;
    public event EventHandler<LanguageMode>? LanguageChanged;
    public event EventHandler<UIColor>? AccentColorChanged;
    public event EventHandler? ExportDataRequested;
    public event EventHandler? ImportDataRequested;
    public event EventHandler? DeleteAllDataRequested;
    public event EventHandler? PauseRequested;
    public event EventHandler? BackupRequested;
    public event EventHandler? DiagnosticsRequested;

    public SettingsPage(ThemeMode mode, LanguageMode languageMode, UIColor accentColor)
    {
        _languagePreference = languageMode;
        _language = LanguageResolver.Resolve(languageMode);
        _accentColor = accentColor;

        var p = new StackPanel
        {
            Spacing = 24,
            Padding = new Thickness(28),
            MaxWidth = 860,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        pageTitle = new TextBlock
        {
            FontSize = 30,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 2)
        };
        p.Children.Add(pageTitle);

        // General
        generalTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 0)
        };
        p.Children.Add(generalTitle);

        startupEnabled = StartupEnabled();
        startupText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        startupBox = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1.5),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        startupToggle = new Grid
        {
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(14)
        };
        startupToggle.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        startupToggle.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(startupBox, 0);
        Grid.SetColumn(startupText, 1);
        startupToggle.Children.Add(startupBox);
        startupToggle.Children.Add(startupText);
        startupToggle.PointerPressed += (_, _) => ToggleStartup();
        p.Children.Add(CreateSettingsCard(startupToggle));
        ApplyStartupAccent(_accentColor);

        // Appearance
        appearanceTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        p.Children.Add(appearanceTitle);

        themeModeLabel = new TextBlock
        {
            FontSize = 13,
            Opacity = .65,
            Margin = new Thickness(2, 0, 0, 0)
        };
        p.Children.Add(themeModeLabel);
        theme = new ComboBox { Width = 260 };
        theme.SelectionChanged += Theme_SelectionChanged;
        ApplyComboBoxAccent(theme, _accentColor);
        p.Children.Add(theme);

        paletteTitle = new TextBlock { FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };

        var palette = new Grid { ColumnSpacing = 10 };
        for (int i = 0; i < paletteButtons.Length; i++)
        {
            palette.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) });
            var index = i;
            var button = new Button
            {
                Width = 52,
                Height = 52,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(26),
                BorderThickness = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            button.Click += (_, _) => SelectPreset(index);
            Grid.SetColumn(button, i);
            palette.Children.Add(button);
            paletteButtons[i] = button;
        }

        customColorTitle = new TextBlock
        {
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 0)
        };
        accentHint = new TextBlock { FontSize = 12, Opacity = .65, TextWrapping = TextWrapping.Wrap };

        themeContent = new StackPanel { Spacing = 8, Padding = new Thickness(14, 4, 14, 14) };

        colorPickerHost = new Grid();
        colorSpectrumInputSurface = new Border
        {
            Background = new SolidColorBrush(UIColor.FromArgb(1, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 0,
            Height = 0
        };
        colorSpectrumInputSurface.PointerEntered += ColorSpectrumInput_PointerEntered;
        colorSpectrumInputSurface.PointerMoved += ColorSpectrumInput_PointerMoved;
        colorSpectrumInputSurface.PointerPressed += ColorSpectrumInput_PointerPressed;
        colorSpectrumInputSurface.PointerReleased += ColorSpectrumInput_PointerReleased;
        colorSpectrumInputSurface.PointerExited += ColorSpectrumInput_PointerExited;
        colorSpectrumInputSurface.PointerCaptureLost += ColorSpectrumInput_PointerCaptureLost;

        colorSpectrumTooltipText = new TextBlock
        {
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        colorSpectrumTooltip = new Border
        {
            Background = new SolidColorBrush(UIColor.FromArgb(248, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(UIColor.FromArgb(80, 120, 120, 120)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 9, 16, 9),
            MinWidth = 78,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = colorSpectrumTooltipText,
            RenderTransform = new TranslateTransform()
        };

        colorPickerHost.Children.Add(colorSpectrumInputSurface);
        colorPickerHost.Children.Add(colorSpectrumTooltip);
        CreateColorPicker();

        themeContent.Children.Add(paletteTitle);
        themeContent.Children.Add(palette);
        themeContent.Children.Add(customColorTitle);
        themeContent.Children.Add(accentHint);
        themeContent.Children.Add(colorPickerHost);

        themeColorsExpander = new Expander
        {
            IsExpanded = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            Content = themeContent
        };
        p.Children.Add(CreateSettingsCard(themeColorsExpander));

        // Language
        languageTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        p.Children.Add(languageTitle);
        languageModeLabel = new TextBlock
        {
            FontSize = 13,
            Opacity = .65,
            Margin = new Thickness(2, 0, 0, 0)
        };
        p.Children.Add(languageModeLabel);
        language = new ComboBox { Width = 260 };
        language.SelectionChanged += Language_SelectionChanged;
        ApplyComboBoxAccent(language, _accentColor);
        p.Children.Add(language);

        // Data & Privacy
        dataSectionTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        p.Children.Add(dataSectionTitle);

        dataHint = new TextBlock { FontSize = 12, Opacity = .65, TextWrapping = TextWrapping.Wrap };
        exportDataButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        importDataButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        deleteAllDataButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        exportDataButton.Click += (_, _) => ExportDataRequested?.Invoke(this, EventArgs.Empty);
        importDataButton.Click += (_, _) => ImportDataRequested?.Invoke(this, EventArgs.Empty);
        deleteAllDataButton.Click += (_, _) => DeleteAllDataRequested?.Invoke(this, EventArgs.Empty);

        var dataButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        dataButtons.Children.Add(exportDataButton);
        dataButtons.Children.Add(importDataButton);
        dataButtons.Children.Add(deleteAllDataButton);
        var dataContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        dataCardTitle = new TextBlock
        {
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        dataContent.Children.Add(dataCardTitle);
        dataContent.Children.Add(dataHint);
        dataContent.Children.Add(dataButtons);
        dataCard = CreateSettingsCard(dataContent);
        p.Children.Add(dataCard);

        termsButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        termsButton.Click += TermsButton_Click;
        var legalContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        legalSectionTitle = new TextBlock
        {
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        legalContent.Children.Add(legalSectionTitle);
        legalHint = new TextBlock
        {
            FontSize = 12,
            Opacity = .65,
            TextWrapping = TextWrapping.Wrap
        };
        legalContent.Children.Add(legalHint);
        legalContent.Children.Add(termsButton);
        legalCard = CreateSettingsCard(legalContent);
        p.Children.Add(legalCard);

        // Advanced
        advancedTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        p.Children.Add(advancedTitle);

        dataLocationTitle = new TextBlock { FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        dataLocationText = new TextBlock
        {
            FontSize = 12,
            Opacity = .65,
            TextWrapping = TextWrapping.Wrap,
            Text = GetDataDirectory()
        };
        dataLocationHint = new TextBlock { FontSize = 12, Opacity = .65, TextWrapping = TextWrapping.Wrap };
        openDataFolderButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        openDataFolderButton.Click += (_, _) => OpenDataFolder();
        pauseButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        pauseButton.Click += (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty);
        backupButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        backupButton.Click += (_, _) => BackupRequested?.Invoke(this, EventArgs.Empty);
        diagnosticsButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        diagnosticsButton.Click += (_, _) => DiagnosticsRequested?.Invoke(this, EventArgs.Empty);
        var advancedButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        advancedButtons.Children.Add(openDataFolderButton);
        advancedButtons.Children.Add(pauseButton);
        advancedButtons.Children.Add(backupButton);
        advancedButtons.Children.Add(diagnosticsButton);
        var advancedContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        advancedContent.Children.Add(dataLocationTitle);
        advancedContent.Children.Add(dataLocationText);
        advancedContent.Children.Add(dataLocationHint);
        advancedContent.Children.Add(advancedButtons);
        advancedCard = CreateSettingsCard(advancedContent);
        p.Children.Add(advancedCard);

        aboutTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        p.Children.Add(aboutTitle);

        aboutText = new TextBlock
        {
            FontSize = 12,
            Opacity = .55,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4, 0, 0, 0)
        };
        p.Children.Add(aboutText);

        Content = new ScrollViewer
        {
            Content = p,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        SetLanguage(languageMode, LanguageResolver.Resolve(languageMode));
        SetThemeMode(mode);
        UpdatePaletteVisuals();
        LocalizeColorPickerText();
    }

    private static Border CreateSettingsCard(UIElement content) => new()
    {
        BorderBrush = new SolidColorBrush(UIColor.FromArgb(55, 128, 128, 128)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Child = content,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static string GetDataDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenTimeRS", "ScreenTime RS", "data");

    private static void OpenDataFolder()
    {
        try
        {
            var path = GetDataDirectory();
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void CreateColorPicker()
    {
        colorPicker = new ColorPicker
        {
            Color = _accentColor,
            IsMoreButtonVisible = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            ColorSpectrumComponents = ColorSpectrumComponents.HueSaturation,
            Language = _language == LanguageMode.English ? "en-US" : _language == LanguageMode.TraditionalChinese ? "zh-TW" : "zh-CN"
        };

        colorPickerHost.Children.Insert(0, colorPicker);
        colorPicker.ColorChanged += ColorPicker_ColorChanged;
        colorPicker.Loaded += (_, _) =>
        {
            LocalizeColorPickerText();
            BindColorSpectrumInteraction();
        };
        colorPicker.SizeChanged += (_, _) =>
        {
            LocalizeColorPickerText();
            UpdateColorSpectrumInteractionBounds();
        };
    }

    private void BindColorSpectrumInteraction()
    {
        var spectrum = FindVisualChild<ColorSpectrum>(colorPicker);
        if (spectrum is null) return;

        if (!ReferenceEquals(colorSpectrum, spectrum))
        {
            if (colorSpectrum is not null)
                colorSpectrum.SizeChanged -= ColorSpectrum_SizeChanged;

            colorSpectrum = spectrum;
            colorSpectrum.IsHitTestVisible = false;
            colorSpectrum.SizeChanged += ColorSpectrum_SizeChanged;
        }

        UpdateColorSpectrumInteractionBounds();
    }

    private void ColorSpectrum_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateColorSpectrumInteractionBounds();
    }

    private static T? FindVisualChild<T>(DependencyObject? root) where T : DependencyObject
    {
        if (root is null) return null;
        if (root is T match) return match;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var found = FindVisualChild<T>(VisualTreeHelper.GetChild(root, i));
            if (found is not null) return found;
        }
        return null;
    }

    private void UpdateColorSpectrumInteractionBounds()
    {
        if (colorSpectrum is null || !colorSpectrum.IsLoaded || colorSpectrum.ActualWidth <= 1 || colorSpectrum.ActualHeight <= 1)
        {
            colorSpectrumInputSurface.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var origin = colorSpectrum.TransformToVisual(colorPickerHost)
                .TransformPoint(new Windows.Foundation.Point(0, 0));

            colorSpectrumInputSurface.Margin = new Thickness(origin.X, origin.Y, 0, 0);
            colorSpectrumInputSurface.Width = colorSpectrum.ActualWidth;
            colorSpectrumInputSurface.Height = colorSpectrum.ActualHeight;
            colorSpectrumInputSurface.Visibility = Visibility.Visible;
        }
        catch
        {
            colorSpectrumInputSurface.Visibility = Visibility.Collapsed;
        }
    }

    private void ColorSpectrumInput_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateSpectrumPointer(e, commit: false);
    }

    private void ColorSpectrumInput_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        UpdateSpectrumPointer(e, commit: colorSpectrumPointerDown);

        var point = e.GetCurrentPoint(colorSpectrumInputSurface);
        if (colorSpectrumPointerDown || point.Properties.IsLeftButtonPressed)
            e.Handled = true;
    }

    private void ColorSpectrumInput_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(colorSpectrumInputSurface);
        if (!point.Properties.IsLeftButtonPressed) return;

        colorSpectrumPointerDown = true;
        colorSpectrumInputSurface.CapturePointer(e.Pointer);
        UpdateSpectrumPointer(e, commit: true);
        e.Handled = true;
    }

    private void ColorSpectrumInput_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Only a pointer sequence started by our left-button handler may commit
        // a color. This also prevents right-click release events from selecting.
        if (colorSpectrumPointerDown)
            UpdateSpectrumPointer(e, commit: true);

        colorSpectrumPointerDown = false;
        colorSpectrumInputSurface.ReleasePointerCaptures();
        e.Handled = true;
    }

    private void ColorSpectrumInput_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!colorSpectrumPointerDown)
            HideColorSpectrumTooltip();
    }

    private void ColorSpectrumInput_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        colorSpectrumPointerDown = false;
        HideColorSpectrumTooltip();
    }

    private void UpdateSpectrumPointer(PointerRoutedEventArgs e, bool commit)
    {
        if (colorSpectrum is null || colorSpectrum.ActualWidth <= 1 || colorSpectrum.ActualHeight <= 1)
            return;

        var point = e.GetCurrentPoint(colorSpectrumInputSurface).Position;
        point = new Windows.Foundation.Point(
            Math.Clamp(point.X, 0, colorSpectrumInputSurface.ActualWidth),
            Math.Clamp(point.Y, 0, colorSpectrumInputSurface.ActualHeight));

        var hue = point.X / colorSpectrumInputSurface.ActualWidth * 360.0;
        var saturation = 1.0 - point.Y / colorSpectrumInputSurface.ActualHeight;
        var text = GetLocalizedSpectrumColorName(hue, saturation, 1.0, _language);

        UpdateColorSpectrumTooltipTheme();
        colorSpectrumTooltipText.Text = text;
        colorSpectrumTooltip.Visibility = Visibility.Visible;
        PositionColorSpectrumTooltip(point);

        if (commit)
        {
            var selected = HsvToColor(hue, saturation, 1.0);
            if (!ColorsEqual(colorPicker.Color, selected))
                colorPicker.Color = selected;
        }
    }

    private void PositionColorSpectrumTooltip(Windows.Foundation.Point point)
    {
        if (colorSpectrum is null || colorSpectrumTooltip.Visibility != Visibility.Visible)
            return;

        var tooltipOffsetX = 16.0;
        var tooltipOffsetY = 16.0;
        var x = point.X + tooltipOffsetX;
        var y = point.Y + tooltipOffsetY;

        colorSpectrumTooltip.Measure(new Windows.Foundation.Size(
            double.PositiveInfinity, double.PositiveInfinity));
        var size = colorSpectrumTooltip.DesiredSize;
        var maxX = Math.Max(0, colorSpectrumInputSurface.ActualWidth - size.Width);
        var maxY = Math.Max(0, colorSpectrumInputSurface.ActualHeight - size.Height);
        x = Math.Clamp(x, 0, maxX);
        y = Math.Clamp(y, 0, maxY);

        if (colorSpectrumTooltip.RenderTransform is TranslateTransform transform)
        {
            transform.X = x;
            transform.Y = y;
        }
    }

    private void UpdateColorSpectrumTooltipTheme()
    {
        var dark = colorPickerHost.ActualTheme == ElementTheme.Dark;
        colorSpectrumTooltip.Background = new SolidColorBrush(
            dark
                ? UIColor.FromArgb(244, 45, 45, 45)
                : UIColor.FromArgb(248, 255, 255, 255));
        colorSpectrumTooltip.BorderBrush = new SolidColorBrush(
            dark
                ? UIColor.FromArgb(95, 255, 255, 255)
                : UIColor.FromArgb(80, 120, 120, 120));
        colorSpectrumTooltipText.Foreground = new SolidColorBrush(
            dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black);
    }

    private void HideColorSpectrumTooltip()
    {
        colorSpectrumTooltip.Visibility = Visibility.Collapsed;
    }

    private static bool ColorsEqual(UIColor left, UIColor right) =>
        left.A == right.A && left.R == right.R && left.G == right.G && left.B == right.B;

    private static UIColor HsvToColor(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        var c = value * saturation;
        var x = c * (1 - Math.Abs((hue / 60.0 % 2) - 1));
        var m = value - c;
        double r, g, b;

        if (hue < 60) (r, g, b) = (c, x, 0);
        else if (hue < 120) (r, g, b) = (x, c, 0);
        else if (hue < 180) (r, g, b) = (0, c, x);
        else if (hue < 240) (r, g, b) = (0, x, c);
        else if (hue < 300) (r, g, b) = (x, 0, c);
        else (r, g, b) = (c, 0, x);

        return UIColor.FromArgb(
            255,
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    private static string GetLocalizedSpectrumColorName(double hue, double saturation, double value, LanguageMode language)
    {
        var en = language == LanguageMode.English;
        var hant = language == LanguageMode.TraditionalChinese;
        string zh(string s, string t) => hant ? t : s;
        if (value < 0.16) return en ? "Black" : zh("黑色", "黑色");
        if (saturation < 0.08)
            return value > 0.85 ? (en ? "White" : zh("白色", "白色")) : (en ? "Gray" : zh("灰色", "灰色"));
        if (hue < 15 || hue >= 345) return en ? "Red" : zh("红色", "紅色");
        if (hue < 45) return en ? "Orange" : zh("橙色", "橙色");
        if (hue < 75) return en ? "Yellow" : zh("黄色", "黃色");
        if (hue < 165) return en ? "Green" : zh("绿色", "綠色");
        if (hue < 195) return en ? "Cyan" : zh("青色", "青色");
        if (hue < 255) return en ? "Blue" : zh("蓝色", "藍色");
        if (hue < 285) return en ? "Purple" : zh("紫色", "紫色");
        if (hue < 330) return en ? "Magenta" : zh("洋红色", "洋紅色");
        return en ? "Pink" : zh("粉色", "粉紅色");
    }

    private void LocalizeColorPickerText()
    {
        if (colorPicker is null) return;

        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        colorPicker.Language = en ? "en-US" : hant ? "zh-TW" : "zh-CN";
        var labels = en
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["更多"] = "More", ["更多功能"] = "More", ["更少"] = "Less", ["红色"] = "Red", ["绿色"] = "Green",
                ["蓝色"] = "Blue", ["青色"] = "Cyan", ["紫色"] = "Purple", ["洋红色"] = "Magenta",
                ["粉色"] = "Pink", ["橙色"] = "Orange", ["黄色"] = "Yellow", ["白色"] = "White", ["灰色"] = "Gray",
                ["黑色"] = "Black", ["浅蓝色"] = "Light blue", ["浅绿色"] = "Light green",
                ["色调"] = "Hue", ["饱和度"] = "Saturation", ["值"] = "Value",
                ["透明度"] = "Alpha", ["十六进制"] = "Hex", ["颜色"] = "Color"
            }
            : hant
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["更多"] = "更多", ["More"] = "更多", ["更少"] = "更少", ["紅色"] = "紅色", ["红色"] = "紅色", ["綠色"] = "綠色", ["绿色"] = "綠色",
                    ["藍色"] = "藍色", ["蓝色"] = "藍色", ["青色"] = "青色", ["紫色"] = "紫色", ["洋紅色"] = "洋紅色", ["洋红色"] = "洋紅色",
                    ["粉紅色"] = "粉紅色", ["粉色"] = "粉紅色", ["橙色"] = "橙色", ["黄色"] = "黃色", ["黃色"] = "黃色",
                    ["白色"] = "白色", ["灰色"] = "灰色", ["黑色"] = "黑色", ["浅蓝色"] = "淺藍色", ["浅绿色"] = "淺綠色",
                    ["色调"] = "色相", ["色調"] = "色相", ["饱和度"] = "飽和度", ["值"] = "明度", ["透明度"] = "透明度",
                    ["十六进制"] = "十六進位", ["十六進制"] = "十六進位", ["颜色"] = "顏色", ["顏色"] = "顏色",
                    ["Red"] = "紅色", ["Green"] = "綠色", ["Blue"] = "藍色", ["Cyan"] = "青色", ["Purple"] = "紫色", ["Magenta"] = "洋紅色", ["Pink"] = "粉紅色", ["Orange"] = "橙色", ["Yellow"] = "黃色", ["White"] = "白色", ["Gray"] = "灰色", ["Black"] = "黑色", ["Light blue"] = "淺藍色", ["Light green"] = "淺綠色", ["Hue"] = "色相", ["Saturation"] = "飽和度", ["Value"] = "明度", ["Alpha"] = "透明度", ["Hex"] = "十六進位", ["Color"] = "顏色", ["Less"] = "更少"
                }
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["More"] = "更多", ["Less"] = "更少", ["Red"] = "红色", ["Green"] = "绿色", ["Blue"] = "蓝色", ["Cyan"] = "青色", ["Purple"] = "紫色", ["Magenta"] = "洋红色", ["Pink"] = "粉色", ["Orange"] = "橙色", ["Yellow"] = "黄色", ["White"] = "白色", ["Gray"] = "灰色", ["Black"] = "黑色", ["Light blue"] = "浅蓝色", ["Light green"] = "浅绿色", ["Hue"] = "色调", ["Saturation"] = "饱和度", ["Value"] = "值", ["Alpha"] = "透明度", ["Hex"] = "十六进制", ["Color"] = "颜色"
                };

        ReplaceColorPickerStrings(colorPicker, labels);
    }

    private static void ReplaceColorPickerStrings(DependencyObject root, Dictionary<string, string> labels)
    {
        if (root is TextBlock textBlock)
        {
            var text = textBlock.Text?.Trim();
            if (!string.IsNullOrEmpty(text) && labels.TryGetValue(text, out var translated))
                textBlock.Text = translated;
        }
        else if (root is ContentControl contentControl && contentControl.Content is string content)
        {
            var text = content.Trim();
            if (!string.IsNullOrEmpty(text) && labels.TryGetValue(text, out var translated))
                contentControl.Content = translated;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
            ReplaceColorPickerStrings(VisualTreeHelper.GetChild(root, i), labels);
    }

    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updatingTheme || theme.SelectedIndex < 0) return;
        ThemeModeChanged?.Invoke(this, (ThemeMode)theme.SelectedIndex);
    }

    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updatingLanguage || language.SelectedIndex < 0) return;
        _languagePreference = (LanguageMode)language.SelectedIndex;
        _language = LanguageResolver.Resolve(_languagePreference);
        LanguageChanged?.Invoke(this, _languagePreference);
    }

    private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (updatingColor) return;
        _accentColor = args.NewColor;
        UpdatePaletteVisuals();
        AccentColorChanged?.Invoke(this, _accentColor);
    }

    private void SelectPreset(int index)
    {
        _accentColor = ThemeManager.PresetColors[index];
        updatingColor = true;
        colorPicker.Color = _accentColor;
        updatingColor = false;
        UpdatePaletteVisuals();
        AccentColorChanged?.Invoke(this, _accentColor);
    }

    private void UpdatePaletteVisuals()
    {
        for (int i = 0; i < paletteButtons.Length; i++)
        {
            var color = ThemeManager.PresetColors[i];
            paletteButtons[i].Background = new SolidColorBrush(color);
            paletteButtons[i].BorderBrush = new SolidColorBrush(
                ThemeManager.IsClose(_accentColor, color) ? Microsoft.UI.Colors.White : UIColor.FromArgb(90, 255, 255, 255));
            paletteButtons[i].Content = ThemeManager.IsClose(_accentColor, color) ? "✓" : "";
            paletteButtons[i].Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }
    }

    public void ApplyAccent(UIColor color)
    {
        _accentColor = color;
        ApplyStartupAccent(color);
        ApplyComboBoxAccent(theme, color);
        ApplyComboBoxAccent(language, color);
        updatingColor = true;
        colorPicker.Color = color;
        updatingColor = false;
        UpdatePaletteVisuals();
        BindColorSpectrumInteraction();
    }

    void ApplyStartupAccent(UIColor color)
    {
        // WinUI 3 CheckBox can keep the system accent inside its materialized
        // template even after application resources change. Use a tiny
        // in-page checkbox visual so this specific setting is guaranteed to
        // follow the same accent color as the rest of the application.
        var accent = new SolidColorBrush(color);
        var border = new SolidColorBrush(ColorWithAlpha(color, 255));
        startupBox.BorderBrush = border;
        startupBox.Background = startupEnabled ? accent : new SolidColorBrush(UIColor.FromArgb(0, 0, 0, 0));
        startupBox.Child = startupEnabled
            ? new TextBlock
            {
                Text = "✓",
                FontSize = 17,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
            : null;
    }

    void ToggleStartup()
    {
        startupEnabled = !startupEnabled;
        SetStartup(startupEnabled);
        ApplyStartupAccent(_accentColor);
    }

    static void ApplyComboBoxAccent(ComboBox combo, UIColor color)
    {
        var accent = new SolidColorBrush(color);
        combo.Resources["ComboBoxItemPillFillBrush"] = accent;
        combo.Resources["ComboBoxItemBackgroundSelected"] = new SolidColorBrush(ColorWithAlpha(color, 40));
        combo.Resources["ComboBoxItemBackgroundSelectedUnfocused"] = new SolidColorBrush(ColorWithAlpha(color, 34));
        combo.Resources["ComboBoxItemBackgroundSelectedPointerOver"] = new SolidColorBrush(ColorWithAlpha(color, 56));
        combo.Resources["ComboBoxItemBackgroundSelectedPressed"] = new SolidColorBrush(ColorWithAlpha(color, 72));
        combo.Resources["ComboBoxItemForegroundSelected"] = accent;
        combo.Resources["ComboBoxItemForegroundSelectedPointerOver"] = accent;
        combo.Resources["ComboBoxSelectedBackground"] = new SolidColorBrush(ColorWithAlpha(color, 40));
        combo.Resources["ComboBoxSelectedPointerOverBackground"] = new SolidColorBrush(ColorWithAlpha(color, 56));
    }

    static UIColor ColorWithAlpha(UIColor color, byte alpha) =>
        UIColor.FromArgb(alpha, color.R, color.G, color.B);

    public void SetMonitoringState(bool paused)
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        _monitorPaused = paused;
        pauseButton.Content = paused ? (en ? "Resume recording" : hant ? "繼續記錄" : "继续记录") : (en ? "Pause recording" : hant ? "暫停記錄" : "暂停记录");
        ToolTipService.SetToolTip(pauseButton, paused ? (en ? "Resume usage tracking" : hant ? "恢復使用時間記錄" : "恢复使用时间记录") : (en ? "Pause usage tracking" : hant ? "暂停使用时间记录" : "暂停使用时间记录"));
    }

    public void SetThemeMode(ThemeMode mode)
    {
        updatingTheme = true;
        theme.SelectedIndex = (int)mode;
        updatingTheme = false;
    }

    public void SetLanguage(LanguageMode preference, LanguageMode effective)
    {
        _languagePreference = preference;
        _language = effective;
        updatingLanguage = true;
        language.Items.Clear();
        language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.System, effective));
        language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.SimplifiedChinese, effective));
        language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.TraditionalChinese, effective));
        language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.English, effective));
        language.SelectedIndex = (int)preference;
        updatingLanguage = false;

        var en = effective == LanguageMode.English;
        var hant = effective == LanguageMode.TraditionalChinese;
        pageTitle.Text = en ? "Settings" : hant ? "設定" : "设置";
        generalTitle.Text = en ? "General" : hant ? "一般" : "常规";
        startupText.Text = en ? "Start ScreenTime RS silently in the background when I sign in to Windows" : hant ? "登入 Windows 後在背景靜默啟動 ScreenTime RS" : "登录 Windows 后在后台静默启动";
        appearanceTitle.Text = en ? "Appearance" : hant ? "外觀" : "外观";
        themeColorsExpander.Header = en ? "Theme colors & custom color" : hant ? "主題色彩與自訂顏色" : "主题色彩与自定义颜色";
        paletteTitle.Text = en ? "Preset colors" : hant ? "預設主題色" : "预设主题色";
        customColorTitle.Text = en ? "Custom color" : hant ? "自訂顏色" : "自定义颜色";
        accentHint.Text = en
            ? "Choose a preset or pick any custom color. Changes apply immediately across the interface."
            : hant ? "選擇預設主題色或自由取色，修改後會立即套用到整個介面。"
            : "选择预设主题色或自由取色，修改后会立即应用到整个界面。";
        languageTitle.Text = en ? "Language" : hant ? "語言" : "语言";
        dataSectionTitle.Text = en ? "Data & Privacy" : hant ? "資料與隱私" : "数据与隐私";
        themeModeLabel.Text = en ? "Theme" : hant ? "主題" : "主题";
        languageModeLabel.Text = en ? "Display language" : hant ? "顯示語言" : "显示语言";
        dataHint.Text = en
            ? "Export or import local usage history. Importing creates an automatic local safety backup before replacement."
            : hant ? "匯出或匯入本機使用歷史記錄。匯入取代資料前會自動建立本機安全備份。"
            : "导出或导入本机使用历史记录。导入替换数据前会自动创建本地安全备份。";
        dataCardTitle.Text = en ? "Data management" : hant ? "資料管理" : "数据管理";
        exportDataButton.Content = en ? "Export data" : hant ? "匯出資料" : "导出数据";
        importDataButton.Content = en ? "Import data" : hant ? "匯入資料" : "导入数据";
        deleteAllDataButton.Content = en ? "Delete all data" : hant ? "刪除所有資料" : "删除全部数据";
        deleteAllDataButton.Background = new SolidColorBrush(UIColor.FromArgb(230, 196, 43, 28));
        deleteAllDataButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        legalSectionTitle.Text = en ? "Terms & Privacy" : hant ? "使用條款與隱私" : "使用条款与隐私";
        legalHint.Text = en ? "Review how ScreenTime RS records, stores, and handles local usage data." : hant ? "查看 ScreenTime RS 如何記錄、儲存及處理本機使用資料。" : "查看 ScreenTime RS 如何记录、存储和处理本机使用数据。";
        termsButton.Content = en ? "View terms and privacy policy" : hant ? "檢視使用條款與隱私權政策" : "查看用户条款与隐私政策";
        advancedTitle.Text = en ? "Advanced" : hant ? "進階" : "高级";
        aboutTitle.Text = en ? "About" : hant ? "關於" : "关于";
        dataLocationTitle.Text = en ? "Data location" : hant ? "資料位置" : "数据位置";
        dataLocationHint.Text = en
            ? "Usage records, snapshots, and automatic import backups are stored here."
            : hant ? "使用記錄、快照與匯入前的自動備份會儲存在此處。"
            : "使用记录、快照以及导入前的自动备份会存储在这里。";
        openDataFolderButton.Content = en ? "Open data folder" : hant ? "開啟資料夾" : "打开数据文件夹";
        openDataFolderButton.Content = en ? "Open data folder" : hant ? "開啟資料夾" : "打开数据文件夹";
        pauseButton.Content = ""; // state is applied by SetMonitoringState
        backupButton.Content = en ? "Create backup" : hant ? "建立備份" : "创建备份";
        diagnosticsButton.Content = en ? "Diagnostics" : hant ? "診斷" : "诊断";
        SetMonitoringState(_monitorPaused);
        aboutText.Text = en
            ? "ScreenTime RS · Version 0.8.0 · Rust monitoring core + WinUI 3 / Fluent UI"
            : hant ? "ScreenTime RS · 版本 0.8.0 · Rust 監控核心 + WinUI 3 / Fluent UI"
            : "ScreenTime RS · 版本 0.8.0 · Rust monitoring core + WinUI 3 / Fluent UI";

        var themeIndex = theme.SelectedIndex;
        updatingTheme = true;
        theme.Items.Clear();
        if (en)
        {
            theme.Items.Add("Follow system"); theme.Items.Add("Light"); theme.Items.Add("Dark");
        }
        else if (hant)
        {
            theme.Items.Add("跟隨系統"); theme.Items.Add("淺色"); theme.Items.Add("深色");
        }
        else
        {
            theme.Items.Add("跟随系统"); theme.Items.Add("浅色"); theme.Items.Add("深色");
        }
        theme.SelectedIndex = Math.Clamp(themeIndex, 0, 2);
        updatingTheme = false;
        UpdatePaletteVisuals();
        LocalizeColorPickerText();
        BindColorSpectrumInteraction();
        HideColorSpectrumTooltip();
    }

    private async void TermsButton_Click(object sender, RoutedEventArgs e)
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        var dialog = new ContentDialog
        {
            RequestedTheme = ActualTheme,
            Title = en ? "Terms of Use & Privacy" : hant ? "使用條款與隱私" : "使用条款与隐私政策",
            Content = new ScrollViewer
            {
                MaxHeight = 520,
                Content = new TextBlock
                {
                    Text = en ? TermsEnglish : hant ? TermsTraditional : TermsChinese,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                }
            },
            CloseButtonText = en ? "Close" : hant ? "關閉" : "关闭",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    const string TermsChinese = "使用条款\n\n1. ScreenTime RS 用于在本机统计 Windows 应用与屏幕使用时间。统计结果仅供个人管理和参考。\n2. 软件按现有功能提供，不保证在所有 Windows 环境、第三方应用或未来系统更新中始终正常工作。\n3. 用户应自行确认软件记录范围，并对基于统计结果作出的决定负责。\n4. 不得利用本软件进行违反适用法律法规或侵犯他人合法权益的活动。\n\n隐私政策\n\n1. ScreenTime RS 的核心统计数据保存在本机，不由软件主动上传到远程服务器。\n2. 为完成统计，软件可能保存应用名称、可执行文件路径、使用时长以及必要的本机运行状态。\n3. 数据默认存储在当前 Windows 用户的 LocalAppData 目录中。卸载程序不会自动删除这些统计数据。\n4. 软件不以广告追踪为目的收集个人信息，也不会主动将统计数据出售或共享给第三方。\n5. Windows、杀毒软件或其他系统组件可能拥有独立的系统级数据访问能力，本政策不涵盖这些第三方行为。\n\n最后更新：ScreenTime RS v0.8.0";
    const string TermsTraditional = "使用條款\n\n1. ScreenTime RS 用於在本機統計 Windows 應用程式與螢幕使用時間，統計結果僅供個人管理與參考。\n2. 軟體依現有功能提供，不保證在所有 Windows 環境、第三方應用程式或未來系統更新中始終正常運作。\n3. 使用者應自行確認軟體記錄範圍，並對根據統計結果作出的決定負責。\n4. 不得利用本軟體進行違反適用法律法規或侵犯他人合法權益的活動。\n\n隱私權政策\n\n1. ScreenTime RS 的核心統計資料儲存在本機，軟體不會主動上傳至遠端伺服器。\n2. 為提供統計功能，軟體可能儲存應用程式名稱、可執行檔路徑、使用時間以及必要的本機執行狀態。\n3. 資料預設儲存在目前 Windows 使用者的 LocalAppData 目錄中。解除安裝程式不會自動刪除這些統計資料。\n4. 軟體不會以廣告追蹤為目的收集個人資訊，也不會主動出售或分享統計資料給第三方。\n5. Windows、防毒軟體或其他系統元件可能具有獨立的系統層級資料存取能力；這些第三方行為不在本政策範圍內。\n\n最後更新：ScreenTime RS v0.8.0";
    const string TermsEnglish = "Terms of Use\n\n1. ScreenTime RS is designed to record Windows application and screen usage time locally for personal management and reference.\n2. The software is provided as implemented and may not work identically on every Windows environment, third-party application, or future system update.\n3. Users are responsible for reviewing the recorded scope and for decisions made based on the statistics.\n4. Do not use the software for activities that violate applicable laws or the legitimate rights of others.\n\nPrivacy Policy\n\n1. ScreenTime RS stores its core statistics locally and does not actively upload them to a remote server.\n2. To provide usage statistics, the software may store application names, executable paths, usage durations, and necessary local runtime state.\n3. Data is stored by default under the current Windows user's LocalAppData directory. Uninstalling the program does not automatically delete these statistics.\n4. The software does not collect personal information for advertising tracking and does not actively sell or share usage statistics with third parties.\n5. Windows, antivirus software, or other system components may have independent system-level access to data; those third-party practices are outside this policy.\n\nLast updated: ScreenTime RS v0.8.0";

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
