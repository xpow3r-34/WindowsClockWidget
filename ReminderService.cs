using System.IO;
using System.Text.Json;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace WindowsClockWidget;

public class ReminderService
{
    private static bool _loadErrorReported;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WindowsClockWidget", "reminders.json");

    public List<Reminder> Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<List<Reminder>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch (Exception ex)
        {
            App.Log(ex);
            try
            {
                if (File.Exists(FilePath))
                    File.Copy(FilePath, $"{FilePath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmssfff}.bak");
            }
            catch (Exception backupError)
            {
                App.Log(backupError);
            }
            if (!_loadErrorReported)
            {
                _loadErrorReported = true;
                MessageBox.Show(
                    $"Hatırlatma dosyası okunamadı. Boş listeyle devam edilecek; özgün dosyanın yedeği alınmaya çalışıldı.\n\nAyrıntılar: {App.LogPath}",
                    "WindowsClockWidget - Hatırlatmalar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        return new();
    }

    public void Save(List<Reminder> reminders)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(reminders, Options));
    }

    public List<Reminder> GetDueReminders(List<Reminder> reminders, DateTime now)
    {
        return reminders.Where(r =>
        {
            if (!r.IsEnabled) return false;

            // DateTime etkinliğin saatidir; bildirim, ayarlanan süre kadar önce verilir.
            var notificationTime = r.DateTime.AddMinutes(-Math.Max(0, r.ReminderMinutesBefore));
            return notificationTime <= now && notificationTime > now.AddMinutes(-5);
        }).ToList();
    }

    public List<Reminder> GetRemindersForDate(List<Reminder> reminders, DateTime date)
    {
        return reminders.Where(r =>
            r.DateTime.Year == date.Year &&
            r.DateTime.Month == date.Month &&
            r.DateTime.Day == date.Day
        ).OrderBy(r => r.DateTime).ToList();
    }

    public bool HasRemindersOnDate(List<Reminder> reminders, DateTime date)
    {
        return reminders.Any(r =>
            r.IsEnabled &&
            r.DateTime.Year == date.Year &&
            r.DateTime.Month == date.Month &&
            r.DateTime.Day == date.Day
        );
    }
}
