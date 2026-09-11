using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.PlatformControl
{
    public class PopupMenu : View
    {
        public static int menu_width = DeviceInfo.Idiom != DeviceIdiom.Phone ? 300 : 200;
        public static int menu_item_height = DeviceInfo.Idiom != DeviceIdiom.Phone ? 50 : 45;
        public static int menu_textsize = (int)CommonViewSetting.INPUT_FONTSIZE;

        public class MenuSetting
        {
            public int? Width { get; set; }
            public int? Height { get; set; }
            public int? TextSize { get; set; }
            public double? Density { get; set; }
            public string? ImageName { get; set; }
            public bool Hidden { get; set; }
            public bool ShowOnly { get; set; }
        }

        public class MenuItemSetting : MenuSetting
        {
            public object? Data { get; set; }
        }


        public static readonly BindableProperty MenuItemsProperty =
            BindableProperty.Create(nameof(MenuItems), typeof(List<KeyValuePair<string, MenuItemSetting?>>), typeof(PopupMenu), new List<KeyValuePair<string, MenuItemSetting?>>());
        public List<KeyValuePair<string, MenuItemSetting?>> MenuItems
        {
            get => (List<KeyValuePair<string, MenuItemSetting?>>)GetValue(MenuItemsProperty);
            set => SetValue(MenuItemsProperty, value);
        }

        public event EventHandler? ShowMenu;
        public void DoShowMenu(int x, int y, List<(int, bool)>? enablist = null)
        {
            ShowMenu?.Invoke(this, new ShowMenuEventArgs { x = x, y = y, enablist = enablist });
            Handler?.Invoke(nameof(ShowMenu), new ShowMenuEventArgs { x = x, y = y, enablist = enablist });
        }

        public event EventHandler? HideMenu;
        public void DoHideMenu()
        {
            HideMenu?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(HideMenu), EventArgs.Empty);
        }


        public MenuSetting Setting { get; set; }
        public EventHandler<TappedEventArgs>? OnTapped;

        public PopupMenu(MenuSetting? setting = null)
        {
            Setting = setting ?? new MenuSetting
            {
                Width = (int)(menu_width * DeviceDisplay.Current.MainDisplayInfo.Density),
                Height = (int)(menu_item_height * DeviceDisplay.Current.MainDisplayInfo.Density),
                TextSize = menu_textsize,
                Density = DeviceDisplay.Current.MainDisplayInfo.Density
            };

            TapGestureRecognizer recognizer = new TapGestureRecognizer();
            recognizer.Tapped += (s, e) => OnTapped?.Invoke(s, e);
            GestureRecognizers.Add(recognizer);
        }

        public event EventHandler<MenuTappedEventArgs>? OnMenuTapped;
        public void MenuClicked(int index, string text, object? data)
        {
            OnMenuTapped?.Invoke(this, new MenuTappedEventArgs { Index = index, Text = text, Data = data });
        }
    }

    public class ShowMenuEventArgs : EventArgs
    {
        public int x { get; set; }
        public int y { get; set; }
        public List<(int, bool)>? enablist { get; set; }
    }

    public class MenuTappedEventArgs : EventArgs
    {
        public int Index { get; set; }
        public string? Text { get; set; }
        public object? Data { get; set; }
    }
}
