using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsClockWidget;

public class AppSettings
{
    public string City { get; set; } = "İstanbul";

    // Saat (HH)
    public string HourFontFamily { get; set; } = "Segoe UI";
    public double HourFontSize { get; set; } = 76;
    public string HourFontWeight { get; set; } = "Light";
    public string HourColor { get; set; } = "#1F1E1D";

    // Dakika (mm)
    public string MinuteFontFamily { get; set; } = "Segoe UI";
    public double MinuteFontSize { get; set; } = 76;
    public string MinuteFontWeight { get; set; } = "Light";
    public string MinuteColor { get; set; } = "#1F1E1D";

    // Tarih yazı rengi
    public string TextColor { get; set; } = "#1F1E1D";

    // Tarih gösterimi (dd.MM.yyyy - Salı)
    public bool ShowDate { get; set; } = true;
    public string DateFontFamily { get; set; } = "Segoe UI";
    public double DateFontSize { get; set; } = 14;
    public string DateFontWeight { get; set; } = "Normal";
    public string DateColor { get; set; } = "#1F1E1D";
    public string DatePosition { get; set; } = "Above";  // "Above" | "Below"
    public double DateOffsetX { get; set; } = 0;
    public double DateOffsetY { get; set; } = 0;

    // ":" ayacı ve hava durumu
    public bool ShowColon { get; set; } = true;
    public double WeatherFontSize { get; set; } = 20;
    public string WeatherColor { get; set; } = "#1F1E1D";
    public string WeatherPosition { get; set; } = "Below";   // "Above" | "Below"
    public string WeatherAlign { get; set; } = "Left";       // "Left" | "Center" | "Right"
    public bool ShowWeatherDetails { get; set; } = false;
    public double WeatherOffsetX { get; set; } = 0;
    public double WeatherOffsetY { get; set; } = 0;
    public bool TopMost { get; set; } = true;
    public bool LockPosition { get; set; } = false;
    public double PosX { get; set; } = double.NaN;
    public double PosY { get; set; } = double.NaN;

    // Saat başı uyarısı
    public bool HourlyChime { get; set; } = false;
    public string ChimeSoundFile { get; set; } = "";

    // Takvim
    public bool ShowCalendar { get; set; } = false;
    public string CalendarColor { get; set; } = "#1F1E1D";

    // Windows ile başlat
    public bool StartWithWindows { get; set; } = false;

    public int WeatherRefreshMinutes { get; set; } = 15;

    [JsonIgnore]
    public static string AppVersion => "1.2.1";

    [JsonIgnore]
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WindowsClockWidget", "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
    }
}
