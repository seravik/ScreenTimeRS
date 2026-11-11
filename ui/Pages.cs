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
    readonly TextBlock todayValue, todayTitle, todaySub;
    readonly InfoBar status;
    readonly TextBlock pageTitle;
    readonly Button pauseToggle;
    readonly TextBlock sessionValue, longestValue, unlockValue, hourlyTitle, hourlyHint;
    readonly TextBlock[] metricTitles = new TextBlock[3];
    readonly Border[] hourlyBars = new Border[24];
    readonly Border hourlyHoverCard;
    readonly TextBlock hourlyHoverText;
    readonly TextBlock topAppsTitle;
    readonly StackPanel topApps;
    readonly List<Border> topAppBars = new();
    string? _topAppsSignature;
    int _hoveredHourlyIndex = -1;
    LanguageMode _language = LanguageMode.SimplifiedChinese;
    Snapshot? _lastSnapshot;
    bool statusClosed;

    public event Action? PauseToggleRequested;

    public OverviewPage()
    {
        var panel = new StackPanel { Spacing = 18, Padding = new Thickness(28) };
        pageTitle = new TextBlock { FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        pauseToggle = new Button { VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(14, 6, 14, 6) };
        pauseToggle.Click += (_, _) => PauseToggleRequested?.Invoke();

        var header = new Grid { ColumnSpacing = 12 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pageTitle, 0);
        Grid.SetColumn(pauseToggle, 1);
        header.Children.Add(pageTitle);
        header.Children.Add(pauseToggle);
        panel.Children.Add(header);

        var cards = new Grid();
        cards.ColumnDefinitions.Add(new ColumnDefinition());
        AddCard(cards, 0, out todayValue, out todayTitle, out todaySub);
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

        var topBorder = new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1)
        };
        var topPanel = new StackPanel { Spacing = 12 };
        topAppsTitle = new TextBlock { FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        topApps = new StackPanel { Spacing = 10 };
        topPanel.Children.Add(topAppsTitle);
        topPanel.Children.Add(topApps);
        topBorder.Child = topPanel;
        panel.Children.Add(topBorder);

        status = new InfoBar { IsOpen = true, IsClosable = true };
        status.Closed += (_, _) => statusClosed = true;
        panel.Children.Add(status);



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
        foreach (var bar in hourlyBars)
            bar.Background = brush;
        foreach (var bar in topAppBars)
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
        todayTitle.Text = en ? "Today" : hant ? "今天" : "今天";
        todaySub.Text = en ? "Active time" : hant ? "活躍時間" : "活跃时间";
        metricTitles[0].Text = en ? "Current continuous use" : hant ? "目前連續使用" : "当前连续使用";
        metricTitles[1].Text = en ? "Longest continuous use today" : hant ? "今日最長連續使用" : "今日最长连续使用";
        metricTitles[2].Text = en ? "Unlocks today" : hant ? "今日解鎖次數" : "今日解锁次数";
        topAppsTitle.Text = en ? "Today's top 5 apps" : hant ? "今日應用程式 Top 5" : "今日应用 Top 5";
        hourlyTitle.Text = en ? "Today by hour" : hant ? "今天每小時" : "今日按小时";
        hourlyHint.Text = "";
        _topAppsSignature = null;
        UpdateSnapshot(_lastSnapshot ?? new Snapshot());
        if (_hoveredHourlyIndex >= 0)
            ShowHourlyHover(_hoveredHourlyIndex);
    }
    public void UpdateSnapshot(Snapshot s)
    {
        _lastSnapshot = s;
        SetText(todayValue, UiHelpers.Format(s.today, _language));

        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        var pauseText = s.paused
            ? (en ? "Resume recording" : hant ? "繼續記錄" : "继续记录")
            : (en ? "Pause recording" : hant ? "暫停記錄" : "暂停记录");
        if (!Equals(pauseToggle.Content, pauseText))
            pauseToggle.Content = pauseText;
        SetText(sessionValue, UiHelpers.Format(s.current_session_seconds, _language));
        SetText(longestValue, UiHelpers.Format(s.longest_session_today, _language));
        SetText(unlockValue, s.unlocks_today.ToString());
        var hourlyMax = Math.Max(1, s.hourly_today.Select(x => Math.Max(0, x.seconds)).DefaultIfEmpty(0).Max());
        var hourlyRecordedTotal = s.hourly_today.Sum(x => Math.Max(0, x.seconds));
        for (int i = 0; i < hourlyBars.Length; i++) {
            var seconds = i < s.hourly_today.Length ? Math.Max(0, s.hourly_today[i].seconds) : 0;
            var barHeight = seconds <= 0 ? 0 : Math.Max(6, 105 * seconds / (double)hourlyMax);
            if (Math.Abs(hourlyBars[i].Height - barHeight) > 0.5)
                hourlyBars[i].Height = barHeight;
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
            status.Message = _language == LanguageMode.English ? "Resume recording from the Overview header or Settings → Recording." : _language == LanguageMode.TraditionalChinese ? "可在概覽頂部或「設定 → 記錄狀態」中繼續記錄。" : "可在概览顶部或「设置 → 记录状态」中继续记录。";
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

        UpdateTopApps(s);
    }

    private void UpdateTopApps(Snapshot s)
    {
        var top = s.apps.Where(a => a.seconds > 0).OrderByDescending(a => a.seconds).Take(5).ToArray();
        var signature = string.Join("|", top.Select(a => $"{a.name}\u0001{a.seconds}\u0001{a.exe_path}"));
        if (signature == _topAppsSignature) return;
        _topAppsSignature = signature;

        topApps.Children.Clear();
        topAppBars.Clear();

        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        if (top.Length == 0)
        {
            topApps.Children.Add(new TextBlock
            {
                Text = en ? "No application usage recorded today." : hant ? "今天尚無應用程式使用記錄。" : "今天还没有应用使用记录。",
                Opacity = .6,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        var max = Math.Max(1, top[0].seconds);
        foreach (var app in top)
        {
            var row = new Grid { ColumnSpacing = 10 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var name = new TextBlock
            {
                Text = UiHelpers.FriendlyName(app, _language),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            var time = new TextBlock
            {
                Text = UiHelpers.Format(app.seconds, _language),
                Opacity = .65,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(name, 0);
            Grid.SetColumn(time, 1);
            row.Children.Add(name);
            row.Children.Add(time);

            // A static two-column bar instead of ProgressBar: ProgressBar runs an
            // implicit animation whenever Value changes, which made the list
            // flicker while the snapshot refreshed.
            var percent = Math.Clamp(app.seconds * 100.0 / max, 1.0, 100.0);
            var track = new Grid { Height = 4 };
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(percent, GridUnitType.Star) });
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.01, 100.0 - percent), GridUnitType.Star) });
            var background = new Border
            {
                Background = new SolidColorBrush(UIColor.FromArgb(28, 128, 128, 128)),
                CornerRadius = new CornerRadius(2)
            };
            Grid.SetColumnSpan(background, 2);
            var fill = new Border
            {
                Background = UiHelpers.AccentBrush(),
                CornerRadius = new CornerRadius(2)
            };
            Grid.SetColumn(fill, 0);
            track.Children.Add(background);
            track.Children.Add(fill);

            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(row);
            stack.Children.Add(track);
            topApps.Children.Add(stack);
            topAppBars.Add(fill);
        }
    }

    private static void SetText(TextBlock block, string text)
    {
        if (!string.Equals(block.Text, text, StringComparison.Ordinal))
            block.Text = text;
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
    readonly Border detailPanel = new();
    readonly StackPanel detailContent = new();
    readonly TextBlock detailPlaceholder = new();
    string? _selectedKey;
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
    AppStat? _detailApp;
    AppHistoryResponse? _detailHistory;
    long _detailTotal;
    long _rangeTotal;

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
        Grid.SetRow(emptyState, 0);

        var scroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var listHost = new Grid();
        listHost.Children.Add(emptyState);
        listHost.Children.Add(scroll);

        detailPanel.BorderBrush = new SolidColorBrush(UIColor.FromArgb(45, 128, 128, 128));
        detailPanel.BorderThickness = new Thickness(1);
        detailPanel.CornerRadius = new CornerRadius(10);
        detailPanel.Padding = new Thickness(16);
        detailContent.Spacing = 10;
        detailPlaceholder.Text = "选择一个应用查看详情";
        detailPlaceholder.Opacity = .6;
        detailPlaceholder.TextWrapping = TextWrapping.Wrap;
        detailContent.Children.Add(detailPlaceholder);
        detailPanel.Child = new ScrollViewer
        {
            Content = detailContent,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var contentGrid = new Grid { ColumnSpacing = 20 };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        Grid.SetColumn(listHost, 0);
        Grid.SetColumn(detailPanel, 1);
        contentGrid.Children.Add(listHost);
        contentGrid.Children.Add(detailPanel);
        Grid.SetRow(contentGrid, 2);
        root.Children.Add(contentGrid);

        Content = root;
        UpdateDatePickerState();
        UpdateDaySummary();
    }

    public int SelectedPeriodIndex => period.SelectedIndex;
    public long RangeTotal => _rangeTotal;
    public DateTime? SelectedDay => datePicker.Date?.DateTime.Date;
    public long? SelectedDayTotal => _selectedDayData?.total_seconds;
    public long? AllTimeAppsTotal => period.SelectedIndex == 5 && _allTimeLoaded
        ? _allTimeApps.Sum(x => Math.Max(0, x.seconds))
        : null;

    async void Period_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingPeriod || period.SelectedIndex < 0) return;
        ClearDetail();
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
        ClearDetail();
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

    void ClearDetail()
    {
        _selectedKey = null;
        _detailApp = null;
        _detailHistory = null;
        foreach (var row in rows.Values)
            row.SetSelected(false);
        detailContent.Children.Clear();
        detailContent.Children.Add(detailPlaceholder);
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        var en = language == LanguageMode.English;
        var hant = language == LanguageMode.TraditionalChinese;
        title.Text = en ? "Apps" : hant ? "應用程式" : "应用";
        search.PlaceholderText = en ? "Search apps" : hant ? "搜尋應用程式" : "搜索应用";
        detailPlaceholder.Text = en ? "Select an app to see details." : hant ? "選擇應用程式以查看詳情。" : "选择一个应用查看详情。";
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

        // Re-render the open detail so its dates and labels follow the new UI
        // language instead of staying in the previous one.
        if (_detailApp is not null && _detailHistory is not null)
            ShowDetails(_detailHistory, _detailApp, _detailTotal);
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
        _rangeTotal = denominator;

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
                row = new AppRow(app, _language, _accentColor, SelectApp);
                rows[key] = row;
            }
            row.ApplyAccent(_accentColor);
            row.Update(app, denominator, _language);
            row.SetSelected(string.Equals(key, _selectedKey, StringComparison.OrdinalIgnoreCase));
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

    async Task SelectApp(AppStat app)
    {
        _selectedKey = Key(app);
        foreach (var pair in rows)
            pair.Value.SetSelected(string.Equals(pair.Key, _selectedKey, StringComparison.OrdinalIgnoreCase));

        ShowDetailLoading(app);
        if (_showAppDetailsAsync is not null)
            await _showAppDetailsAsync(app);
    }

    public void ShowDetailLoading(AppStat app)
    {
        _detailApp = null;
        _detailHistory = null;
        detailContent.Children.Clear();
        detailContent.Children.Add(new TextBlock
        {
            Text = UiHelpers.FriendlyName(app, _language),
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        detailContent.Children.Add(new ProgressRing
        {
            IsActive = true,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        });
    }

    public void ShowDetailError()
    {
        if (_selectedKey is null) return;
        _detailApp = null;
        _detailHistory = null;
        detailContent.Children.Clear();
        detailContent.Children.Add(new TextBlock
        {
            Text = _language == LanguageMode.English
                ? "Could not load app details."
                : _language == LanguageMode.TraditionalChinese
                    ? "無法載入應用程式詳情。"
                    : "无法加载应用详情。",
            Opacity = .65,
            TextWrapping = TextWrapping.Wrap
        });
    }

    public void ShowDetails(AppHistoryResponse history, AppStat app, long periodTotal)
    {
        if (!string.Equals(_selectedKey, Key(app), StringComparison.OrdinalIgnoreCase)) return;

        _detailApp = app;
        _detailHistory = history;
        _detailTotal = periodTotal;
        detailContent.Children.Clear();
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;

        var header = new Grid { ColumnSpacing = 12 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
        header.ColumnDefinitions.Add(new ColumnDefinition());
        var icon = new WinUIImage { Width = 42, Height = 42, Stretch = Stretch.Uniform };
        UiHelpers.SetAppIcon(icon, app);
        header.Children.Add(icon);

        var details = new StackPanel { Spacing = 2 };
        details.Children.Add(new TextBlock
        {
            Text = UiHelpers.FriendlyName(app, _language),
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        var share = periodTotal > 0 ? history.total_seconds * 100.0 / periodTotal : 0;
        details.Children.Add(new TextBlock
        {
            Text = en
                ? $"{UiHelpers.Format(history.total_seconds, _language)}  ·  {share:F1}% of period"
                : hant
                    ? $"{UiHelpers.Format(history.total_seconds, _language)}  ·  佔期間 {share:F1}%"
                    : $"{UiHelpers.Format(history.total_seconds, _language)}  ·  占期间 {share:F1}%",
            Opacity = .68,
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetColumn(details, 1);
        header.Children.Add(details);
        detailContent.Children.Add(header);

        detailContent.Children.Add(new TextBlock
        {
            Text = en ? "Daily history" : hant ? "每日使用歷史" : "每日使用历史",
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 0)
        });

        var days = history.daily.Where(x => x.seconds > 0).ToArray();
        if (days.Length == 0)
        {
            detailContent.Children.Add(new TextBlock
            {
                Text = en ? "No usage recorded." : hant ? "暫無使用記錄。" : "暂无使用记录。",
                Opacity = .6
            });
            return;
        }

        foreach (var day in days)
        {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock
            {
                Text = FormatHistoryDate(day.label),
                VerticalAlignment = VerticalAlignment.Center
            });
            var value = new TextBlock
            {
                Text = UiHelpers.Format(day.seconds, _language),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = .75
            };
            Grid.SetColumn(value, 1);
            row.Children.Add(value);
            detailContent.Children.Add(row);
        }
    }

    string FormatHistoryDate(string raw)
    {
        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return _language == LanguageMode.English ? day.ToString("MMM d, yyyy") : day.ToString("yyyy年M月d日");
        return raw;
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
        readonly Func<AppStat, Task>? onSelect;
        string iconKey = "";
        AppStat _app = new();
        UIColor _accentColor;
        bool _selected;
        static readonly Dictionary<string, BitmapImage> IconCache = new(StringComparer.OrdinalIgnoreCase);

        public AppRow(AppStat app, LanguageMode language, UIColor accentColor, Func<AppStat, Task>? onSelect)
        {
            this.onSelect = onSelect;
            _accentColor = accentColor;
            var grid = new Grid { Padding = new Thickness(10), ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });

            icon = new WinUIImage { Width = 36, Height = 36, Stretch = Stretch.Uniform };
            Grid.SetColumn(icon, 0); grid.Children.Add(icon);

            var info = new StackPanel();
            name = new TextBlock { FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
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
                if (this.onSelect is not null)
                    await this.onSelect(_app);
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
            _accentColor = color;
            var accent = new SolidColorBrush(color);
            var track = new SolidColorBrush(UIColor.FromArgb(40, color.R, color.G, color.B));
            bar.Foreground = accent;
            bar.Background = track;
            bar.Resources["ProgressBarIndicatorForeground"] = accent;
            bar.Resources["ProgressBarIndicatorForegroundPointerOver"] = accent;
            bar.Resources["ProgressBarTrackFill"] = track;
            ApplySelectionBackground();
        }

        public void SetSelected(bool selected)
        {
            if (_selected == selected) return;
            _selected = selected;
            ApplySelectionBackground();
        }

        void ApplySelectionBackground()
        {
            Border.Background = _selected
                ? new SolidColorBrush(UIColor.FromArgb(32, _accentColor.R, _accentColor.G, _accentColor.B))
                : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
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
        title.Text = en ? "Trends" : hant ? "趨勢" : "趋势";
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
    readonly TextBlock pageTitle, generalTitle, appearanceTitle, startupText, languageTitle, aboutText;
    readonly TextBlock dataSectionTitle, legalSectionTitle, aboutTitle;
    readonly TextBlock themeModeLabel, languageModeLabel, legalHint, dataCardTitle;
    readonly TextBlock dataHint;
    readonly TextBlock dataLocationTitle, dataLocationText, dataLocationHint;
    readonly TextBlock monitoringTitle, monitoringHint, dangerTitle, dangerHint, diagnosticsTitle, diagnosticsHint;
    readonly Button exportDataButton, importDataButton, deleteAllDataButton, openDataFolderButton, pauseButton, backupButton, diagnosticsButton;
    readonly Button termsButton;
    readonly Grid rootGrid;
    readonly ScrollViewer scroll;
    readonly Button[] sectionChips = new Button[3];
    readonly FrameworkElement[] sectionAnchors;
    bool updatingTheme;
    bool updatingLanguage;
    LanguageMode _language;
    LanguageMode _languagePreference;
    UIColor _accentColor;
    bool _monitorPaused;
    int _activeSection;

    public event EventHandler<ThemeMode>? ThemeModeChanged;
    public event EventHandler<LanguageMode>? LanguageChanged;
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
            Spacing = 40,
            Padding = new Thickness(28, 6, 28, 28),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var generalPanel = new StackPanel { Spacing = 24 };
        var dataPanel = new StackPanel { Spacing = 24 };
        var aboutPanel = new StackPanel { Spacing = 24 };

        pageTitle = new TextBlock
        {
            FontSize = 30,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 2)
        };

        // General
        generalTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 0)
        };
        p.Children.Add(generalPanel);

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
        generalPanel.Children.Add(CreateSettingsCard(startupToggle));
        ApplyStartupAccent(_accentColor);

        // Appearance
        appearanceTitle = new TextBlock
        {
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        generalPanel.Children.Add(appearanceTitle);

        themeModeLabel = new TextBlock
        {
            FontSize = 13,
            Opacity = .65,
            Margin = new Thickness(2, 0, 0, 0)
        };
        generalPanel.Children.Add(themeModeLabel);
        theme = new ComboBox { Width = 260 };
        theme.SelectionChanged += Theme_SelectionChanged;
        ApplyComboBoxAccent(theme, _accentColor);
        generalPanel.Children.Add(theme);

        // Language
        languageTitle = new TextBlock
        {
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        generalPanel.Children.Add(languageTitle);
        languageModeLabel = new TextBlock
        {
            FontSize = 13,
            Opacity = .65,
            Margin = new Thickness(2, 0, 0, 0)
        };
        generalPanel.Children.Add(languageModeLabel);
        language = new ComboBox { Width = 260 };
        language.SelectionChanged += Language_SelectionChanged;
        ApplyComboBoxAccent(language, _accentColor);
        generalPanel.Children.Add(language);

        // Recording state belongs to General: it changes how the app runs, not where data lives.
        monitoringTitle = new TextBlock { FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        monitoringHint = new TextBlock { FontSize = 12, Opacity = .65, TextWrapping = TextWrapping.Wrap };
        pauseButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        pauseButton.Click += (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty);
        var monitoringActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        monitoringActions.Children.Add(pauseButton);
        var monitoringContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        monitoringContent.Children.Add(monitoringTitle);
        monitoringContent.Children.Add(monitoringHint);
        monitoringContent.Children.Add(monitoringActions);
        generalPanel.Children.Add(CreateSettingsCard(monitoringContent));

        // Data & Privacy
        dataSectionTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        p.Children.Add(dataPanel);

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
        var dataContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        dataCardTitle = new TextBlock
        {
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        dataContent.Children.Add(dataCardTitle);
        dataContent.Children.Add(dataHint);
        dataContent.Children.Add(dataButtons);
        dataPanel.Children.Add(CreateSettingsCard(dataContent));

        // Destructive actions get their own visually separated card.
        dangerTitle = new TextBlock { FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        dangerHint = new TextBlock { FontSize = 12, Opacity = .72, TextWrapping = TextWrapping.Wrap };
        var dangerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        dangerActions.Children.Add(deleteAllDataButton);
        var dangerContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        dangerContent.Children.Add(dangerTitle);
        dangerContent.Children.Add(dangerHint);
        dangerContent.Children.Add(dangerActions);
        dataPanel.Children.Add(CreateDangerCard(dangerContent));

        // Data location and maintenance stay in the data section.
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
        backupButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        backupButton.Click += (_, _) => BackupRequested?.Invoke(this, EventArgs.Empty);
        var maintenanceButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        maintenanceButtons.Children.Add(openDataFolderButton);
        maintenanceButtons.Children.Add(backupButton);
        var locationContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        locationContent.Children.Add(dataLocationTitle);
        locationContent.Children.Add(dataLocationText);
        locationContent.Children.Add(dataLocationHint);
        locationContent.Children.Add(maintenanceButtons);
        dataPanel.Children.Add(CreateSettingsCard(locationContent));

        // About: version, diagnostics, then the legal notices.
        diagnosticsButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        diagnosticsButton.Click += (_, _) => DiagnosticsRequested?.Invoke(this, EventArgs.Empty);

        aboutTitle = new TextBlock
        {
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        p.Children.Add(aboutPanel);

        aboutText = new TextBlock
        {
            FontSize = 12,
            Opacity = .55,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4, 0, 0, 0)
        };

        diagnosticsTitle = new TextBlock { FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        diagnosticsHint = new TextBlock { FontSize = 12, Opacity = .65, TextWrapping = TextWrapping.Wrap };

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

        var aboutContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        aboutContent.Children.Add(aboutText);
        aboutPanel.Children.Add(CreateSettingsCard(aboutContent));

        var diagnosticsContent = new StackPanel { Spacing = 8, Padding = new Thickness(14) };
        diagnosticsContent.Children.Add(diagnosticsTitle);
        diagnosticsContent.Children.Add(diagnosticsHint);
        diagnosticsContent.Children.Add(diagnosticsButton);
        aboutPanel.Children.Add(CreateSettingsCard(diagnosticsContent));

        aboutPanel.Children.Add(CreateSettingsCard(legalContent));

        sectionAnchors = new FrameworkElement[] { generalPanel, dataPanel, aboutPanel };

        // A fixed section switcher replaces the earlier floating header: it stays
        // visible while the body scrolls, so the current section is always obvious
        // and clicking it jumps to the group.
        var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        for (int i = 0; i < sectionChips.Length; i++)
        {
            sectionChips[i] = CreateSectionChip(i);
            chips.Children.Add(sectionChips[i]);
        }

        var header = new StackPanel
        {
            Spacing = 12,
            Padding = new Thickness(28, 24, 28, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        header.Children.Add(pageTitle);
        header.Children.Add(chips);

        scroll = new ScrollViewer
        {
            Content = p,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        scroll.ViewChanged += (_, _) => UpdateSectionChip();
        scroll.SizeChanged += (_, _) => UpdateSectionChip();

        rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(header, 0);
        Grid.SetRow(scroll, 1);
        rootGrid.Children.Add(header);
        rootGrid.Children.Add(scroll);
        Content = rootGrid;

        ActualThemeChanged += (_, _) => UpdateSectionChip();

        SetLanguage(languageMode, LanguageResolver.Resolve(languageMode));
        SetThemeMode(mode);
    }

    private static Border CreateSettingsCard(UIElement content) => new()
    {
        BorderBrush = new SolidColorBrush(UIColor.FromArgb(55, 128, 128, 128)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Child = content,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static Border CreateDangerCard(UIElement content) => new()
    {
        BorderBrush = new SolidColorBrush(UIColor.FromArgb(110, 196, 43, 28)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Background = new SolidColorBrush(UIColor.FromArgb(12, 196, 43, 28)),
        Child = content,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private Button CreateSectionChip(int index)
    {
        var chip = new Button
        {
            Padding = new Thickness(16, 5, 16, 5),
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(1),
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        chip.Click += (_, _) => ScrollToSection(index);
        return chip;
    }

    private void ScrollToSection(int index)
    {
        try
        {
            if (scroll.Content is not UIElement content || sectionAnchors.Length == 0)
                return;

            var target = sectionAnchors[Math.Clamp(index, 0, sectionAnchors.Length - 1)];
            var y = target.TransformToVisual(content).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
            var maxOffset = Math.Max(0, content.ActualSize.Y - scroll.ViewportHeight);
            var wanted = Math.Max(0, y - 8);

            // The last group can never reach the top of the viewport, so jump to the
            // end instead of stopping short and leaving the wrong chip highlighted.
            var destination = index >= sectionAnchors.Length - 1 || wanted > maxOffset - 1
                ? maxOffset
                : wanted;
            scroll.ChangeView(null, destination, null, true);
        }
        catch { }
    }

    private void UpdateSectionChip()
    {
        try
        {
            if (scroll.Content is not UIElement content || sectionAnchors.Length == 0 || scroll.ActualHeight <= 0)
            {
                UpdateChipVisuals(_activeSection);
                return;
            }

            var offset = scroll.VerticalOffset;
            var maxOffset = Math.Max(0, content.ActualSize.Y - scroll.ViewportHeight);
            var active = 0;
            for (int i = 0; i < sectionAnchors.Length; i++)
            {
                var heading = sectionAnchors[i];
                if (heading.ActualHeight <= 0)
                    continue;
                var y = heading.TransformToVisual(content).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
                if (y <= offset + 24)
                    active = i;
            }

            // At the very bottom the last group is the one being read, even though its
            // top edge never crosses the trigger line.
            if (maxOffset > 0 && offset >= maxOffset - 2)
                active = sectionAnchors.Length - 1;

            _activeSection = active;
            UpdateChipVisuals(active);
        }
        catch
        {
            UpdateChipVisuals(_activeSection);
        }
    }

    private void UpdateChipVisuals(int activeIndex)
    {
        for (int i = 0; i < sectionChips.Length; i++)
        {
            var chip = sectionChips[i];
            if (chip is null)
                continue;

            var selected = i == activeIndex;
            chip.Background = selected
                ? new SolidColorBrush(ColorWithAlpha(_accentColor, 32))
                : new SolidColorBrush(UIColor.FromArgb(0, 0, 0, 0));
            chip.BorderBrush = selected
                ? new SolidColorBrush(ColorWithAlpha(_accentColor, 150))
                : new SolidColorBrush(UIColor.FromArgb(45, 128, 128, 128));
            chip.Foreground = selected
                ? new SolidColorBrush(_accentColor)
                : new SolidColorBrush(ActualTheme == ElementTheme.Dark
                    ? UIColor.FromArgb(255, 225, 225, 225)
                    : UIColor.FromArgb(255, 60, 60, 60));
        }
    }

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

    public void ApplyAccent(UIColor color)
    {
        _accentColor = color;
        ApplyStartupAccent(color);
        ApplyComboBoxAccent(theme, color);
        ApplyComboBoxAccent(language, color);
        UpdateChipVisuals(_activeSection);
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
        languageTitle.Text = en ? "Language" : hant ? "語言" : "语言";
        dataSectionTitle.Text = en ? "Data" : hant ? "資料" : "数据";
        themeModeLabel.Text = en ? "Theme" : hant ? "主題" : "主题";
        languageModeLabel.Text = en ? "Display language" : hant ? "顯示語言" : "显示语言";
        dataHint.Text = en
            ? "Export or import local usage history. Importing creates an automatic local safety backup before replacement."
            : hant ? "匯出或匯入本機使用歷史記錄。匯入取代資料前會自動建立本機安全備份。"
            : "导出或导入本机使用历史记录。导入替换数据前会自动创建本地安全备份。";
        dataCardTitle.Text = en ? "Data management" : hant ? "資料管理" : "数据管理";
        monitoringTitle.Text = en ? "Recording" : hant ? "記錄狀態" : "记录状态";
        monitoringHint.Text = en
            ? "Pause the collector to stop accumulating usage time. The Overview header offers the same control."
            : hant ? "暫停後不再累計使用時間，概覽頂部提供相同的控制項。"
            : "暂停后不再累计使用时间，概览顶部提供同样的控制。";
        dangerTitle.Text = en ? "Danger zone" : hant ? "危險操作" : "危险操作";
        dangerHint.Text = en
            ? "Deleting all data permanently removes local usage records and focus history. This cannot be undone."
            : hant ? "刪除所有資料會永久移除本機使用記錄與專注歷史，且無法復原。"
            : "删除全部数据会永久移除本机使用记录与专注历史，且无法撤销。";
        diagnosticsTitle.Text = en ? "Diagnostics" : hant ? "診斷" : "诊断";
        diagnosticsHint.Text = en
            ? "Inspect database size, record count, and collector state when something looks wrong."
            : hant ? "檢查資料庫大小、記錄數量與收集器狀態，用於疑難排解。"
            : "检查数据库大小、记录数量与采集状态，用于排查问题。";
        exportDataButton.Content = en ? "Export data" : hant ? "匯出資料" : "导出数据";
        importDataButton.Content = en ? "Import data" : hant ? "匯入資料" : "导入数据";
        deleteAllDataButton.Content = en ? "Delete all data" : hant ? "刪除所有資料" : "删除全部数据";
        deleteAllDataButton.Background = new SolidColorBrush(UIColor.FromArgb(230, 196, 43, 28));
        deleteAllDataButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        legalSectionTitle.Text = en ? "Terms & Privacy" : hant ? "使用條款與隱私" : "使用条款与隐私";
        legalHint.Text = en ? "Review how ScreenTime RS records, stores, and handles local usage data." : hant ? "查看 ScreenTime RS 如何記錄、儲存及處理本機使用資料。" : "查看 ScreenTime RS 如何记录、存储和处理本机使用数据。";
        termsButton.Content = en ? "View terms and privacy policy" : hant ? "檢視使用條款與隱私權政策" : "查看使用条款与隐私政策";
        aboutTitle.Text = en ? "About" : hant ? "關於" : "关于";
        dataLocationTitle.Text = en ? "Data location" : hant ? "資料位置" : "数据位置";
        dataLocationHint.Text = en
            ? "Usage records, snapshots, and automatic import backups are stored here."
            : hant ? "使用記錄、快照與匯入前的自動備份會儲存在此處。"
            : "使用记录、快照以及导入前的自动备份会存储在这里。";
        openDataFolderButton.Content = en ? "Open data folder" : hant ? "開啟資料夾" : "打开数据文件夹";
        pauseButton.Content = ""; // state is applied by SetMonitoringState
        backupButton.Content = en ? "Create backup" : hant ? "建立備份" : "创建备份";
        diagnosticsButton.Content = en ? "View diagnostics" : hant ? "檢視診斷" : "查看诊断";
        SetMonitoringState(_monitorPaused);
        aboutText.Text = en
            ? "ScreenTime RS · Version 0.9.2 · Rust monitoring core + WinUI 3 / Fluent UI"
            : hant ? "ScreenTime RS · 版本 0.9.2 · Rust 監控核心 + WinUI 3 / Fluent UI"
            : "ScreenTime RS · 版本 0.9.2 · Rust 监控核心 + WinUI 3 / Fluent UI";
        sectionChips[0].Content = generalTitle.Text;
        sectionChips[1].Content = dataSectionTitle.Text;
        sectionChips[2].Content = aboutTitle.Text;
        UpdateSectionChip();

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
                    Text = LegalDocuments.Combined(_language),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                }
            },
            CloseButtonText = en ? "Close" : hant ? "關閉" : "关闭",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
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
