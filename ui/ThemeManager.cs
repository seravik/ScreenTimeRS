using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ScreenTimeRS.UI;

/// <summary>
/// One accent for the whole app: the classic Windows blue on the light theme
/// and a lighter tint on dark. The app deliberately does NOT follow the Windows
/// personalisation colour; every accent brush the Fluent controls read is
/// overridden here, including the ones resolved inside popups.
/// </summary>
public static class ThemeManager
{
    public static readonly Color LightAccent = Color.FromArgb(255, 0x00, 0x78, 0xD4);
    public static readonly Color DarkAccent = Color.FromArgb(255, 0x4C, 0xC2, 0xFF);
    public static readonly Color DefaultAccent = LightAccent;

    public static Color AccentFor(ElementTheme theme) =>
        theme == ElementTheme.Dark ? DarkAccent : LightAccent;

    /// <summary>Readable foreground for a control filled with <paramref name="accent"/>.</summary>
    public static Color OnAccent(Color accent) =>
        IsLight(accent) ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.White;

    /// <param name="current">The accent for the theme that is active right now.
    /// It is used for the app-level brushes read directly by our own code.</param>
    public static void ApplyAccent(Color current)
    {
        var resources = Application.Current.Resources;
        resources["ScreenTimeAccentBrush"] = new SolidColorBrush(current);
        resources["ScreenTimeAccentSubtleBrush"] = new SolidColorBrush(Color.FromArgb(52, current.R, current.G, current.B));

        WriteAccent(resources, current);

        // ComboBox items and ContentDialog buttons live in a Popup, and the
        // NavigationView indicator is a ThemeResource. Both resolve from theme
        // dictionaries, where a plain application-level key never wins. Each
        // dictionary gets its OWN fixed colour: writing the current accent into
        // all three is what made the light theme show the dark tint after a
        // theme switch. HighContrast is left to Windows.
        foreach (var (key, color) in new[] { ("Light", LightAccent), ("Dark", DarkAccent) })
        {
            if (!resources.ThemeDictionaries.TryGetValue(key, out var existing) || existing is not ResourceDictionary dictionary)
            {
                dictionary = new ResourceDictionary();
                resources.ThemeDictionaries[key] = dictionary;
            }
            WriteAccent(dictionary, color);
        }
    }

    /// <summary>
    /// Writes the same set of keys into any resource scope. Keeping this in one
    /// place is what guarantees that the navigation indicator, the tab
    /// underline, the dropdown pill and the dialog buttons end up identical.
    /// </summary>
    static void WriteAccent(ResourceDictionary resources, Color accent)
    {
        var hover = Mix(accent, Microsoft.UI.Colors.White, .18);
        var pressed = Mix(accent, Microsoft.UI.Colors.White, .34);
        var dark = Mix(accent, Microsoft.UI.Colors.Black, .16);
        var onAccent = OnAccent(accent);

        // Colour resources that framework brushes derive from.
        resources["SystemAccentColor"] = accent;
        resources["SystemAccentColorLight1"] = hover;
        resources["SystemAccentColorLight2"] = pressed;
        resources["SystemAccentColorLight3"] = Mix(accent, Microsoft.UI.Colors.White, .62);
        resources["SystemAccentColorDark1"] = dark;
        resources["SystemAccentColorDark2"] = Mix(accent, Microsoft.UI.Colors.Black, .34);
        resources["SystemAccentColorDark3"] = Mix(accent, Microsoft.UI.Colors.Black, .52);

        var fill = new SolidColorBrush(accent);
        var onFill = new SolidColorBrush(onAccent);

        // Filled accent controls (AccentButtonStyle, ContentDialog default button,
        // ToggleSwitch, CheckBox, Slider, ProgressBar).
        resources["AccentFillColorDefaultBrush"] = fill;
        resources["AccentFillColorSecondaryBrush"] = new SolidColorBrush(hover);
        resources["AccentFillColorTertiaryBrush"] = new SolidColorBrush(pressed);
        resources["AccentFillColorDisabledBrush"] = new SolidColorBrush(Color.FromArgb(90, accent.R, accent.G, accent.B));
        resources["AccentFillColorSelectedTextBackgroundBrush"] = fill;
        resources["AccentTextFillColorPrimaryBrush"] = fill;
        resources["AccentTextFillColorSecondaryBrush"] = fill;
        resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(dark);
        resources["TextOnAccentFillColorPrimaryBrush"] = onFill;
        resources["TextOnAccentFillColorSecondaryBrush"] = onFill;
        resources["TextOnAccentFillColorDisabledBrush"] = onFill;

        // AccentButtonStyle (used by a ContentDialog's default button) reads
        // these keys through ThemeResource. They are StaticResource aliases
        // created when XamlControlsResources loads, so replacing
        // AccentFillColorDefaultBrush afterwards leaves the button holding the
        // original Windows accent brush. These keys have to be replaced too.
        var disabledFill = new SolidColorBrush(Color.FromArgb(90, accent.R, accent.G, accent.B));
        resources["AccentButtonBackground"] = fill;
        resources["AccentButtonBackgroundPointerOver"] = new SolidColorBrush(hover);
        resources["AccentButtonBackgroundPressed"] = new SolidColorBrush(pressed);
        resources["AccentButtonBackgroundDisabled"] = disabledFill;
        resources["AccentButtonForeground"] = onFill;
        resources["AccentButtonForegroundPointerOver"] = onFill;
        resources["AccentButtonForegroundPressed"] = onFill;
        resources["AccentButtonForegroundDisabled"] = onFill;
        resources["AccentButtonBorderBrush"] = fill;
        resources["AccentButtonBorderBrushPointerOver"] = new SolidColorBrush(hover);
        resources["AccentButtonBorderBrushPressed"] = new SolidColorBrush(pressed);
        resources["AccentButtonBorderBrushDisabled"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        // Legacy keys still referenced by some control templates.
        resources["SystemControlHighlightAccentBrush"] = fill;
        resources["SystemControlBackgroundAccentBrush"] = fill;
        resources["SystemControlForegroundAccentBrush"] = fill;
        resources["SystemControlHighlightListAccentHighBrush"] = fill;
        resources["SystemControlHighlightListAccentMediumBrush"] = fill;
        resources["SystemControlHighlightListAccentLowBrush"] = fill;

        // Selection / state indicators.
        resources["NavigationViewSelectionIndicatorForeground"] = fill;
        resources["ComboBoxItemPillFillBrush"] = fill;
        resources["CheckBoxCheckBackgroundFillOn"] = fill;
        resources["CheckBoxCheckBackgroundFillOnPointerOver"] = new SolidColorBrush(hover);
        resources["CheckBoxCheckBackgroundFillOnPressed"] = new SolidColorBrush(dark);
        resources["RadioButtonOuterEllipseCheckedFill"] = fill;
        resources["ToggleSwitchFillOn"] = fill;
        resources["SliderTrackValueFill"] = fill;
        resources["ProgressBarIndicatorForeground"] = fill;
        resources["ProgressBarIndicatorForegroundPointerOver"] = fill;
        resources["ProgressBarTrackFill"] = new SolidColorBrush(Color.FromArgb(40, accent.R, accent.G, accent.B));
    }

    static bool IsLight(Color color) =>
        (color.R * 299 + color.G * 587 + color.B * 114) / 1000 > 140;

    static Color Mix(Color a, Color b, double amount)
    {
        amount = System.Math.Clamp(amount, 0, 1);
        return Color.FromArgb(255,
            (byte)(a.R + (b.R - a.R) * amount),
            (byte)(a.G + (b.G - a.G) * amount),
            (byte)(a.B + (b.B - a.B) * amount));
    }
}
