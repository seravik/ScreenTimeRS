using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIImage = Microsoft.UI.Xaml.Controls.Image;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace ScreenTimeRS.UI;

public sealed class OverviewPage : Page
{
    public OverviewPage(Snapshot s)
    {
        var root = new ScrollViewer();
        var panel = new StackPanel { Spacing = 18, Padding = new Thickness(28) };
        panel.Children.Add(new TextBlock { Text = "概览", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var cards = new Grid { ColumnSpacing = 14 };
        for (int i=0;i<4;i++) cards.ColumnDefinitions.Add(new ColumnDefinition());
        AddCard(cards,0,"今天",UiHelpers.Format(s.today),"活跃时间"); AddCard(cards,1,"昨天",UiHelpers.Format(s.yesterday),"活跃时间"); AddCard(cards,2,"本周",UiHelpers.Format(s.week),"周一至今天"); AddCard(cards,3,"本月",UiHelpers.Format(s.month),"本月累计");
        panel.Children.Add(cards);
        var status = new InfoBar { IsOpen=true, Severity=s.locked?InfoBarSeverity.Warning:InfoBarSeverity.Success, Title=s.locked?"当前已锁定":"正在记录使用时间", Message=$"当前应用：{(string.IsNullOrWhiteSpace(s.current_app)?"—":UiHelpers.FriendlyName(new AppStat{name=s.current_app}))}    CPU {s.cpu:F1}%    内存 {s.memory_mb} MB" };
        panel.Children.Add(status);
        var trend = new Border { Padding=new Thickness(20), CornerRadius=new CornerRadius(12), Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderBrush=new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness=new Thickness(1) };
        var tp = new StackPanel { Spacing=8 }; tp.Children.Add(new TextBlock{Text="最近 14 天",FontSize=18,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});
        var max=s.daily.Select(d=>d.seconds).DefaultIfEmpty(1).Max();
        foreach (var d in s.daily.TakeLast(14)){var v=d.seconds;tp.Children.Add(new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,Children={new TextBlock{Text=d.label,Width=55},new ProgressBar{Minimum=0,Maximum=Math.Max(1,max),Value=v,Width=420,Height=18}}});}
        trend.Child=tp; panel.Children.Add(trend);
        root.Content=panel; Content=root;
    }
    static void AddCard(Grid g,int col,string title,string value,string sub){var b=new Border{Padding=new Thickness(18),CornerRadius=new CornerRadius(12),BorderBrush=new SolidColorBrush(Microsoft.UI.Colors.Gray),BorderThickness=new Thickness(1)}; var p=new StackPanel{Spacing=5};p.Children.Add(new TextBlock{Text=title,Opacity=.65});p.Children.Add(new TextBlock{Text=value,FontSize=25,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});p.Children.Add(new TextBlock{Text=sub,Opacity=.6,FontSize=12});b.Child=p;Grid.SetColumn(b,col);g.Children.Add(b);}
}

public sealed class AppsPage : Page
{
    private readonly StackPanel _list = new(); private readonly TextBox _search = new(); private readonly Snapshot _snapshot;
    public AppsPage(Snapshot s){_snapshot=s; var root=new StackPanel{Spacing=16,Padding=new Thickness(28)};root.Children.Add(new TextBlock{Text="应用使用时间",FontSize=30,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});_search.PlaceholderText="搜索应用";_search.TextChanged+=(a,b)=>Render();root.Children.Add(_search);root.Children.Add(new ScrollViewer{Content=_list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Content=root;Render();}
    void Render(){_list.Children.Clear();foreach(var a in _snapshot.apps.Where(x=>UiHelpers.FriendlyName(x).Contains(_search.Text??"",StringComparison.OrdinalIgnoreCase))){var row=new Grid{Padding=new Thickness(10),ColumnSpacing=14};row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(46)});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(120)});var img=new WinUIImage{Width=36,Height=36,Stretch=Stretch.Uniform};TryIcon(img,a);Grid.SetColumn(img,0);row.Children.Add(img);var p=new StackPanel();p.Children.Add(new TextBlock{Text=UiHelpers.FriendlyName(a),FontSize=15,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});p.Children.Add(new TextBlock{Text=a.name+(string.IsNullOrWhiteSpace(a.exe_path)?"":" · "+a.exe_path),Opacity=.55,FontSize=11,TextTrimming=TextTrimming.CharacterEllipsis});Grid.SetColumn(p,1);row.Children.Add(p);var right=new StackPanel{HorizontalAlignment=HorizontalAlignment.Right};right.Children.Add(new TextBlock{Text=UiHelpers.Format(a.seconds),FontSize=15});right.Children.Add(new ProgressBar{Value=Math.Min(100,a.seconds*100.0/Math.Max(1,_snapshot.today)),Maximum=100,Width=110});Grid.SetColumn(right,2);row.Children.Add(right);_list.Children.Add(new Border{Child=row,BorderBrush=new SolidColorBrush(Microsoft.UI.Colors.Gray),BorderThickness=new Thickness(0,0,0,1)});}}
    static void TryIcon(WinUIImage image,AppStat a){try{if(!File.Exists(a.exe_path))return;using var icon=Icon.ExtractAssociatedIcon(a.exe_path);if(icon==null)return;var dir=Path.Combine(Path.GetTempPath(),"ScreenTimeRS-icons");Directory.CreateDirectory(dir);var path=Path.Combine(dir,Math.Abs(a.exe_path.GetHashCode())+".png");using var bmp=icon.ToBitmap();bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);image.Source=new BitmapImage(new Uri(path));}catch{}}
}

public sealed class StatsPage : Page
{
    public StatsPage(Snapshot s)
    {
        var p = new StackPanel { Spacing = 18, Padding = new Thickness(28) };
        p.Children.Add(new TextBlock { Text = "统计", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        p.Children.Add(new TextBlock { Text = "实时统计来自后台采集器，每秒更新一次。", Opacity = .65 });

        var summary = new Grid { ColumnSpacing = 12 };
        for (var i = 0; i < 4; i++) summary.ColumnDefinitions.Add(new ColumnDefinition());
        AddSummary(summary, 0, "今天", s.today);
        AddSummary(summary, 1, "昨天", s.yesterday);
        AddSummary(summary, 2, "本周", s.week);
        AddSummary(summary, 3, "本月", s.month);
        p.Children.Add(summary);

        var trend = new Border { Padding = new Thickness(20), CornerRadius = new CornerRadius(12), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var tp = new StackPanel { Spacing = 10 };
        tp.Children.Add(new TextBlock { Text = "最近 14 天", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var max = s.daily.Select(x => x.seconds).DefaultIfEmpty(1).Max();
        foreach (var d in s.daily)
        {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.Children.Add(new TextBlock { Text = d.label, VerticalAlignment = VerticalAlignment.Center });
            var bar = new ProgressBar { Minimum = 0, Maximum = Math.Max(1, max), Value = d.seconds, Height = 16 };
            Grid.SetColumn(bar, 1); row.Children.Add(bar);
            var value = new TextBlock { Text = UiHelpers.Format(d.seconds), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(value, 2); row.Children.Add(value);
            tp.Children.Add(row);
        }
        trend.Child = tp; p.Children.Add(trend);

        var top = new Border { Padding = new Thickness(20), CornerRadius = new CornerRadius(12), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var topPanel = new StackPanel { Spacing = 10 };
        topPanel.Children.Add(new TextBlock { Text = "今日应用排行", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        foreach (var app in s.apps.Take(10))
        {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            row.Children.Add(new TextBlock { Text = UiHelpers.FriendlyName(app), TextTrimming = TextTrimming.CharacterEllipsis });
            var value = new TextBlock { Text = UiHelpers.Format(app.seconds), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(value, 1); row.Children.Add(value);
            topPanel.Children.Add(row);
        }
        if (s.apps.Length == 0) topPanel.Children.Add(new TextBlock { Text = "今天还没有记录到应用使用时间。", Opacity = .65 });
        top.Child = topPanel; p.Children.Add(top);

        Content = new ScrollViewer { Content = p };
    }

    static void AddSummary(Grid g, int col, string title, long seconds)
    {
        var b = new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(12), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray), BorderThickness = new Thickness(1) };
        var p = new StackPanel { Spacing = 5 };
        p.Children.Add(new TextBlock { Text = title, Opacity = .65 });
        p.Children.Add(new TextBlock { Text = UiHelpers.Format(seconds), FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        b.Child = p; Grid.SetColumn(b, col); g.Children.Add(b);
    }
}

public sealed class SettingsPage : Page
{
    public SettingsPage(bool dark){var p=new StackPanel{Spacing=18,Padding=new Thickness(28),MaxWidth=720};p.Children.Add(new TextBlock{Text="设置",FontSize=30,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});p.Children.Add(new TextBlock{Text="常规",FontSize=20,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});p.Children.Add(new CheckBox{Content="登录 Windows 后自动启动",IsChecked=true});p.Children.Add(new CheckBox{Content="后台继续记录使用时间",IsChecked=true});p.Children.Add(new TextBlock{Text="外观",FontSize=20,Margin=new Thickness(0,15,0,0),FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});var combo=new ComboBox{Width=240};combo.Items.Add("跟随系统");combo.Items.Add("浅色");combo.Items.Add("深色");combo.SelectedIndex=dark?2:0;p.Children.Add(combo);p.Children.Add(new TextBlock{Text="关于",FontSize=20,Margin=new Thickness(0,15,0,0),FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});p.Children.Add(new TextBlock{Text="ScreenTime RS\n版本 0.2.0\nRust monitoring core + WinUI 3 / Fluent UI"});Content=new ScrollViewer{Content=p};}
}
