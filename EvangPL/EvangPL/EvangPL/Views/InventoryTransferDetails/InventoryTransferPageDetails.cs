//using Android.Webkit;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using EvangPL.Components;
using EvangPL.Utils;
using MauiIcons.Core;
using MauiIcons.Fluent;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics.Text;
using System.Collections.Generic;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using InventoryTransferInfo = EvangPL.Utils.InventoryTransferInfo;

namespace EvangPL.Views.InventoryTransferPageDetails
{
    public class InventoryTransferPageDetails : EvangContentVM
    {
        private Border? pageHeaderInfo;
        private readonly string poNo = "PO-2026-0114";
        private readonly string customName = "山田工業(株)";
        private readonly string arrivalPlanDate = "2026-07-08";
        private InventoryTransferPageDetails paramInfoToNext;

        // ページ分割
        private Grid? paginationLayout;
        private Button? prevButton;
        private Button? nextButton;
        private Label? pageLabel;
        private int currentPage = 0;
        private int pageSize = 5;
        private int totalPages = 0;

        // 新增：全局滚动容器引用 + 全部明细数据源
        private VerticalStackLayout? _scrollContainer;
        private readonly List<StockInRow> _allBottomData = new List<StockInRow>();

        public InventoryTransferPageDetails() : base("strInventoryTransfer")
        {
            BuildUI();
        }

        private void BuildUI()
        {
            #region 接口请求占位
            //var request = new RequestData<ProcessParamInfo, EvangJsonModel>("GetStockInDetail");
            //request.Info = paramInfoToNext;
            // var resultList = await this.Post<ProcessParamInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
            // if (resultList == null || resultList!.SubData[0]!.SubJson == null)
            //     return;
            // var dbJson = resultList!.SubData[0]!.SubJson;
            #endregion

            List<StockInRow> topExistLotList = new List<StockInRow>()
            {
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260620", Qty = 50 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260615", Qty = 30 }
            };

            List<StockInRow> registeredLotList = new List<StockInRow>()
            {
                new StockInRow{ LotNo = "LOT20260708", Qty = 80 },
                new StockInRow{ LotNo = "LOT20260709", Qty = 40 }
            };

            // 明细数据存入全局字段，供分页使用
            _allBottomData.AddRange(new List<StockInRow>()
            {
                new StockInRow{ ItemCode = "部品Z-9999", LotNo = "LOT20260615", Qty = 10 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260728", Qty = 20 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260738", Qty = 30 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260748", Qty = 40 },
                new StockInRow{ ItemCode = "部品N-1010", LotNo = "LOT20260758", Qty = 50 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260708", Qty = 60 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260768", Qty = 70 },
                new StockInRow{ ItemCode = "部品B-1010", LotNo = "LOT20260708", Qty = 80 },
                new StockInRow{ ItemCode = "部品G-1010", LotNo = "LOT20260778", Qty = 90 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260708", Qty = 80 },
                new StockInRow{ ItemCode = "部品E-1010", LotNo = "LOT20260708", Qty = 70 },
                new StockInRow{ ItemCode = "部品F-1010", LotNo = "LOT20260708", Qty = 60 },
                new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260708", Qty = 50 },
                new StockInRow{ ItemCode = "部品D-1010", LotNo = "LOT20260708", Qty = 40 }
            });

            // 计算总页数
            totalPages = (int)Math.Ceiling((double)_allBottomData.Count / pageSize);

            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star }
                },
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            //pageHeaderInfo = BuildStockInHeader(poNo, customName, arrivalPlanDate, topExistLotList);
            var detailInputArea = BuildDetailInputArea(registeredLotList);
            CreatePaginationControls();

            // 初始化加载第一页数据
            var firstPageData = _allBottomData.Take(pageSize).ToList();
            var bottomTable = BuildBottomRegisteredTable(firstPageData, _allBottomData.Count);

            var saveBtn = new Button
            {
                Text = "保存",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                Margin = new Thickness(10, 5, 10, 10),
                CornerRadius = 6
            };

            // 滚动容器存入全局字段，供分页刷新使用
            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            //_scrollContainer.Children.Add(pageHeaderInfo);
            _scrollContainer.Children.Add(detailInputArea);
            _scrollContainer.Children.Add(paginationLayout);
            _scrollContainer.Children.Add(bottomTable);
            _scrollContainer.Children.Add(saveBtn);

            var scrollView = new ScrollView
            {
                Content = _scrollContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            mainGrid.Add(scrollView, 0, 0);
            Content = mainGrid;

            // 初始化分页按钮状态
            UpdatePaginationControls();
        }

        private Border BuildStockInHeader(string poNumber, string customer, string planDate, List<StockInRow> topLotRows)
        {
            var innerGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition()
                },
                ColumnDefinitions =
                 {
                    new ColumnDefinition(),
                    new ColumnDefinition()
                 },
                Padding = new Thickness(5, 5, 5, 8),
                BackgroundColor = Color.FromArgb("#edeff3")
            };
            innerGrid.Add(new Label { Text = "顧客", FontSize = 12, TextColor = Colors.Gray });
            innerGrid.Add(new Label { Text = "出荷予定日", FontSize = 12, TextColor = Colors.Gray }, 1, 0);
            var customerBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(0, 0, 2, 15)
            };
            var customerLabel = new Label { Text = customer, FontSize = 14, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            customerBorder.Content = customerLabel;
            innerGrid.Add(customerBorder, 0, 1);
            var dateBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(2, 0, 0, 15)
            };
            var dateLabel = new Label { Text = planDate, FontSize = 15, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            dateBorder.Content = dateLabel;
            innerGrid.Add(dateBorder, 1, 1);

            var topTable = BuildSimpleTable(
                new List<string> { "アイテム", "入庫済みロット", "数量" },
                topLotRows.ConvertAll(r => new List<string> { r.ItemCode, r.LotNo, r.Qty.ToString() }),
                new List<GridLength>
                {
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star)
                }
            );
            Grid.SetRow(topTable, 2);
            Grid.SetColumnSpan(topTable, 2);
            innerGrid.Add(topTable);

            var headerBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(1)
            };
            headerBorder.Content = innerGrid;

            return headerBorder;
        }

        private Border BuildDetailInputArea(List<StockInRow> regLots)
        {
            var border = new Border
            {
                Stroke = Color.FromArgb("#b4cee8"),
                Background = Color.FromArgb("#e6f0fa"),
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(5),
                StrokeThickness = 2
            };

            var layout = new VerticalStackLayout { Spacing = 10 };
            layout.Children.Add(new Label { Text = "明細登録", FontSize = 15, FontAttributes = FontAttributes.Bold });

            var locRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 50 } }
            };
            locRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.Children.Add(new Label { Text = "入庫先ロケーション (スキャン可)", FontSize = 12, TextColor = Colors.Gray });
            var locPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = 0,
                BackgroundColor = Colors.White,
                ItemsSource = new List<string> { "WH1-A-03" }
            };
            locRow.Add(locPicker, 0, 1);
            MauiIcon barcodeIcon1 = new MauiIcon
            {
                Icon = FluentIcons.BarcodeScanner20,
                IconSize = 40,
                IconColor = Color.FromRgba("#1e3a5f"),
                HorizontalOptions = LayoutOptions.End

            };
            locRow.Add(barcodeIcon1, 1, 1);
            layout.Children.Add(locRow);

            var innerGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition()
                },
                ColumnDefinitions =
                 {
                    new ColumnDefinition(),
                    new ColumnDefinition()
                 },
                //Padding = new Thickness(5, 5, 5, 8),
                //BackgroundColor = Color.FromArgb("#edeff3")
            };
            innerGrid.Add(new Label { Text = "移動元ロケーション", FontSize = 12, TextColor = Colors.Gray });
            innerGrid.Add(new Label { Text = "移動先ロケーション", FontSize = 12, TextColor = Colors.Gray }, 1, 0);
            var vendorBorder = new Border
            {
                //Stroke = Color.FromArgb("#b8bec9"),
                //StrokeShape = new RoundRectangle { CornerRadius = 5 },
                //StrokeThickness = 2,
                //Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(0),
                Margin = new Thickness(2, 0, 0, 0)
            };
            var formPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = 0,
                BackgroundColor = Colors.White,
                ItemsSource = new List<string> { "WH1-A-03" }
            };
            vendorBorder.Content = formPicker;
            innerGrid.Add(vendorBorder, 0, 1);
            var dateBorder = new Border
            {
                //Stroke = Color.FromArgb("#b8bec9"),
                //StrokeShape = new RoundRectangle { CornerRadius = 5 },
                //StrokeThickness = 2,
                //Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 2, 0)
            };
            var toPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = 0,
                BackgroundColor = Colors.White,
                ItemsSource = new List<string> { "WH2-C-01" }
            };
            dateBorder.Content = toPicker;
            innerGrid.Add(dateBorder, 1, 1);
            layout.Children.Add(innerGrid);

            layout.Children.Add(new Label { Text = "ロット / シリアル (スキャン可)", FontSize = 12, TextColor = Colors.Gray });

            var lotRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 50 } }
            };
            lotRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lotEntry = new Entry { Text = "LOT20260708", BackgroundColor = Colors.White, };
            lotRow.Add(lotEntry, 0, 1);
            MauiIcon barcodeIcon2 = new MauiIcon
            {
                Icon = FluentIcons.BarcodeScanner20,
                IconSize = 40,
                IconColor = Color.FromRgba("#1e3a5f"),
                HorizontalOptions = LayoutOptions.End
            };
            lotRow.Add(barcodeIcon2, 1, 1);
            layout.Children.Add(lotRow);

            var qtyRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 60 } }
            };
            //var qtyBorder = new Border
            //{
            //    Stroke = Color.FromArgb("#b8bec9"),
            //    StrokeShape = new RoundRectangle { CornerRadius = 5 },
            //    StrokeThickness = 2,
            //    Margin = new Thickness(0, 0, 7, 0)
            //};
            var qtyEntry = new Entry { Text = "120", Keyboard = Keyboard.Numeric, BackgroundColor = Colors.White, };
            //qtyBorder.Content = qtyEntry;
            qtyRow.Add(qtyEntry, 0, 0);
            qtyRow.Add(new Label { Text = "個", VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            layout.Children.Add(new Label { Text = "移動数量", FontSize = 12, TextColor = Colors.Gray });
            layout.Children.Add(qtyRow);

            var innerTable = BuildSimpleTable(
                new List<string> { "登録済みロット", "数量", "" },
                regLots.ConvertAll(r => new List<string> { r.LotNo, $"{r.Qty}個", "❌" }),
                new List<GridLength>
                {
                    new GridLength(3, GridUnitType.Star),
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star)
                }
            );
            layout.Children.Add(innerTable);

            var addLotBtn = new Button
            {
                Text = "+ロットを追加",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#245a96"),
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 3,
                FontAttributes = FontAttributes.Bold
            };
            layout.Children.Add(addLotBtn);

            border.Content = layout;
            return border;
        }

        private HorizontalStackLayout BuildPager()
        {
            var pager = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 8) };
            var prevBtn = new Button { Text = "◀ 前明細", IsEnabled = false, BackgroundColor = Colors.LightGray };
            pager.Children.Add(prevBtn);
            var nextBtn = new Button { Text = "次明細 ▶", BackgroundColor = Color.FromArgb("#245a96"), TextColor = Colors.White };
            pager.Children.Add(nextBtn);
            return pager;
        }

        private void CreatePaginationControls()
        {
            paginationLayout = new Grid
            {
                IsVisible = true,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }
                },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 5),
                RowSpacing = 3
            };
            var buttonRow = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 25
            };

            prevButton = new Button
            {
                Text = "◀ 前明細",
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 2,
                CornerRadius = 5,
                WidthRequest = 100,
                HeightRequest = 45,
                MinimumWidthRequest = 60,
                MinimumHeightRequest = 30,
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(2),
            };
            // 绑定上一页点击事件
            prevButton.Clicked += OnPrevButtonClicked;

            pageLabel = new Label
            {
                Text = "1 / 1",
                FontSize = 12,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.End,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(0, 5, 0, 0),
            };

            nextButton = new Button
            {
                Text = "次明細 ▶",
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 2,
                CornerRadius = 5,
                WidthRequest = 100,
                HeightRequest = 45,
                MinimumWidthRequest = 60,
                MinimumHeightRequest = 30,
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(2),
            };
            // 绑定下一页点击事件
            nextButton.Clicked += OnNextButtonClicked;

            buttonRow.Children.Add(prevButton);
            buttonRow.Children.Add(pageLabel);
            buttonRow.Children.Add(nextButton);

            Grid.SetRow(buttonRow, 0);
            paginationLayout.Children.Add(buttonRow);
        }

        /// <summary>
        /// 底部明细表格：总数动态显示
        /// </summary>
        private VerticalStackLayout BuildBottomRegisteredTable(List<StockInRow> rows, int totalCount)
        {
            var container = new VerticalStackLayout { Spacing = 4 };
            // 总数动态显示，不再写死
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({totalCount}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });
            var table = BuildSimpleTable(
                new List<string> { "品目", "ロット", "数量", "" },
                rows.ConvertAll(r => new List<string> { r.ItemCode, r.LotNo, $"{r.Qty}個", "❌" }),
                new List<GridLength>
                {
                    new GridLength(3, GridUnitType.Star),
                    new GridLength(3, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star)
                }
            );
            container.Children.Add(table);
            return container;
        }

        private Border BuildSimpleTable(List<string> headers, List<List<string>> rowDatas, List<GridLength>? columnWidths = null)
        {
            Grid tableGrid = new Grid();

            if (columnWidths != null && columnWidths.Count == headers.Count)
            {
                foreach (var width in columnWidths)
                    tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
            }
            else
            {
                foreach (var _ in headers)
                    tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            // 表头行
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < headers.Count; c++)
            {
                tableGrid.Add(new Label
                {
                    Text = headers[c],
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    BackgroundColor = Color.FromArgb("#dbe2ec"),
                    Padding = new Thickness(2)
                }, c, 0);
            }

            // 数据行 + 分隔线
            for (int r = 0; r < rowDatas.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView
                {
                    Color = Color.FromArgb("#e0e3e8"),
                    HeightRequest = 1
                };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var rowData = rowDatas[r];
                for (int c = 0; c < rowData.Count; c++)
                {
                    tableGrid.Add(new Label
                    {
                        Text = rowData[c],
                        FontSize = 11,
                        Padding = new Thickness(4)
                    }, c, dataRowIndex);
                }
            }

            var tableBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { },
                Background = Colors.White
            };
            tableBorder.Content = tableGrid;

            return tableBorder;
        }

        #region 分页逻辑
        private void OnPrevButtonClicked(object sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                LoadPage(currentPage - 1);
            }
        }

        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            if (currentPage < totalPages - 1)
            {
                LoadPage(currentPage + 1);
            }
        }

        /// <summary>
        /// 加载指定页数据，仅刷新底部表格区域
        /// </summary>
        private void LoadPage(int pageIndex)
        {
            if (_allBottomData == null || pageIndex < 0 || pageIndex >= totalPages)
                return;

            currentPage = pageIndex;
            // 截取当前页数据
            var pageData = _allBottomData.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            // 生成新的底部表格
            var newBottomTable = BuildBottomRegisteredTable(pageData, _allBottomData.Count);

            // 替换滚动容器中的底部表格（索引2：0头部 0明细区 1分页 2底部表格 3保存按钮）
            if (_scrollContainer != null && _scrollContainer.Children.Count > 2)
            {
                _scrollContainer.Children[2] = newBottomTable;
            }

            // 更新按钮状态
            UpdatePaginationControls();
        }

        /// <summary>
        /// 更新分页按钮状态与页码显示
        /// </summary>
        private void UpdatePaginationControls()
        {
            if (paginationLayout == null || pageLabel == null ||
                prevButton == null || nextButton == null)
                return;

            // 更新页码文本
            pageLabel.Text = $"{currentPage + 1} / {totalPages}";

            // 控制按钮启用状态
            prevButton.IsEnabled = currentPage > 0;
            nextButton.IsEnabled = currentPage < totalPages - 1;

            // 按钮颜色同步：禁用变灰，启用为蓝色b8bec9
            prevButton.BackgroundColor = prevButton.IsEnabled ? Color.FromArgb("#245a96") : Colors.LightGray;
            nextButton.BackgroundColor = nextButton.IsEnabled ? Color.FromArgb("#245a96") : Colors.LightGray;
            prevButton.TextColor = prevButton.IsEnabled ? Colors.White : Colors.DarkGray;
            nextButton.TextColor = nextButton.IsEnabled ? Colors.White : Colors.DarkGray;
            prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#245a96") : Colors.DarkGray;
            nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#245a96") : Colors.DarkGray;
        }
        #endregion

        private void ShowPagination()
        {
            if (paginationLayout != null)
            {
                paginationLayout.IsVisible = true;
            }
        }

        private void HidePagination()
        {
            if (paginationLayout != null)
            {
                paginationLayout.IsVisible = false;
            }
        }

        public class StockInRow
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }

        private string GetJsonStringValue(JsonElement jsonElement, string propertyName)
        {
            try
            {
                if (jsonElement.TryGetProperty(propertyName, out JsonElement propertyValue))
                {
                    return propertyValue.GetString() ?? string.Empty;
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int GetJsonIntValue(JsonElement jsonElement, string propertyName)
        {
            try
            {
                if (jsonElement.TryGetProperty(propertyName, out JsonElement propertyValue))
                {
                    if (propertyValue.ValueKind == JsonValueKind.Number)
                    {
                        return propertyValue.GetInt32();
                    }
                    else if (propertyValue.ValueKind == JsonValueKind.String)
                    {
                        string strValue = propertyValue.GetString() ?? "0";
                        int.TryParse(strValue, out int intValue);
                        return intValue;
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}