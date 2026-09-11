using EvangSol.Mobibrary.Utilities.Common;
#if ANDROID
using Android.Widget;
#endif

namespace EvangSol.Mobibrary.EvangComposite
{
    public abstract class EvangIconComposite : EvangShowComposite
    {
        public event EventHandler<EventArgs>? OnClear; //SIR0188536

        public Microsoft.Maui.Controls.ImageButton? KeyBoardIcon { get; set; }
        public Microsoft.Maui.Controls.ImageButton? ClearIcon { get; set; }

        public EvangIconComposite(
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
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
#if ANDROID
            if (ShowKeyBoardIcon && InputControl!.Handler?.PlatformView is EditText et)
                et.ShowSoftInputOnFocus = false;
#endif
        }

        public override void AddControl()
        {
            base.AddControl();

            if (ShowKeyBoardIcon)
            {
                KeyBoardIcon = new Microsoft.Maui.Controls.ImageButton
                {
                    Source = ImageSource.FromResource("EvangSol.Mobibrary.Resources.Images.ic_keyboard.png"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HeightRequest = icon_width_height,
                    WidthRequest = icon_width_height,
                    IsVisible = true,
                    BackgroundColor = DefaultBackgroudColor,
                    BorderWidth = 0,
                    BorderColor = Colors.Transparent,
                    Aspect = Aspect.AspectFit
                };
                KeyBoardIcon.Clicked += OnKeyBoardIconClicked;

                if (Pattern == CompositeLayoutPattern.Parallel)
                    Grid.Add(KeyBoardIcon, 1);
                else if (Pattern == CompositeLayoutPattern.Tandem)
                    Grid.Add(KeyBoardIcon, 0, 1);
            }

            ClearIcon = new Microsoft.Maui.Controls.ImageButton
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
            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => OnClearEntryClicked(s, e);
            ClearIcon!.GestureRecognizers.Add(tapGestureRecognizer);
#endif

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(ClearIcon, 3);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(ClearIcon, 2, 1);
        }

        public abstract void OnKeyBoardIconClicked(object? sender, EventArgs e);

        //SIR0188536
        public virtual void OnClearEntryClicked(object? sender, EventArgs e)
        {
            OnClear?.Invoke(this, EventArgs.Empty);
        }

        public override void ShowErrorIcon()
        {
            //SIR0188663 fix a bug that error icon is not showing
            Blur();
            if (ClearIcon != null)
                ClearIcon.IsVisible = false;
            base.ShowErrorIcon();
        }

        public override bool ShowOnly
        {
            get => ShowLabel != null && ShowLabel.IsVisible;
            set
            {
                base.ShowOnly = value;

                if (value)
                {
                    if (KeyBoardIcon != null)
                        KeyBoardIcon.IsVisible = false;
                    if (ClearIcon != null)
                        ClearIcon.IsVisible = false;
                }
                else
                {
                    if (KeyBoardIcon != null)
                        KeyBoardIcon.IsVisible = true;
                }
            }
        }
    }
}
