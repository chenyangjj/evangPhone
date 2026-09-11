using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json;
using System.Linq;
// ✅ 直接引用命名空间，不需要 using static
using EvangPL.Views.InboundSearch;

namespace EvangPL.Views.StockIn
{
    public class StockIn : EvangContentVM
    {
        public static SearchCondition? PassedCondition { get; set; }

        // UI控件缓存
        private Grid? mainGrid;
        private Grid? paginationGrid;
        private Label? pageInfoLabel;
        private Button? prevPageBtn;
        private Button? nextPageBtn;
        private StackLayout? listContainer;

        // 分页参数
        private int _currentPage = 1;
        private int _totalPage = 1;
        private const int PageSize = 4;

        // ✅ 存储从检索页面传来的数据
        private List<OrderInfo>? _searchResultData;
        private string? _keyword;
        private string? _inboundType;
        private string? _status;
        private DateTime? _scheduledDate;

        // ✅ 标记是否已加载数据（防止重复加载）
        private bool _isDataLoaded = false;

        // 查询条件实体
        private StockInPageInfo SearchCondition;

        public StockIn() : base("strStockInSearch")
        {
            Title = "入庫処理-検索結果";
            SearchCondition = new StockInPageInfo();
            BuildUI();
        }

        

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // ✅ 在 OnAppearing 中读取静态属性
            if (!_isDataLoaded)
            {
                // 从静态属性读取传入的数据
                if (PassedCondition != null)
                {
                    _searchResultData = PassedCondition.SearchResult;
                    _keyword = PassedCondition.Keyword;
                    _inboundType = PassedCondition.InboundType;
                    _status = PassedCondition.Status;
                    _scheduledDate = PassedCondition.ScheduledDate;

                    // 使用完后清空
                    PassedCondition = null;
                }

                _ = LoadStockInData();
                _isDataLoaded = true;
            }
        }

        #region 页面布局构建
        private void BuildUI()
        {
            mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = 60 },
                    new RowDefinition { Height = GridLength.Star }
                },
                ColumnDefinitions = { new ColumnDefinition() },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0
            };

            paginationGrid = CreatePaginationBar();
            Grid.SetRow(paginationGrid, 0);
            mainGrid.Children.Add(paginationGrid);

            listContainer = new StackLayout
            {
                Spacing = 8,
                Padding = new Thickness(10)
            };
            var scrollView = new ScrollView
            {
                Content = listContainer
            };
            Grid.SetRow(scrollView, 1);
            mainGrid.Children.Add(scrollView);

            Content = mainGrid;
        }

        private Grid CreatePaginationBar()
        {
            paginationGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 15,
                Padding = new Thickness(10, 10),
                BackgroundColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };

            prevPageBtn = new Button
            {
                Text = "◀ 前へ",
                WidthRequest = 110,
                HeightRequest = 40,
                BackgroundColor = Color.FromArgb("#e6e6e6"),
                TextColor = Colors.Gray,
                BorderWidth = 0,
                VerticalOptions = LayoutOptions.Center
            };
            prevPageBtn.Clicked += async (s, e) => await PrevPage();

            pageInfoLabel = new Label
            {
                Text = "1/1",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            };

            nextPageBtn = new Button
            {
                Text = "次へ ▶",
                WidthRequest = 110,
                HeightRequest = 40,
                BackgroundColor = Colors.Transparent,
                BorderColor = Colors.DarkBlue,
                BorderWidth = 1,
                TextColor = Colors.DarkBlue,
                VerticalOptions = LayoutOptions.Center
            };
            nextPageBtn.Clicked += async (s, e) => await NextPage();

            paginationGrid.Add(prevPageBtn, 0, 0);
            paginationGrid.Add(pageInfoLabel, 1, 0);
            paginationGrid.Add(nextPageBtn, 2, 0);
            return paginationGrid;
        }
        #endregion

        #region 搜索分页逻辑
        private void CollectSearchCondition()
        {
            SearchCondition.PageIndex = _currentPage;
            SearchCondition.PageSize = PageSize;
        }

        /// <summary>
        /// 加载入库数据
        /// </summary>
        private async Task LoadStockInData()
        {
            try
            {
                CollectSearchCondition();
                List<StockInItem> dataList = new List<StockInItem>();

                // ✅ 优先使用从检索页面传来的真实数据
                if (_searchResultData != null && _searchResultData.Count > 0)
                {
                    // 将 OrderInfo 转换为 StockInItem
                    foreach (var order in _searchResultData)
                    {
                        var item = new StockInItem
                        {
                            OrderId = order.orderId ?? "",
                            OrderNo = order.orderNumber ?? order.orderId ?? "",
                            Status = order.status ?? "未入库",
                            SupplierName = order.supplierName ?? "",
                            ScheduleDate = order.scheduledDate?.ToString("yyyy-MM-dd") ?? "",
                            // 以下字段从 OrderInfo 映射，如果 OrderInfo 没有这些字段，需要设置默认值
                            ItemCount = 1,  // 如果 API 返回了明细数量，使用实际值
                            TotalQty = (int)(order.totalQuantity ?? 0),
                            InboundType = order.inboundType,
                            ItemCode = order.itemCode,
                            ItemName = order.itemName
                        };
                        dataList.Add(item);
                    }

                    // 分页计算
                    int totalRecordCount = dataList.Count;
                    _totalPage = (int)Math.Ceiling((double)totalRecordCount / PageSize);

                    if (_currentPage > _totalPage && _totalPage > 0)
                        _currentPage = _totalPage;

                    var pagedData = dataList
                        .Skip((_currentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                    dataList = pagedData;
                }
                else
                {
                    // ✅ 如果没有真实数据，使用假数据（用于从菜单直接进入）
                    dataList = GetMockData();
                }

                RefreshPageUI();
                RenderCardList(dataList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStockInData Error: {ex.Message}");
                ShowErrorTip();
            }
        }

        /// <summary>
        /// 获取假数据（用于从菜单直接进入）
        /// </summary>
        private List<StockInItem> GetMockData()
        {
            var allData = new List<StockInItem>
            {
                new StockInItem
                {
                    OrderNo = "PO-2026-0114",
                    Status = "未入库",
                    SupplierName = "",
                    ScheduleDate = "2026-07-08",
                    ItemCount = 5,
                    TotalQty = 320
                },
                new StockInItem
                {
                    OrderNo = "PO-2026-0115",
                    Status = "一部入库",
                    SupplierName = "",
                    ScheduleDate = "2026-07-09",
                    ItemCount = 3,
                    TotalQty = 150
                },
                new StockInItem
                {
                    OrderNo = "RMA-0032",
                    Status = "未处理",
                    SupplierName = "大和精密工業(株)",
                    ScheduleDate = "",
                    ItemCount = 2,
                    TotalQty = 40
                },
                new StockInItem
                {
                    OrderNo = "PO-2026-0116",
                    Status = "未入库",
                    SupplierName = "",
                    ScheduleDate = "2026-07-10",
                    ItemCount = 2,
                    TotalQty = 80
                }
            };

            // 分页计算
            int totalRecordCount = allData.Count;
            _totalPage = (int)Math.Ceiling((double)totalRecordCount / PageSize);

            if (_currentPage > _totalPage && _totalPage > 0)
                _currentPage = _totalPage;

            return allData
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private void RefreshPageUI()
        {
            if (pageInfoLabel != null)
                pageInfoLabel.Text = $"{_currentPage}/{_totalPage}";

            if (prevPageBtn != null)
            {
                prevPageBtn.IsEnabled = _currentPage > 1;
                prevPageBtn.BackgroundColor = _currentPage > 1 ? Colors.LightGray : Color.FromArgb("#e6e6e6");
                prevPageBtn.TextColor = _currentPage > 1 ? Colors.Black : Colors.Gray;
            }
            if (nextPageBtn != null)
            {
                nextPageBtn.IsEnabled = _currentPage < _totalPage;
                nextPageBtn.BorderColor = _currentPage < _totalPage ? Colors.DarkBlue : Colors.LightGray;
                nextPageBtn.TextColor = _currentPage < _totalPage ? Colors.DarkBlue : Colors.Gray;
                nextPageBtn.BackgroundColor = _currentPage < _totalPage ? Colors.Transparent : Color.FromArgb("#e6e6e6");
            }
        }

        private async Task PrevPage()
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            _isDataLoaded = false;  // 允许重新加载
            await LoadStockInData();
        }

        private async Task NextPage()
        {
            if (_currentPage >= _totalPage) return;
            _currentPage++;
            _isDataLoaded = false;  // 允许重新加载
            await LoadStockInData();
        }
        #endregion

        #region 单据卡片渲染
        private void RenderCardList(List<StockInItem> dataList)
        {
            if (listContainer == null) return;
            listContainer.Children.Clear();

            if (dataList == null || dataList.Count == 0)
            {
                ShowEmptyTip();
                return;
            }

            foreach (var item in dataList)
            {
                var cardFrame = CreateCardFrame(item);
                listContainer.Children.Add(cardFrame);
            }
        }

        private Frame CreateCardFrame(StockInItem item)
        {
            var cardFrame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                HasShadow = false
            };

            var cardGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                RowSpacing = 4
            };

            // 伝票番号
            var lblOrderNo = new Label
            {
                Text = item.OrderNo,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            var statusTag = CreateStatusTag(item.Status);

            Grid.SetRow(lblOrderNo, 0);
            Grid.SetColumn(lblOrderNo, 0);
            cardGrid.Children.Add(lblOrderNo);

            Grid.SetRow(statusTag, 0);
            Grid.SetColumn(statusTag, 1);
            cardGrid.Children.Add(statusTag);

            // 入荷予定日（RMA单据显示顾客信息）
            if (!string.IsNullOrEmpty(item.SupplierName))
            {
                var lblSupplier = new Label
                {
                    Text = $"顧客: {item.SupplierName}",
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                Grid.SetRow(lblSupplier, 1);
                Grid.SetColumnSpan(lblSupplier, 2);
                cardGrid.Children.Add(lblSupplier);

                var lblRmaReason = new Label
                {
                    Text = "返品理由: 納出荷",
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                Grid.SetRow(lblRmaReason, 2);
                Grid.SetColumnSpan(lblRmaReason, 2);
                cardGrid.Children.Add(lblRmaReason);
            }
            else
            {
                var lblDate = new Label
                {
                    Text = $"入荷予定日: {item.ScheduleDate}",
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                Grid.SetRow(lblDate, 1);
                Grid.SetColumnSpan(lblDate, 2);
                cardGrid.Children.Add(lblDate);
            }

            // 品目数 / 数量
            int rowIndex = !string.IsNullOrEmpty(item.SupplierName) ? 3 : 2;
            var lblSummary = new Label
            {
                Text = $"品目数: {item.ItemCount} / 数量: {item.TotalQty}",
                FontSize = 12,
                TextColor = Colors.Gray
            };
            Grid.SetRow(lblSummary, rowIndex);
            Grid.SetColumnSpan(lblSummary, 2);
            cardGrid.Children.Add(lblSummary);

            cardFrame.Content = cardGrid;

            // ✅ 点击卡片跳转详情，传递完整数据
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                try
                {
                    var detailInfo = new InputDetailInfo
                    {
                        OrderId = item.OrderId,                      // ✅ 新增：传递单据ID
                        PoNo = item.OrderNo,
                        SupplierName = item.SupplierName,
                        ArrivalPlanDate = item.ScheduleDate,
                        ItemCount = item.ItemCount,
                        TotalQty = item.TotalQty,
                        Status = item.Status,
                        InboundType = item.InboundType,
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName
                    };

                    var detailPage = new EvangPL.Views.InputDetail.InputDetail(detailInfo);
                    await Navigation.PushAsync(detailPage);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
                }
            };
            cardFrame.GestureRecognizers.Add(tap);

            return cardFrame;
        }

        private View CreateStatusTag(string statusText)
        {
            Color bgColor = Colors.Gray;
            switch (statusText)
            {
                case "未入库":
                    bgColor = Color.FromArgb("#E68922");
                    break;
                case "一部入库":
                    bgColor = Color.FromArgb("#255499");
                    break;
                case "未处理":
                    bgColor = Color.FromArgb("#808080");
                    break;
            }

            var label = new Label
            {
                Text = statusText,
                TextColor = Colors.White,
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var frame = new Frame
            {
                Content = label,
                BackgroundColor = bgColor,
                CornerRadius = 12,
                Padding = new Thickness(12, 4),
                HorizontalOptions = LayoutOptions.End,
                HasShadow = false
            };
            return frame;
        }

        private void ShowEmptyTip()
        {
            if (listContainer == null) return;
            listContainer.Children.Clear();
            listContainer.Children.Add(new Label
            {
                Text = "検索条件に一致するデータはありません。",
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
        }

        private void ShowErrorTip()
        {
            if (listContainer == null) return;
            listContainer.Children.Clear();
            listContainer.Children.Add(new Label
            {
                Text = "予期しないエラーが発生しました。管理者に連絡してください。",
                FontSize = 12,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
        }
        #endregion
    }

    #region 入庫用Model
    public class StockInPageInfo : EvangJsonModel
    {
        public string Keyword { get; set; } = "";
        public string Status { get; set; } = "";
        public string TargetDate { get; set; } = "";
        public string Supplier { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class StockInItem
    {
        public string? OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string Status { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string ScheduleDate { get; set; } = "";
        public int ItemCount { get; set; }
        public int TotalQty { get; set; }
        // ✅ 新增字段，用于传递更多信息到详情页
        public string? InboundType { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
    }

    public class StockInApiWrap
    {
        public List<StockInItem> List { get; set; } = new();
        public int TotalPage { get; set; }
        public int TotalCount { get; set; }
    }
    #endregion
}