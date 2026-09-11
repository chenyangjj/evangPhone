using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.Utilities.Common;
#if ANDROID
using EvangSol.Mobibrary.PlatformControl;
#endif

namespace EvangSol.Mobibrary.EvangWidget
{
    public class UnifiedDropDown : EvangContentView, IInputControl
    {
        public static BindableProperty SelectionProperty =
            BindableProperty.Create(nameof(Selection), typeof(string), typeof(UnifiedDropDown), defaultBindingMode: BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvlaue) =>
            {
                if (oldvalue != newvlaue)
                {
                    var ddb = (UnifiedDropDown)bindable;
                    ddb.Value = (string)newvlaue;
                }
            });
        public string? Selection
        {
            get => (string)GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        public event EventHandler<UnifiedPickerLoadedEventArgs>? OnDropDownLoaded;
        public event EventHandler<UnifiedPickerSelectEventArgs>? OnDropDownBoxItemSelected;

#if ANDROID
        DropDownSelect? picker;
#else
        Picker? picker;
#endif
        List<KeyValuePair<string, string>>? items;
        CommonViewSetting? inputviewsetting;
        bool required;
        string? initvalue;
        bool loaded = false;

        StackLayout? ddStack;

        public UnifiedDropDown(bool required = false, CommonViewSetting ? inputviewsetting = null)
        {
            this.required = required;
            this.inputviewsetting = inputviewsetting;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (loaded)
                return;
            loaded = true;

#if ANDROID
            // Use Spinner for Android
            picker = new DropDownSelect();
            picker.OnDropDownBoxItemSelected += OnDropDownItemSelected;
            if (items != null)
                picker.Options = items;
            // Add picker to the layout
            ddStack = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    picker,
                    new BoxView
                    {
                        Color = GetColor("Gray500"),
                        HeightRequest = 1,
                        HorizontalOptions = LayoutOptions.Fill,
                        Margin = new Thickness(0, -10, 0, 0)
                    },
                }
            };
#else
            // Use Picker for other platforms (e.g., Windows)
            picker = new Picker
            {
                FontSize = inputviewsetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                TextColor = inputviewsetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                BackgroundColor = inputviewsetting?.BackgroundColor ?? (required ? GetColor("RequiredBackground") : GetColor("OptionalBackground")),
                FontAttributes = inputviewsetting?.FontAttributes ?? FontAttributes.None,
                HorizontalTextAlignment = inputviewsetting?.Alignment ?? TextAlignment.Start,
            };
            picker.SelectedIndexChanged += OnSelectedIndexChanged;
            if (items != null)
                picker.ItemsSource = items.Select(x => x.Value).ToList();
            // Add picker to the layout
            ddStack = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    picker,
                    new BoxView
                    {
                        Color = GetColor("Gray300"),
                        HeightRequest = 1,
                        HorizontalOptions = LayoutOptions.Fill,
                        Margin = new Thickness(0, -8, 0, 0)
                    },
                }
            };
#endif
            Content = ddStack;

            OnDropDownLoaded?.Invoke(this, new UnifiedPickerLoadedEventArgs { Value = initvalue, Text = GetTextByValue(initvalue ?? string.Empty) });

            if (initvalue != null)
            {
                Value = initvalue;
                initvalue = null;
            }
        }

#if ANDROID
        private void OnDropDownItemSelected(object? sender, DropDownBoxSelectEventArgs e)
        {
            Selection = e.Key;
            OnDropDownBoxItemSelected?.Invoke(sender, new UnifiedPickerSelectEventArgs { Key = e.Key, Value = e.Value, Position = e.Position });
        }
#else
        private void OnSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (items == null || picker == null)
                return;
            var sel = items[picker.SelectedIndex];
            Selection = sel.Key;
            OnDropDownBoxItemSelected?.Invoke(sender, new UnifiedPickerSelectEventArgs { Key = sel.Key, Value = sel.Value, Position = picker.SelectedIndex });
        }
#endif

        public List<KeyValuePair<string, string>> Options
        {
            get => items ?? new List<KeyValuePair<string, string>>();
            set
            {
                items = value;
#if ANDROID
                if (picker == null)
                    return;
                picker.Options = value;
#else
                if (picker == null)
                    return;
                picker.ItemsSource = items.Select(x => x.Value).ToList();
#endif
            }
        }

        public string GetTextByValue(string val)
        {
            if (items == null || items.Count < 1)
                return string.Empty;
            var qset = items.Where(x => x.Key == val).Select(x => x.Value);
            if (qset.Any())
                return qset.First();
            return string.Empty;
        }

        public string GetValueByText(string txt)
        {
            if (items == null || items.Count < 1)
                return string.Empty;
            var qset = items.Where(x => x.Value == txt).Select(x => x.Key);
            if (qset.Any())
                return qset.First();
            return string.Empty;
        }

        public bool ShowOnly { get => !IsEnabled; set => IsEnabled = !value; }

        public string Text
        {
            get
            {
#if ANDROID
                if (picker == null)
                    return string.Empty;
                return picker.Text;
#else
                if (picker == null)
                    return string.Empty;
                return (string)(picker.SelectedItem ?? string.Empty);
#endif
            }
            set
            {
#if ANDROID
                if (picker == null)
                    return;
                picker.Text = value;
#else
                if (picker == null)
                    return;
                picker.SelectedItem = value;
#endif
            }
        }

        public string Value
        {
            get
            {
                if (items == null)
                    return string.Empty;
                var qset = items.Where(x => x.Value == Text).Select(x => x.Key);
                if (qset.Any())
                    return qset.First();
                return string.Empty;
            }
            set
            {
                if (items == null)
                    return;
                var qset = items.Where(x => x.Key == value).Select(x => x.Value);
                if (qset.Any())
                {
#if ANDROID
                    if (picker == null)
                    {
                        initvalue = value;
                        return;
                    }
                    picker.Value = value;
#else
                    if (picker == null)
                    {
                        initvalue = value;
                        return;
                    }
                    picker.SelectedItem = qset.FirstOrDefault() ?? string.Empty;
#endif
                }
            }
        }

        public IList<Behavior> ControlBehaviors => new List<Behavior>();

        public void Blur()
        {
#if ANDROID
            picker!.Blur();
#else
            picker!.Unfocus();
#endif
        }
    }

    public class UnifiedPickerLoadedEventArgs : EventArgs
    {
        public string? Value { get; set; }
        public string? Text { get; set; }
    }

    public class UnifiedPickerSelectEventArgs : EventArgs
    {
        public int Position { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}
