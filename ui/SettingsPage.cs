using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using UIColor = Windows.UI.Color;

namespace ScreenTimeRS.UI;

/// <summary>
/// Settings: a Windows-11-style settings page. Each group is a single large
/// card whose rows are separated by hairline dividers, with a label (and an
/// optional description) on the left and the control on the right. Rows share
/// one height and density, so toggles, dropdowns and buttons no longer look
/// mismatched. The danger zone is a red sub-region inside the Data card.
/// </summary>
public sealed class SettingsPage : Page
{
    const string Version = "0.9.3";
    const string RepositoryUrl = "https://github.com/wuchen128/ScreenTimeRS";

    readonly TextBlock _title;
    readonly TextBlock _appearanceHeading, _themeLabel;
    readonly TextBlock _generalHeading, _startupText, _startupHint, _languageLabel;
    readonly TextBlock _dataHeading, _dataLabel, _dataHint, _dangerHeading, _dangerHint;
    readonly TextBlock _aboutHeading, _aboutText, _termsLabel, _termsHint;

    readonly ComboBox _theme, _language;
    readonly ToggleSwitch _startup;
    readonly Button _exportButton, _importButton, _deleteButton, _githubButton, _termsButton;

    LanguageMode _languageMode = LanguageMode.SimplifiedChinese;
    LanguageMode _languagePreference = LanguageMode.System;
    bool _updatingCombos;
    bool _suppressStartupToggle;
    ThemeMode _themeMode = ThemeMode.System;
    UIColor _accent = UIColor.FromArgb(255, 0, 120, 212);

    public event EventHandler<ThemeMode>? ThemeModeChanged;
    public event EventHandler<LanguageMode>? LanguageChanged;
    public event EventHandler? ExportDataRequested;
    public event EventHandler? ImportDataRequested;
    public event EventHandler? DeleteAllDataRequested;

    public SettingsPage(ThemeMode mode, LanguageMode languageMode, UIColor accentColor)
    {
        _accent = accentColor;
        _languageMode = LanguageResolver.Resolve(languageMode);
        _languagePreference = languageMode;

        _title = Ui.PageTitle("");

        // ---- Appearance ----
        _appearanceHeading = Heading();
        _themeLabel = Ui.Caption("");
        _theme = new ComboBox { Width = 220 };
        Ui.ApplyComboAccent(_theme, _accent);
        _theme.SelectionChanged += (_, _) =>
        {
            if (_updatingCombos || _theme.SelectedIndex < 0) return;
            ThemeModeChanged?.Invoke(this, (ThemeMode)_theme.SelectedIndex);
        };

        // ---- General ----
        _generalHeading = Heading();
        _startupText = new TextBlock { FontSize = Ui.BodySize, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        _startupHint = Ui.Caption("");
        _startup = new ToggleSwitch { OnContent = "", OffContent = "", MinWidth = 0 };
        _startup.Toggled += (_, _) =>
        {
            if (_suppressStartupToggle) return;
            SetStartup(_startup.IsOn);
        };
        _suppressStartupToggle = true;
        _startup.IsOn = StartupEnabled();
        _suppressStartupToggle = false;

        _languageLabel = Ui.Caption("");
        _language = new ComboBox { Width = 220 };
        Ui.ApplyComboAccent(_language, _accent);
        _language.SelectionChanged += (_, _) =>
        {
            if (_updatingCombos || _language.SelectedIndex < 0) return;
            _languagePreference = (LanguageMode)_language.SelectedIndex;
            _languageMode = LanguageResolver.Resolve(_languagePreference);
            LanguageChanged?.Invoke(this, _languagePreference);
        };

        // ---- Data ----
        _dataHeading = Heading();
        _dataLabel = Ui.Caption("");
        _dataHint = Ui.Caption("");
        _exportButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _importButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _exportButton.Click += (_, _) => ExportDataRequested?.Invoke(this, EventArgs.Empty);
        _importButton.Click += (_, _) => ImportDataRequested?.Invoke(this, EventArgs.Empty);
        var dataButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        dataButtons.Children.Add(_exportButton);
        dataButtons.Children.Add(_importButton);

        _dangerHeading = new TextBlock
        {
            FontSize = Ui.BodySize,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(UIColor.FromArgb(255, 196, 43, 28))
        };
        _dangerHint = new TextBlock
        {
            FontSize = Ui.CaptionSize,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(UIColor.FromArgb(255, 196, 43, 28)),
            Opacity = .8
        };
        _deleteButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        StyleDeleteButton(_deleteButton);
        _deleteButton.Click += (_, _) => DeleteAllDataRequested?.Invoke(this, EventArgs.Empty);

        // ---- About ----
        _aboutHeading = Heading();
        _aboutText = new TextBlock { FontSize = Ui.CaptionSize, Opacity = .6 };
        _githubButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _githubButton.Click += (_, _) => OpenRepository();

        _termsLabel = Ui.Caption("");
        _termsHint = Ui.Caption("");
        _termsButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _termsButton.Click += async (_, _) => await ShowTermsAsync();

        // ---- Assemble groups (Win11 settings style: one card per group) ----
        var root = new StackPanel { Spacing = 16, Padding = Ui.PagePadding };
        root.Children.Add(_title);

        // Appearance group
        root.Children.Add(_appearanceHeading);
        root.Children.Add(GroupCard(
            Row(_themeLabel, _theme)));

        // General group
        root.Children.Add(_generalHeading);
        root.Children.Add(GroupCard(
            Row(_startupText, _startup, _startupHint),
            Row(_languageLabel, _language)));

        // Data group
        root.Children.Add(_dataHeading);
        root.Children.Add(GroupCard(
            Row(_dataLabel, dataButtons, _dataHint),
            DangerBlock()));

        // About group
        root.Children.Add(_aboutHeading);
        root.Children.Add(GroupCard(
            Row(_aboutText, _githubButton),
            Row(_termsLabel, _termsButton, _termsHint)));

        Content = new ScrollViewer
        {
            Content = root,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        SetLanguage(languageMode, LanguageResolver.Resolve(languageMode));
        SetThemeMode(mode);
    }

    static TextBlock Heading() => new()
    {
        FontSize = 20,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        Margin = new Thickness(0, 16, 0, 0)
    };

    /// <summary>A large rounded card that holds one or more rows (or blocks).</summary>
    static Border GroupCard(params UIElement[] children)
    {
        var stack = new StackPanel { Spacing = 0 };
        for (int i = 0; i < children.Length; i++)
        {
            if (i > 0) stack.Children.Add(Divider());
            stack.Children.Add(children[i]);
        }
        return new Border
        {
            BorderBrush = new SolidColorBrush(UIColor.FromArgb(30, 128, 128, 128)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = stack
        };
    }

    /// <summary>
    /// One settings row: a label column on the left (title plus an optional
    /// description below it) and a control pinned to the right edge. Both the
    /// label and the control are vertically centred for a consistent row height.
    /// </summary>
    static Grid Row(TextBlock label, FrameworkElement control, TextBlock? hint = null)
    {
        var left = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(label);
        if (hint != null) left.Children.Add(hint);

        control.VerticalAlignment = VerticalAlignment.Center;
        control.HorizontalAlignment = HorizontalAlignment.Right;

        var grid = new Grid { Padding = new Thickness(20, 16, 20, 16), ColumnSpacing = 24 };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(control, 1);
        grid.Children.Add(left);
        grid.Children.Add(control);
        return grid;
    }

    /// <summary>A hairline divider between rows inside a group card.</summary>
    static Border Divider() => new()
    {
        Height = 1,
        Background = new SolidColorBrush(UIColor.FromArgb(24, 128, 128, 128)),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Margin = new Thickness(20, 0, 20, 0)
    };

    /// <summary>The danger zone: a red-tinted sub-region inside the Data card.</summary>
    Border DangerBlock()
    {
        var text = new StackPanel { Spacing = 4 };
        text.Children.Add(_dangerHeading);
        text.Children.Add(_dangerHint);

        var grid = new Grid { Padding = new Thickness(20, 16, 20, 16), ColumnSpacing = 24 };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(text, 0);
        Grid.SetColumn(_deleteButton, 1);
        _deleteButton.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(text);
        grid.Children.Add(_deleteButton);

        return new Border
        {
            Background = new SolidColorBrush(UIColor.FromArgb(10, 196, 43, 28)),
            Child = grid,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    public void SetLanguage(LanguageMode preference, LanguageMode effective)
    {
        _languagePreference = preference;
        _languageMode = effective;

        _title.Text = Str.Settings.Title(effective);
        _appearanceHeading.Text = Str.Settings.Appearance(effective);
        _themeLabel.Text = Str.Settings.Theme(effective);
        _generalHeading.Text = Str.Settings.General(effective);
        _startupText.Text = Str.Settings.Startup(effective);
        _startupHint.Text = Str.Settings.StartupHint(effective);
        _languageLabel.Text = Str.Settings.DisplayLanguage(effective);
        _dataHeading.Text = Str.Settings.Data(effective);
        _dataLabel.Text = Str.Settings.DataLabel(effective);
        _dataHint.Text = Str.Settings.DataHint(effective);
        _exportButton.Content = Str.Settings.Export(effective);
        _importButton.Content = Str.Settings.Import(effective);
        _dangerHeading.Text = Str.Settings.Danger(effective);
        _dangerHint.Text = Str.Settings.DangerHint(effective);
        _deleteButton.Content = Str.Settings.DeleteAll(effective);
        _aboutHeading.Text = Str.Settings.About(effective);
        _aboutText.Text = Str.Settings.Version(Version, effective);
        _githubButton.Content = Str.Settings.ViewSource(effective);
        _termsLabel.Text = Str.Settings.Terms(effective);
        _termsHint.Text = Str.Settings.TermsHint(effective);
        _termsButton.Content = Str.Settings.ViewTerms(effective);

        _updatingCombos = true;
        _theme.Items.Clear();
        for (int i = 0; i < 3; i++)
            _theme.Items.Add(Str.Settings.ThemeOption(i, effective));
        _theme.SelectedIndex = (int)_themeMode;

        _language.Items.Clear();
        _language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.System, effective));
        _language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.SimplifiedChinese, effective));
        _language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.TraditionalChinese, effective));
        _language.Items.Add(UiText.LocalizedLanguageName(LanguageMode.English, effective));
        _language.SelectedIndex = (int)preference;
        _updatingCombos = false;
    }

    public void SetThemeMode(ThemeMode mode)
    {
        _themeMode = mode;
        _updatingCombos = true;
        if (_theme.SelectedIndex != (int)mode)
            _theme.SelectedIndex = (int)mode;
        _updatingCombos = false;
    }

    public void ApplyAccent(UIColor color)
    {
        _accent = color;
        Ui.ApplyComboAccent(_theme, color);
        Ui.ApplyComboAccent(_language, color);
    }

    async System.Threading.Tasks.Task ShowTermsAsync()
    {
        var dialog = new ContentDialog
        {
            Title = Str.Settings.Terms(_languageMode),
            Content = new ScrollViewer
            {
                MaxHeight = 520,
                Content = new TextBlock
                {
                    Text = LegalDocuments.Combined(_languageMode),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = Ui.CaptionSize
                }
            },
            CloseButtonText = UiText.Close(_languageMode),
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    static void OpenRepository()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RepositoryUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }

    /// <summary>Paints a button as a solid red danger action, matching the
    /// confirmation dialog's primary button.</summary>
    static void StyleDeleteButton(Button button) => button.Style = Ui.DangerButtonStyle();

    static string StartupCommand()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "screentime-rs.exe");
        return $"\"{exe}\" --background";
    }

    static bool StartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            // A value left over from a previous install path must read as off,
            // otherwise the toggle claims tracking starts with Windows while the
            // stored command points at an executable that no longer exists.
            return key?.GetValue("ScreenTimeRS") is string value
                && string.Equals(value.Trim(), StartupCommand(), StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    static void SetStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (key == null) return;
            if (enabled) key.SetValue("ScreenTimeRS", StartupCommand());
            else key.DeleteValue("ScreenTimeRS", false);
        }
        catch { }
    }
}
