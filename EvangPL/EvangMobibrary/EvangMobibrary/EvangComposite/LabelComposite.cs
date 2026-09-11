using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Converter;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class LabelComposite : EvangCompositeView
    {
        public Label? ShowLabel { get; set; }
        //SIR0189273
        FormatDecimalConverter? decimalConverter { get; set; }
        Binding? binding { get; set; }

        public LabelComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public LabelComposite(
            string labeltext,
            double height,
            bool required = false,
            bool keyboardicon = false,
            CommonViewSetting? labelviewsetting = null,
            CommonViewSetting? inputviewsetting = null,
            int rightwidth = 0,
            CompositeLayoutPattern? pattern = null)
            : base(labeltext, height, required, keyboardicon, labelviewsetting, inputviewsetting, rightwidth, pattern)
        {
        }

        public override bool ShowOnly { get => true; set => _ = true; }

        public override string Text { get => ShowLabel!.Text; set => ShowLabel!.Text = value; }

        public override View? InputControl => null;

        public override void AddControl()
        {
            ShowLabel = new Label
            {
                FontSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                BackgroundColor = InputViewSetting?.BackgroundColor ?? Colors.Transparent,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                HorizontalTextAlignment = InputViewSetting?.Alignment ?? ((InputViewSetting?.InputType == "Integer" || InputViewSetting?.InputType == "Decimal") ? TextAlignment.End : TextAlignment.Start),
                VerticalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(5, 0),
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(ShowLabel, 2);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(ShowLabel, 1, 1);
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            if ((string.IsNullOrEmpty(unit) || bpvm == null) && string.IsNullOrEmpty(InputViewSetting?.DecimalFormat))
            {
                ShowLabel!.SetBinding(Label.TextProperty, propname);
            }
            else
            {
                //SIR0189273
                decimalConverter = new FormatDecimalConverter(unit, bpvm, InputViewSetting?.DecimalFormat);
                binding = new Binding(propname, BindingMode.TwoWay, decimalConverter);
                ShowLabel!.SetBinding(Label.TextProperty, binding);
            }
        }

        public override void Blur()
        {
        }

        //SIR0189273
        public void SetFormat(string format)
        {
            if (decimalConverter != null && ShowLabel != null)
            {
                decimalConverter.SetFormat(format);
                ShowLabel.RemoveBinding(Label.TextProperty);
                ShowLabel!.SetBinding(Label.TextProperty, binding);
            }
        }
    }
}
