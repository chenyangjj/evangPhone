using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.UI.Xaml.Controls;

namespace EvangSol.Mobibrary.Platforms.Windows
{
    public class MauiTimePicker : Microsoft.UI.Xaml.Controls.TimePicker, IDisposable
    {
        PlatformTimePicker timepicker;

        public MauiTimePicker(PlatformTimePicker timepicker) : base()
        {
            this.timepicker = timepicker;
            init();
        }

        void init()
        {
            FontSize = timepicker.setting.TextSize ?? CommonViewSetting.INPUT_FONTSIZE;
            Foreground = WindowsUtils.ConvertToWinColor(timepicker.setting.TextColor);
            Background = WindowsUtils.ConvertToWinColor(Colors.Transparent);
            if (timepicker.is24hour)
                ClockIdentifier = "24HourClock";
            TimeChanged += OnTimeChanged;
        }

        private void OnTimeChanged(object? sender, TimePickerValueChangedEventArgs e)
        {
            timepicker.TriggerDataChanged(new PlatformTimeChangedEventArgs { Time = e.NewTime });
        }

        public void Dispose()
        {
        }

        public void ShowDialog(TimeSpan ts)
        {
        }

        public string? Text { get => Value; set => Value = value; }

        public string? Value
        {
            get => FormatTime(LeadingZero(Time.Hours), LeadingZero(Time.Minutes), LeadingZero(Time.Seconds));
            set
            {
                if (DateTime.TryParse(value, out var dt))
                    Time = dt.TimeOfDay;
                else
                    Time = new TimeSpan();
            }
        }

        string LeadingZero(int t)
        {
            if (t.ToString().Length == 1)
                return "0" + t;
            return t.ToString();
        }

        string FormatTime(string hour, string min, string sec)
        {
            return timepicker.setting.TimeFormat!.Replace("HH", hour).Replace("mm", min).Replace("ss", sec);
        }

        public void Blur() { }
    }
}
