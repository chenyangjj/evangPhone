using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public enum CompositeLayoutPattern
    {
        Parallel,
        Tandem,
    }

    public abstract class EvangCompositeView : EvangContentView, IInputControl
    {
        public static double COMPOSITE_HEIGHT = CommonViewSetting.COMPOSITE_HEIGHT;     //default height
        public static double COMPOSITE_TANDEMLABEL_HEIGHT = CommonViewSetting.COMPOSITE_TANDEMLABEL_HEIGHT; //default tandem label height
        readonly Thickness label_padding = new Thickness(5, 0); //default label padding
        protected const double icon_width_height = 28;          //default icon width and height

        public double CompositeHeight { get; set; }
        public double GivenHeight { get; set; }
        public Label Label { get; set; }
        public bool Required { get; set; }
        public bool ShowKeyBoardIcon { get; set; }
        public CommonViewSetting? LabelViewSetting { get; set; }
        public CommonViewSetting? InputViewSetting { get; set; }
        public int RightWidth { get; set; }
        public CompositeLayoutPattern Pattern { get; set; }
        public Grid? Grid { get; set; }
        public View? ExtraControl { get; set; }
        public string? ExtraControlName { get; set; }    //SIR0189273
        public string? ControlName { get; set; }

        public EvangCompositeView(
            string labeltext,
            double height,
            bool required = false,
            bool keyboardicon = false,
            CommonViewSetting? labelviewsetting = null,
            CommonViewSetting ? inputviewsetting = null,
            int rightwidth = 0,
            CompositeLayoutPattern? pattern = null)
        {
            GivenHeight = height;
            Required = required;
            ShowKeyBoardIcon = keyboardicon;
            LabelViewSetting = labelviewsetting;
            InputViewSetting = inputviewsetting;
            RightWidth = rightwidth;
            Pattern = pattern ?? (DeviceInfo.Idiom == DeviceIdiom.Phone ? CompositeLayoutPattern.Tandem : CompositeLayoutPattern.Parallel);

            if(height <= 0)
                height = COMPOSITE_HEIGHT;

            Grid = CreateGrid(height);

            var fontsize = CommonViewSetting.LABEL_FONTSIZE;
            if (LabelViewSetting != null && LabelViewSetting.TextSize > 0)
                fontsize = (int)LabelViewSetting.TextSize;

            Label = new Label
            {
                Text = GetCustomString(labeltext) ?? labeltext,
                FontSize = fontsize,
                TextColor = LabelViewSetting?.TextColor ?? GetColor(CommonViewSetting.LABEL_FONTCOLOR),
                BackgroundColor = LabelViewSetting?.BackgroundColor ?? Colors.Transparent,
                FontAttributes = LabelViewSetting?.FontAttributes ?? FontAttributes.Bold,
                Padding = label_padding,
                HorizontalTextAlignment = LabelViewSetting?.Alignment ?? (Pattern == CompositeLayoutPattern.Tandem ? TextAlignment.Start : TextAlignment.End),
                VerticalTextAlignment = Pattern == CompositeLayoutPattern.Tandem ? TextAlignment.End : TextAlignment.Center,
                IsVisible = (Pattern == CompositeLayoutPattern.Tandem && LabelViewSetting?.LabelHeight != 0) || (Pattern == CompositeLayoutPattern.Parallel && LabelViewSetting?.Width != 0),//SIR0189147
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(Label, GetLabelPosition());
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.AddWithSpan(Label, columnSpan: Grid.ColumnDefinitions.Count);

            AddControl();

            Content = Grid;

            IsVisible = labelviewsetting == null || labelviewsetting.Visibility;

        }

        public virtual int GetLabelPosition()
        {
            return 0;
        }

        public virtual Grid CreateGrid(double height)
        {
            Grid grid = new();
            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = height });
                CompositeHeight = height;
                var columns = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = LabelViewSetting?.Width ?? new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition { Width = InputViewSetting?.Width ?? new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }
                };
                if (RightWidth > 0)
                    columns.Add(new ColumnDefinition { Width = new GridLength(RightWidth, GridUnitType.Absolute) });
                grid.ColumnDefinitions = columns;
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                var labelheight = COMPOSITE_TANDEMLABEL_HEIGHT;
                //SIR0189147
                if (LabelViewSetting != null && LabelViewSetting.LabelHeight >= 0)
                    labelheight = LabelViewSetting.LabelHeight;
                grid.RowDefinitions.Add(new RowDefinition { Height = labelheight });
                grid.RowDefinitions.Add(new RowDefinition { Height = height });
                CompositeHeight = labelheight + height;
                var columns = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition { Width = InputViewSetting?.Width ?? new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }
                };
                if (RightWidth > 0)
                    columns.Add(new ColumnDefinition { Width = new GridLength(RightWidth, GridUnitType.Absolute) });
                grid.ColumnDefinitions = columns;
            }
            return grid;
        }

        public virtual void InsertExtraControl(View control, string? name)
        {
            if (RightWidth > 0)
            {
                if (Pattern == CompositeLayoutPattern.Parallel)
                {
                    Grid.Add(control, 4);
                    ExtraControl = control;
                }
                else if (Pattern == CompositeLayoutPattern.Tandem)
                {
                    Grid.Add(control, 3, 1);
                    ExtraControl = control;
                }
                ExtraControlName = name;    //SIR0189273
            }
        }

        public Color? DefaultBackgroudColor => Required ? GetColor("RequiredBackground") : GetColor("OptionalBackground");

        public abstract void AddControl();

        public abstract View? InputControl { get; }

        public abstract void SetInputBinding(string propname, string? unit = null, BindableBrokerView? bpvm = null);

        #region IInputControl
        public abstract bool ShowOnly { get; set; }

        public abstract string Text { get; set; }

        public virtual string Value { get => Text; set => Text = value; }

        public IList<Behavior> ControlBehaviors => InputControl?.Behaviors ?? new List<Behavior>();

        public abstract void Blur();
        #endregion
    }
}
