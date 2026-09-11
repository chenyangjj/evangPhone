using EvangSol.Mobibrary.EvangWidget;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Converter;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class DatePickerCompareComposite : DatePickerComposite
    {
        public StackLayout? Stack { get; set; }

        Label? ExtraShowLabel;

        public DatePickerCompareComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public DatePickerCompareComposite(
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

            Stack = new StackLayout
            {
                BackgroundColor = DefaultBackgroudColor,
            };

            var box = new BoxView
            {
                WidthRequest = 8,
                HeightRequest = COMPOSITE_HEIGHT,
                BackgroundColor = Colors.Transparent
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                Grid.Add(Stack, 1);
                Grid.Add(box, 2);
                Grid.Add(DateTimeStack, 3);
                Grid.Add(ClearIcon, 4);
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                Grid.Add(Stack, 0, 1);
                Grid.Add(box, 1, 1);
                Grid.Add(DateTimeStack, 2, 1);
                Grid.Add(ClearIcon, 3, 1);
            }
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
                    new ColumnDefinition { Width = RightWidth },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
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
                    new ColumnDefinition { Width = RightWidth },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                };
                grid.ColumnDefinitions = columns;
            }
            return grid;
        }

        public override void InsertExtraControl(View control, string? name)
        {
            ExtraControl = control;
            Stack!.Add(control);
            if (ExtraControl is UnifiedDropDown udd)
            {
                udd.OnDropDownLoaded += OnDropDownLoaded;
            }
            ExtraControlName = name;    //SIR0189273
        }

        private void OnDropDownLoaded(object? sender, UnifiedPickerLoadedEventArgs e)
        {
            if (ExtraShowLabel != null)
                ExtraShowLabel.Text = e.Text;
        }

        bool _showonly;
        public override bool ShowOnly
        {
            get => _showonly;
            set
            {
                if (ShowLabel == null || ExtraShowLabel == null)
                {
                    CreateShowLabel();
                }
                ShowLabel!.Text = Text;
                if (ExtraControl != null && ExtraControl is UnifiedDropDown udd)
                    ExtraShowLabel!.Text = udd.Text;

                _showonly = value;
                if (ShowLabel != null)
                    ShowLabel.IsVisible = value;
                if (ExtraShowLabel != null)
                    ExtraShowLabel.IsVisible = value;
                if (DateTimeStack != null)
                    DateTimeStack.IsVisible = !value;
                if (Stack != null)
                    Stack.IsVisible = !value;
            }
        }

        public override void CreateShowLabel()
        {
            ShowLabel = new Label
            {
                Text = Text,
                FontSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                BackgroundColor = InputViewSetting?.BackgroundColor ?? Colors.Transparent,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                VerticalTextAlignment = TextAlignment.Center,
                IsVisible = false,
            };
            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(ShowLabel, 3);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(ShowLabel, 2, 1);

            ExtraShowLabel = new Label
            {
                Text = Text,
                FontSize = LabelViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                BackgroundColor = LabelViewSetting?.BackgroundColor ?? Colors.Transparent,
                TextColor = LabelViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = LabelViewSetting?.FontAttributes ?? FontAttributes.None,
                VerticalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(5, 0, 0, 0),
                IsVisible = false,
            };
            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(ExtraShowLabel, 1);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(ExtraShowLabel, 0, 1);
        }
    }
}
