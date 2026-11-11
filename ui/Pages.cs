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
