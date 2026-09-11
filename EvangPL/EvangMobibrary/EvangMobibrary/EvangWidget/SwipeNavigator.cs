using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangWidget
{
    public class SwipeNavigator : EvangContentView
    {
        public EventHandler? OnPrevious;
        public EventHandler? OnNext;

        public ImageButton? PrevArrow { get; set; }
        public ImageButton? NextArrow { get; set; }
        public Label? Folio { get; set; }

        bool onmoving;
        int debounce;       //this is for CarouselView, wait the scroll animation to finish

        public SwipeNavigator(CommonViewSetting setting, int debounce = 600)
        {
            this.debounce = debounce;

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT, GridUnitType.Absolute) },
                    new ColumnDefinition { Width = new GridLength(setting.Width ?? 300, GridUnitType.Absolute) },
                    new ColumnDefinition { Width = new GridLength(setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT, GridUnitType.Absolute) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT }
                }
            };

            PrevArrow = new ImageButton
            {
                Source = new FontImageSource
                {
                    Glyph = "\u25c0",
                    FontFamily = "OpenSansSemibold",
                    Color = GetColor("Primary")
                },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HeightRequest = setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT,
                WidthRequest = setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT,
                Aspect = Aspect.AspectFit,
                IsEnabled = false,
            };
            PrevArrow.Clicked += OnLeftArrowClicked;
            grid.Add(PrevArrow, 1);

            NextArrow = new ImageButton
            {
                Source = new FontImageSource
                {
                    Glyph = "\u25b6",
                    FontFamily = "OpenSansSemibold",
                    Color = GetColor("Primary")
                },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HeightRequest = setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT,
                WidthRequest = setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT,
                Aspect = Aspect.AspectFit,
                IsEnabled = false,
            };
            NextArrow.Clicked += OnRightArrowClicked;
            grid.Add(NextArrow, 3);

            Folio = new Label
            {
                FontSize = setting.TextSize ?? 30,
                FontAttributes = FontAttributes.Bold,
                TextColor = setting.TextColor ?? GetColor(CommonViewSetting.LABEL_FONTCOLOR),
                HorizontalTextAlignment = TextAlignment.Center,
            };
            grid.Add(Folio, 2);

            Content = grid;

            IsVisible = setting.Visibility;
        }

        private void OnLeftArrowClicked(object? sender, EventArgs e)
        {
            if (onmoving)
                return;

            onmoving = true;
            SetTimer(debounce, () => onmoving = false);
            OnPrevious?.Invoke(this, e);
        }

        private void OnRightArrowClicked(object? sender, EventArgs e)
        {
            if (onmoving)
                return;

            onmoving = true;
            SetTimer(debounce, () => onmoving = false);
            OnNext?.Invoke(this, e);
        }

        int _current;
        public int CurrentSwipe
        {
            get => _current;
            set
            {
                _current = value;
                Folio!.Text = $"{_current} / {_count}";
            }
        }

        int _count;
        public int SwipeCount
        {
            get => _count;
            set
            {
                _count = value;
                Folio!.Text = $"{_current} / {_count}";
            }
        }
    }
}
