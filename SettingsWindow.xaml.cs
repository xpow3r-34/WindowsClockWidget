using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComboBox = System.Windows.Controls.ComboBox;

namespace WindowsClockWidget;

public partial class SettingsWindow : Window
{
    public AppSettings Result { get; private set; }
    public event Action<AppSettings>? ApplyRequested;    // Uygula butonu
    public event Action<AppSettings>? PreviewRequested;  // canlı önizleme (renk seçici)
    private readonly AppSettings _original;

    private static readonly string[] Cities =
    {
        "Adana", "Adıyaman", "Afyonkarahisar", "Ankara", "Antalya", "Aydın", "Balıkesir",
        "Bursa", "Çanakkale", "Denizli", "Diyarbakır", "Edirne", "Elazığ", "Erzurum",
        "Eskişehir", "Gaziantep", "Hatay", "Isparta", "İstanbul", "İzmir", "Kahramanmaraş",
        "Kayseri", "Kocaeli", "Konya", "Malatya", "Manisa", "Mardin", "Mersin", "Muğla",
        "Muş", "Nevşehir", "Ordu", "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt",
        "Sivas", "Tekirdağ", "Tokat", "Trabzon", "Van", "Yalova", "Zonguldak"
    };

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();

        var fonts = LoadFonts();
        FillCombo(HourFontBox, fonts, current.HourFontFamily);
        FillCombo(MinuteFontBox, fonts, current.MinuteFontFamily);
        FillCombo(DateFontBox, fonts, current.DateFontFamily);
        FillCombo(HourWeightBox, Weights(), current.HourFontWeight);
        FillCombo(MinuteWeightBox, Weights(), current.MinuteFontWeight);
        FillCombo(DateWeightBox, Weights(), current.DateFontWeight);

        InitColorButton(HourColorBtn, current.HourColor);
        InitColorButton(MinuteColorBtn, current.MinuteColor);
        InitColorButton(WeatherColorBtn, current.WeatherColor);
        InitColorButton(DateColorBtn, current.DateColor);

        HourSizeSlider.Value = Math.Clamp(current.HourFontSize, 24, 400);
        MinuteSizeSlider.Value = Math.Clamp(current.MinuteFontSize, 24, 400);
        WeatherSizeSlider.Value = Math.Clamp(current.WeatherFontSize, 10, 60);
        DateSizeSlider.Value = Math.Clamp(current.DateFontSize, 8, 80);
        ColonCheck.IsChecked = current.ShowColon;
        DetailsCheck.IsChecked = current.ShowWeatherDetails;
        TopMostCheck.IsChecked = current.TopMost;
        LockCheck.IsChecked = current.LockPosition;
        AutoStartCheck.IsChecked = current.StartWithWindows;
        ChimeCheck.IsChecked = current.HourlyChime;
        CalendarCheck.IsChecked = current.ShowCalendar;
        InitColorButton(CalendarColorBtn, current.CalendarColor);
        DateCheck.IsChecked = current.ShowDate;
        LoadSounds(current.ChimeSoundFile);

        foreach (var city in Cities) CityBox.Items.Add(city);
        CityBox.SelectedItem = CityBox.Items.Contains(current.City) ? current.City : Cities[0];

        PositionBox.SelectedIndex = current.WeatherPosition == "Above" ? 1 : 0;
        DatePositionBox.SelectedIndex = current.DatePosition == "Below" ? 1 : 0;
        WeatherOffsetXSlider.Value = Math.Clamp(current.WeatherOffsetX, -200, 200);
        WeatherOffsetYSlider.Value = Math.Clamp(current.WeatherOffsetY, -200, 200);
        DateOffsetXSlider.Value = Math.Clamp(current.DateOffsetX, -200, 200);
        DateOffsetYSlider.Value = Math.Clamp(current.DateOffsetY, -200, 200);

        _original = current;
        Result = Clone(current);
        UpdateLabels();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MaxHeight = Math.Max(300, SystemParameters.WorkArea.Height - 40);
    }

    private static List<string> LoadFonts()
    {
        try
        {
            return System.Windows.Media.Fonts.SystemFontFamilies
                .Select(f => f.Source).OrderBy(s => s).ToList();
        }
        catch
        {
            return new List<string> { "Segoe UI", "Segoe UI Light", "Segoe UI Black", "Arial",
                "Calibri", "Consolas", "Courier New", "Georgia", "Tahoma", "Times New Roman", "Verdana" };
        }
    }

    private static List<string> Weights() => new()
    { "Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold" };

    // ---- Windows renk seçici ----
    private static void InitColorButton(System.Windows.Controls.Button btn, string hex)
    {
        btn.Tag = hex;
        btn.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }

    private void PickColorViaWindows(System.Windows.Controls.Button btn)
    {
        using var dlg = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.ColorTranslator.FromHtml(btn.Tag as string ?? "#1F1E1D")
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        string hex = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
        InitColorButton(btn, hex);
        PreviewRequested?.Invoke(BuildResult());   // anında önizle
    }

    private void PickHourColor(object sender, RoutedEventArgs e) => PickColorViaWindows(HourColorBtn);
    private void PickMinuteColor(object sender, RoutedEventArgs e) => PickColorViaWindows(MinuteColorBtn);
    private void PickWeatherColor(object sender, RoutedEventArgs e) => PickColorViaWindows(WeatherColorBtn);
    private void PickDateColor(object sender, RoutedEventArgs e) => PickColorViaWindows(DateColorBtn);
    private void PickCalendarColor(object sender, RoutedEventArgs e) => PickColorViaWindows(CalendarColorBtn);

    private static void FillCombo(ComboBox box, IEnumerable<string> items, string selected)
    {
        foreach (var i in items) box.Items.Add(i);
        box.SelectedItem = box.Items.Contains(selected) ? selected : box.Items[0];
    }

    private static AppSettings Clone(AppSettings s) => new()
    {
        City = s.City,
        HourFontFamily = s.HourFontFamily, HourFontSize = s.HourFontSize,
        HourFontWeight = s.HourFontWeight, HourColor = s.HourColor,
        MinuteFontFamily = s.MinuteFontFamily, MinuteFontSize = s.MinuteFontSize,
        MinuteFontWeight = s.MinuteFontWeight, MinuteColor = s.MinuteColor,
        TextColor = s.TextColor, ShowColon = s.ShowColon,
        WeatherFontSize = s.WeatherFontSize, WeatherColor = s.WeatherColor,
        WeatherPosition = s.WeatherPosition, WeatherAlign = s.WeatherAlign,
        ShowWeatherDetails = s.ShowWeatherDetails,
        TopMost = s.TopMost, LockPosition = s.LockPosition,
        PosX = s.PosX, PosY = s.PosY,
        WeatherRefreshMinutes = s.WeatherRefreshMinutes,
        StartWithWindows = s.StartWithWindows,
        HourlyChime = s.HourlyChime, ChimeSoundFile = s.ChimeSoundFile,
        ShowCalendar = s.ShowCalendar, CalendarColor = s.CalendarColor,
        ShowDate = s.ShowDate, DateFontFamily = s.DateFontFamily,
        DateFontSize = s.DateFontSize, DateFontWeight = s.DateFontWeight,
        DateColor = s.DateColor, DatePosition = s.DatePosition,
        DateOffsetX = s.DateOffsetX, DateOffsetY = s.DateOffsetY,
        WeatherOffsetX = s.WeatherOffsetX, WeatherOffsetY = s.WeatherOffsetY
    };

    private void UpdateLabels()
    {
        if (HourSizeLabel is null || MinuteSizeLabel is null || WeatherSizeLabel is null || DateSizeLabel is null) return;
        HourSizeLabel.Text = $"{HourSizeSlider.Value:0} pt";
        MinuteSizeLabel.Text = $"{MinuteSizeSlider.Value:0} pt";
        WeatherSizeLabel.Text = $"{WeatherSizeSlider.Value:0} pt";
        DateSizeLabel.Text = $"{DateSizeSlider.Value:0} pt";
        WeatherOffsetXLabel.Text = $"{WeatherOffsetXSlider.Value:0} px";
        WeatherOffsetYLabel.Text = $"{WeatherOffsetYSlider.Value:0} px";
        DateOffsetXLabel.Text = $"{DateOffsetXSlider.Value:0} px";
        DateOffsetYLabel.Text = $"{DateOffsetYSlider.Value:0} px";
    }

    private void Any_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateLabels();

    // ---- Windows sesleri ----
    private static string MediaFolder => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");

    private string? SelectedSoundPath => (SoundBox.SelectedItem as ComboBoxItem)?.Tag as string;

    private void LoadSounds(string selectedFile)
    {
        try
        {
            var files = Directory.Exists(MediaFolder)
                ? Directory.GetFiles(MediaFolder, "*.wav")
                : Array.Empty<string>();

            foreach (var f in files.OrderBy(f => System.IO.Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
                SoundBox.Items.Add(new ComboBoxItem
                {
                    Content = System.IO.Path.GetFileName(f),
                    Tag = f
                });

            // kayıtlı dosyayı seç; yoksa chimes.wav, o da yoksa ilkini seç
            var match = SoundBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag as string, selectedFile, StringComparison.OrdinalIgnoreCase))
                ?? SoundBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => "chimes.wav".Equals(i.Content as string, StringComparison.OrdinalIgnoreCase));

            if (match != null) SoundBox.SelectedItem = match;
            else if (SoundBox.Items.Count > 0) SoundBox.SelectedIndex = 0;
        }
        catch { }
    }

    private void PreviewSound_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = SelectedSoundPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            using var player = new System.Media.SoundPlayer(path);
            player.Play();
        }
        catch { }
    }

    private AppSettings BuildResult()
    {
        var r = Clone(_original);
        r.HourFontFamily = HourFontBox.SelectedItem as string ?? "Segoe UI";
        r.HourFontWeight = HourWeightBox.SelectedItem as string ?? "Light";
        r.HourFontSize = HourSizeSlider.Value;
        r.HourColor = HourColorBtn.Tag as string ?? "#1F1E1D";

        r.MinuteFontFamily = MinuteFontBox.SelectedItem as string ?? "Segoe UI";
        r.MinuteFontWeight = MinuteWeightBox.SelectedItem as string ?? "Light";
        r.MinuteFontSize = MinuteSizeSlider.Value;
        r.MinuteColor = MinuteColorBtn.Tag as string ?? "#1F1E1D";

        r.WeatherFontSize = WeatherSizeSlider.Value;
        r.WeatherColor = WeatherColorBtn.Tag as string ?? "#1F1E1D";
        r.ShowColon = ColonCheck.IsChecked == true;
        r.ShowWeatherDetails = DetailsCheck.IsChecked == true;
        r.TopMost = TopMostCheck.IsChecked == true;
        r.LockPosition = LockCheck.IsChecked == true;
        r.StartWithWindows = AutoStartCheck.IsChecked == true;
        r.HourlyChime = ChimeCheck.IsChecked == true;
        r.ChimeSoundFile = SelectedSoundPath ?? "";
        r.ShowCalendar = CalendarCheck.IsChecked == true;
        r.CalendarColor = CalendarColorBtn.Tag as string ?? "#1F1E1D";
        r.ShowDate = DateCheck.IsChecked == true;
        r.DateFontFamily = DateFontBox.SelectedItem as string ?? "Segoe UI";
        r.DateFontWeight = DateWeightBox.SelectedItem as string ?? "Normal";
        r.DateFontSize = DateSizeSlider.Value;
        r.DateColor = DateColorBtn.Tag as string ?? "#1F1E1D";
        r.DatePosition = DatePositionBox.SelectedItem is ComboBoxItem dpi && dpi.Tag as string == "Below"
            ? "Below" : "Above";
        r.DateOffsetX = DateOffsetXSlider.Value;
        r.DateOffsetY = DateOffsetYSlider.Value;
        r.WeatherOffsetX = WeatherOffsetXSlider.Value;
        r.WeatherOffsetY = WeatherOffsetYSlider.Value;
        r.WeatherPosition = PositionBox.SelectedItem is ComboBoxItem pi && pi.Tag as string == "Above"
            ? "Above" : "Below";

        if (CityBox.SelectedItem is string city) r.City = city;
        return r;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        Result = BuildResult();
        ApplyRequested?.Invoke(Result);   // pencere açık kalır, değişiklikler anında görünür
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = BuildResult();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
