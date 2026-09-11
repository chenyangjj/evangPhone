using EvangSol.Mobibrary.EvangCustom;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.TitleBar;

namespace EvangPL.TitleBar;

public class LoginTitle : EvangTitleBar
{
    public override Grid? GridLayout { get; set; }

    public Label? Title { get; set; }
    public PopupMenu? Setting { get; set; }
    public TopIconImage? CogIcon { get; set; }

    public LoginTitle(INavigation navi, string caption) : base(navi, caption)
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
            },
            BackgroundColor = GetColor("Primary")
        };
#if WINDOWS
        GridLayout.ColumnDefinitions[1].Width = width - 128;
#endif

        GridLayout.Add(GetLogo());

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

        CogIcon = new TopIconImage { Source = ImageSource.FromResource("TestEvangMobibrary.Resources.Images.setting.png") };
        GridLayout.Add(CogIcon, 2);

        Setting = new PopupMenu();
        var menuItems = new List<KeyValuePair<string, PopupMenu.MenuItemSetting?>>
        {
            new KeyValuePair<string, PopupMenu.MenuItemSetting?>(S("strSetting"), null),
            new KeyValuePair<string, PopupMenu.MenuItemSetting?>(S("strLicense"), null),
        };
        CustomMenuItem(ref menuItems);
        Setting.MenuItems = menuItems;
#if ANDROID
        Setting.OnTapped += OnIconTapped;
#elif WINDOWS
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnIconTapped;
        CogIcon.GestureRecognizers.Add(tapGesture);
#endif
        Setting.OnMenuTapped += OnMenuTapped;
        GridLayout.Add(Setting, 2);

        return GridLayout;
    }

    public virtual TopIconImage? GetLogo()
    {
        return null;
    }

    public virtual async void OnMenuTapped(object? sender, MenuTappedEventArgs e)
    {
        //if (e.Index == 0)
        //{
        //    if (Navi != null)
        //        await Navi.PushAsync(DeviceInfo.Idiom != DeviceIdiom.Phone ? ClassMapping.CreatePageInstance(typeof(TabEditConfigure)) : ClassMapping.CreatePageInstance(typeof(EditConfigure)));
        //}
        //else if (e.Index == 1)
        //{
        //    if (Navi != null)
        //        await Navi.PushAsync(DeviceInfo.Idiom != DeviceIdiom.Phone ? ClassMapping.CreatePageInstance(typeof(TabLicense)) : ClassMapping.CreatePageInstance(typeof(License)));
        //}
        Setting?.DoHideMenu();
    }

    private void OnIconTapped(object? sender, TappedEventArgs e)
    {
        ShowPopupMenu(CogIcon!, Setting!);
    }
}
