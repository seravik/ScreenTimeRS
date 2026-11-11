using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI;

namespace ScreenTimeRS.UI;

/// <summary>
/// Container for the focus workflow. The timer and the full history belong to
/// the same task, so history is a tab here instead of a separate page reached
/// through a "view all" button.
/// </summary>
public sealed class FocusShellPage : Page
{
    private readonly SegmentedTabs _tabs = new();
    private readonly ContentControl _focusHost = new();
    private readonly Func<UIElement> _focusFactory;
    private readonly FocusHistoryPage _history;
    private LanguageMode _language = LanguageMode.SimplifiedChinese;
    private UIElement? _focusContent;
    private int _selected;

    public FocusShellPage(Func<UIElement> focusFactory, FocusHistoryPage history)
    {
        _focusFactory = focusFactory;
        _history = history;
        _history.Visibility = Visibility.Collapsed;

        _tabs.AddTab("");
        _tabs.AddTab("");
        _tabs.SelectionChanged += (_, index) =>
        {
            if (index == 0) EnsureFocusContent();
            Show(index);
        };

        var body = new Grid();
        body.Children.Add(_focusHost);
        body.Children.Add(_history);

        var host = new Grid();
        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_tabs, 0);
        Grid.SetRow(body, 1);
        host.Children.Add(_tabs);
        host.Children.Add(body);

        Content = host;

        EnsureFocusContent();
        Show(0);
        UpdateHeaders();
    }

    /// <summary>Builds the focus timer on first entry, preserving the lazy
    /// construction that keeps a focus-page failure from breaking the shell.</summary>
    public void Activate()
    {
        EnsureFocusContent();
        Show(0);
    }

    public void SelectHistory()
    {
        Show(1);
        _history.Refresh();
    }

    public void SelectFocus()
    {
        Show(0);
        EnsureFocusContent();
    }

    public void UpdateSnapshot(Snapshot snapshot)
    {
        if (_selected == 0 && _focusContent is FocusPage page)
            page.UpdateSnapshot(snapshot);
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        if (_focusContent is FocusPage page)
            page.ApplyLanguage(language);
        _history.ApplyLanguage(language);
        UpdateHeaders();
    }

    public void ApplyAccent(Color color)
    {
        if (_focusContent is FocusPage page)
            page.ApplyAccent(color);
        _history.ApplyAccent(color);
        _tabs.SetAccent(color);
    }

    void Show(int index)
    {
        _selected = index;
        _focusHost.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        _history.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    void EnsureFocusContent()
    {
        if (_focusContent is not null) return;
        try
        {
            _focusContent = _focusFactory();
            _focusHost.Content = _focusContent;
            if (_focusContent is FocusPage page)
                page.ApplyLanguage(_language);
        }
        catch
        {
            // GetFocusContent already logs the failure and returns a fallback page.
        }
    }

    void UpdateHeaders()
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        _tabs.SetLabel(0, en ? "Timer" : hant ? "計時" : "计时");
        _tabs.SetLabel(1, en ? "History" : hant ? "歷史" : "历史");
    }
}
