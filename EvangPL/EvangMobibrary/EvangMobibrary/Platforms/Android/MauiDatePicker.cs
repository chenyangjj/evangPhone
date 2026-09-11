using EvangSol.Mobibrary.PlatformControl;
using Android.App;
using Android.Content;
using Andwidg = Android.Widget;

namespace EvangSol.Mobibrary.Platforms.Android
{
    public class MauiDatePicker : Andwidg.TextView
    {
        PlatformDatePicker datepicker;
        DateTime? datime;

        public MauiDatePicker(Context? context, PlatformDatePicker datepicker) : base(context)
        {
            this.datepicker = datepicker;

            if (datepicker.setting != null)
            {
                datepicker.setting.Padding ??= (10, 15, 0, 0);
                this.ApplyStyle(datepicker.setting);
            }
        }

        public void ShowDialog(DateTime dt)
        {
            var dialog = new DatePickerDialog(
                Platform.CurrentActivity!,
                (sender, e) =>
                {
                    datime = e.Date;
                    Text = datime?.ToString(datepicker.format);
                    datepicker.TriggerDataChanged(new PlatformDateChangedEventArgs { Date = e.Date });
                },
                dt.Year,
                dt.Month - 1,
                dt.Day
            );
            dialog.Show();
        }

        public string? Value
        {
            get
            {
                return datime?.ToString(datepicker.format);
            }
            set
            {
                if (DateTime.TryParseExact(value, datepicker.format, null, System.Globalization.DateTimeStyles.None, out DateTime exact))
                {
                    datime = exact;
                    Text = exact.ToString(datepicker.format);
                }
                else if (DateTime.TryParse(value, out var dt))
                {
                    datime = dt;
                    Text = dt.ToString(datepicker.format);
                }
                else
                {
                    datime = null;
                    Text = null;
                }
            }
        }

        public DateTime? Current => datime;

        public void Blur() => ClearFocus();
    }
}
