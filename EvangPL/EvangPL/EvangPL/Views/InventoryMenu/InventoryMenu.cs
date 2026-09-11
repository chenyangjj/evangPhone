using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;

namespace EvangPL.Views.InventoryMenu;

/// <summary>
/// 在庫管理サブメニュー画面
/// メインメニューの「在庫管理」カードから遷移する
/// </summary>
public class InventoryMenu : EvangContentVM
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

    public InventoryMenu() : base("strInventoryManagement", null)
    {
        // ヘッダーバーをナビゲーションバーに設定
        var headerBar = new Grid
        {
            BackgroundColor = HeaderColor,
            HeightRequest = 50,
            Padding = new Thickness(15, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        var headerTitle = new Label
        {
            Text = "在庫管理",
            TextColor = Colors.White,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        };

        headerBar.Add(headerTitle, 0, 0);

        NavigationPage.SetHasNavigationBar(this, true);
        NavigationPage.SetTitleView(this, headerBar);

        gridstack = new VerticalStackLayout
        {
            Padding = new Thickness(5, 5),
            Spacing = 15
        };

        var stack = new StackLayout
        {
            gridstack,
        };

        Content = new ScrollView
        {
            Content = stack
        };

        BuildCardUI();
    }

    /// <summary>
    /// カードUIを構築しgridstackに追加する
    /// </summary>
    private void BuildCardUI()
    {
        if (gridstack == null) return;

        // 3カード横並びグリッド
        var cardGrid = new Grid
        {
            RowDefinitions = { new RowDefinition { Height = 140 } },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };

        // TODO: 各カードの遷移先は後で変更する
        cardGrid.Add(CreateMenuCard("振", "在庫振替", "ロケーション間移動", "InventoryTransfer"), 0, 0);
        cardGrid.Add(CreateMenuCard("調", "棚卸調整", "増減登録", "StockAdjust"), 1, 0);
        cardGrid.Add(CreateMenuCard("照", "在庫照会", "商品番号検索", "InventoryInquiry"), 2, 0);

        gridstack.Children.Add(cardGrid);
    }

    /// <summary>
    /// サブメニューカードを作成する
    /// </summary>
    private Border CreateMenuCard(string iconText, string title, string subtitle, string? targetView)
    {
        var cardContent = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 8
        };

        var iconBox = new Border
        {
            WidthRequest = 45,
            HeightRequest = 45,
            BackgroundColor = IconBgColor,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = iconText,
                TextColor = Colors.White,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        cardContent.Children.Add(iconBox);
        cardContent.Children.Add(new Label
        {
            Text = title,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = TextPrimary,
            HorizontalOptions = LayoutOptions.Center
        });
        cardContent.Children.Add(new Label
        {
            Text = subtitle,
            FontSize = 9,
            TextColor = TextSecondary,
            HorizontalOptions = LayoutOptions.Center
        });

        var card = new Border
        {
            BackgroundColor = CardBgColor,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = Color.FromArgb("#eeeeee"),
            StrokeThickness = 1,
            Padding = new Thickness(8),
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
}