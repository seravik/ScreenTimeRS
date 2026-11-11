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
