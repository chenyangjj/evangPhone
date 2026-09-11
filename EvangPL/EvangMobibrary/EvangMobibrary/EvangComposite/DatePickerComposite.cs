using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Converter;
using System.Globalization;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class DatePickerComposite : EvangDateTimeComposite
    {
        protected FormatDateTimeConverter? formatconverter;

        public DatePickerComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public DatePickerComposite(
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

        public override void AddToLayout()
        {
            formatconverter = new FormatDateTimeConverter(date_format);

            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                Grid.Add(DateTimeStack, 2);
                Grid.Add(ClearIcon, 3);
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                Grid.Add(DateTimeStack, 1, 1);
                Grid.Add(ClearIcon, 2, 1);
            }
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            MixedDatePicker.SetBinding(PlatformDatePicker.DateTextProperty, propname, converter: formatconverter);
            MixedDatePicker!.PropertyChanged += OnMixedDatePickerPropertyChanged;
        }

        private void OnMixedDatePickerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DateText")
            {
                if (ShowLabel != null)
                    ShowLabel.Text = MixedDatePicker?.Text ?? string.Empty;
            }
        }

        public override string Text
        {
            get => MixedDatePicker!.Text;
            set
            {
                if (MixedDatePicker != null)
                {
                    var str = formatconverter?.Convert(value, typeof(string), null, CultureInfo.CurrentCulture) as string ?? value;
                    MixedDatePicker.Text = str;
                    if (ShowOnly)
                    {
                        if (ShowLabel == null)
                            CreateShowLabel();
                        ShowLabel!.Text = str;
                    }
                }
            }
        }

        public override View? InputControl => DateTimeStack;

        public override bool ShowOnly
        {
            get => base.ShowOnly;
            set
            {
                base.ShowOnly = value;
                if (ClearIcon != null)
                    ClearIcon.IsVisible = !value && !string.IsNullOrEmpty(Text);
            }
        }

        public override void Blur()
        {
            MixedDatePicker?.Blur();
        }
    }
}
