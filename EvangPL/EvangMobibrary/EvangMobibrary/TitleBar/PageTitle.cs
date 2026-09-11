using EvangSol.Mobibrary.PlatformControl;
using CommunityToolkit.Mvvm.Messaging;
using static EvangSol.Mobibrary.PlatformControl.PopupMenu;
using EvangSol.Mobibrary.EvangCustom;

namespace EvangSol.Mobibrary.TitleBar
{
    public class PageTitle : EvangTitleBar
    {
        public override Grid? GridLayout { get; set; }

        public Label? Title { get; set; }
        public TopIconImage? DeviateIcon { get; set; }
        public PopupMenu? PopMenu { get; set; }
        public TopIconImage? MenuIcon { get; set; }

        public PageTitle(INavigation navi, string caption) : base(navi, caption)
        {
        }

        public override View GetTitleView(double width)
        {
            GridLayout = new Grid
            {
                RowDefinitions = { new RowDefinition() },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(64, GridUnitType.Auto) },
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
            GridLayout.Add(Title);

            DeviateIcon = new TopIconImage
            {
                Source = new FontImageSource
                {
                    Glyph = "\u26d4",
                    FontFamily = "OpenSansSemibold",
                },
                IsVisible = false,
#if WINDOWS
                HeightRequest = 32,
                WidthRequest = 32,
#endif
            };
            GridLayout.Add(DeviateIcon, 1);

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

            var menulist = new List<KeyValuePair<string, PopupMenu.MenuItemSetting?>>();
            CustomMenuItem(ref menulist);

            PopMenu = new PopupMenu();
            PopMenu.MenuItems = menulist;
#if ANDROID
            PopMenu.OnTapped += OnIconTapped;
#elif WINDOWS
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnIconTapped;
            MenuIcon.GestureRecognizers.Add(tapGesture);
#endif
            PopMenu.OnMenuTapped += OnMenuTapped;

            GridLayout.Add(PopMenu, 2);
            return GridLayout;
        }

        protected override void CustomMenuItem(ref List<KeyValuePair<string, MenuItemSetting?>> menulist)
        {
            //var access = LocalMemory.GetMaster("usrgrpfuncaccessf") as List<MasterAccess>;
            //if (access != null)
            //{
            //    foreach (var item in access)
            //    {
            //        if (item.allowFlg == "Y")
            //        {
            //            FunctionInfo func = (from x in LocalMemory.funclist where x.funcId == item.functionId select x).First();
            //            menulist.Add(new KeyValuePair<string, PopupMenu.MenuItemSetting?>(GetCustomString(func.funcName!) ?? func.funcName!, new PopupMenu.MenuItemSetting { Data = func }));
            //        }
            //    }
            //}
        }

        public virtual void OnIconTapped(object? sender, TappedEventArgs e)
        {
            ShowPopupMenu(MenuIcon!, PopMenu!);
        }

        public virtual async void OnMenuTapped(object? sender, MenuTappedEventArgs e)
        {
            //if ((e.Data is MenuItemSetting) && (e.Data as MenuItemSetting)!.Data is FunctionInfo funcinfo)
            //{
            //    await Navi!.PopToRootAsync(false);
            //    WeakReferenceMessenger.Default.Send(new PopupMenuMessage(funcinfo.winId!));
            //}
            PopMenu!.DoHideMenu();
        }
    }
}
