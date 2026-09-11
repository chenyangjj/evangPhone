using EvangSol.Mobibrary.EvangCustom;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.TitleBar;

namespace EvangPL.TitleBar;

public class MenuTitle : EvangTitleBar
{
    public override Grid? GridLayout { get; set; }

    public Label? Title { get; set; }
    public PopupMenu? Setting { get; set; }
    public TopIconImage? MenuIcon { get; set; }

    public MenuTitle(INavigation navi, string caption) : base(navi, caption)
    {
    }

    public override View GetTitleView(double width)
    {
        GridLayout = new Grid
        {
            RowDefinitions = { new RowDefinition() },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(64, GridUnitType.Absolute) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(64, GridUnitType.Absolute) },
            }
        };

        Title = new Label
        {
            FontSize = title_fontsize,
            TextColor = GetColor(title_textcolor),
            FontAttributes = GetFontAttr(title_fontattr) ?? FontAttributes.None,
            HorizontalTextAlignment = GetAlignment(title_alignment) ?? TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(10, 0),
            Text = Caption,
        };
        GridLayout.Add(Title, 1);

        MenuIcon = new TopIconImage
        {
            Source = new FontImageSource
            {
                Glyph = "\u2630",
                FontFamily = "OpenSansSemibold",
            },
#if WINDOWS
            HeightRequest = 32,
            WidthRequest = 32,
#endif
        };
        GridLayout.Add(MenuIcon, 2);

        Setting = new PopupMenu();
        var menuItems = new List<KeyValuePair<string, PopupMenu.MenuItemSetting?>>
        {
            new KeyValuePair<string, PopupMenu.MenuItemSetting?>(S("lblPasswordReset"), null),
        };
        CustomMenuItem(ref menuItems);
        Setting.MenuItems = menuItems;
#if ANDROID
        Setting.OnTapped += OnIconTapped;
#elif WINDOWS
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnIconTapped;
        MenuIcon.GestureRecognizers.Add(tapGesture);
#endif
        Setting.OnMenuTapped += OnMenuTapped;
        GridLayout.Add(Setting, 2);

        return GridLayout;
    }

    public virtual async void OnMenuTapped(object? sender, MenuTappedEventArgs e)
    {
        //if (e.Index == 0 && Navi != null)
        //{
        //    if (DeviceInfo.Idiom != DeviceIdiom.Phone)
        //    {
        //        TabPasswordReset? page = ClassMapping.CreatePageInstance(typeof(TabPasswordReset)) as TabPasswordReset;
        //        if (page != null)
        //        {
        //            page.IsFromMenu = true;
        //            await Navi.PushAsync(page);
        //        }
        //    }
        //    else
        //    {
        //        PasswordReset? page = ClassMapping.CreatePageInstance(typeof(PasswordReset)) as PasswordReset;
        //        if (page != null)
        //        {
        //            page.IsFromMenu = true;
        //            await Navi.PushAsync(page);
        //        }
        //    }
        //    Setting?.DoHideMenu();
        //}
    }

    private void OnIconTapped(object? sender, TappedEventArgs e)
    {
        ShowPopupMenu(MenuIcon!, Setting!);
    }
}
