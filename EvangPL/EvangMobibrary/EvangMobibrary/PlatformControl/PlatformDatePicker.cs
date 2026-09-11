using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.PlatformHandler;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.PlatformControl
{
    public class PlatformDatePicker : View, IInputControl
    {
        public static BindableProperty DateTextProperty =
            BindableProperty.Create(nameof(DateText), typeof(string), typeof(PlatformDatePicker), defaultBindingMode: BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvlaue) =>
            {
                if (oldvalue != newvlaue)
                {
                    var dp = (PlatformDatePicker)bindable;
                    dp.Value = (string)newvlaue;
                    dp.DateChanged?.Invoke(dp, new PlatformDateChangedEventArgs { Date = dp.Current });
                }
            });
        public string? DateText
        {
            get => (string)GetValue(DateTextProperty);
            set => SetValue(DateTextProperty, value);
        }

        public event EventHandler? ShowDialog;
        public void ShowDateDialog(DateTime dt)
        {
            var args = new PlatformDateChangedEventArgs { Date = dt };
            ShowDialog?.Invoke(this, args);
            Handler?.Invoke(nameof(ShowDialog), args);
        }

        public event EventHandler? ClearFocus;
        public void Blur()
        {
            ClearFocus?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(ClearFocus));
        }

        public CommonViewSetting setting { get; set; }
        public string format;
        string? initvalue;

        public PlatformDatePicker(CommonViewSetting setting, string format) : base()
        {
            this.setting = setting;
            this.format = format;
            Loaded += OnLoaded;
        }

        public event EventHandler? HandlerLoaded;
        private void OnLoaded(object? sender, EventArgs e)
        {
            if (initvalue != null)
            {
                Value = initvalue;
                initvalue = null;
            }
            HandlerLoaded?.Invoke(this, e);
        }

        public event EventHandler<PlatformDateChangedEventArgs>? DateChanged;
        public void TriggerDataChanged(PlatformDateChangedEventArgs e)
        {
            DateChanged?.Invoke(this, e);
            DateText = e.Date?.ToString(format);
        }

        public bool ShowOnly { get => false; set => _ = value; }

        public string Text
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((PlatformDatePickerHandler)Handler).Text;
            }
            set
            {
                if (Handler == null)
                {
                    initvalue = value;
                    return;
                }
                ((PlatformDatePickerHandler)Handler).Text = value;
            }
        }

        public string Value
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((PlatformDatePickerHandler)Handler).Value;
            }
            set
            {
                if (Handler == null)
                {
                    initvalue = value;
                    return;
                }
                ((PlatformDatePickerHandler)Handler).Value = value;
            }
        }

        public DateTime? Current => Handler == null ? null : ((PlatformDatePickerHandler)Handler).Current;

        public IList<Behavior> ControlBehaviors => new List<Behavior>();
    }

    public class PlatformDateChangedEventArgs : EventArgs
    {
        public DateTime? Date { get; set; }
    }
}
