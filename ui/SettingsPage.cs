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
/// Settings: four groups, each a heading plus plain cards. No custom drawing, no
/// accent picker — the only stateful controls are the two combos, the startup
/// switch and the pause button.
/// </summary>
public sealed class SettingsPage : Page
{
    const string Version = "0.9.3";

    readonly TextBlock _title;
    readonly TextBlock _appearanceHeading, _themeLabel;
    readonly TextBlock _generalHeading, _startupText, _languageLabel, _recordingHeading, _recordingHint;
    readonly TextBlock _dataHeading, _dataHint, _dangerHeading, _dangerHint, _locationHeading, _locationHint;
    readonly TextBlock _aboutHeading, _aboutText, _diagnosticsHeading, _diagnosticsHint, _termsHeading, _termsHint;
    readonly TextBlock _locationPath;

    readonly ComboBox _theme, _language;
    readonly ToggleSwitch _startup;
    readonly Button _pauseButton, _exportButton, _importButton, _deleteButton, _openFolderButton, _backupButton;
    readonly Button _diagnosticsButton, _termsButton;

    LanguageMode _languageMode = LanguageMode.SimplifiedChinese;
    LanguageMode _languagePreference = LanguageMode.System;
    bool _updatingCombos;
    bool _paused;
    bool _suppressStartupToggle;
    ThemeMode _themeMode = ThemeMode.System;
    UIColor _accent = UIColor.FromArgb(255, 0, 120, 212);

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
        _accent = accentColor;
        _languageMode = LanguageResolver.Resolve(languageMode);
        _languagePreference = languageMode;

        _title = Ui.PageTitle("");

        _appearanceHeading = Heading();
        _themeLabel = Ui.Caption("");
        _theme = new ComboBox { Width = 220 };
        Ui.ApplyComboAccent(_theme, _accent);
        _theme.SelectionChanged += (_, _) =>
        {
            if (_updatingCombos || _theme.SelectedIndex < 0) return;
            ThemeModeChanged?.Invoke(this, (ThemeMode)_theme.SelectedIndex);
        };

        _generalHeading = Heading();
        _startupText = new TextBlock { FontSize = Ui.BodySize, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        _startup = new ToggleSwitch { OnContent = "", OffContent = "", MinWidth = 0 };
        _startup.Toggled += (_, _) =>
        {
            if (_suppressStartupToggle) return;
            SetStartup(_startup.IsOn);
        };
        _suppressStartupToggle = true;
        _startup.IsOn = StartupEnabled();
        _suppressStartupToggle = false;

        var startupRow = new Grid { ColumnSpacing = 16 };
        startupRow.ColumnDefinitions.Add(new ColumnDefinition());
        startupRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_startup, 1);
        startupRow.Children.Add(_startupText);
        startupRow.Children.Add(_startup);

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

        _recordingHeading = SubHeading();
        _recordingHint = Ui.Caption("");
        _pauseButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _pauseButton.Click += (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty);
        var recordingStack = new StackPanel { Spacing = 10 };
        recordingStack.Children.Add(_recordingHeading);
        recordingStack.Children.Add(_recordingHint);
        recordingStack.Children.Add(_pauseButton);

        _dataHeading = Heading();
        _dataHint = Ui.Caption("");
        _exportButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _importButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _exportButton.Click += (_, _) => ExportDataRequested?.Invoke(this, EventArgs.Empty);
        _importButton.Click += (_, _) => ImportDataRequested?.Invoke(this, EventArgs.Empty);
        var dataButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
        dataButtons.Children.Add(_exportButton);
        dataButtons.Children.Add(_importButton);
        var dataStack = new StackPanel { Spacing = 8 };
        dataStack.Children.Add(_dataHint);
        dataStack.Children.Add(dataButtons);

        _dangerHeading = SubHeading();
        _dangerHint = Ui.Caption("");
        _deleteButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _deleteButton.Click += (_, _) => DeleteAllDataRequested?.Invoke(this, EventArgs.Empty);
        var dangerStack = new StackPanel { Spacing = 8, Padding = new Thickness(20) };
        dangerStack.Children.Add(_dangerHeading);
        dangerStack.Children.Add(_dangerHint);
        dangerStack.Children.Add(_deleteButton);
        var dangerCard = new Border
        {
            BorderBrush = new SolidColorBrush(UIColor.FromArgb(110, 196, 43, 28)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(UIColor.FromArgb(10, 196, 43, 28)),
            Child = dangerStack,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _locationHeading = SubHeading();
        _locationHint = Ui.Caption("");
        _locationPath = new TextBlock { FontSize = Ui.CaptionSize, Opacity = .55, IsTextSelectionEnabled = true, TextWrapping = TextWrapping.Wrap };
        _openFolderButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _backupButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _openFolderButton.Click += (_, _) => OpenDataFolder();
        _backupButton.Click += (_, _) => BackupRequested?.Invoke(this, EventArgs.Empty);
        var locationButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
        locationButtons.Children.Add(_openFolderButton);
        locationButtons.Children.Add(_backupButton);
        var locationStack = new StackPanel { Spacing = 8 };
        locationStack.Children.Add(_locationHeading);
        locationStack.Children.Add(_locationHint);
        locationStack.Children.Add(_locationPath);
        locationStack.Children.Add(locationButtons);

        _aboutHeading = Heading();
        _aboutText = new TextBlock { FontSize = Ui.CaptionSize, Opacity = .6 };
        _diagnosticsHeading = SubHeading();
        _diagnosticsHint = Ui.Caption("");
        _diagnosticsButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _diagnosticsButton.Click += (_, _) => DiagnosticsRequested?.Invoke(this, EventArgs.Empty);
        var diagnosticsStack = new StackPanel { Spacing = 8 };
        diagnosticsStack.Children.Add(_diagnosticsHeading);
        diagnosticsStack.Children.Add(_diagnosticsHint);
        diagnosticsStack.Children.Add(_diagnosticsButton);

        _termsHeading = SubHeading();
        _termsHint = Ui.Caption("");
        _termsButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _termsButton.Click += async (_, _) => await ShowTermsAsync();
        var termsStack = new StackPanel { Spacing = 8 };
        termsStack.Children.Add(_termsHeading);
        termsStack.Children.Add(_termsHint);
        termsStack.Children.Add(_termsButton);

        _locationPath.Text = GetDataDirectory();

        var root = new StackPanel { Spacing = 16, Padding = Ui.PagePadding };
        root.Children.Add(_title);
        root.Children.Add(_appearanceHeading);
        root.Children.Add(Ui.Card(LabeledControl(_themeLabel, _theme)));
        root.Children.Add(_generalHeading);
        root.Children.Add(Ui.Card(startupRow));
        root.Children.Add(Ui.Card(LabeledControl(_languageLabel, _language)));
        root.Children.Add(Ui.Card(recordingStack));
        root.Children.Add(_dataHeading);
        root.Children.Add(Ui.Card(dataStack));
        root.Children.Add(dangerCard);
        root.Children.Add(Ui.Card(locationStack));
        root.Children.Add(_aboutHeading);
        root.Children.Add(Ui.Card(new StackPanel { Spacing = 8, Children = { _aboutText } }));
        root.Children.Add(Ui.Card(diagnosticsStack));
        root.Children.Add(Ui.Card(termsStack));

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

    static TextBlock SubHeading() => new()
    {
        FontSize = Ui.BodySize,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    };

    static StackPanel LabeledControl(TextBlock label, UIElement control)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(label);
        stack.Children.Add(control);
        return stack;
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
        _languageLabel.Text = Str.Settings.DisplayLanguage(effective);
        _recordingHint.Text = Str.Settings.RecordingHint(effective);
        _recordingHeading.Text = Str.Settings.Recording(effective);
        _dataHeading.Text = Str.Settings.Data(effective);
        _dataHint.Text = Str.Settings.DataHint(effective);
        _exportButton.Content = Str.Settings.Export(effective);
        _importButton.Content = Str.Settings.Import(effective);
        _dangerHeading.Text = Str.Settings.Danger(effective);
        _dangerHint.Text = Str.Settings.DangerHint(effective);
        _deleteButton.Content = Str.Settings.DeleteAll(effective);
        _locationHeading.Text = Str.Settings.Location(effective);
        _locationHint.Text = Str.Settings.LocationHint(effective);
        _openFolderButton.Content = Str.Settings.OpenFolder(effective);
        _backupButton.Content = Str.Settings.Backup(effective);
        _aboutHeading.Text = Str.Settings.About(effective);
        _aboutText.Text = Str.Settings.Version(Version, effective);
        _diagnosticsHeading.Text = Str.Settings.Diagnostics(effective);
        _diagnosticsHint.Text = Str.Settings.DiagnosticsHint(effective);
        _diagnosticsButton.Content = Str.Settings.ViewDiagnostics(effective);
        _termsHeading.Text = Str.Settings.Terms(effective);
        _termsHint.Text = Str.Settings.TermsHint(effective);
        _termsButton.Content = Str.Settings.ViewTerms(effective);
        SetMonitoringState(_paused);

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

    public void SetMonitoringState(bool paused)
    {
        _paused = paused;
        _pauseButton.Content = paused ? Str.Common.Resume(_languageMode) : Str.Common.Pause(_languageMode);
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

    static ThemeMode LoadThemeMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\ScreenTimeRS");
            return key?.GetValue("ThemeMode") switch
            {
                string text when string.Equals(text, "light", StringComparison.OrdinalIgnoreCase) => ThemeMode.Light,
                string text when string.Equals(text, "dark", StringComparison.OrdinalIgnoreCase) => ThemeMode.Dark,
                _ => ThemeMode.System
            };
        }
        catch { return ThemeMode.System; }
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

    static string GetDataDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenTimeRS", "ScreenTime RS", "data");

    static void OpenDataFolder()
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
}
