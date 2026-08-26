using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;

namespace WindowsClockWidget;

public partial class CalendarWindow : Window
{
    private readonly ReminderService _service = new();
    private List<Reminder> _reminders;
    private int _year, _month;
    private DateTime _selectedDate;

    public CalendarWindow(DateTime? selectDate = null)
    {
        InitializeComponent();
        _selectedDate = selectDate ?? DateTime.Today;
        _year = _selectedDate.Year;
        _month = _selectedDate.Month;
        _reminders = _service.Load();

        BuildHeaders();
        RefreshCalendar();
        UpdateReminderList();
    }

    private void BuildHeaders()
    {
        DayHeaders.ColumnDefinitions.Clear();
        DayHeaders.Children.Clear();
        string[] days = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        var dimBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
        for (int c = 0; c < 7; c++)
        {
            DayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var tb = new TextBlock
            {
                Text = days[c],
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = dimBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };
            Grid.SetColumn(tb, c);
            DayHeaders.Children.Add(tb);
        }
    }

    private void RefreshCalendar()
    {
        var culture = new CultureInfo("tr-TR");
        MonthLabel.Text = culture.DateTimeFormat.MonthNames[_month - 1].ToUpper() + " " + _year;

        var mainBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x1E, 0x1D));
        var dimBrush = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
        var selectedBrush = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB));
        var today = DateTime.Today;

        DayGrid.RowDefinitions.Clear();
        DayGrid.ColumnDefinitions.Clear();
        DayGrid.Children.Clear();

        for (int r = 0; r < 6; r++)
            DayGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        for (int c = 0; c < 7; c++)
            DayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var firstDay = new DateTime(_year, _month, 1);
        int startDay = ((int)firstDay.DayOfWeek + 6) % 7;
        int daysInMonth = DateTime.DaysInMonth(_year, _month);

        int row = 0, col = startDay;
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(_year, _month, d);
            bool isToday = date == today;
            bool isSelected = date == _selectedDate;
            bool hasReminder = _service.HasRemindersOnDate(_reminders, date);

            var container = new Grid { Cursor = Cursors.Hand, Tag = date };

            if (isSelected)
            {
                container.Children.Add(new Border
                {
                    Background = selectedBrush,
                    CornerRadius = new CornerRadius(14),
                    Width = 28, Height = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            else if (isToday)
            {
                container.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x1E, 0x1D)) { Opacity = 0.08 },
                    CornerRadius = new CornerRadius(14),
                    Width = 28, Height = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            var tb = new TextBlock
            {
                Text = d.ToString(),
                FontSize = 12,
                Foreground = isSelected ? Brushes.White : (isToday ? mainBrush : dimBrush),
                FontWeight = isToday || isSelected ? FontWeights.Bold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            container.Children.Add(tb);

            if (hasReminder)
            {
                container.Children.Add(new Border
                {
                    Background = selectedBrush,
                    Width = 4, Height = 4,
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 1)
                });
            }

            container.MouseLeftButtonDown += Day_Click;
            Grid.SetRow(container, row);
            Grid.SetColumn(container, col);
            DayGrid.Children.Add(container);

            col++;
            if (col >= 7) { col = 0; row++; }
        }
    }

    private void UpdateReminderList()
    {
        SelectedDateLabel.Text = _selectedDate.ToString("dd MMMM yyyy, dddd", new CultureInfo("tr-TR"));
        var list = _service.GetRemindersForDate(_reminders, _selectedDate);
        ReminderListBox.ItemsSource = null;
        ReminderListBox.ItemsSource = list;
    }

    private void Day_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is DateTime date)
        {
            _selectedDate = date;
            RefreshCalendar();
            UpdateReminderList();
        }
    }

    private void Prev_Click(object sender, MouseButtonEventArgs e)
    {
        _month--;
        if (_month < 1) { _month = 12; _year--; }
        RefreshCalendar();
    }

    private void Next_Click(object sender, MouseButtonEventArgs e)
    {
        _month++;
        if (_month > 12) { _month = 1; _year++; }
        RefreshCalendar();
    }

    private void AddReminder_Click(object sender, RoutedEventArgs e)
    {
        var dt = DateTime.Now;
        var r = new Reminder
        {
            DateTime = new DateTime(_selectedDate.Year, _selectedDate.Month, _selectedDate.Day,
                (dt.Hour + 1) % 24, 0, 0),
            Repeat = RepeatType.None,
            ReminderMinutesBefore = 5,
            IsEnabled = true
        };
        var win = new ReminderWindow(r) { Owner = this };
        if (win.ShowDialog() == true)
        {
            _reminders = _service.Load();
            _selectedDate = r.DateTime.Date;
            RefreshCalendar();
            UpdateReminderList();
        }
    }

    private void EditReminder_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Reminder r)
        {
            var win = new ReminderWindow(r) { Owner = this };
            if (win.ShowDialog() == true)
            {
                _reminders = _service.Load();
                RefreshCalendar();
                UpdateReminderList();
            }
        }
    }

    private void DeleteReminder_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Reminder r)
        {
            if (MessageBox.Show($"\"{r.Title}\" hatırlatmasını silmek istediğinize emin misiniz?",
                    "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            _reminders.Remove(r);
            _service.Save(_reminders);
            RefreshCalendar();
            UpdateReminderList();
        }
    }

    private void ReminderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Seçim değiştiğinde bir şey yapma — düzenleme sil ile yapılıyor
    }
}
