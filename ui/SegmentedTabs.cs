using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using UIColor = Windows.UI.Color;

namespace ScreenTimeRS.UI;

/// <summary>
/// Two or three text tabs drawn by hand. TabView drags its own close button,
/// keyboard accelerators and a "Ctrl+F4" tooltip into the page, none of which a
/// settings-style app needs.
/// </summary>
public sealed class SegmentedTabs : StackPanel
{
    readonly List<(Button Button, TextBlock Label, Border Underline)> _tabs = new();
    int _selected;
    UIColor _accent = UIColor.FromArgb(255, 0, 120, 212);

    public event EventHandler<int>? SelectionChanged;

    public SegmentedTabs()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 24;
        Padding = new Thickness(28, 14, 28, 0);
    }

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            if (value < 0 || value >= _tabs.Count || value == _selected)
                return;
            _selected = value;
            Refresh();
            SelectionChanged?.Invoke(this, value);
        }
    }

    public void AddTab(string text)
    {
        var label = new TextBlock { Text = text, FontSize = Ui.BodySize };
        var underline = new Border
        {
            Height = 2,
            CornerRadius = new CornerRadius(1),
            Margin = new Thickness(0, 6, 0, 0)
        };
        var stack = new StackPanel();
        stack.Children.Add(label);
        stack.Children.Add(underline);
        var button = new Button
        {
            Content = stack,
            Padding = new Thickness(0, 4, 0, 6),
            Background = new SolidColorBrush(UIColor.FromArgb(0, 0, 0, 0)),
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        int index = _tabs.Count;
        button.Click += (_, _) => SelectedIndex = index;
        _tabs.Add((button, label, underline));
        Children.Add(button);
        Refresh();
    }

    public void SetLabel(int index, string text)
    {
        if (index >= 0 && index < _tabs.Count)
            _tabs[index].Label.Text = text;
    }

    public void SetAccent(UIColor accent)
    {
        _accent = accent;
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            var (_, label, underline) = _tabs[i];
            var selected = i == _selected;
            label.FontWeight = selected
                ? Microsoft.UI.Text.FontWeights.SemiBold
                : Microsoft.UI.Text.FontWeights.Normal;
            label.Opacity = selected ? 1 : .6;
            underline.Background = new SolidColorBrush(selected
                ? _accent
                : UIColor.FromArgb(0, 0, 0, 0));
        }
    }
}
