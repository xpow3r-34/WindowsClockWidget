using System.IO;
using System.Windows;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;

namespace WindowsClockWidget;

public partial class ReminderWindow : Window
{
    private readonly ReminderService _service = new();
    private readonly Reminder _reminder;

    public ReminderWindow(Reminder reminder)
    {
        InitializeComponent();
        _reminder = reminder;

        // Saatleri doldur
        for (int h = 0; h < 24; h++)
            HourBox.Items.Add(h.ToString("D2"));

        // Dakikaları doldur (5'er dk)
        for (int m = 0; m < 60; m += 5)
            MinuteBox.Items.Add(m.ToString("D2"));

        // Ses dosyalarını doldur
        LoadSounds();

        // Formu mevcut hatırlatmayla doldur
        TitleBox.Text = _reminder.Title == "Hatırlatma" ? "" : _reminder.Title;
        DatePicker.SelectedDate = _reminder.DateTime.Date;
        HourBox.SelectedIndex = _reminder.DateTime.Hour;
        MinuteBox.SelectedIndex = _reminder.DateTime.Minute / 5;

        // Tekrar
        foreach (ComboBoxItem item in RepeatBox.Items)
        {
            if (item.Tag as string == _reminder.Repeat.ToString())
            {
                RepeatBox.SelectedItem = item;
                break;
            }
        }

        // Ses
        if (!string.IsNullOrEmpty(_reminder.SoundFile))
        {
            foreach (ComboBoxItem item in SoundBox.Items)
            {
                if (string.Equals(item.Tag as string, _reminder.SoundFile, StringComparison.OrdinalIgnoreCase))
                {
                    SoundBox.SelectedItem = item;
                    break;
                }
            }
        }

        // Hatırlatma zamanı
        foreach (ComboBoxItem item in BeforeBox.Items)
        {
            if (item.Tag as string == _reminder.ReminderMinutesBefore.ToString())
            {
                BeforeBox.SelectedItem = item;
                break;
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Başlık
        _reminder.Title = string.IsNullOrWhiteSpace(TitleBox.Text)
            ? "Hatırlatma"
            : TitleBox.Text.Trim();

        // Tarih + saat
        var date = DatePicker.SelectedDate ?? DateTime.Now;
        int hour = HourBox.SelectedIndex >= 0 ? HourBox.SelectedIndex : 0;
        int minute = MinuteBox.SelectedIndex >= 0 ? MinuteBox.SelectedIndex * 5 : 0;
        _reminder.DateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

        // Tekrar
        if (RepeatBox.SelectedItem is ComboBoxItem ri)
            _reminder.Repeat = Enum.TryParse<RepeatType>(ri.Tag as string, out var rt) ? rt : RepeatType.None;

        // Ses
        _reminder.SoundFile = (SoundBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "";

        // Hatırlatma zamanı
        if (BeforeBox.SelectedItem is ComboBoxItem bi)
            _reminder.ReminderMinutesBefore = int.TryParse(bi.Tag as string, out var bm) ? bm : 5;

        _reminder.IsEnabled = true;

        // Tüm listeyi yükle, bu hatırlatmayı ekle/güncelle, kaydet
        var all = _service.Load();
        var idx = all.FindIndex(r => r.Id == _reminder.Id);
        if (idx >= 0)
            all[idx] = _reminder;
        else
            all.Add(_reminder);
        _service.Save(all);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LoadSounds()
    {
        try
        {
            var mediaFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
            if (!Directory.Exists(mediaFolder)) return;

            foreach (var f in Directory.GetFiles(mediaFolder, "*.wav")
                         .OrderBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
                SoundBox.Items.Add(new ComboBoxItem
                {
                    Content = Path.GetFileName(f),
                    Tag = f
                });

            var match = SoundBox.Items.Cast<ComboBoxItem>()
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
            var path = (SoundBox.SelectedItem as ComboBoxItem)?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            using var player = new System.Media.SoundPlayer(path);
            player.Play();
        }
        catch { }
    }
}
