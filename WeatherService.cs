using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WindowsClockWidget;

public record WeatherInfo(double Temperature, double Apparent, int Humidity,
                          double WindSpeed, int Code, string Description);

public class WeatherService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private const string GeoUrl = "https://geocoding-api.open-meteo.com/v1/search?name={0}&count=1&language=tr&format=json";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast?latitude={1}&longitude={2}&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto";

    public async Task<WeatherInfo> GetWeatherAsync(string city)
    {
        var geo = await Http.GetFromJsonAsync<GeoResponse>(string.Format(GeoUrl, Uri.EscapeDataString(city)));
        var loc = geo?.Results?.FirstOrDefault()
            ?? throw new Exception($"'{city}' bulunamadı.");

        var url = string.Format(ForecastUrl, "", loc.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                loc.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var forecast = await Http.GetFromJsonAsync<ForecastResponse>(url)
            ?? throw new Exception("Hava durumu alınamadı.");
        var c = forecast.Current!;

        return new WeatherInfo(c.Temperature2m, c.ApparentTemperature, c.Humidity2m,
                               c.WindSpeed10m, c.WeatherCode, Describe(c.WeatherCode));
    }

    private static string Describe(int code) => code switch
    {
        0 => "Açık",
        1 => "Az bulutlu",
        2 => "Parçalı bulutlu",
        3 => "Kapalı",
        45 or 48 => "Puslu",
        51 or 53 or 55 => "Çisenti",
        56 or 57 => "Donan çisenti",
        61 or 63 or 65 => "Yağmurlu",
        66 or 67 => "Donan yağmur",
        71 or 73 or 75 => "Karlı",
        77 => "Kar taneleri",
        80 or 81 or 82 => "Sağanak",
        85 or 86 => "Kar sağanağı",
        95 => "Gök gürültülü",
        96 or 99 => "Dolulu fırtına",
        _ => "Bilinmiyor"
    };

    public static char IconFor(int code) => code switch
    {
        0 => '\u2600',              // ☀
        1 or 2 => '\u26C5',         // ⛅
        45 or 48 => '\u2601',       // ☁
        >= 51 and <= 67 => '\u2614',// ☔
        >= 71 and <= 77 => '\u2744',// ❄
        >= 80 and <= 82 => '\u2614',
        >= 85 and <= 86 => '\u2744',
        >= 95 => '\u26A1',          // ⚡
        _ => '\u2601'
    };

    private sealed class GeoResponse
    {
        [JsonPropertyName("results")] public List<GeoResult>? Results { get; set; }
    }
    private sealed class GeoResult
    {
        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }
    private sealed class ForecastResponse
    {
        [JsonPropertyName("current")] public CurrentWeather? Current { get; set; }
    }
    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")] public double Temperature2m { get; set; }
        [JsonPropertyName("apparent_temperature")] public double ApparentTemperature { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public int Humidity2m { get; set; }
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double WindSpeed10m { get; set; }
    }
}
