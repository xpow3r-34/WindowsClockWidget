using System.IO;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace WindowsClockWidget;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;

    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WindowsClockWidget", "error.log");

    public static string Version =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    protected override void OnStartup(StartupEventArgs e)
    {
        // Tek örnek kilidi
        _singleInstanceMutex = new Mutex(true, @"Local\WindowsClockWidget_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("WindowsClockWidget zaten çalışıyor.\n(Görev yöneticisinden veya sistem tepsisinden kontrol edin)",
                "WindowsClockWidget", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Log(args.Exception);
            MessageBox.Show(
                $"{args.Exception.Message}\n\nAyrıntılar:\n{LogPath}",
                "WindowsClockWidget", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
    }

    private static void Log(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch { }
    }
}
