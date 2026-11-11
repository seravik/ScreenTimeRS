namespace ScreenTimeRS.UI;

/// <summary>
/// All user-facing text, one method per string, three languages per method.
/// Keeping it here means a missing translation is a missing method rather than
/// a silent fallback buried in a ternary.
/// </summary>
internal static class Str
{
    internal static class Common
    {
        public static string Pause(LanguageMode l) => l switch
        {
            LanguageMode.English => "Pause",
            LanguageMode.TraditionalChinese => "暫停",
            _ => "暂停"
        };

        public static string Resume(LanguageMode l) => l switch
        {
            LanguageMode.English => "Resume",
            LanguageMode.TraditionalChinese => "繼續",
            _ => "继续"
        };

        public static string Recording(LanguageMode l) => l switch
        {
            LanguageMode.English => "Recording",
            LanguageMode.TraditionalChinese => "記錄中",
            _ => "记录中"
        };

        public static string Paused(LanguageMode l) => l switch
        {
            LanguageMode.English => "Paused",
            LanguageMode.TraditionalChinese => "已暫停",
            _ => "已暂停"
        };

        public static string Hours(LanguageMode l) => l switch
        {
            LanguageMode.English => "h",
            LanguageMode.TraditionalChinese => "小時",
            _ => "小时"
        };

        public static string Minutes(LanguageMode l) => l switch
        {
            LanguageMode.English => "m",
            LanguageMode.TraditionalChinese => "分鐘",
            _ => "分钟"
        };
    }

    internal static class Usage
    {
        public static string TrendsTab(LanguageMode l) => l switch
        {
            LanguageMode.English => "Trends",
            LanguageMode.TraditionalChinese => "趨勢",
            _ => "趋势"
        };

        public static string AppsTab(LanguageMode l) => l switch
        {
            LanguageMode.English => "Apps",
            LanguageMode.TraditionalChinese => "應用",
            _ => "应用"
        };

        public static string ShareHint(LanguageMode l) => l switch
        {
            LanguageMode.English => "Share is measured against the selected period.",
            LanguageMode.TraditionalChinese => "百分比以所選時間段的總時長為分母。",
            _ => "百分比以所选时间段的总时长为分母。"
        };

        public static string SearchPlaceholder(LanguageMode l) => l switch
        {
            LanguageMode.English => "Search apps",
            LanguageMode.TraditionalChinese => "搜尋應用程式",
            _ => "搜索应用"
        };

        public static string NoUsage(LanguageMode l) => l switch
        {
            LanguageMode.English => "Nothing recorded in this period.",
            LanguageMode.TraditionalChinese => "此時間段沒有記錄。",
            _ => "此时间段没有记录。"
        };

        public static string NoSelection(LanguageMode l) => l switch
        {
            LanguageMode.English => "Select an app to see its daily history.",
            LanguageMode.TraditionalChinese => "選擇應用程式以查看每日明細。",
            _ => "选择应用以查看每日明细。"
        };

        public static string LoadFailed(LanguageMode l) => l switch
        {
            LanguageMode.English => "Could not load this app. Try again.",
            LanguageMode.TraditionalChinese => "無法載入該應用程式，請重試。",
            _ => "无法加载该应用，请重试。"
        };

        public static string MetricLabel(int index, LanguageMode l) => (index, l) switch
        {
            (0, LanguageMode.English) => "Period total",
            (0, LanguageMode.TraditionalChinese) => "期間總計",
            (0, _) => "期间总计",
            (1, LanguageMode.English) => "Daily average",
            (1, LanguageMode.TraditionalChinese) => "日均使用",
            (1, _) => "日均使用",
            (2, LanguageMode.English) => "Active days",
            (2, LanguageMode.TraditionalChinese) => "活躍天數",
            (2, _) => "活跃天数",
            (3, LanguageMode.English) => "Busiest day",
            (3, LanguageMode.TraditionalChinese) => "最高單日",
            _ => "最高单日"
        };

        public static string TrendRange(int index, LanguageMode l) => (index, l) switch
        {
            (0, LanguageMode.English) => "Last 30 days",
            (0, LanguageMode.TraditionalChinese) => "最近 30 天",
            (0, _) => "最近 30 天",
            (1, LanguageMode.English) => "Last 90 days",
            (1, LanguageMode.TraditionalChinese) => "最近 90 天",
            (1, _) => "最近 90 天",
            (2, LanguageMode.English) => "Last 6 months",
            (2, LanguageMode.TraditionalChinese) => "近半年",
            (2, _) => "近半年",
            (3, LanguageMode.English) => "Last year",
            (3, LanguageMode.TraditionalChinese) => "近一年",
            (3, _) => "近一年",
            (_, LanguageMode.English) => "All time",
            (_, LanguageMode.TraditionalChinese) => "全部時間",
            (_, _) => "全部时间"
        };

        public static string AppRange(int index, LanguageMode l) => (index, l) switch
        {
            (0, LanguageMode.English) => "Today",
            (0, LanguageMode.TraditionalChinese) => "今天",
            (0, _) => "今天",
            (1, LanguageMode.English) => "This week",
            (1, LanguageMode.TraditionalChinese) => "本週",
            (1, _) => "本周",
            (2, LanguageMode.English) => "This month",
            (2, LanguageMode.TraditionalChinese) => "本月",
            (2, _) => "本月",
            (3, LanguageMode.English) => "Last 6 months",
            (3, LanguageMode.TraditionalChinese) => "近半年",
            (3, _) => "近半年",
            (4, LanguageMode.English) => "Last year",
            (4, LanguageMode.TraditionalChinese) => "近一年",
            (4, _) => "近一年",
            (5, LanguageMode.English) => "All time",
            (5, LanguageMode.TraditionalChinese) => "全部時間",
            (5, _) => "全部时间",
            (_, LanguageMode.English) => "Selected day",
            (_, LanguageMode.TraditionalChinese) => "指定日期",
            (_, _) => "指定日期"
        };
    }

    internal static class Overview
    {
        public static string Title(LanguageMode l) => l switch
        {
            LanguageMode.English => "Overview",
            LanguageMode.TraditionalChinese => "概覽",
            _ => "概览"
        };

        public static string Today(LanguageMode l) => l switch
        {
            LanguageMode.English => "Today",
            LanguageMode.TraditionalChinese => "今日使用",
            _ => "今日使用"
        };

        public static string CurrentSession(LanguageMode l) => l switch
        {
            LanguageMode.English => "Current session",
            LanguageMode.TraditionalChinese => "目前連續",
            _ => "当前连续"
        };

        public static string LongestSession(LanguageMode l) => l switch
        {
            LanguageMode.English => "Longest today",
            LanguageMode.TraditionalChinese => "今日最長",
            _ => "今日最长"
        };

        public static string Unlocks(LanguageMode l) => l switch
        {
            LanguageMode.English => "Unlocks",
            LanguageMode.TraditionalChinese => "解鎖次數",
            _ => "解锁次数"
        };

        public static string ByHour(LanguageMode l) => l switch
        {
            LanguageMode.English => "Today by hour",
            LanguageMode.TraditionalChinese => "今日按小時",
            _ => "今日按小时"
        };

        public static string TopApps(LanguageMode l) => l switch
        {
            LanguageMode.English => "Top five today",
            LanguageMode.TraditionalChinese => "今日應用前五",
            _ => "今日应用前五"
        };

        public static string NoApps(LanguageMode l) => l switch
        {
            LanguageMode.English => "No application usage recorded today.",
            LanguageMode.TraditionalChinese => "今天尚無應用程式使用記錄。",
            _ => "今天还没有应用使用记录。"
        };
    }
}
