using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public abstract class EvangShowComposite : EvangCompositeView
    {
        Image? ErrorIcon { get; set; }
        public Label? ShowLabel { get; set; }

        public EvangShowComposite(
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

        public override void AddControl()
        {
            ErrorIcon = new Image
            {
                Source = ImageSource.FromResource("EvangSol.Mobibrary.Resources.Images.ic_error.png"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HeightRequest = icon_width_height,
                WidthRequest = icon_width_height,
                IsVisible = false,
                Aspect = Aspect.AspectFit
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(ErrorIcon, 3);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(ErrorIcon, 2, 1);
        }

        public virtual void ShowErrorIcon()
        {
            if (ErrorIcon != null)
                ErrorIcon.IsVisible = true;
        }

        public virtual void HideErrorIcon()
        {
            if (ErrorIcon != null)
                ErrorIcon.IsVisible = false;
        }

        public override bool ShowOnly
        {
            get => ShowLabel != null && ShowLabel.IsVisible;
            set
            {
                if (value)
                {
                    if (ShowLabel == null)
                    {
                        CreateShowLabel();
                    }
                    else
                    {
                        ShowLabel.Text = Text;
                    }

                    if (InputControl != null)
                        InputControl.IsVisible = false;
                    if(ErrorIcon != null)
                        ErrorIcon!.IsVisible = false;
                    ShowLabel!.IsVisible = true;
                }
                else
                {
                    if (ShowLabel != null)
                        ShowLabel.IsVisible = false;
                    if (InputControl != null)
                        InputControl.IsVisible = true;
                }
                if (ExtraControl != null)
                {
                    ExtraControl.IsEnabled = !value;
                }
            }
        }

        public virtual void CreateShowLabel()
        {
            ShowLabel = new Label
            {
                Text = Text,
                FontSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                BackgroundColor = InputViewSetting?.BackgroundColor ?? Colors.Transparent,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                HorizontalTextAlignment = InputViewSetting?.Alignment ?? ((InputViewSetting?.InputType == "Integer" || InputViewSetting?.InputType == "Decimal") ? TextAlignment.End : TextAlignment.Start),
                VerticalTextAlignment = TextAlignment.Center,
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(ShowLabel, 2);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(ShowLabel, 1, 1);
        }
    }
}
