using System.IO;
using System.Text.Json;

namespace WindowsClockWidget;

public class ReminderService
{
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
        catch { }
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
            r.IsEnabled &&
            r.DateTime <= now &&
            r.DateTime > now.AddMinutes(-5)   // en fazla 5 dk gecikmeli
        ).ToList();
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
