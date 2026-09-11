using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.EvangModel;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;

namespace EvangPL.Components
{
    /// <summary>
    /// 入庫履歴単一カードコンポーネント
    /// </summary>
    public class InboundHistoryCard : Border
    {
        public InboundHistoryCard(string slipNo, string itemName, string supplier, string status, double quantity)
        {
            BuildUI(slipNo, itemName, supplier, status, quantity);
        }

        private void BuildUI(string slipNo, string itemName, string supplier, string status, double quantity)
        {
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }, // ヘッダー行（伝票No + ステータス）
                    new RowDefinition { Height = GridLength.Auto }, // 品目行
                    new RowDefinition { Height = GridLength.Auto }  // 取引先 + 数量行
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowSpacing = 6,
                Margin = new Thickness(0)
            };

            // --- 第1行: 伝票No & ステータスバッジ ---
            var headerRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var slipLayout = new HorizontalStackLayout();
            var slipLabel = new Label
            {
                Text = "伝票No: ",
                FontSize = 14,
                TextColor = Colors.Gray,
                VerticalTextAlignment = TextAlignment.Center
            };
            var slipValue = new Label
            {
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1f3854"),
                Text = slipNo,
                VerticalTextAlignment = TextAlignment.Center
            };
            slipLayout.Children.Add(slipLabel);
            slipLayout.Children.Add(slipValue);
            Grid.SetColumn(slipLayout, 0);
            headerRow.Children.Add(slipLayout);

            // ステータスバッジ
            var statusBorder = new Border
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(5) },
                Padding = new Thickness(8, 3),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                MinimumWidthRequest = 60
            };

            var statusLabel = new Label
            {
                Text = status,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            statusBorder.BackgroundColor = GetStatusColor(status);
            statusBorder.Content = statusLabel;
            Grid.SetColumn(statusBorder, 1);
            headerRow.Children.Add(statusBorder);

            Grid.SetColumnSpan(headerRow, 2);
            mainGrid.Children.Add(headerRow);
            Grid.SetRow(headerRow, 0);

            // --- 第2行: 品目名 ---
            var itemLayout = new HorizontalStackLayout
            {
                Margin = new Thickness(0)
            };
            var itemLabelPrefix = new Label
            {
                Text = "品目: ",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var itemNameLabel = new Label
            {
                FontSize = 13,
                TextColor = Colors.Black,
                Text = itemName,
                FontAttributes = FontAttributes.Bold
            };
            itemLayout.Children.Add(itemLabelPrefix);
            itemLayout.Children.Add(itemNameLabel);

            Grid.SetColumnSpan(itemLayout, 2);
            mainGrid.Children.Add(itemLayout);
            Grid.SetRow(itemLayout, 1);

            // --- 第3行: 取引先 & 数量 ---
            var footerRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // 左側: 取引先
            var supplierLayout = new HorizontalStackLayout();
            var supplierLabelPrefix = new Label
            {
                Text = "取引先: ",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var supplierValue = new Label
            {
                FontSize = 13,
                TextColor = Colors.Black,
                Text = supplier
            };
            supplierLayout.Children.Add(supplierLabelPrefix);
            supplierLayout.Children.Add(supplierValue);
            Grid.SetColumn(supplierLayout, 0);
            footerRow.Children.Add(supplierLayout);

            // 右側: 数量
            var quantityLayout = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.End
            };
            var qtyLabelPrefix = new Label
            {
                Text = "数量: ",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var qtyValue = new Label
            {
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#4CAF50"),
                Text = quantity.ToString("N0") // 3桁区切り表示
            };
            var unitLabel = new Label
            {
                Text = " 個",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            quantityLayout.Children.Add(qtyLabelPrefix);
            quantityLayout.Children.Add(qtyValue);
            quantityLayout.Children.Add(unitLabel);
            Grid.SetColumn(quantityLayout, 1);
            footerRow.Children.Add(quantityLayout);

            Grid.SetColumnSpan(footerRow, 2);
            mainGrid.Children.Add(footerRow);
            Grid.SetRow(footerRow, 2);

            // カード全体のスタイル設定
            this.Stroke = Color.FromArgb("#e0e0e0");
            this.StrokeThickness = 1;
            this.StrokeShape = new RoundRectangle { CornerRadius = 5 };
            this.BackgroundColor = Colors.White;
            this.Padding = new Thickness(15);
            this.Margin = new Thickness(10, 5);
            this.Content = mainGrid;
        }

        /// <summary>
        /// ステータスに応じた背景色を返す
        /// </summary>
        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "入庫済み" => Color.FromArgb("#4CAF50"),   // 緑
                "一部入庫" => Color.FromArgb("#FF9800"),   // オレンジ
                "未入庫" => Color.FromArgb("#F44336"),     // 赤
                _ => Color.FromArgb("#757575")             // グレー
            };
        }
    }

    /// <summary>
    /// 入庫履歴データモデル
    /// </summary>
    public class InboundRecord : EvangJsonModel
    {
        public string SlipNo { get; set; }
        public string ItemName { get; set; }
        public string Supplier { get; set; }
        public string Status { get; set; }
        public double Quantity { get; set; }

        public InboundRecord(string slipNo, string itemName, string supplier, string status, double quantity)
        {
            SlipNo = slipNo;
            ItemName = itemName;
            Supplier = supplier;
            Status = status;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// 入庫履歴カード一覧ビュー（ページング対応）
    /// </summary>
    public class InboundHistoryCardView : EvangContentView
    {
        private StackLayout cardContainer;

        public InboundHistoryCardView(List<InboundRecord>? pageData)
        {
            BuildUI(pageData);
        }

        private void BuildUI(List<InboundRecord>? pageData)
        {
            var mainLayout = new VerticalStackLayout
            {
                Spacing = 0,
                BackgroundColor = Color.FromArgb("#eff0f0")
            };

            cardContainer = new StackLayout
            {
                Spacing = 0,
                Padding = new Thickness(0)
            };

            if (pageData != null && pageData.Count > 0)
            {
                foreach (var item in pageData)
                {
                    var card = new InboundHistoryCard(item.SlipNo, item.ItemName, item.Supplier, item.Status, item.Quantity);
                    cardContainer.Children.Add(card);
                }
            }

            mainLayout.Children.Add(cardContainer);
            Content = new ScrollView
            {
                Content = mainLayout
            };
        }

        /// <summary>
        /// ページ切り替え時にデータを更新する
        /// </summary>
        public void UpdateData(List<InboundRecord>? pageData)
        {
            cardContainer.Children.Clear();
            if (pageData != null && pageData.Count > 0)
            {
                foreach (var item in pageData)
                {
                    var card = new InboundHistoryCard(item.SlipNo, item.ItemName, item.Supplier, item.Status, item.Quantity);
                    cardContainer.Children.Add(card);
                }
            }
        }
    }
}