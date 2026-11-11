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
    private readonly TabView _tabs = new()
    {
        IsAddTabButtonVisible = false,
        TabWidthMode = TabViewWidthMode.SizeToContent
    };
    private readonly Func<UIElement> _focusFactory;
    private readonly FocusHistoryPage _history;
    private LanguageMode _language = LanguageMode.SimplifiedChinese;
    private UIElement? _focusContent;

    public FocusShellPage(Func<UIElement> focusFactory, FocusHistoryPage history)
    {
        _focusFactory = focusFactory;
        _history = history;

        _tabs.TabItems.Add(new TabViewItem { IsClosable = false });
        _tabs.TabItems.Add(new TabViewItem { Content = history, IsClosable = false });
        _tabs.SelectionChanged += (_, _) =>
        {
            if (_tabs.SelectedIndex == 0) EnsureFocusContent();
            UpdateHeaders();
        };
        Content = _tabs;
        UpdateHeaders();
    }

    /// <summary>Builds the focus timer on first entry, preserving the lazy
    /// construction that keeps a focus-page failure from breaking the shell.</summary>
    public void Activate()
    {
        _tabs.SelectedIndex = 0;
        EnsureFocusContent();
    }

    public void SelectHistory()
    {
        _tabs.SelectedIndex = 1;
        _history.Refresh();
    }

    public void SelectFocus()
    {
        _tabs.SelectedIndex = 0;
        EnsureFocusContent();
    }

    public void UpdateSnapshot(Snapshot snapshot)
    {
        if (_tabs.SelectedIndex == 0 && _focusContent is FocusPage page)
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
    }

    private void EnsureFocusContent()
    {
        if (_focusContent is not null) return;
        try
        {
            _focusContent = _focusFactory();
            if (_tabs.TabItems.Count > 0 && _tabs.TabItems[0] is TabViewItem timer)
                timer.Content = _focusContent;
            if (_focusContent is FocusPage page)
                page.ApplyLanguage(_language);
        }
        catch
        {
            // GetFocusContent already logs the failure and returns a fallback page.
        }
    }

    private void UpdateHeaders()
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        if (_tabs.TabItems.Count < 2) return;
        if (_tabs.TabItems[0] is TabViewItem timer)
            timer.Header = en ? "Timer" : hant ? "計時" : "计时";
        if (_tabs.TabItems[1] is TabViewItem history)
            history.Header = en ? "History" : hant ? "歷史" : "历史";
    }
}
