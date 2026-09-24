using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls;

namespace EvangPL.Views.Report
{
    public class Report : EvangContentVM
    {
        private Grid? mainGrid;
        private VerticalStackLayout? buttonContainer;

        public Report() : base("strReportMenu")
        {
            Title = "レポート";
            
        }

        public override void BeforeBaseRendering(string caption, object? viewmodel)
        {
            base.BeforeBaseRendering(caption, viewmodel);

           
            BuildUI();
        }

        private void BuildUI()
        {
            mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star }
                },
                Padding = new Thickness(20, 5),
                BackgroundColor = Color.FromArgb("#f5f5f5")
            };

            buttonContainer = new VerticalStackLayout
            {
                Spacing = 18,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Start
            };

            // 按钮1：入庫履歴レポート → InboundHistory
            var btnStockInHistory = new Button
            {
                Text = "入庫履歴レポート",
                HeightRequest = 75,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#1e3a5f"),
                CornerRadius = 10,
                BorderColor = Color.FromArgb("#1e3a5f"),
                BorderWidth = 2,
                Margin = new Thickness(10, 0)
            };
            btnStockInHistory.Clicked += async (s, e) =>
            {
                await NavToPage("InboundHistory");
            };

            // 按钮2：出荷履歴レポート → OutboundHistory
            var btnStockOutHistory = new Button
            {
                Text = "出荷履歴レポート",
                HeightRequest = 75,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#1e3a5f"),
                CornerRadius = 10,
                BorderColor = Color.FromArgb("#1e3a5f"),
                BorderWidth = 2,
                Margin = new Thickness(10, 0)
            };
            btnStockOutHistory.Clicked += async (s, e) =>
            {
                await NavToPage("OutboundHistory");
            };

            // 按钮3：在庫照会 → InventoryInquiry
            var btnStockBalance = new Button
            {
                Text = "在庫照会",
                HeightRequest = 75,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#1e3a5f"),
                CornerRadius = 10,
                BorderColor = Color.FromArgb("#1e3a5f"),
                BorderWidth = 2,
                Margin = new Thickness(10, 0)
            };
            btnStockBalance.Clicked += async (s, e) =>
            {
                await NavToPage("InventoryInquiry");
            };

            buttonContainer.Add(btnStockInHistory);
            buttonContainer.Add(btnStockOutHistory);
            buttonContainer.Add(btnStockBalance);

            mainGrid.Children.Add(buttonContainer);

  
            Content = mainGrid;
        }

       
        private async Task NavToPage(string viewName)
        {
            try
            {
                var pageType = ClassMapping.CreatePageInstance(viewName);
                if (pageType == null)
                {
                    ShowError($"画面[{viewName}]が見つかりません");
                    return;
                }
                await Navigation.PushAsync(pageType);
            }
            catch (Exception ex)
            {
                ShowError($"遷移失敗：{ex.Message}");
            }
        }
    }
}