using System.Text.Json.Serialization;

namespace WindowsClockWidget;

public enum RepeatType
{
    None,
    Daily,
    Weekly,
    Monthly
}

public class Reminder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = "";
    public DateTime DateTime { get; set; } = DateTime.Now.AddHours(1);
    public RepeatType Repeat { get; set; } = RepeatType.None;
    public string SoundFile { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public int ReminderMinutesBefore { get; set; } = 5;

    public Reminder AdvanceToNext()
    {
        if (Repeat == RepeatType.None)
        {
            IsEnabled = false;
            return this;
        }

        DateTime = Repeat switch
        {
            RepeatType.Daily => DateTime.AddDays(1),
            RepeatType.Weekly => DateTime.AddDays(7),
            RepeatType.Monthly => DateTime.AddMonths(1),
            _ => DateTime
        };
        return this;
    }
}
