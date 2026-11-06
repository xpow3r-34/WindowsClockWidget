using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WindowsClockWidget;

public record WeatherInfo(double Temperature, double Apparent, int Humidity,
                          double WindSpeed, int Code, string Description, char? Icon = null);

public class WeatherService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly Dictionary<string, GeoResult> GeoCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, DateTime> RateLimitedUntilUtc = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, (DateTime FetchedAtUtc, WeatherInfo Info)> MetCache = new(StringComparer.OrdinalIgnoreCase);

    private const string GeoUrl = "https://geocoding-api.open-meteo.com/v1/search?name={0}&count=1&language=tr&format=json";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast?latitude={1}&longitude={2}&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto";
    private const string OpenWeatherGeoUrl = "https://api.openweathermap.org/geo/1.0/direct?q={0},TR&limit=1&appid={1}";
    private const string OpenWeatherCurrentUrl = "https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&appid={2}&units=metric&lang=tr";
    private const string WeatherApiCurrentUrl = "https://api.weatherapi.com/v1/current.json?key={0}&q={1}&lang=tr";
    private const string MetForecastUrl = "https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={0}&lon={1}";

    public async Task<WeatherInfo> GetWeatherAsync(AppSettings settings)
    {
        var source = settings.WeatherSource;
        if (source == "MetNorway" && MetCache.TryGetValue(settings.City, out var cached) &&
            DateTime.UtcNow - cached.FetchedAtUtc < TimeSpan.FromHours(3))
            return cached.Info;

        return source switch
        {
            "OpenWeather" => await GetOpenWeatherAsync(settings),
            "WeatherAPI" => await GetWeatherApiAsync(settings),
            "MetNorway" => await GetMetNorwayAsync(settings),
            _ => await GetOpenMeteoAsync(settings.City)
        };
    }

    private static async Task<WeatherInfo> GetOpenMeteoAsync(string city)
    {
        var loc = await GetOpenMeteoLocationAsync(city);
        var url = string.Format(ForecastUrl, "", loc.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                loc.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var forecast = await GetJsonAsync<ForecastResponse>(url, "OpenMeteo");
        var c = forecast.Current ?? throw new InvalidOperationException("Open-Meteo güncel hava durumu verisi döndürmedi.");

        return new WeatherInfo(c.Temperature2m, c.ApparentTemperature, c.Humidity2m,
                               c.WindSpeed10m, c.WeatherCode, Describe(c.WeatherCode), IconFor(c.WeatherCode));
    }

    private static async Task<WeatherInfo> GetOpenWeatherAsync(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.OpenWeatherApiKey))
            throw new InvalidOperationException("OpenWeather kaynağını kullanmak için API anahtarını ayarlara girin.");

        var geoUrl = string.Format(OpenWeatherGeoUrl, Uri.EscapeDataString(settings.City), Uri.EscapeDataString(settings.OpenWeatherApiKey));
        var locations = await GetJsonAsync<List<OpenWeatherGeoResult>>(geoUrl, "OpenWeather");
        var location = locations.FirstOrDefault() ?? throw new InvalidOperationException($"'{settings.City}' OpenWeather konum aramasında bulunamadı.");
        var url = string.Format(OpenWeatherCurrentUrl,
            location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Uri.EscapeDataString(settings.OpenWeatherApiKey));
        var data = await GetJsonAsync<OpenWeatherResponse>(url, "OpenWeather");
        var condition = data.Weather?.FirstOrDefault();
        if (condition is null) throw new InvalidOperationException("OpenWeather hava durumu açıklaması döndürmedi.");
        return new WeatherInfo(data.Main.Temperature, data.Main.FeelsLike, data.Main.Humidity,
            (data.Wind?.Speed ?? 0) * 3.6, condition.Id, condition.Description, IconForOpenWeather(condition.Id));
    }

    private static async Task<WeatherInfo> GetWeatherApiAsync(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.WeatherApiKey))
            throw new InvalidOperationException("WeatherAPI.com kaynağını kullanmak için API anahtarını ayarlara girin.");
        var url = string.Format(WeatherApiCurrentUrl,
            Uri.EscapeDataString(settings.WeatherApiKey), Uri.EscapeDataString(settings.City));
        var data = await GetJsonAsync<WeatherApiResponse>(url, "WeatherAPI");
        var current = data.Current ?? throw new InvalidOperationException("WeatherAPI.com güncel hava durumu verisi döndürmedi.");
        var condition = current.Condition ?? throw new InvalidOperationException("WeatherAPI.com hava durumu açıklaması döndürmedi.");
        return new WeatherInfo(current.TemperatureC, current.FeelsLikeC, current.Humidity,
            current.WindKph, condition.Code, condition.Text, IconForWeatherApi(condition.Code));
    }

    private static async Task<WeatherInfo> GetMetNorwayAsync(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.MetNorwayUserAgent) || !settings.MetNorwayUserAgent.Contains('@'))
            throw new InvalidOperationException("MET Norway için uygulama adınızı ve iletişim e-posta adresinizi içeren bir User-Agent girin.");
        var loc = await GetOpenMeteoLocationAsync(settings.City);
        var url = string.Format(MetForecastUrl,
            loc.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            loc.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var forecast = await GetJsonAsync<MetForecastResponse>(url, "MetNorway", settings.MetNorwayUserAgent);
        var now = DateTimeOffset.UtcNow;
        var entry = forecast.Properties?.Timeseries?
            .OrderBy(x => DateTimeOffset.Parse(x.Time, System.Globalization.CultureInfo.InvariantCulture))
            .FirstOrDefault(x => DateTimeOffset.Parse(x.Time, System.Globalization.CultureInfo.InvariantCulture) >= now)
            ?? forecast.Properties?.Timeseries?.LastOrDefault();
        var details = entry?.Data?.Instant?.Details ?? throw new InvalidOperationException("MET Norway tahminde güncel ölçüm bulunamadı.");
        var symbol = entry.Data.Next1Hours?.Summary?.SymbolCode
            ?? entry.Data.Next6Hours?.Summary?.SymbolCode
            ?? "cloudy";
        var info = new WeatherInfo(details.Temperature, details.ApparentTemperature ?? details.Temperature,
            (int)Math.Round(details.Humidity), details.WindSpeed * 3.6, 0,
            DescribeMetSymbol(symbol), IconForMetSymbol(symbol));
        MetCache[settings.City] = (DateTime.UtcNow, info);
        return info;
    }

    private static async Task<GeoResult> GetOpenMeteoLocationAsync(string city)
    {
        if (GeoCache.TryGetValue(city, out var cached)) return cached;
        var geo = await GetJsonAsync<GeoResponse>(string.Format(GeoUrl, Uri.EscapeDataString(city)), "OpenMeteo");
        var loc = geo.Results?.FirstOrDefault()
            ?? throw new InvalidOperationException($"'{city}' bulunamadı.");
        GeoCache[city] = loc;
        return loc;
    }

    private static async Task<T> GetJsonAsync<T>(string url, string source, string? userAgent = null) where T : class
    {
        if (RateLimitedUntilUtc.TryGetValue(source, out var retryAt) && retryAt > DateTime.UtcNow)
            throw new WeatherRateLimitException(retryAt, source, isRetrySuppressed: true);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(userAgent)) request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        using var response = await Http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var now = DateTime.UtcNow;
            var retryAfter = response.Headers.RetryAfter;
            retryAt = retryAfter?.Date?.UtcDateTime
                ?? now.Add(retryAfter?.Delta ?? TimeSpan.FromMinutes(1));
            if (retryAt <= now) retryAt = now.AddMinutes(1);
            RateLimitedUntilUtc[source] = retryAt;

            throw new WeatherRateLimitException(retryAt, source, isRetrySuppressed: false,
                new HttpRequestException($"{source} 429 Too Many Requests", null, response.StatusCode));
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Hava durumu sunucusu boş yanıt verdi.");
    }

    private static char IconForOpenWeather(int id) => id switch
    {
        >= 200 and < 300 => '\u26A1',
        >= 300 and < 600 => '\u2614',
        >= 600 and < 700 => '\u2744',
        >= 700 and < 800 => '\u2601',
        800 => '\u2600',
        801 => '\u26C5',
        _ => '\u2601'
    };

    private static char IconForWeatherApi(int code) => code switch
    {
        1000 => '\u2600',
        1003 => '\u26C5',
        1006 or 1009 or 1030 or 1135 or 1147 => '\u2601',
        1087 => '\u26A1',
        >= 1063 and <= 1201 => '\u2614',
        >= 1204 and <= 1237 => '\u2744',
        >= 1240 and <= 1264 => '\u2614',
        >= 1273 => '\u26A1',
        _ => '\u2601'
    };

    private static string DescribeMetSymbol(string symbol)
    {
        var code = symbol.ToLowerInvariant();
        if (code.Contains("thunder")) return "Gök gürültülü";
        if (code.Contains("snow")) return "Karlı";
        if (code.Contains("sleet")) return "Karla karışık yağmurlu";
        if (code.Contains("rain")) return "Yağmurlu";
        if (code.Contains("fog")) return "Puslu";
        if (code.Contains("cloud")) return code.Contains("partly") ? "Parçalı bulutlu" : "Bulutlu";
        if (code.Contains("clearsky") || code.Contains("fair")) return "Açık";
        return "Bilinmiyor";
    }

    private static char IconForMetSymbol(string symbol)
    {
        var code = symbol.ToLowerInvariant();
        if (code.Contains("thunder")) return '\u26A1';
        if (code.Contains("snow") || code.Contains("sleet")) return '\u2744';
        if (code.Contains("rain")) return '\u2614';
        if (code.Contains("clearsky") || code.Contains("fair")) return '\u2600';
        if (code.Contains("partly")) return '\u26C5';
        return '\u2601';
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

    private sealed class OpenWeatherGeoResult
    {
        [JsonPropertyName("lat")] public double Latitude { get; set; }
        [JsonPropertyName("lon")] public double Longitude { get; set; }
    }
    private sealed class OpenWeatherResponse
    {
        [JsonPropertyName("main")] public OpenWeatherMain Main { get; set; } = new();
        [JsonPropertyName("weather")] public List<OpenWeatherCondition>? Weather { get; set; }
        [JsonPropertyName("wind")] public OpenWeatherWind? Wind { get; set; }
    }
    private sealed class OpenWeatherMain
    {
        [JsonPropertyName("temp")] public double Temperature { get; set; }
        [JsonPropertyName("feels_like")] public double FeelsLike { get; set; }
        [JsonPropertyName("humidity")] public int Humidity { get; set; }
    }
    private sealed class OpenWeatherCondition
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; } = "";
    }
    private sealed class OpenWeatherWind
    {
        [JsonPropertyName("speed")] public double Speed { get; set; }
    }
    private sealed class WeatherApiResponse
    {
        [JsonPropertyName("current")] public WeatherApiCurrent? Current { get; set; }
    }
    private sealed class WeatherApiCurrent
    {
        [JsonPropertyName("temp_c")] public double TemperatureC { get; set; }
        [JsonPropertyName("feelslike_c")] public double FeelsLikeC { get; set; }
        [JsonPropertyName("humidity")] public int Humidity { get; set; }
        [JsonPropertyName("wind_kph")] public double WindKph { get; set; }
        [JsonPropertyName("condition")] public WeatherApiCondition? Condition { get; set; }
    }
    private sealed class WeatherApiCondition
    {
        [JsonPropertyName("text")] public string Text { get; set; } = "";
        [JsonPropertyName("code")] public int Code { get; set; }
    }
    private sealed class MetForecastResponse
    {
        [JsonPropertyName("properties")] public MetProperties? Properties { get; set; }
    }
    private sealed class MetProperties
    {
        [JsonPropertyName("timeseries")] public List<MetTimeseries>? Timeseries { get; set; }
    }
    private sealed class MetTimeseries
    {
        [JsonPropertyName("time")] public string Time { get; set; } = "";
        [JsonPropertyName("data")] public MetData? Data { get; set; }
    }
    private sealed class MetData
    {
        [JsonPropertyName("instant")] public MetInstant? Instant { get; set; }
        [JsonPropertyName("next_1_hours")] public MetNextHours? Next1Hours { get; set; }
        [JsonPropertyName("next_6_hours")] public MetNextHours? Next6Hours { get; set; }
    }
    private sealed class MetInstant
    {
        [JsonPropertyName("details")] public MetDetails? Details { get; set; }
    }
    private sealed class MetDetails
    {
        [JsonPropertyName("air_temperature")] public double Temperature { get; set; }
        [JsonPropertyName("apparent_air_temperature")] public double? ApparentTemperature { get; set; }
        [JsonPropertyName("relative_humidity")] public double Humidity { get; set; }
        [JsonPropertyName("wind_speed")] public double WindSpeed { get; set; }
    }
    private sealed class MetNextHours
    {
        [JsonPropertyName("summary")] public MetSummary? Summary { get; set; }
    }
    private sealed class MetSummary
    {
        [JsonPropertyName("symbol_code")] public string SymbolCode { get; set; } = "";
    }
}

public sealed class WeatherRateLimitException : Exception
{
    public DateTime RetryAt { get; }
    public string Provider { get; }
    public bool IsRetrySuppressed { get; }

    public WeatherRateLimitException(DateTime retryAt, string source, bool isRetrySuppressed, Exception? innerException = null)
        : base($"{source} servisi istek sınırına ulaştı. Yeniden deneme: {retryAt.ToLocalTime():HH:mm}.", innerException)
    {
        RetryAt = retryAt;
        Provider = source;
        IsRetrySuppressed = isRetrySuppressed;
    }
}
