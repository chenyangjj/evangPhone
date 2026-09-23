using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;

namespace EvangPL.Views.Menu;

public class Menu : EvangContentVM
{
    private static readonly Color HeaderColor = Color.FromArgb("#1e3a5f");
    private static readonly Color BgColor = Color.FromArgb("#f5f5f5");
    private static readonly Color CardBgColor = Colors.White;
    private static readonly Color IconBgColor = Color.FromArgb("#1e3a5f");
    private static readonly Color TextPrimary = Color.FromArgb("#333333");
    private static readonly Color TextSecondary = Color.FromArgb("#888888");

    public Label? version;
    public Button? btnlogout;

    private VerticalStackLayout? gridstack;

    private readonly Dictionary<Button, MenuItem> _buttonMenuMap = new();

    List<MenuItem> _menulist =
    [];

    public Menu() : base("strMenu", null)
    {
        // ヘッダーバーをナビゲーションバーに設定
        var headerBar = new Grid
        {
            BackgroundColor = HeaderColor,
            HeightRequest = 50,
            Padding = new Thickness(15, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var headerTitle = new Label
        {
            Text = "在庫管理APP",
            TextColor = Colors.White,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        };
        headerGrid_Add(headerBar, headerTitle, 0);

        NavigationPage.SetHasNavigationBar(this, true);
        NavigationPage.SetTitleView(this, headerBar);

        version = new Label
        {
            FontSize = 10,
            TextColor = TextSecondary,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Text = LocalMemory.Account?.AccountId
        };

        btnlogout = new Button()
        {
            FontSize = CommonViewSetting.LABEL_FONTSIZE,
            TextColor = Colors.White,
            BackgroundColor = HeaderColor,
            Text = GetCustomString("strLogout") ?? "Logout",
            WidthRequest = 200,
        };
        btnlogout.Clicked += OnLogoutClicked;

        gridstack = new VerticalStackLayout
        {
            Padding = new Thickness(5, 5),
            Spacing = 15
        };

        var stack = new StackLayout
        {
            gridstack,
            btnlogout,
            version
        };

        Content = new ScrollView
        {
            Content = stack
        };

        GetBaseMasterData();
        BuildNewCardUI();
        CreateLegacyMenuButtons();
    }

    private static void headerGrid_Add(Grid grid, View view, int column)
    {
        grid.Add(view, column, 0);
    }

    /// <summary>
    /// カードUIを構築しgridstackに追加する
    /// </summary>
    private void BuildNewCardUI()
    {
        if (gridstack == null) return;

        var mainGrid = new Grid
        {
            RowDefinitions = { new RowDefinition { Height = 140 }, new RowDefinition { Height = 140 } },
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
            RowSpacing = 15,
            ColumnSpacing = 15
        };

        // TODO: 各カードの遷移先は後でサブメニュー画面に変更する
        mainGrid.Add(CreateMainCard("入", "入庫処理", "PO/RMA/振替", "InboundSearch"), 0, 0);
        mainGrid.Add(CreateMainCard("出", "出荷処理", "SO/返品/振替", "OutboundSearch"), 1, 0);
        mainGrid.Add(CreateMainCard("庫", "在庫管理", "振替/棚卸/照会", "InventoryMenu"), 0, 1);
        mainGrid.Add(CreateMainCard("表", "レポート", "入出庫/在庫照会", "Report"), 1, 1);

        gridstack.Children.Add(mainGrid);

        gridstack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#dddddd"), Margin = new Thickness(0, 10, 0, 5) });
        gridstack.Children.Add(new Label
        {
            Text = "その他の機能",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = TextPrimary,
            Margin = new Thickness(0, 5, 0, 5)
        });
    }

    /// <summary>
    /// メイン機能カードを作成する
    /// </summary>
    private Border CreateMainCard(string iconText, string title, string subtitle, string? targetView)
    {
        var cardContent = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 10
        };

        var iconBox = new Border
        {
            WidthRequest = 50,
            HeightRequest = 50,
            BackgroundColor = IconBgColor,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = iconText,
                TextColor = Colors.White,
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        cardContent.Children.Add(iconBox);
        cardContent.Children.Add(new Label
        {
            Text = title,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = TextPrimary,
            HorizontalOptions = LayoutOptions.Center
        });
        cardContent.Children.Add(new Label
        {
            Text = subtitle,
            FontSize = 12,
            TextColor = TextSecondary,
            HorizontalOptions = LayoutOptions.Center
        });

        var card = new Border
        {
            BackgroundColor = CardBgColor,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = Color.FromArgb("#eeeeee"),
            StrokeThickness = 1,
            Padding = new Thickness(10),
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.05f, Radius = 5, Offset = new Point(0, 2) },
            Content = cardContent
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            card.BackgroundColor = Color.FromArgb("#f0f0f0");
            if (string.IsNullOrEmpty(targetView))
            {
                // 遷移先が未定の場合はアラートを表示
                await DisplayAlert("準備中", $"{title}機能は現在開発中です。", "OK");
            }
            else
            {
                await NavigateToView(targetView);
            }
            card.BackgroundColor = CardBgColor;
        };
        card.GestureRecognizers.Add(tapGesture);

        return card;
    }

    /// <summary>
    /// 既存メニューボタンを作成しgridstackに追加する
    /// </summary>
    protected virtual void CreateLegacyMenuButtons()
    {
        if (gridstack == null) return;
        _buttonMenuMap.Clear();

        foreach (var item in _menulist)
        {
            var btn = new Button
            {
                Text = S(item.Name!),
                FontSize = 15,
                TextColor = Colors.White,
                BackgroundColor = HeaderColor,
                CornerRadius = 8,
                HeightRequest = 45,
                HorizontalOptions = LayoutOptions.Fill
            };

            _buttonMenuMap[btn] = item;
            btn.Clicked += OnMenuButtonClick;
            gridstack.Children.Add(btn);
        }
    }

    public async virtual void OnLogoutClicked(object? sender, EventArgs e)
    {
    }

    /// <summary>
    /// 画面遷移処理
    /// </summary>
    private async Task NavigateToView(string? viewName)
    {
        if (string.IsNullOrEmpty(viewName)) return;

        try
        {
            var pagetype = ClassMapping.CreatePageInstance(viewName);
            if (pagetype == null)
            {
                ShowError($"Can not create the view {viewName}.");
                return;
            }
            (pagetype as EvangContentVM)!.IsFromMenu = true;
            await Navigation.PushAsync(pagetype as EvangContentVM);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException != null)
                msg += Environment.NewLine + ex.InnerException.Message;
            ShowError(msg);
        }
    }

    /// <summary>
    /// メニューボタンクリックイベント
    /// </summary>
    protected virtual async void OnMenuButtonClick(object? sender, EventArgs e)
    {
        if (sender is not Button menubtn) return;
        if (!_buttonMenuMap.TryGetValue(menubtn, out var menuinfo)) return;
        await NavigateToView(menuinfo.View);
    }

    private async void GetBaseMasterData()
    {
        if (LocalMemory.Account?.EntryUrl == null)
            return;

        LocalMemory.restlets.Clear();

        var entry = "EntryPoint";
        LocalMemory.restlets.Add(entry, LocalMemory.Account.EntryUrl);
        var result = await this.Post(entry);
        if (result == null || result.SubData == null)
            return;

        foreach (var item in result.SubData)
        {
            switch (item.SubName)
            {
                case "restlet":
                    var restlets = BaseUtils.JsonToClass<EvangDatum<EvangJsonModel, RestletInfo>>(item.SubJson!);
                    if (restlets != null && restlets.Data != null)
                        foreach (var info in restlets.Data)
                            LocalMemory.restlets.Add(info.restlet_id!, info.restlet_url);
                    break;
                case "unit":
                    var units = BaseUtils.JsonToClass<EvangDatum<EvangJsonModel, MasterUnit>>(item.SubJson!);
                    if (units != null && units.Data != null)
                        LocalMemory.SetMaster("unit", units.Data);
                    break;
                case "unitexchange":
                    var unitexchgs = BaseUtils.JsonToClass<EvangDatum<EvangJsonModel, MasterUnitExchg>>(item.SubJson!);
                    if (unitexchgs != null && unitexchgs.Data != null)
                        LocalMemory.SetMaster("unitexchange", unitexchgs.Data);
                    break;
            }
        }

        //test code
        LocalMemory.restlets.Clear();
        LocalMemory.restlets.Add("GetItemrecept", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2063&deploy=1");
        LocalMemory.restlets.Add("Getitemfulfill", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2065&deploy=1");
        LocalMemory.restlets.Add("GetInventdetail", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2066&deploy=1");
        LocalMemory.restlets.Add("GetAdjust", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2067&deploy=1");
        LocalMemory.restlets.Add("SaveAdjust", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2068&deploy=1");
        LocalMemory.restlets.Add("GetTransfer", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2071&deploy=1");
        LocalMemory.restlets.Add("SaveTransfer", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2072&deploy=1");
        LocalMemory.restlets.Add("GetStockInDetail", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2061&deploy=1");
        LocalMemory.restlets.Add("SaveStockIn", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2061&deploy=1");
        LocalMemory.restlets.Add("GetOrderList", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2056&deploy=1");
        
        LocalMemory.restlets.Add("GetStockOutList", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2069&deploy=1");
        LocalMemory.restlets.Add("GetPickingDetail", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2070&deploy=1");
    }
}