using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FlowDirection = System.Windows.FlowDirection;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace WindowsClockWidget;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _weatherTimer;
    private readonly WeatherService _weather = new();
    private readonly ReminderService _reminderService = new();
    private AppSettings _settings;
    private WeatherInfo? _lastWeather;
    private int _lastChimeHour = -1;
    private int _calYear, _calMonth;
    private List<Reminder> _reminders = new();
    private readonly DispatcherTimer _reminderCheckTimer;

    public MainWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        ApplySettings();

        FooterItem.Header = $"xpow3r 2026  •  v{AppSettings.AppVersion}";

        // Kayıtlı konum varsa orada aç
        if (_settings.LockPosition && !double.IsNaN(_settings.PosX) && !double.IsNaN(_settings.PosY))
        {
            Left = _settings.PosX;
            Top = _settings.PosY;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(_settings.WeatherRefreshMinutes) };
        _weatherTimer.Tick += async (_, _) => await LoadWeatherAsync();

        UpdateClock();
        _lastChimeHour = DateTime.Now.Hour;   // açılışta çalmasın
        _ = LoadWeatherAsync();
        InitCalendar();

        _reminderCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _reminderCheckTimer.Tick += (_, _) => CheckReminders();
        _reminderCheckTimer.Start();
        CheckReminders();

        // Windows bildirim ikonu
        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "")!,
            Visible = true
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (_settings.LockPosition)
        {
            _settings.PosX = Left;
            _settings.PosY = Top;
            _settings.Save();
        }
        _clockTimer?.Stop();
        _weatherTimer?.Stop();
        _reminderCheckTimer?.Stop();
        _notificationTimer?.Stop();
        _chimePlayer?.Dispose();
        _reminderPlayer?.Dispose();
        _trayIcon?.Dispose();
        Application.Current.Shutdown();   // süreç tamamen sonlansın
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        HourText.Text = now.ToString("HH", CultureInfo.InvariantCulture);
        MinuteText.Text = now.ToString("mm", CultureInfo.InvariantCulture);

        // Tarih gösterimi
        if (_settings.ShowDate && _notificationTimer is not { IsEnabled: true })
        {
            var tr = new CultureInfo("tr-TR");
            DateText.Text = now.ToString("dd.MM.yyyy - dddd", tr);
        }

        // Saat başı uyarısı (yeni saate geçiş anında bir kez)
        if (_settings.HourlyChime && now.Minute == 0 && now.Second < 5 && _lastChimeHour != now.Hour)
        {
            _lastChimeHour = now.Hour;
            PlayChime();
        }
    }

    private System.Media.SoundPlayer? _chimePlayer;
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    private void PlayChime()
    {
        try
        {
            var path = _settings.ChimeSoundFile;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", "chimes.wav");
            if (!File.Exists(path)) return;

            _chimePlayer?.Dispose();
            _chimePlayer = new System.Media.SoundPlayer(path);
            _chimePlayer.Play();
        }
        catch { }
    }

    private async Task LoadWeatherAsync()
    {
        try
        {
            DescText.Opacity = 0.5;
            DescText.Text = "yükleniyor…";
            var w = await _weather.GetWeatherAsync(_settings.City);
            _lastWeather = w;

            TempText.Text = $"{w.Temperature:0}°";
            DescText.Text = $"{w.Description} - {_settings.City}";
            DescText.Opacity = 0.75;
            WeatherIcon.Text = WeatherService.IconFor(w.Code).ToString();

            ToolTip = $"Hissedilen {w.Apparent:0}° • Nem %{w.Humidity} • Rüzgâr {w.WindSpeed:0} km/s";
        }
        catch
        {
            _lastWeather = null;
            DescText.Text = "bağlantı yok";
            DescText.Opacity = 0.5;
        }
        finally
        {
            UpdateDetails();
        }
    }

    private void UpdateDetails()
    {
        if (_settings.ShowWeatherDetails && _lastWeather is { } w)
        {
            DetailsText.Text = $"Hissedilen {w.Apparent:0}°   •   Nem %{w.Humidity}   •   Rüzgâr {w.WindSpeed:0} km/s";
            DetailsText.Visibility = Visibility.Visible;
        }
        else
        {
            DetailsText.Visibility = Visibility.Collapsed;
        }
    }

    public void ApplySettings()
    {
        // Arka plan kalıcı olarak tamamen saydam
        WidgetBorder.Background = Brushes.Transparent;

        // 1) ":" ayacı
        ColonText.Visibility = _settings.ShowColon ? Visibility.Visible : Visibility.Collapsed;

        // 2) Saat / dakika stilleri
        ApplyPart(HourText, _settings.HourFontFamily, _settings.HourFontSize, _settings.HourFontWeight, _settings.HourColor);
        ApplyPart(MinuteText, _settings.MinuteFontFamily, _settings.MinuteFontSize, _settings.MinuteFontWeight, _settings.MinuteColor);
        ApplyPart(ColonText, _settings.MinuteFontFamily, _settings.MinuteFontSize, _settings.MinuteFontWeight, _settings.MinuteColor);

        // 3) ÜSTTEN hizalama — gerçek mürekkep (ink) ölçümü:
        // rakam gliflerinin çizim geometrisinden KESİN üst boşluk çıkar,
        // boşluğu küçük olan parça fark kadar aşağı kaydırılır. Varsanım yok.
        double hs = _settings.HourFontSize, ms = _settings.MinuteFontSize;
        double topSpace = 0, bottomSpace = 0;
        try
        {
            var (blH, lineH, inkTopH) = MeasureInk(_settings.HourFontFamily, hs, _settings.HourFontWeight);
            var (blM, lineM, inkTopM) = MeasureInk(_settings.MinuteFontFamily, ms, _settings.MinuteFontWeight);

            double shiftHour = Math.Max(0, inkTopM - inkTopH);
            double shiftMinute = Math.Max(0, inkTopH - inkTopM);
            HourText.RenderTransform = new TranslateTransform(0, shiftHour);
            ColonText.RenderTransform = new TranslateTransform(0, shiftMinute);
            MinuteText.RenderTransform = new TranslateTransform(0, shiftMinute);

            topSpace = Math.Min(inkTopH, inkTopM);
            bottomSpace = Math.Max(lineH - blH, lineM - blM);
        }
        catch
        {
            HourText.RenderTransform = Transform.Identity;
            MinuteText.RenderTransform = Transform.Identity;
            ColonText.RenderTransform = Transform.Identity;
        }
        ClockRow.Margin = new Thickness(0, -topSpace, 0, 0);

        // Tarih bölümü
        DateText.Visibility = _settings.ShowDate ? Visibility.Visible : Visibility.Collapsed;
        if (_settings.ShowDate)
        {
            DateText.FontFamily = new FontFamily(_settings.DateFontFamily);
            DateText.FontSize = _settings.DateFontSize;
            DateText.FontWeight = ParseWeight(_settings.DateFontWeight);
            DateText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(_settings.DateColor)!;

            bool dateAbove = _settings.DatePosition == "Above";
            DockPanel.SetDock(DateText, dateAbove ? Dock.Top : Dock.Bottom);
            DateText.Margin = dateAbove ? new Thickness(0, 2, 0, 0) : new Thickness(0, 0, 0, 2);
            DateText.RenderTransform = new TranslateTransform(_settings.DateOffsetX, -_settings.DateOffsetY);
        }

        // Her zaman üstte (isteğe bağlı)
        Topmost = _settings.TopMost;

        // Hava durumu konumu ve hizalaması
        bool above = _settings.WeatherPosition == "Above";
        DockPanel.SetDock(WeatherPanel, above ? Dock.Top : Dock.Bottom);

        if (above)
        {
            // Hava durumu üstte: rakamlar tam üste çekilir, hava durumu rakamlara yapışır
            ClockRow.Margin = new Thickness(0, -topSpace, 0, 0);
            WeatherPanel.Margin = new Thickness(0, 0, 0, 4);
        }
        else
        {
            // Hava durumu altta: rakamlar üste yapışsın, hava durumu rakam altına yapışsın
            ClockRow.Margin = new Thickness(0, -topSpace, 0, 0);
            WeatherPanel.Margin = new Thickness(0, -Math.Min(bottomSpace, 180), 0, 0);
        }

        // Hava durumu bölümü
        var wBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(_settings.WeatherColor)!;
        TempText.FontSize = _settings.WeatherFontSize;
        TempText.Foreground = wBrush;
        DescText.FontSize = Math.Max(9, _settings.WeatherFontSize * 0.55);
        DescText.Foreground = wBrush;
        DetailsText.FontSize = Math.Max(9, _settings.WeatherFontSize * 0.5);
        DetailsText.Foreground = wBrush;
        WeatherIcon.FontSize = _settings.WeatherFontSize * 1.4;
        WeatherIcon.Foreground = wBrush;
        // İkonun satır kutusu, sıcaklığın satırıyla aynı yüksekliğe kilitlenir
        // (emoji fontunun devasa lead'i paneli şişirip boşluk yaratmasın)
        WeatherIcon.LineHeight = TempText.FontSize * 1.35;
        WeatherIcon.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;

        WeatherPanel.RenderTransform = new TranslateTransform(_settings.WeatherOffsetX, -_settings.WeatherOffsetY);

        // Takvim görünürlüğü
        CalendarPanel.Visibility = _settings.ShowCalendar ? Visibility.Visible : Visibility.Collapsed;
        if (_settings.ShowCalendar)
        {
            _reminders = _reminderService.Load();
            RefreshCalendar();
        }

        UpdateDetails();
    }

    private static Typeface CreateTypeface(string family, string weight)
        => new(new FontFamily(family), FontStyles.Normal, ParseWeight(weight), FontStretches.Normal);

    /// "00" rakamlarının GERÇEK mürekkep üstü konumu + taban çizgisi + satır yüksekliği (px)
    private static (double Baseline, double Height, double InkTop) MeasureInk(string family, double size, string weight)
    {
        var face = CreateTypeface(family, weight);
        var ft = new FormattedText("00", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                   face, size, Brushes.Transparent, 1.0);
        var geo = ft.BuildGeometry(new Point(0, 0));
        return (ft.Baseline, ft.Height, geo.Bounds.Top);
    }

    private static void ApplyPart(TextBlock part, string family, double size, string weight, string color)
    {
        part.FontFamily = new FontFamily(family);
        part.FontSize = size;
        part.FontWeight = ParseWeight(weight);
        part.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
    }

    private static FontWeight ParseWeight(string w) => w switch
    {
        "Thin" => FontWeights.Thin,
        "ExtraLight" => FontWeights.ExtraLight,
        "Light" => FontWeights.Light,
        "Normal" => FontWeights.Normal,
        "Medium" => FontWeights.Medium,
        "SemiBold" => FontWeights.SemiBold,
        "Bold" => FontWeights.Bold,
        _ => FontWeights.Normal
    };

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.LockPosition) return;   // konum sabitse taşıma yok
        DragMove();
    }

    private void ApplyNewSettings(AppSettings s)
    {
        s.PosX = Left;
        s.PosY = Top;
        _settings = s;
        _settings.Save();
        ApplySettings();

        StartupManager.Set(_settings.StartWithWindows);

        _weatherTimer.Interval = TimeSpan.FromMinutes(_settings.WeatherRefreshMinutes);
        _weatherTimer.Start();   // sıfırla
        _ = LoadWeatherAsync();
    }

    private void PreviewSettings(AppSettings s)
    {
        s.PosX = Left;
        s.PosY = Top;
        _settings = s;      // yalnızca görünüm; kaydetme ve hava durumu isteği yok
        ApplySettings();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_settings) { Owner = this };
        win.PreviewRequested += PreviewSettings;          // saydamlık kaydırıcısı (canlı)
        win.ApplyRequested += ApplyNewSettings;           // Uygula: kalıcı + hava durumu yenile
        if (win.ShowDialog() == true)
            ApplyNewSettings(win.Result);                 // Kaydet
    }

    private void Calendar_Click(object sender, RoutedEventArgs e)
    {
        var win = new CalendarWindow(DateTime.Today) { Owner = this };
        win.ShowDialog();
        _reminders = _reminderService.Load();
        if (_settings.ShowCalendar) RefreshCalendar();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();

    // ── Mini Takvim ──────────────────────────────────────────────

    private void InitCalendar()
    {
        var now = DateTime.Now;
        _calYear = now.Year;
        _calMonth = now.Month;
        _reminders = _reminderService.Load();
        BuildCalendarGrid();
        RefreshCalendar();
    }

    private void BuildCalendarGrid()
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(_settings.CalendarColor)!;
        var dimBrush = new SolidColorBrush(brush.Color) { Opacity = 0.45 };
        double fs = 9;

        // Gün isimleri: Pzt ... Paz
        CalDayHeaders.ColumnDefinitions.Clear();
        CalDayHeaders.Children.Clear();
        string[] days = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        for (int c = 0; c < 7; c++)
        {
            CalDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var tb = new TextBlock
            {
                Text = days[c],
                FontSize = fs - 1,
                FontWeight = FontWeights.SemiBold,
                Foreground = dimBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 1)
            };
            Grid.SetColumn(tb, c);
            CalDayHeaders.Children.Add(tb);
        }

        // 6 satır × 7 sütun gün hücresi
        CalDays.RowDefinitions.Clear();
        CalDays.ColumnDefinitions.Clear();
        CalDays.Children.Clear();
        for (int r = 0; r < 6; r++)
            CalDays.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
        for (int c = 0; c < 7; c++)
            CalDays.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

    private void RefreshCalendar()
    {
        var culture = new CultureInfo("tr-TR");
        CalMonthLabel.Text = culture.DateTimeFormat.MonthNames[_calMonth - 1].ToUpper() + " " + _calYear;

        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(_settings.CalendarColor)!;
        var todayBrush = new SolidColorBrush(brush.Color) { Opacity = 0.18 };
        var dimBrush = new SolidColorBrush(brush.Color) { Opacity = 0.4 };
        double fs = 9;

        CalDays.Children.Clear();

        var firstDay = new DateTime(_calYear, _calMonth, 1);
        int startDay = ((int)firstDay.DayOfWeek + 6) % 7; // Pzt=0
        int daysInMonth = DateTime.DaysInMonth(_calYear, _calMonth);
        var today = DateTime.Today;

        int row = 0, col = startDay;
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(_calYear, _calMonth, d);
            bool isToday = date == today;
            bool hasReminder = _reminderService.HasRemindersOnDate(_reminders, date);

            var container = new Grid();

            if (isToday)
            {
                var highlight = new Border
                {
                    Background = todayBrush,
                    CornerRadius = new CornerRadius(9),
                    Width = 18, Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                container.Children.Add(highlight);
            }

            var tb = new TextBlock
            {
                Text = d.ToString(),
                FontSize = fs,
                Foreground = isToday ? brush : dimBrush,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            container.Children.Add(tb);

            if (hasReminder)
            {
                var dot = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB)),
                    Width = 4, Height = 4,
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 1)
                };
                container.Children.Add(dot);
            }

            container.Cursor = System.Windows.Input.Cursors.Hand;
            container.Tag = date;
            container.MouseLeftButtonDown += CalDay_Click;

            Grid.SetRow(container, row);
            Grid.SetColumn(container, col);
            CalDays.Children.Add(container);

            col++;
            if (col >= 7) { col = 0; row++; }
        }

        // Takvim altında hatırlatma detayları
        RefreshReminderDetails();
    }

    private void RefreshReminderDetails()
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(_settings.CalendarColor)!;
        var dimBrush = new SolidColorBrush(brush.Color) { Opacity = 0.55 };
        var monthStart = new DateTime(_calYear, _calMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var today = DateTime.Today;

        var upcoming = _reminders
            .Where(r => r.DateTime.Date >= today && r.DateTime.Date <= monthEnd)
            .OrderBy(r => r.DateTime)
            .Take(4)
            .ToList();

        if (upcoming.Count == 0)
        {
            ReminderDetailsText.Text = "";
            return;
        }

        var lines = upcoming.Select(r =>
        {
            var dayStr = r.DateTime == today ? "Bugün" :
                         r.DateTime.Date == today.AddDays(1) ? "Yarın" :
                         r.DateTime.ToString("dd.MM");
            return $"• {dayStr} {r.DateTime:HH:mm} — {r.Title}";
        });

        ReminderDetailsText.Text = string.Join("\n", lines);
        ReminderDetailsText.Foreground = dimBrush;
    }

    private void CalPrev_Click(object sender, MouseButtonEventArgs e)
    {
        _calMonth--;
        if (_calMonth < 1) { _calMonth = 12; _calYear--; }
        RefreshCalendar();
    }

    private void CalNext_Click(object sender, MouseButtonEventArgs e)
    {
        _calMonth++;
        if (_calMonth > 12) { _calMonth = 1; _calYear++; }
        RefreshCalendar();
    }

    private void CalDay_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is DateTime date)
        {
            var dt = DateTime.Now;
            var r = new Reminder
            {
                DateTime = new DateTime(date.Year, date.Month, date.Day,
                    (dt.Hour + 1) % 24, 0, 0),
                Repeat = RepeatType.None,
                ReminderMinutesBefore = 5,
                IsEnabled = true
            };
            var win = new ReminderWindow(r) { Owner = this };
            if (win.ShowDialog() == true)
            {
                _reminders = _reminderService.Load();
                RefreshCalendar();
            }
        }
    }

    // ── Hatırlatma Kontrol ───────────────────────────────────────

    private System.Media.SoundPlayer? _reminderPlayer;

    private void CheckReminders()
    {
        var now = DateTime.Now;
        var due = _reminderService.GetDueReminders(_reminders, now);
        foreach (var r in due)
        {
            ShowReminderNotification(r);
            r.AdvanceToNext();
        }
        if (due.Count > 0)
            _reminderService.Save(_reminders);
    }

    private void ShowReminderNotification(Reminder r)
    {
        // Ses çal
        try
        {
            var path = r.SoundFile;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", "chimes.wav");
            if (File.Exists(path))
            {
                _reminderPlayer?.Dispose();
                _reminderPlayer = new System.Media.SoundPlayer(path);
                _reminderPlayer.Play();
            }
        }
        catch { }

        // Widget'ta bildirim göster
        ShowNotificationOnWidget($"⏰ {r.Title}");

        // Windows bildirim merkezine gönder
        ShowWindowsNotification(r.Title, r.DateTime);
    }

    private void ShowWindowsNotification(string title, DateTime dt)
    {
        try
        {
            _trayIcon?.ShowBalloonTip(
                8000,
                $"Hatırlatma — {dt:HH:mm}",
                title,
                System.Windows.Forms.ToolTipIcon.Info);
        }
        catch { }
    }

    private DispatcherTimer? _notificationTimer;

    private void ShowNotificationOnWidget(string text)
    {
        _notificationTimer?.Stop();
        DateText.Text = text;
        DateText.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB));
        DateText.FontSize = _settings.DateFontSize;
        DateText.Opacity = 1;

        _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _notificationTimer.Tick += (_, _) =>
        {
            _notificationTimer.Stop();
            DateText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(_settings.DateColor)!;
            DateText.FontSize = _settings.DateFontSize;
            DateText.FontFamily = new FontFamily(_settings.DateFontFamily);
            DateText.FontWeight = ParseWeight(_settings.DateFontWeight);
            UpdateClock();
        };
        _notificationTimer.Start();
    }
}
