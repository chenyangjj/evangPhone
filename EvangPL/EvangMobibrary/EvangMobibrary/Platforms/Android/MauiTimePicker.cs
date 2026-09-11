using EvangSol.Mobibrary.PlatformControl;
using Android.App;
using Android.Content;
using Andwidg = Android.Widget;

namespace EvangSol.Mobibrary.Platforms.Android
{
    public class MauiTimePicker : Andwidg.TextView
    {
        PlatformTimePicker timepicker;
        TimeSpan? timespan;

        public MauiTimePicker(Context? context, PlatformTimePicker timepicker) : base(context)
        {
            this.timepicker = timepicker;

            if (timepicker.setting != null)
            {
                timepicker.setting.Padding ??= (10, 15, 0, 0);
                this.ApplyStyle(timepicker.setting);
            }
        }

        public void ShowDialog(TimeSpan ts)
        {
            timespan = ts;
            var dialog = new TimePickerDialog(
                //Platform.CurrentActivity!,
                null,
                (sender, e) =>
                {
                    timespan = new TimeSpan(e.HourOfDay, e.Minute, 0);
                    Text = FormatTime(LeadingZero(e.HourOfDay), LeadingZero(e.Minute), "00");
                    timepicker.TriggerDataChanged(new PlatformTimeChangedEventArgs { Time = timespan });
                },
                timespan.Value.Hours, timespan.Value.Minutes, timepicker.is24hour
            );
            dialog.Show();
        }

        public string? Value
        {
            get
            {
                return timespan?.ToString();
            }
            set
            {
                if (DateTime.TryParse(value, out var dt))
                {
                    timespan = dt.TimeOfDay;
                    Text = FormatTime(LeadingZero(dt.Hour), LeadingZero(dt.Minute), LeadingZero(dt.Second));
                }
                else
                {
                    timespan = null;
                    Text = null;
                }
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

        public void Blur() => ClearFocus();
    }
}
