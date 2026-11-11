using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ScreenTimeRS.UI;

/// <summary>
/// Applies the monochrome interface accent: black on the light theme, white on
/// the dark theme. ScreenTime RS no longer offers a colour picker, so this is
/// the only accent source and it still has to refresh the Fluent resources that
/// controls have already materialised.
/// </summary>
public static class ThemeManager
{
    public static readonly Color DefaultAccent = Color.FromArgb(255, 0, 0, 0);

    public static Color AccentFor(ElementTheme theme) =>
        theme == ElementTheme.Dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black;

    public static void ApplyAccent(Color accent)
    {
        var resources = Application.Current.Resources;
        resources["ScreenTimeAccentBrush"] = new SolidColorBrush(accent);
        resources["ScreenTimeAccentSubtleBrush"] = new SolidColorBrush(Color.FromArgb(52, accent.R, accent.G, accent.B));

        // Override the standard WinUI accent color resources so Fluent controls
        // follow the monochrome accent instead of the Windows system colour.
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

        resources["AccentFillColorSelectedTextBackgroundBrush"] = accentBrush;
        resources["AccentFillColorDefaultBrush"] = accentMid;
        resources["AccentFillColorSecondaryBrush"] = accentLight;
        resources["AccentFillColorTertiaryBrush"] = new SolidColorBrush(Mix(accent, Microsoft.UI.Colors.White, .62));
        resources["AccentTextFillColorPrimaryBrush"] = accentDark;
        resources["AccentTextFillColorSecondaryBrush"] = accentDark;
        resources["AccentTextFillColorTertiaryBrush"] = accentMid;
        resources["CheckBoxCheckBackgroundFillOn"] = accentBrush;
        resources["CheckBoxCheckBackgroundFillOnPointerOver"] = accentLight;
        resources["CheckBoxCheckBackgroundFillOnPressed"] = accentDark;
        resources["RadioButtonOuterEllipseCheckedFill"] = accentBrush;
        resources["ToggleSwitchFillOn"] = accentBrush;
        resources["SliderTrackValueFill"] = accentBrush;
        resources["ProgressBarIndicatorForeground"] = accentBrush;
        resources["ProgressBarIndicatorForegroundPointerOver"] = accentBrush;
        resources["ProgressBarTrackFill"] = new SolidColorBrush(Color.FromArgb(40, accent.R, accent.G, accent.B));

    }

    static Color Mix(Color a, Color b, double amount)
    {
        amount = System.Math.Clamp(amount, 0, 1);
        return Color.FromArgb(255,
            (byte)(a.R + (b.R - a.R) * amount),
            (byte)(a.G + (b.G - a.G) * amount),
            (byte)(a.B + (b.B - a.B) * amount));
    }
}
