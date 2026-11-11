using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI;

namespace ScreenTimeRS.UI;

/// <summary>
/// Container for the usage views. Statistics and per-app usage answer the same
/// question over the same time range, so they share one top-level entry and are
/// switched with tabs instead of duplicated navigation pages.
/// </summary>
public sealed class UsageShellPage : Page
{
    private readonly TabView _tabs = new()
    {
        IsAddTabButtonVisible = false,
        TabWidthMode = TabViewWidthMode.SizeToContent
    };
    private readonly StatsPage _stats;
    private readonly AppsPage _apps;
    private LanguageMode _language = LanguageMode.SimplifiedChinese;
    private Snapshot? _lastSnapshot;

    public UsageShellPage(StatsPage stats, AppsPage apps)
    {
        _stats = stats;
        _apps = apps;

        _tabs.TabItems.Add(new TabViewItem { Content = stats, IsClosable = false });
        _tabs.TabItems.Add(new TabViewItem { Content = apps, IsClosable = false });
        _tabs.SelectionChanged += (_, _) =>
        {
            UpdateHeaders();
            if (_lastSnapshot is not null)
                ApplySnapshotToCurrentTab(_lastSnapshot);
        };
        Content = _tabs;
        UpdateHeaders();
    }

    public void UpdateSnapshot(Snapshot snapshot)
    {
        _lastSnapshot = snapshot;
        ApplySnapshotToCurrentTab(snapshot);
    }

    private void ApplySnapshotToCurrentTab(Snapshot snapshot)
    {
        if (_tabs.SelectedIndex == 1)
            _apps.UpdateSnapshot(snapshot);
        else
            _stats.UpdateSnapshot(snapshot);
    }

    public void ApplyLanguage(LanguageMode language)
    {
        _language = language;
        _stats.ApplyLanguage(language);
        _apps.ApplyLanguage(language);
        UpdateHeaders();
    }

    public void ApplyAccent(Color color)
    {
        _stats.ApplyAccent(color);
        _apps.ApplyAccent(color);
    }

    private void UpdateHeaders()
    {
        var en = _language == LanguageMode.English;
        var hant = _language == LanguageMode.TraditionalChinese;
        if (_tabs.TabItems.Count < 2) return;
        if (_tabs.TabItems[0] is TabViewItem trend)
            trend.Header = en ? "Trends" : hant ? "趨勢" : "趋势";
        if (_tabs.TabItems[1] is TabViewItem apps)
            apps.Header = en ? "Apps" : hant ? "應用程式" : "应用";
    }
}
