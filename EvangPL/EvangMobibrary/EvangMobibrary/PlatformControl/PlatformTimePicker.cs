using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.PlatformHandler;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.PlatformControl
{
    public class PlatformTimePicker : View, IInputControl
    {
        public static BindableProperty TimeTextProperty =
            BindableProperty.Create(nameof(TimeText), typeof(string), typeof(PlatformTimePicker), defaultBindingMode: BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvlaue) =>
            {
                if (oldvalue != newvlaue)
                {
                    var ac = (PlatformTimePicker)bindable;
                    ac.Value = (string)newvlaue;
                }
            });
        public string? TimeText
        {
            get => (string)GetValue(TimeTextProperty);
            set => SetValue(TimeTextProperty, value);
        }

        public event EventHandler? ShowDialog;
        public void ShowTimeDialog(TimeSpan ts)
        {
            var args = new PlatformTimeChangedEventArgs { Time = ts };
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
        public bool is24hour;
        string? initvalue;

        public PlatformTimePicker(CommonViewSetting setting, bool is24hour) : base()
        {
            this.setting = setting;
            this.is24hour = is24hour;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (initvalue != null)
            {
                Value = initvalue;
                initvalue = null;
            }
        }

        public event EventHandler<PlatformTimeChangedEventArgs>? TimeChanged;
        public void TriggerDataChanged(PlatformTimeChangedEventArgs e)
        {
            TimeChanged?.Invoke(this, e);
            TimeText = e.Time?.ToString();
        }

        public bool ShowOnly { get => false; set => _ = value; }

        public string Text
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((PlatformTimePickerHandler)Handler).Text;
            }
            set
            {
                if (Handler == null)
                    return;
                ((PlatformTimePickerHandler)Handler).Text = value;
            }
        }

        public string Value
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((PlatformTimePickerHandler)Handler).Value;
            }
            set
            {
                if (Handler == null)
                {
                    initvalue = value;
                    return;
                }
                ((PlatformTimePickerHandler)Handler).Value = value;
            }
        }

        public IList<Behavior> ControlBehaviors => new List<Behavior>();
    }

    public class PlatformTimeChangedEventArgs : EventArgs
    {
        public TimeSpan? Time { get; set; }
    }
}
