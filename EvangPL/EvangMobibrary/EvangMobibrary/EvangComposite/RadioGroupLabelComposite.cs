using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class RadioGroupLabelComposite : RadioGroupComposite
    {
        readonly Thickness label_padding = new Thickness(5, 0); //default label padding
        public Label? Label { get; set; }
        public CompositeLayoutPattern Pattern { get; set; }
        public CommonViewSetting? LabelSetting { get; set; }
        public string LabelText { get; set; }

        public RadioGroupLabelComposite(
            string labeltext,
            string groupname,
            string radios,
            CompositeLayoutPattern? pattern = null,
            CommonViewSetting? labelsetting = null,
            CommonViewSetting? radiosetting = null,
            CommonFlexSetting? flexsetting = null)
            : base(groupname, radios, radiosetting, flexsetting)
        {
            LabelText = labeltext;
            Pattern = pattern ?? (DeviceInfo.Idiom == DeviceIdiom.Phone ? CompositeLayoutPattern.Tandem : CompositeLayoutPattern.Parallel);
            LabelSetting = labelsetting;
        }

        public override Layout CreateLayout()
        {
            base.CreateLayout();

            Label = new Label
            {
                Text = GetCustomString(LabelText) ?? LabelText,
                FontSize = LabelSetting?.TextSize ?? CommonViewSetting.LABEL_FONTSIZE,
                TextColor = LabelSetting?.TextColor ?? GetColor(CommonViewSetting.LABEL_FONTCOLOR),
                BackgroundColor = LabelSetting?.BackgroundColor ?? Colors.Transparent,
                FontAttributes = LabelSetting?.FontAttributes ?? FontAttributes.Bold,
                Padding = Pattern == CompositeLayoutPattern.Tandem ? label_padding : new Thickness(5),
                HorizontalTextAlignment = LabelSetting?.Alignment ?? (Pattern == CompositeLayoutPattern.Tandem ? TextAlignment.Start : TextAlignment.End),
                VerticalTextAlignment = Pattern == CompositeLayoutPattern.Tandem ? TextAlignment.End : TextAlignment.Start,
            };

            Grid grid = new();
            var height = FlexSetting?.Height ?? CommonViewSetting.COMPOSITE_HEIGHT;
            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = FlexSetting?.FlexHeight > 0 ? FlexSetting.FlexHeight : height });
                var columns = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = LabelSetting?.Width ?? new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                };
                grid.ColumnDefinitions = columns;
                grid.Add(GroupLayout, 1);
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                var labelheight = CommonViewSetting.COMPOSITE_TANDEMLABEL_HEIGHT;
                if (LabelSetting != null && LabelSetting.Height != null && LabelSetting.Height > 0)
                    labelheight = LabelSetting.Height.Value;
                grid.RowDefinitions.Add(new RowDefinition { Height = labelheight });
                grid.RowDefinitions.Add(new RowDefinition { Height = FlexSetting?.FlexHeight > 0 ? FlexSetting.FlexHeight : height });
                CompositeHeight = labelheight + height;
                grid.ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition() };
                grid.Add(GroupLayout, 0, 1);
            }
            grid.Add(Label);
            return grid;
        }
    }
}
