using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Converter;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class CheckBoxComposite : EvangCompositeView
    {
        public CheckBox? CheckBox { get; set; }
        public Label? CheckLabel { get; set; }

        public CheckBoxComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public CheckBoxComposite(
            string labeltext,
            double height,
            bool required = false,
            bool keyboardicon = false,
            CommonViewSetting? labelviewsetting = null,
            CommonViewSetting? inputviewsetting = null,
            int leftwidth = 0,
            CompositeLayoutPattern? pattern = null)
            : base(labeltext, height, required, false, labelviewsetting, inputviewsetting, leftwidth, CompositeLayoutPattern.Parallel)
        {
        }

        public override int GetLabelPosition()
        {
            Label.HorizontalTextAlignment = TextAlignment.Start;
            return 2;
        }

        public override void AddControl()
        {
            CheckBox = new CheckBox { Scale = 1.5 };
            Grid.Add(CheckBox, 1);
        }

        public override Grid CreateGrid(double height)
        {
            return new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = height },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = LabelViewSetting!.Width > 0 ? LabelViewSetting!.Width.Value : new GridLength(1, GridUnitType.Star)},
                    new ColumnDefinition { Width = height },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)},
                }
            };
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            CheckBox.SetBinding(CheckBox.IsCheckedProperty, propname, converter: new StringToBooleanConverter());
        }

        public override bool ShowOnly { get => false; set => _ = value; }

        public override string Text
        {
            get => CheckBox!.IsChecked.ToString();
            set
            {
                bool val = false;
                bool.TryParse(value, out val);
                CheckBox!.IsChecked = val;
            }
        }

        public override View? InputControl => null;

        public override void Blur()
        {
            CheckBox?.Unfocus();
        }
    }
}
