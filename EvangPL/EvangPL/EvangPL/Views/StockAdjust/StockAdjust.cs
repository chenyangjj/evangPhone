using CommunityToolkit.Mvvm.Messaging;
using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System.Linq;
using System.Text.Json;
using static EvangPL.Views.InventoryAdjustment.InventoryAdjustment;

namespace EvangPL.Views.StockAdjust
{
    /// <summary>
    /// 棚卸調整 - 一覧画面 
    /// </summary>
    public class StockAdjust : EvangContentVM
    {
        
        private Grid? mainGrid;
        private Grid? filterGrid;
        private Button? btnCreateNew;
        private Grid? paginationGrid;
        private Label? pageInfoLabel;
        private Button? prevPageBtn;
        private Button? nextPageBtn;
        private StackLayout? listContainer;

        
        private DatePicker? startDatePicker;
        private DatePicker? endDatePicker;
        private Entry? dateRangeEntry;
        private Entry? keywordEntry;

        
        private int _currentPage = 1;
        private int _totalPage = 1;
        private const int PageSize = 4; 
        private CancellationTokenSource? _keywordCts;

        
        private StockAdjustPageInfo SearchCondition;
        private bool _hasNavigatedToDetail = false;

        public StockAdjust() : base("strStockAdjustSearch")
        {
            SearchCondition = new StockAdjustPageInfo();
            Title = "棚卸調整 - 一覧";
            BuildUI();
            _ = LoadAdjustData();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_hasNavigatedToDetail)
            {
                _hasNavigatedToDetail = false;
                await LoadAdjustData();
            }
        }

        #region 页面布局构建
        private void BuildUI()
        {
            
            var outerLayout = new Grid
            {
                Padding = new Thickness(10, 10, 10, 0),
                BackgroundColor = Color.FromArgb("#eff0f0")
            };

            mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },    
                    new RowDefinition { Height = GridLength.Auto },    
                    new RowDefinition { Height = 60 },                 
                    new RowDefinition { Height = GridLength.Star }      
                },
                ColumnDefinitions = { new ColumnDefinition() },
                RowSpacing = 8
            };

            filterGrid = CreateFilterArea();
            Grid.SetRow(filterGrid, 0);
            mainGrid.Children.Add(filterGrid);

            btnCreateNew = new Button
            {
                Text = "+ 新規登録",
                BackgroundColor = Color.FromArgb("#1954aa"),
                TextColor = Colors.White,
                HeightRequest = 50,
                FontSize = 15,
                CornerRadius = 6,
                FontAttributes = FontAttributes.Bold
            };
            btnCreateNew.Clicked += async (s, e) =>
            {
                try
                {
                    var detailPage = new EvangPL.Views.InventoryAdjustment.InventoryAdjustment();
                    await Navigation.PushAsync(detailPage);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
                }
            };
            Grid.SetRow(btnCreateNew, 1);
            mainGrid.Children.Add(btnCreateNew);

            paginationGrid = CreatePaginationBar();
            Grid.SetRow(paginationGrid, 2);
            mainGrid.Children.Add(paginationGrid);

            listContainer = new StackLayout
            {
                Spacing = 8,
                Padding = new Thickness(0, 5, 0, 0)
            };
            var scrollView = new ScrollView
            {
                Content = listContainer
            };
            Grid.SetRow(scrollView, 3);
            mainGrid.Children.Add(scrollView);

            outerLayout.Children.Add(mainGrid);
            Content = outerLayout;
        }

       
        private Grid CreateFilterArea()
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                ColumnSpacing = 10,
                RowSpacing = 4
            };

            // --- 対象期間 ---
            var lblDateTitle = new Label { Text = "対象期間", FontSize = 12, TextColor = Colors.Gray };

            var dateRangeGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 5,
                VerticalOptions = LayoutOptions.Center
            };

            startDatePicker = new DatePicker
            {
                Date = DateTime.Today.AddDays(-7),
                BackgroundColor = Colors.Transparent,
                HeightRequest = 36,
                Format = "MM/dd",
                TextColor = Colors.Black,
                Margin = new Thickness(8, 0)
            };
            var startBorder = CreateInputBorder(startDatePicker);
            Grid.SetColumn(startBorder, 0);
            dateRangeGrid.Children.Add(startBorder);

            var separator = new Label
            {
                Text = "~",
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                FontSize = 14,
                TextColor = Colors.Gray
            };
            Grid.SetColumn(separator, 1);
            dateRangeGrid.Children.Add(separator);

            endDatePicker = new DatePicker
            {
                Date = DateTime.Today,
                BackgroundColor = Colors.Transparent,
                HeightRequest = 36,
                Format = "MM/dd",
                TextColor = Colors.Black,
                Margin = new Thickness(8, 0)
            };
            var endBorder = CreateInputBorder(endDatePicker);
            Grid.SetColumn(endBorder, 2);
            dateRangeGrid.Children.Add(endBorder);

            // 日付変更時に SearchCondition.DateRange を更新
            async Task UpdateDateRange()
            {
                SearchCondition.DateRangeFrom = startDatePicker.Date.ToString("yyyy/MM/dd");
                SearchCondition.DateRangeTo = endDatePicker.Date.ToString("yyyy/MM/dd");
                _currentPage = 1;
                await LoadAdjustData();
            }
            startDatePicker.DateSelected += (s, e) => UpdateDateRange();
            endDatePicker.DateSelected += (s, e) => UpdateDateRange();
            UpdateDateRange(); // 初期値セット

            // --- 品目キーワード ---
            var lblKeywordTitle = new Label { Text = "品目キーワード", FontSize = 12, TextColor = Colors.Gray };
            keywordEntry = new Entry
            {
                Placeholder = "検索キーワード",
                Text = SearchCondition.Keyword,
                BackgroundColor = Colors.White,
                HeightRequest = 42
            };
            keywordEntry.TextChanged += async (s, e) =>
            {
                SearchCondition.Keyword = e.NewTextValue;
                _keywordCts?.Cancel();
                _keywordCts = new CancellationTokenSource();
                var token = _keywordCts.Token;

                try
                {
                    await Task.Delay(1000, token);   // 400ms 防抖
                    if (token.IsCancellationRequested) return;

                    _currentPage = 1;
                    await LoadAdjustData();
                }
                catch (TaskCanceledException)
                {
                    
                }
            };

            Grid.SetRow(lblDateTitle, 0); Grid.SetColumn(lblDateTitle, 0);
            Grid.SetRow(dateRangeGrid, 1); Grid.SetColumn(dateRangeGrid, 0);

            Grid.SetRow(lblKeywordTitle, 0); Grid.SetColumn(lblKeywordTitle, 1);
            Grid.SetRow(keywordEntry, 1); Grid.SetColumn(keywordEntry, 1);

            grid.Children.Add(lblDateTitle);
            grid.Children.Add(dateRangeGrid);
            grid.Children.Add(lblKeywordTitle);
            grid.Children.Add(keywordEntry);
            return grid;
        }

        private Border CreateInputBorder(View content)
        {
            return new Border
            {
                Stroke = Color.FromArgb("#cccccc"),       // 薄いグレーの枠線
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 }, // 角丸
                BackgroundColor = Colors.White,           // 背景色
                Padding = 0,
                Content = content,
                HeightRequest = 40                        // 全体の高さを統一
            };
        }

        /// <summary>
        /// 分页栏：前へ｜1/2｜次へ
        /// </summary>
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
                Padding = new Thickness(0, 10),
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
        /// 加载棚卸調整データ
        /// </summary>
        private async Task LoadAdjustData()
        {
            try
            {
                CollectSearchCondition();
                bool useMockData = false; 

                List<StockAdjustItem> dataList = new List<StockAdjustItem>();

                if (useMockData)
                {
                    
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0011", ItemCode = "部品E-5050", AdjustReason = "破損", DiffQty = -20, RegisterDate = "2026-07-06" });
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0012", ItemCode = "部品F-6060", AdjustReason = "棚卸差異", DiffQty = 5, RegisterDate = "2026-07-06" });
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0013", ItemCode = "部品I-9090", AdjustReason = "破損", DiffQty = -8, RegisterDate = "2026-07-07" });
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0014", ItemCode = "部品J-1212", AdjustReason = "棚卸差異", DiffQty = 3, RegisterDate = "2026-07-07" });

                   
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0015", ItemCode = "部品A-1010", AdjustReason = "在庫調整", DiffQty = 12, RegisterDate = "2026-07-08" });
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0016", ItemCode = "部品B-2020", AdjustReason = "破損", DiffQty = -5, RegisterDate = "2026-07-08" });
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0017", ItemCode = "部品C-3030", AdjustReason = "棚卸差異", DiffQty = -2, RegisterDate = "2026-07-09" });
                    dataList.Add(new StockAdjustItem { AdjustNo = "ADJ-0018", ItemCode = "部品D-4040", AdjustReason = "在庫調整", DiffQty = 8, RegisterDate = "2026-07-09" });

                    
                    int totalRecordCount = dataList.Count;
                    _totalPage = (int)Math.Ceiling((double)totalRecordCount / PageSize);

                    if (_currentPage > _totalPage && _totalPage > 0)
                        _currentPage = _totalPage;

                    
                    var pagedData = dataList
                        .Skip((_currentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                    dataList = pagedData;

                    RefreshPageUI();
                    //RenderCardList(dataList);
                }
                else
                {
                    var startDate = SearchCondition.DateRangeFrom;
                    var endDate = SearchCondition.DateRangeTo;
                    var keyword = SearchCondition.Keyword;
                    if (string.Compare(startDate, endDate) > 0)
                    {
                        await DisplayAlert("エーラ", "対象期間FROMは対象期間TOより大きくすることはできません。", "OK");
                        return;
                    }
                    //if (string.IsNullOrWhiteSpace(keyword))
                    //{
                    //    return;
                    //}
                    var searchParam = SearchCondition;
                    var request = new RequestData<StockAdjustPageInfo, EvangJsonModel>("GetAdjust");
                    request.Info = searchParam;
                    var resultList = await this.Post<StockAdjustPageInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                    if (resultList == null || resultList.SubData == null)
                    {
                        RefreshPageUI();
                        //RenderCardList(dataList);
                        return;
                    }
                    foreach (var item in resultList.SubData)
                    {
                        switch (item.SubName)
                        {
                            case "PH_DATA":
                                if (item == null || item.SubJson == null)
                                {
                                    RefreshPageUI();
                                    //RenderCardList(dataList);
                                    return;
                                }
                                dataList = BaseUtils.JsonToClass<List<StockAdjustItem>>(item.SubJson);
                                break;
                        }
                    }

                    var groupedData = dataList
                        .GroupBy(x => x.Id)
                        .Select(g => g.ToList())
                        .ToList();
                    
                    int totalRecordCount = groupedData.Count;
                    _totalPage = (int)Math.Ceiling((double)totalRecordCount / PageSize);

                    if (_currentPage > _totalPage && _totalPage > 0)
                        _currentPage = _totalPage;

                    var pagedGroupedData = groupedData
                        .Skip((_currentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();

                    RefreshPageUI();
                    RenderCardList(pagedGroupedData);
                }

                //RefreshPageUI();
                //RenderCardList(dataList);
            }
            catch (Exception)
            {
                ShowErrorTip();
            }
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
            }
        }

        private async Task PrevPage()
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            await LoadAdjustData();
        }

        private async Task NextPage()
        {
            if (_currentPage >= _totalPage) return;
            _currentPage++;
            await LoadAdjustData();
        }
        #endregion

        #region Json工具函数
        private string GetJsonStringValue(JsonElement jsonElement, string propertyName)
        {
            try
            {
                if (jsonElement.TryGetProperty(propertyName, out JsonElement val))
                    return val.GetString() ?? string.Empty;
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
                if (jsonElement.TryGetProperty(propertyName, out JsonElement val))
                {
                    if (val.ValueKind == JsonValueKind.Number)
                        return val.GetInt32();
                    int.TryParse(val.GetString(), out int num);
                    return num;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        #endregion

        #region
        /// <summary>
        /// </summary>
        private void RenderCardList(List<List<StockAdjustItem>> groupedDataList)
        {
            if (listContainer == null) return;
            listContainer.Children.Clear();

            if (groupedDataList == null || groupedDataList.Count == 0)
            {
                ShowEmptyTip();
                return;
            }

            foreach (var group in groupedDataList)
            {
                if (group == null || group.Count == 0) continue;

                var master = group[0];

                var cardFrame = new Frame
                {
                    BackgroundColor = Colors.White,
                    CornerRadius = 8,
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                    HasShadow = false
                };

                var mainStack = new StackLayout { Spacing = 6 };

                var headerGrid = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
                };

                var lblAdjustNo = new Label
                {
                    Text = master.AdjustNo,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    VerticalTextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(lblAdjustNo, 0);

                var lblDate = new Label
                {
                    Text = $"登録日: {master.RegisterDate}",
                    FontSize = 12,
                    TextColor = Colors.Gray,
                    VerticalTextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(lblDate, 1);

                headerGrid.Children.Add(lblAdjustNo);
                headerGrid.Children.Add(lblDate);
                mainStack.Children.Add(headerGrid);

                // 分隔线
                mainStack.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    Color = Color.FromArgb("#eeeeee"),
                    Margin = new Thickness(0, 4, 0, 4)
                });

                foreach (var detail in group)
                {
                    var detailGrid = new Grid
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
                    new RowDefinition { Height = GridLength.Auto }
                },
                        RowSpacing = 2,
                        Padding = new Thickness(0, 2)
                    };

                    // 品目コード
                    var lblItem = new Label
                    {
                        Text = $"品目: {detail.ItemCode}",
                        FontSize = 13,
                        TextColor = Colors.Black
                    };
                    Grid.SetRow(lblItem, 0);
                    Grid.SetColumn(lblItem, 0);

                    // 差異数量
                    var lblDiff = new Label
                    {
                        Text = $"{(detail.DiffQty > 0 ? "+" : "")}{detail.DiffQty}",
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = detail.DiffQty >= 0 ? Color.FromArgb("#2e7d32") : Color.FromArgb("#c62828"),
                        HorizontalTextAlignment = TextAlignment.End
                    };
                    Grid.SetRow(lblDiff, 0);
                    Grid.SetColumn(lblDiff, 1);

                    // 調整理由
                    var lblReason = new Label
                    {
                        Text = $"理由: {detail.AdjustReason}",
                        FontSize = 11,
                        TextColor = Colors.Gray
                    };
                    Grid.SetRow(lblReason, 1);
                    Grid.SetColumnSpan(lblReason, 2);

                    // 場所
                    var lblLoc = new Label
                    {
                        Text = $"場所: {detail.LocationName}",
                        FontSize = 11,
                        TextColor = Colors.Gray
                    };
                    Grid.SetRow(lblLoc, 2);
                    Grid.SetColumnSpan(lblLoc, 2);

                    detailGrid.Children.Add(lblItem);
                    detailGrid.Children.Add(lblDiff);
                    detailGrid.Children.Add(lblReason);
                    detailGrid.Children.Add(lblLoc);

                    mainStack.Children.Add(detailGrid);
                }

                cardFrame.Content = mainStack;

                //tap.Tapped += async (s, e) =>
                //{
                //    try
                //    {
                //        var editPage = new EvangPL.Views.InventoryAdjustment.InventoryAdjustment(group);
                //        _hasNavigatedToDetail = true;
                //        await Navigation.PushAsync(editPage);
                //    }
                //    catch (Exception ex)
                //    {
                //        await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
                //    }
                //};
                //cardFrame.GestureRecognizers.Add(tap);

                listContainer.Children.Add(cardFrame);
            }
        }
        #endregion

        #region 空・エラー提示
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

    #region
    /// <summary>
    /// 棚卸調整 検索条件モデル
    /// </summary>
    public class StockAdjustPageInfo : EvangJsonModel
    {
        public string DateRangeFrom { get; set; } = "";
        public string DateRangeTo { get; set; } = "";
        public string Keyword { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>
    /// 棚卸調整 Item
    /// </summary>
    public class StockAdjustItem
    {
        public int Id { get; set; }
        public int LineNo { get; set; }
        public string AdjustNo { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string AdjustReason { get; set; } = "";
        public int DiffQty { get; set; }
        public string RegisterDate { get; set; } = "";
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public int LineSeq { get; set; }

    }
    #endregion
}