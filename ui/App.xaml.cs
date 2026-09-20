using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace ScreenTimeRS.UI;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += App_UnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            throw;
        }
    }

    private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScreenTimeRS", "ScreenTime RS", "data");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "ui-crash.log");
            File.AppendAllText(path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ScreenTime RS UI crash\n{ex}\n\n");
        }
        catch { }
    }
}
