using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.PlatformHandler;

namespace EvangSol.Mobibrary.PlatformControl
{
    public class DropDownSelect : View, IInputControl
    {
        public static readonly BindableProperty OptionsProperty =
            BindableProperty.Create(nameof(Options), typeof(List<KeyValuePair<string, string>>), typeof(DropDownSelect), new List<KeyValuePair<string, string>>());
        public List<KeyValuePair<string, string>> Options
        {
            get => (List<KeyValuePair<string, string>>)GetValue(OptionsProperty);
            set => SetValue(OptionsProperty, value);
        }

        public static BindableProperty SelectionProperty =
            BindableProperty.Create(nameof(Selection), typeof(string), typeof(DropDownSelect), defaultBindingMode: BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvlaue) =>
            {
                if (oldvalue != newvlaue)
                {
                    var ddb = (DropDownSelect)bindable;
                    ddb.Value = (string)newvlaue;
                }
            });
        public string? Selection
        {
            get => (string)GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        public event EventHandler? ClearFocus;
        public void Blur()
        {
            ClearFocus?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(ClearFocus));
        }

        string? initvalue;

        public DropDownSelect() : base()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            //during page initial time, the Handler is null actually. if give it a value immediately, it can not take the value.
            //so hold the value temporarily and after the control is loaded set the value.
            if (initvalue != null)
            {
                Value = initvalue;
                initvalue = null;
            }
        }

        public bool ShowOnly { get => false; set => _ = value; }

        public string Text
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((DropDownSelectHandler)Handler).Text;
            }
            set
            {
                if (Handler == null)
                    return;
                ((DropDownSelectHandler)Handler).Text = value;
            }
        }

        public string Value
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((DropDownSelectHandler)Handler).Value;
            }
            set
            {
                if (Handler == null)
                {
                    initvalue = value;
                    return;
                }
                ((DropDownSelectHandler)Handler).Value = value;
            }
        }

        public IList<Behavior> ControlBehaviors => new List<Behavior>();


        public event EventHandler<DropDownBoxSelectEventArgs>? OnDropDownBoxItemSelected;
#if ANDROID
        public void OnItemSelected(object? sender, Platforms.Android.SpinnerEventArgs e)
        {
            Selection = e.Key;
            OnDropDownBoxItemSelected?.Invoke(this, new DropDownBoxSelectEventArgs { Position = e.Position, Key = e.Key, Value = e.Value });
        }
#endif
    }

    public class DropDownBoxSelectEventArgs : EventArgs
    {
        public int Position { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}
