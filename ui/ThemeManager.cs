using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;
using Windows.UI;

namespace ScreenTimeRS.UI;

public static class ThemeManager
{
    public static readonly Color DefaultAccent = Color.FromArgb(255, 79, 140, 255);

    public static readonly Color[] PresetColors =
    {
        Color.FromArgb(255, 79, 140, 255),   // Ocean
        Color.FromArgb(255, 139, 92, 246),  // Violet
        Color.FromArgb(255, 34, 197, 94),   // Emerald
        Color.FromArgb(255, 244, 91, 105),  // Rose
        Color.FromArgb(255, 245, 166, 35),  // Amber
        Color.FromArgb(255, 20, 184, 166),  // Teal
        Color.FromArgb(255, 100, 116, 139)  // Slate
    };

    public static void ApplyAccent(Color accent)
    {
        var resources = Application.Current.Resources;
        resources["ScreenTimeAccentBrush"] = new SolidColorBrush(accent);
        resources["ScreenTimeAccentSubtleBrush"] = new SolidColorBrush(Color.FromArgb(52, accent.R, accent.G, accent.B));

        // Override the standard WinUI accent color resources so Fluent controls
        // that use ThemeResource accent brushes follow the selected color too.
        resources["SystemAccentColor"] = accent;
        resources["SystemAccentColorLight1"] = Mix(accent, Microsoft.UI.Colors.White, .22);
        resources["SystemAccentColorLight2"] = Mix(accent, Microsoft.UI.Colors.White, .42);
        resources["SystemAccentColorLight3"] = Mix(accent, Microsoft.UI.Colors.White, .68);
        resources["SystemAccentColorDark1"] = Mix(accent, Microsoft.UI.Colors.Black, .18);
        resources["SystemAccentColorDark2"] = Mix(accent, Microsoft.UI.Colors.Black, .36);
        resources["SystemAccentColorDark3"] = Mix(accent, Microsoft.UI.Colors.Black, .54);
        var accentBrush = new SolidColorBrush(accent);
        var accentLight = new SolidColorBrush(Mix(accent, Microsoft.UI.Colors.White, .22));
        var accentMid = new SolidColorBrush(Mix(accent, Microsoft.UI.Colors.White, .42));
        var accentDark = new SolidColorBrush(Mix(accent, Microsoft.UI.Colors.Black, .18));

        resources["SystemControlHighlightAccentBrush"] = accentBrush;
        resources["SystemControlHighlightListAccentVeryHighBrush"] = new SolidColorBrush(Color.FromArgb(230, accent.R, accent.G, accent.B));
        resources["SystemControlHighlightListAccentMediumLowBrush"] = new SolidColorBrush(Color.FromArgb(191, accent.R, accent.G, accent.B));
        resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(Color.FromArgb(54, accent.R, accent.G, accent.B));
        resources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(Color.FromArgb(90, accent.R, accent.G, accent.B));
        resources["SystemControlHighlightListAccentHighBrush"] = new SolidColorBrush(Color.FromArgb(145, accent.R, accent.G, accent.B));

        // Modern Fluent/WinUI accent resources used by controls such as
        // CheckBox, ComboBox and other selection states. Updating the
        // SystemAccentColor alone is not sufficient when the control has
        // already materialized its template, so keep the derived brushes in
        // the app resource scope as well.
        resources["AccentFillColorSelectedTextBackgroundBrush"] = accentBrush;
        resources["AccentFillColorDefaultBrush"] = accentMid;
        resources["AccentFillColorSecondaryBrush"] = accentLight;
        resources["AccentFillColorTertiaryBrush"] = new SolidColorBrush(Mix(accent, Microsoft.UI.Colors.White, .62));
        resources["AccentTextFillColorPrimaryBrush"] = accentDark;
        resources["AccentTextFillColorSecondaryBrush"] = accentDark;
        resources["AccentTextFillColorTertiaryBrush"] = accentMid;
        resources["NavigationViewSelectionIndicatorForeground"] = accentBrush;
        resources["NavigationViewItemBackgroundSelected"] = new SolidColorBrush(Color.FromArgb(36, accent.R, accent.G, accent.B));
        resources["NavigationViewItemForegroundSelected"] = accentBrush;
        resources["NavigationViewItemIconForegroundSelected"] = accentBrush;
        resources["ButtonBackgroundPressed"] = new SolidColorBrush(Mix(accent, Microsoft.UI.Colors.Black, .12));
        resources["CheckBoxCheckBackgroundFillOn"] = accentBrush;
        resources["CheckBoxCheckBackgroundFillOnPointerOver"] = accentLight;
        resources["CheckBoxCheckBackgroundFillOnPressed"] = accentDark;
        resources["RadioButtonOuterEllipseCheckedFill"] = accentBrush;
        resources["ToggleSwitchFillOn"] = accentBrush;
        resources["SliderTrackValueFill"] = accentBrush;

        // ComboBox popup/selection resources. Without these the drop-down
        // can keep the Windows default blue accent even when the app accent
        // is a custom color.
        resources["ComboBoxItemPillFillBrush"] = accentBrush;
        resources["ComboBoxItemBackgroundSelected"] = new SolidColorBrush(Color.FromArgb(40, accent.R, accent.G, accent.B));
        resources["ComboBoxItemBackgroundSelectedUnfocused"] = new SolidColorBrush(Color.FromArgb(34, accent.R, accent.G, accent.B));
        resources["ComboBoxItemBackgroundSelectedPointerOver"] = new SolidColorBrush(Color.FromArgb(56, accent.R, accent.G, accent.B));
        resources["ComboBoxItemBackgroundSelectedPressed"] = new SolidColorBrush(Color.FromArgb(72, accent.R, accent.G, accent.B));
        resources["ComboBoxItemForegroundSelected"] = accentDark;
        resources["ComboBoxItemForegroundSelectedPointerOver"] = accentDark;
        resources["ComboBoxSelectedBackground"] = new SolidColorBrush(Color.FromArgb(40, accent.R, accent.G, accent.B));
        resources["ComboBoxSelectedPointerOverBackground"] = new SolidColorBrush(Color.FromArgb(56, accent.R, accent.G, accent.B));
    }

    public static bool IsClose(Color a, Color b)
    {
        var d = Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
        return d < 36;
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static bool TryParseHex(string? value, out Color color)
    {
        color = DefaultAccent;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim().TrimStart('#');
        if (text.Length != 6) return false;
        if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var n)) return false;
        color = Color.FromArgb(255, (byte)(n >> 16), (byte)(n >> 8), (byte)n);
        return true;
    }

    static Color Mix(Color a, Color b, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(255,
            (byte)(a.R + (b.R - a.R) * amount),
            (byte)(a.G + (b.G - a.G) * amount),
            (byte)(a.B + (b.B - a.B) * amount));
    }
}
