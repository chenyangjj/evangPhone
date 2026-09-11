using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.UI.Xaml.Controls;
using MicroControl = Microsoft.UI.Xaml.Controls;
using WinText = Windows.UI.Text;

namespace EvangSol.Mobibrary.Platforms.Windows
{
    public class MauiDatePicker : MicroControl.Grid, IDisposable
    {
        PlatformDatePicker datepicker;
        CalendarDatePicker calendar;
        TextBlock textblock;
        DateTime? oldvalue;

        public MauiDatePicker(PlatformDatePicker datepicker) : base()
        {
            this.datepicker = datepicker;

            ColumnDefinitions.Add(new MicroControl.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Pixel) });
            ColumnDefinitions.Add(new MicroControl.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });
            RowDefinitions.Add(new MicroControl.RowDefinition());

            calendar = new CalendarDatePicker();
            calendar.DateChanged += OnDateChanged;
            SetColumn(calendar, 0);
            Children.Add(calendar);

            textblock = new TextBlock
            {
                FontSize = datepicker.setting.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                Foreground = WindowsUtils.ConvertToWinColor(datepicker.setting.TextColor),
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left,
                FontStyle = datepicker.setting.FontAttributes != null && datepicker.setting.FontAttributes == FontAttributes.Italic ? WinText.FontStyle.Italic : WinText.FontStyle.Normal,
                FontWeight = datepicker.setting.FontAttributes !=null && datepicker.setting.FontAttributes == FontAttributes.Bold ? new WinText.FontWeight(0x0260) : new WinText.FontWeight(0x0160),
                Padding = new Microsoft.UI.Xaml.Thickness(
                    datepicker.setting.Padding?.Item1 ?? 12,
                    datepicker.setting.Padding?.Item2 ?? 10,
                    datepicker.setting.Padding?.Item3 ?? 10,
                    datepicker.setting.Padding?.Item4 ?? 8),
            };
            SetColumn(textblock, 1);
            Children.Add(textblock);
        }

        private void OnDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            var newvalue = args.NewDate?.DateTime;
            if (newvalue != oldvalue)
            {
                textblock.Text = newvalue?.ToString(datepicker.format);
                oldvalue = newvalue;
                datepicker.TriggerDataChanged(new PlatformDateChangedEventArgs { Date = newvalue });
            }
        }

        public void Dispose()
        {
        }

        public void ShowDialog(DateTime dt)
        {
            calendar.IsCalendarOpen = true;
        }

        public string? Text { get => Value; set => Value = value; }

        public string? Value
        {
            get
            {
                return calendar.Date?.ToString(datepicker.format);
            }
            set
            {
                if (DateTime.TryParseExact(value, datepicker.format, null, System.Globalization.DateTimeStyles.None, out DateTime exact))
                {
                    calendar.Date = oldvalue = exact;
                    textblock.Text = exact.ToString(datepicker.format);
                }
                else if (DateTime.TryParse(value, out var dt))
                {
                    calendar.Date = oldvalue = dt;
                    textblock.Text = dt.ToString(datepicker.format);
                }
                else
                {
                    calendar.Date = oldvalue = null;
                    textblock.Text = null;
                }
            }
        }

        public DateTime? Current => calendar.Date?.DateTime;

        public void Blur() { }
    }
}
