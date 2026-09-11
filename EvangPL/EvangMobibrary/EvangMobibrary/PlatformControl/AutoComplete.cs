using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.PlatformHandler;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.PlatformControl
{
    public class AutoComplete : View, IInputControl, ISoftInput
    {
        public static readonly BindableProperty OptionsProperty =
            BindableProperty.Create(nameof(Options), typeof(List<IDataSelection>), typeof(AutoComplete), new List<IDataSelection>());
        public List<IDataSelection> Options
        {
            get { return (List<IDataSelection>)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        public static BindableProperty SelectionProperty =
            BindableProperty.Create(nameof(Selection), typeof(string), typeof(AutoComplete), defaultBindingMode: BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvlaue) =>
            {
                if (oldvalue != newvlaue)
                {
                    var ac = (AutoComplete)bindable;
                    ac.Value = (string)newvlaue;
                }
            });
        public string? Selection
        {
            get => (string)GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        public event EventHandler<EventArgs>? OnClear; //SIR0188536
        public event EventHandler? ClearText;
        public void Clear()
        {
            ClearText?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(ClearText));
            OnClear?.Invoke(this, EventArgs.Empty); //SIR0188536
        }

        public event EventHandler? SetFocus;
        public new void Focus()
        {
            SetFocus?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(SetFocus));
        }

        public event EventHandler? ClearFocus;
        public void Blur()
        {
            ClearFocus?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(ClearFocus));
        }

        public event EventHandler? ToggleSoftInput;
        public void SoftInput(bool show)
        {
            ToggleSoftInput?.Invoke(this, new SoftInputEventArgs { Show = show });
            Handler?.Invoke(nameof(ToggleSoftInput), new SoftInputEventArgs { Show = show });
        }


        public CommonViewSetting setting;
        string? initvalue;

        public AutoComplete(CommonViewSetting setting)
        {
            this.setting = setting;
            Loaded += OnLoaded;

#if WINDOWS
            Margin = new Thickness(0, 4, 0, 0);
#endif
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

        public event EventHandler<AutoCompleteSelectEventArgs>? OnAutoCompleteItemSelected;
        public event EventHandler<AutoCompleteFocusChangeEventArgs>? OnAutoCompleteFocusChanged;
#if ANDROID
        public void OnItemSelected(object? sender, Platforms.Android.ItemSelectedEventArgs e)
        {
            Selection = e.Key;
            OnAutoCompleteItemSelected?.Invoke(this, new AutoCompleteSelectEventArgs { Key = e.Key, Value = e.Value });
        }

        public void OnFocusChanged(object? sender, Android.Views.View.FocusChangeEventArgs e)
        {
            OnAutoCompleteFocusChanged?.Invoke(this, new AutoCompleteFocusChangeEventArgs { HasFocus = e.HasFocus });
        }
#elif IOS || MACCATALYST

#elif WINDOWS
        public void OnItemSelected(string key, string value)
        {
            Selection = key;
            OnAutoCompleteItemSelected?.Invoke(this, new AutoCompleteSelectEventArgs { Key = key, Value = value });
        }

        public void OnFocusChanged(bool hasfocus)
        {
            OnAutoCompleteFocusChanged?.Invoke(this, new AutoCompleteFocusChangeEventArgs { HasFocus = hasfocus });
        }
#endif

        public string Text
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((AutoCompleteHandler)Handler).AutoText;
            }
            set
            {
                if (Handler == null)
                    return;
                ((AutoCompleteHandler)Handler).AutoText = value;
            }
        }

        public bool ShowOnly { get => false; set => _ = value; }

        public string Value
        {
            get
            {
                if (Handler == null)
                    return string.Empty;
                return ((AutoCompleteHandler)Handler).AutoValue;
            }
            set
            {
                if (Handler == null)
                {
                    initvalue = value;
                    return;
                }
                ((AutoCompleteHandler)Handler).AutoValue = value;
            }
        }

        public IList<Behavior> ControlBehaviors => new List<Behavior>();
    }

    public class AutoCompleteSelectEventArgs : EventArgs
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class AutoCompleteFocusChangeEventArgs : EventArgs
    {
        public bool HasFocus { get; set; }
    }

    public class SoftInputEventArgs : EventArgs
    {
        public bool Show { get; set; }
    }
}
