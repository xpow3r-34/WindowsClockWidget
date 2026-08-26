using System.IO;
using Microsoft.Win32;

namespace WindowsClockWidget;

/// <summary>
/// "Windows ile başlat": HKCU\...\CurrentVersion\Run kaydı ile yönetilir,
/// yönetici izni gerektirmez. Exe taşınırsa yol her açılışta tazelenir.
/// </summary>
public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowsClockWidget";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
        catch { return false; }
    }

    public static void Set(bool enabled)
    {
        if (enabled) Enable();
        else Disable();
    }

    public static void Enable()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe)) return;
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            key?.SetValue(ValueName, $"\"{exe}\"");
        }
        catch { }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch { }
    }
}
