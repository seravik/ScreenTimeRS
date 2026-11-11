using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UIColor = Windows.UI.Color;

namespace ScreenTimeRS.UI;

/// <summary>
/// The whole visual language in one place: four font sizes, one card shape,
/// one muted brush. Pages never set a colour beyond these helpers and the
/// accent handed to them by the shell.
/// </summary>
internal static class Ui
{
    public const double PageTitleSize = 28;
    public const double ValueSize = 30;
    public const double BodySize = 13;
    public const double CaptionSize = 12;

    public const double CardGap = 16;

    public static readonly Thickness PagePadding = new(28, 24, 28, 32);

    public static Border Card(UIElement content) => new()
    {
        BorderBrush = new SolidColorBrush(UIColor.FromArgb(30, 128, 128, 128)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(20),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Child = content
    };

    public static TextBlock PageTitle(string text) => new()
    {
        Text = text,
        FontSize = PageTitleSize,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    };

    public static TextBlock Caption(string text) => new()
    {
        Text = text,
        FontSize = CaptionSize,
        Opacity = .62,
        TextWrapping = TextWrapping.Wrap
    };

    public static TextBlock Value(string text) => new()
    {
        Text = text,
        FontSize = ValueSize,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    };

    public static SolidColorBrush Brush(UIColor color) => new(color);

    public static SolidColorBrush Subtle(UIColor color, byte alpha) =>
        new(UIColor.FromArgb(alpha, color.R, color.G, color.B));
}
