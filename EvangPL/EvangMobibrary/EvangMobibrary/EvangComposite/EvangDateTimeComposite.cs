using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public abstract class EvangDateTimeComposite : EvangShowComposite
    {
        public static bool IsTime24Hours = true;    //to show 24-hour clock

        public event EventHandler<PlatformDateChangedEventArgs>? DateChanged;
        public event EventHandler<EventArgs>? OnClear; //SIR0188536

        public StackLayout? DateTimeStack { get; set; }
        public ImageButton? ClearIcon { get; set; }
        public PlatformDatePicker? MixedDatePicker { get; set; }

        public string date_format = "yyyy/MM/dd";
        public string time_format = "HH:mm:ss";

        public EvangDateTimeComposite(
            string labeltext,
            double height,
            bool required = false,
            bool keyboardicon = false,
            CommonViewSetting? labelviewsetting = null,
            CommonViewSetting? inputviewsetting = null,
            int rightwidth = 0,
            CompositeLayoutPattern? pattern = null)
            : base(labeltext, height, required, false, labelviewsetting, inputviewsetting, rightwidth, pattern)
        {
            MixedDatePicker!.HandlerLoaded += OnHandlerLoaded;
        }

        private void OnHandlerLoaded(object? sender, EventArgs e)
        {
        	ClearIcon!.IsVisible = !string.IsNullOrEmpty(MixedDatePicker!.Text) && !ShowOnly;
        }

        public sealed override void AddControl()
        {
            var format = GetDateTimeFormat("dateformat");
            date_format = InputViewSetting?.DateFormat ?? format ?? date_format;

            //format = GetDateTimeFormat("timeformat");
            time_format = InputViewSetting?.TimeFormat ?? time_format;

            ClearIcon = new ImageButton
            {
                Source = ImageSource.FromResource("EvangSol.Mobibrary.Resources.Images.ic_clear.png"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HeightRequest = icon_width_height,
                WidthRequest = icon_width_height,
                IsVisible = false,
                BackgroundColor = DefaultBackgroudColor,
                BorderWidth = 0,
                BorderColor = Colors.Transparent,
                CornerRadius = 14,
                Aspect = Aspect.AspectFit
            };
#if ANDROID
            ClearIcon.Clicked += OnClearEntryClicked;
#elif WINDOWS
            TapGestureRecognizer tapClearIconGestureRecognizer = new TapGestureRecognizer();
            tapClearIconGestureRecognizer.Tapped += (s, e) => OnClearEntryClicked(s, e);
            ClearIcon!.GestureRecognizers.Add(tapClearIconGestureRecognizer);
#endif

            MixedDatePicker = new PlatformDatePicker(new CommonViewSetting
            {
                TextSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                Padding = InputViewSetting?.Padding,
                Placeholder = InputViewSetting?.Placeholder,
                DateFormat = InputViewSetting?.DateFormat ?? date_format,
            }, date_format)
            {
                HeightRequest = GivenHeight > 0 ? GivenHeight : CommonViewSetting.COMPOSITE_HEIGHT,
                BackgroundColor = InputViewSetting?.BackgroundColor ?? DefaultBackgroudColor,
            };
            MixedDatePicker.DateChanged += OnDateChanged;

            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => MixedDatePicker.ShowDateDialog(string.IsNullOrEmpty(MixedDatePicker.Value) ? DateTime.Now : DateTime.ParseExact(MixedDatePicker.Value, MixedDatePicker.format, null));
            MixedDatePicker!.GestureRecognizers.Add(tapGestureRecognizer);

            DateTimeStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Children =
                {
                    MixedDatePicker,
                    new BoxView
                    {
                        Color = GetColor("Gray500"),
                        HeightRequest = 1,
                        HorizontalOptions = LayoutOptions.Fill,
                        Margin = new Thickness(0, -8, 0, 0)
                    }
                }
            };

            AddToLayout();
        }

        private void OnDateChanged(object? sender, PlatformDateChangedEventArgs e)
        {
            DateChanged?.Invoke(this, e);
            ClearIcon!.IsVisible = e.Date != null;
        }

        public abstract void AddToLayout();

        public virtual void OnClearEntryClicked(object? sender, EventArgs e)
        {
            MixedDatePicker!.DateText = string.Empty;
            ClearIcon!.IsVisible = false;
            OnClear?.Invoke(this, e); //SIR0188536
        }

        public string? GetDateTimeFormat(string dt)
        {
            string? format = "";
            if (LocalMemory.master.ContainsKey("MasterParams"))
            {
                var df = LocalMemory.GetMaster("MasterParams") as List<MasterParams>;
                if (df == null)
                    throw new Exception("There's no MasterParams master data.");
                var fmt = df.Where(x => x.paramid == dt).Select(x => x.paramval);
                if (fmt.Count() < 1)
                    throw new Exception($"Can not find {dt} definition in MasterParams master data.");
                format = fmt.First();
            }
            else
            {
                format = null; //デフォルト値設定のためnullを返す
            }

            return format;
        }
    }

    public interface IMauiDateTimePicker
    {
        void ShowDatePicker(DatePicker datePicker);

        void ShowTimePicker(TimePicker timePicker);
    }

    public class DateTimeSelectEventArgs : EventArgs
    {
        public DateTime? SelectedDate { get; set; }
        public TimeSpan? SelectedTime { get; set; }
    }
}
