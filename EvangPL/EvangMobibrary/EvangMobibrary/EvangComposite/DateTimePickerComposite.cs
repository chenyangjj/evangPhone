using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class DateTimePickerComposite : DatePickerComposite
    {
        public event EventHandler<PlatformTimeChangedEventArgs>? TimeChanged;

        public static readonly BindableProperty DateTimeStringProperty = BindableProperty.Create(nameof(DateTimeString), typeof(string), typeof(DateTimePickerComposite), string.Empty, BindingMode.TwoWay,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var dtpcv = bindable as DateTimePickerComposite;
                if (dtpcv != null)
                {
                    dtpcv.Text = (string)newValue;
                    if (dtpcv.ShowLabel != null)
                        dtpcv.ShowLabel.Text = dtpcv.MixedDatePicker?.Text ?? string.Empty;
                }
            });
        public string? DateTimeString
        {
            get => (string)GetValue(DateTimeStringProperty);
            set => SetValue(DateTimeStringProperty, value);
        }

        public PlatformTimePicker? MixedTimePicker { get; set; }
        public StackLayout? TimeStack { get; set; }
#if WINDOWS
        public Label? TimeLabel { get; set; }
        public StackLayout? TimeLabelStack { get; set; }
#endif


        public DateTimePickerComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public DateTimePickerComposite(
            string labeltext,
            double height,
            bool required = false,
            bool dummy = false,
            CommonViewSetting? labelviewsetting = null,
            CommonViewSetting? inputviewsetting = null,
            int rightwidth = 0,
            CompositeLayoutPattern? pattern = null)
            : base(labeltext, height, required, false, labelviewsetting, inputviewsetting, rightwidth, pattern)
        {
        }

        public override Grid CreateGrid(double height)
        {
            Grid grid = new();
            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = height });
                CompositeHeight = height;
                var columns = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = LabelViewSetting?.Width ?? new GridLength(10, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(10, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(10, GridUnitType.Absolute) },
#if WINDOWS
                    new ColumnDefinition { Width = new GridLength(42, GridUnitType.Absolute) },
#endif
                    new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(28, GridUnitType.Auto) },
                };
                grid.ColumnDefinitions = columns;
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                var labelheight = COMPOSITE_TANDEMLABEL_HEIGHT;
                if (LabelViewSetting != null && LabelViewSetting.Height != null && LabelViewSetting.Height > 0)
                    labelheight = LabelViewSetting.Height.Value;
                grid.RowDefinitions.Add(new RowDefinition { Height = labelheight });
                grid.RowDefinitions.Add(new RowDefinition { Height = height });
                CompositeHeight = labelheight + height;
                var columns = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(10, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(10, GridUnitType.Absolute) },
#if WINDOWS
                    new ColumnDefinition { Width = new GridLength(42, GridUnitType.Absolute) },
#endif
                    new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(28, GridUnitType.Auto) },
                };
                grid.ColumnDefinitions = columns;
            }
            return grid;
        }

        public override void AddToLayout()
        {
            if (Grid == null)
                return;

            MixedTimePicker = new PlatformTimePicker(new CommonViewSetting
            {
                TextSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                Padding = InputViewSetting?.Padding,
                TimeFormat = InputViewSetting?.TimeFormat ?? time_format,
            }, IsTime24Hours)
            {
                HeightRequest = GivenHeight > 0 ? GivenHeight : CommonViewSetting.COMPOSITE_HEIGHT,
#if ANDROID
                BackgroundColor = InputViewSetting?.BackgroundColor ?? DefaultBackgroudColor,
#elif WINDOWS
                BackgroundColor = Colors.Transparent,
#endif
            };
            MixedTimePicker.TimeChanged += OnTimeChanged;

#if ANDROID
            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnTimeLabelTapped;
            MixedTimePicker!.GestureRecognizers.Add(tapGestureRecognizer);

            TimeStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Children =
                {
                    MixedTimePicker,
                    new BoxView
                    {
                        Color = GetColor("Gray500"),
                        HeightRequest = 1,
                        HorizontalOptions = LayoutOptions.Fill,
                        Margin = new Thickness(0, -8, 0, 0)
                    }
                }
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                Grid.Add(DateTimeStack, 1);
                Grid.Add(TimeStack, 3);
                Grid.Add(ClearIcon, 4);
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                Grid.Add(DateTimeStack, 0, 1);
                Grid.Add(TimeStack, 2, 1);
                Grid.Add(ClearIcon, 3, 1);
            }
#elif WINDOWS
            Image icon = new Image
            {
                Source = ImageSource.FromResource("EvangSol.Mobibrary.Resources.Images.ic_clock.png"),
                HeightRequest = icon_width_height,
                WidthRequest = icon_width_height,
                Aspect = Aspect.AspectFit
            };

            TimeStack = new StackLayout
            {
                BackgroundColor = Colors.Transparent,
                Children =
                {
                    new Grid { icon, MixedTimePicker },
                }
            };

            TimeLabel = new Label
            {
                FontSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                BackgroundColor = InputViewSetting?.BackgroundColor ?? DefaultBackgroudColor,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(InputViewSetting?.Padding?.Item1 ?? 5,
                    InputViewSetting?.Padding?.Item2 ?? 0,
                    InputViewSetting?.Padding?.Item3 ?? 5,
                    InputViewSetting?.Padding?.Item4 ?? 0),
                HeightRequest = GivenHeight > 0 ? GivenHeight : CommonViewSetting.COMPOSITE_HEIGHT,
            };

            TimeLabelStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Children =
                {
                    TimeLabel,
                    new BoxView
                    {
                        Color = GetColor("Gray500"),
                        HeightRequest = 1,
                        HorizontalOptions = LayoutOptions.Fill,
                        Margin = new Thickness(0, -8, 0, 0)
                    }
                }
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                Grid.Add(DateTimeStack, 1);
                Grid.Add(TimeStack, 3);
                Grid.Add(TimeLabelStack, 4);
                Grid.Add(ClearIcon, 5);
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                Grid.Add(DateTimeStack, 0, 1);
                Grid.Add(TimeStack, 2, 1);
                Grid.Add(TimeLabelStack, 3, 1);
                Grid.Add(ClearIcon, 4, 1);
            }
#endif
        }

        public override void OnClearEntryClicked(object? sender, EventArgs e)
        {
            base.OnClearEntryClicked(sender, e);
            MixedTimePicker!.TimeText = string.Empty;
#if ANDROID
            MixedTimePicker!.Text = string.Empty;
#elif WINDOWS
            TimeLabel!.Text = string.Empty;
#endif
        }

        TimeSpan? oldvalue = new TimeSpan();
        private void OnTimeChanged(object? sender, PlatformTimeChangedEventArgs e)
        {
            if (oldvalue != e.Time)
            {
                oldvalue = e.Time;
                TimeChanged?.Invoke(sender, e);
                ClearIcon!.IsVisible = e.Time != null;
#if WINDOWS
                TimeLabel!.Text = FormatTime(LeadingZero(e.Time?.Hours), LeadingZero(e.Time?.Minutes), LeadingZero(e.Time?.Seconds));
#endif
            }
        }

#if WINDOWS
        string LeadingZero(int? t)
        {
            if (t == null)
                return string.Empty;
            if (t.Value.ToString().Length == 1)
                return "0" + t;
            return t.Value.ToString();
        }
        string FormatTime(string hour, string min, string sec) => MixedTimePicker!.setting.TimeFormat!.Replace("HH", hour).Replace("mm", min).Replace("ss", sec);
#endif

        public void OnTimeLabelTapped(object? sender, TappedEventArgs e)
        {
            MixedTimePicker!.ShowTimeDialog(string.IsNullOrEmpty(MixedTimePicker.Value) ? DateTime.Now.TimeOfDay : DateTime.Parse(MixedTimePicker.Value).TimeOfDay);
        }

        public override void SetInputBinding(string propname, string? unit = null, BindableBrokerView? bpvm = null)
        {
            var binding = new Binding(propname, BindingMode.TwoWay);
            SetBinding(DateTimeStringProperty, binding);
        }

        //SIR0187988
        private bool _showOnly = false;
        public override bool ShowOnly
        {
            get => _showOnly;
            set
            {
                _showOnly = value;
                DateTimeStack!.IsEnabled = !value;
                MixedTimePicker!.IsEnabled = !value;

                var _backgroundColor = InputViewSetting?.BackgroundColor ?? DefaultBackgroudColor;
                MixedDatePicker!.Background = value ? Colors.Transparent : _backgroundColor;
#if ANDROID
                MixedTimePicker!.Background = value ? Colors.Transparent: _backgroundColor;
                
                if(value)
                {
                    ClearIcon!.IsVisible = false;
                }
                else
                {
                    if(!string.IsNullOrWhiteSpace(Text))
                        ClearIcon!.IsVisible = true;
                }
                    

#elif WINDOWS
                TimeLabel!.Background = value ? Colors.Transparent: _backgroundColor;

                foreach (var item in TimeLabelStack!.Children)
                {
                    if (item is BoxView boxView)
                    {
                        boxView.IsVisible = !value;
                        break;
                    }
                }
                
                if(value)
                {
                    ClearIcon!.IsVisible = false;
                }
                else
                {
                    if(!string.IsNullOrWhiteSpace(MixedDatePicker?.Text) || 
                       !string.IsNullOrWhiteSpace(TimeLabel?.Text))
                        ClearIcon!.IsVisible = true;
                }
#endif

                foreach (var item in TimeStack!.Children)
                {
                    if (item is BoxView boxView)
                    {
                        boxView.IsVisible = !value;
                        break;
                    }
                }
                foreach (var item in DateTimeStack!.Children)
                {
                    if (item is BoxView boxView)
                    {
                        boxView.IsVisible = !value;
                        break;
                    }
                }
            }
        }

        public override string Text
        {
            get => $"{MixedDatePicker?.Text ?? string.Empty} {MixedTimePicker?.Text ?? string.Empty}";
            set
            {
                DateTime dt;
                if (DateTime.TryParseExact(value, $"{date_format} {time_format}", null, System.Globalization.DateTimeStyles.None, out DateTime exact))
                {
                    MixedDatePicker!.Value = value;
                    ClearIcon!.IsVisible = !string.IsNullOrEmpty(value);
                    MixedTimePicker!.Value = exact.TimeOfDay.ToString();
                }
                else if (DateTime.TryParse(value, out dt))
                {
                    MixedDatePicker!.Value = value;
                    ClearIcon!.IsVisible = !string.IsNullOrEmpty(value);
                    MixedTimePicker!.Value = dt.TimeOfDay.ToString();
                }
                else
                {
                    MixedDatePicker!.Value = "";
                    ClearIcon!.IsVisible = false;
                    MixedTimePicker!.Value = "";
                }
            }
        }

        public override View? InputControl => null;
    }
}
