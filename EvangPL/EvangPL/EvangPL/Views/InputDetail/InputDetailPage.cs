using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using MauiIcons.Core;
using MauiIcons.Fluent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics.Text;
using System.Collections.Generic;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using InputDetailInfo = EvangPL.Utils.InputDetailInfo;

namespace EvangPL.Views.InputDetail
{
    public class InputDetail : EvangContentVM
    {
        private Border? pageHeaderInfo;
        private readonly string poNo = "PO-2026-0114";
        private readonly string supplierName = "東亜電子部品(株)";
        private readonly string arrivalPlanDate = "2026-07-08";
        private InputDetailInfo paramInfoToNext;

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
        private InputDetailInfo? _detailInfo;
        private const string RESTLET_GET_DETAIL = "GetStockInDetail";
        private const string RESTLET_SAVE_STOCKIN = "SaveStockIn";

        // ✅ 新增：页面加载状态标志
        private bool _isDataLoaded = false;

        // ✅ 新增：存储完整明细数据的列表（用于提取头部信息）
        private List<DetailItem> _detailItems = new List<DetailItem>();

        public InputDetail() : base("strInputDetail")
        {
            // 注册 RESTlet（参考 InboundSearch 的写法）
            if (!LocalMemory.restlets.ContainsKey(RESTLET_GET_DETAIL))
            {
                LocalMemory.restlets.Add(RESTLET_GET_DETAIL,
                    "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2062&deploy=1");
            }
            if (!LocalMemory.restlets.ContainsKey(RESTLET_SAVE_STOCKIN))
            {
                LocalMemory.restlets.Add(RESTLET_SAVE_STOCKIN,
                    "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2061&deploy=1");
            }
            _detailInfo = new InputDetailInfo
            {
                PoNo = "PO-2026-0114",
                SupplierName = "東亜電子部品(株)",
                ArrivalPlanDate = "2026-07-08",
                ItemCount = 5,
                TotalQty = 320,
                Status = "未入库"
            };
            BuildUI();
        }

        private string? _orderId;
        public InputDetail(InputDetailInfo detailInfo) : base("strInputDetail")
        {
            if (!LocalMemory.restlets.ContainsKey(RESTLET_SAVE_STOCKIN))
            {
                LocalMemory.restlets.Add(RESTLET_SAVE_STOCKIN,
                    "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2061&deploy=1");
            }
            _detailInfo = detailInfo;
            // ✅ 保存单据ID
            if (_detailInfo != null)
            {
                _orderId = _detailInfo.OrderId;
            }
            // ✅ 新增：注册 GetDetail RESTlet（带参构造函数中也需要注册）
            if (!LocalMemory.restlets.ContainsKey(RESTLET_GET_DETAIL))
            {
                LocalMemory.restlets.Add(RESTLET_GET_DETAIL,
                    "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2062&deploy=1");
            }
            BuildUI();
        }

        // ✅ 修改：BuildUI 改为 async void，增加API调用逻辑
        private async void BuildUI()
        {
            // ✅ 新增：先构建基础UI框架，显示加载中提示
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

            // ✅ 新增：显示加载中提示
            var loadingLabel = new Label
            {
                Text = "読み込み中...",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 16,
                TextColor = Colors.Gray
            };
            mainGrid.Add(loadingLabel, 0, 0);
            Content = mainGrid;

            try
            {
                // ✅ 新增：从后端API加载数据
                await LoadDataFromApi();

                // ✅ 新增：数据加载完成后，重新构建完整UI
                await BuildCompleteUI();
            }
            catch (Exception ex)
            {
                // ✅ 新增：如果API调用失败，使用模拟数据
                System.Diagnostics.Debug.WriteLine($"BuildUI Error: {ex.Message}");
                LoadMockData();
                await BuildCompleteUI();
            }
        }

        // ✅ 新增：从后端API加载数据
        private async Task LoadDataFromApi()
        {
            // 如果没有订单ID，直接使用模拟数据
            if (string.IsNullOrEmpty(_orderId))
            {
                LoadMockData();
                return;
            }

            try
            {
                // ✅ 构建请求参数（与 InboundSearch / StockIn 风格一致）
                var request = new RequestData<StockInDetailParam, EvangJsonModel>(RESTLET_GET_DETAIL);
                request.Info = new StockInDetailParam
                {
                    OrderId = _orderId
                };

                // ✅ 调用后端API
                var result = await this.Post<StockInDetailParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);

                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadDataFromApi: サーバーからの応答がありません。");
                    LoadMockData();
                    return;
                }

                if (!result.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: API Error - {result.ErrorMessage}");
                    LoadMockData();
                    return;
                }

                // ✅ 解析返回的数据
                var detailList = ParseDetailResult(result);

                if (detailList == null || detailList.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("LoadDataFromApi: 取得した明細データがありません。");
                    LoadMockData();
                    return;
                }

                // 清空现有数据并填充
                _allBottomData.Clear();
                _allBottomData.AddRange(detailList);

                // ✅ 修复：从 _detailItems 获取头部信息
                if (_detailInfo != null)
                {
                    // 从完整明细数据中提取头部信息
                    if (_detailItems.Count > 0)
                    {
                        var firstItem = _detailItems.First();
                        if (!string.IsNullOrEmpty(firstItem.tranid))
                            _detailInfo.PoNo = firstItem.tranid;
                        if (!string.IsNullOrEmpty(firstItem.entityName))
                            _detailInfo.SupplierName = firstItem.entityName;
                        if (!string.IsNullOrEmpty(firstItem.scheduledDate))
                            _detailInfo.ArrivalPlanDate = firstItem.scheduledDate;
                    }

                    // 更新数量和件数
                    _detailInfo.ItemCount = _allBottomData.Count;
                    _detailInfo.TotalQty = _allBottomData.Sum(x => x.Qty);

                    // 如果状态为空，设置默认状态
                    if (string.IsNullOrEmpty(_detailInfo.Status))
                    {
                        _detailInfo.Status = "未入库";
                    }
                }

                System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: データ読み込み成功 - {_allBottomData.Count}件");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: API呼び出しエラー - {ex.Message}");
                LoadMockData();
            }
        }

        // ✅ 新增：解析明细结果（与 InboundSearch 的 ParseSearchResult 风格一致）
        private List<StockInRow> ParseDetailResult(ResponseData<EvangJsonModel, EvangJsonModel> result)
        {
            var detailList = new List<StockInRow>();
            _detailItems.Clear();  // ✅ 清空

            if (result.SubData == null || result.SubData.Count == 0)
                return detailList;

            foreach (var subData in result.SubData)
            {
                // 从 SubJson 解析为 DetailItem 列表
                if (subData.SubName == "Data" || subData.SubName == "DETAIL_LIST")
                {
                    try
                    {
                        var items = BaseUtils.JsonToClass<List<DetailItem>>(subData.SubJson!);
                        if (items != null)
                        {
                            _detailItems = items;  // ✅ 保存完整数据
                            foreach (var item in items)
                            {
                                var stockRow = new StockInRow
                                {
                                    ItemCode = item.itemid ?? item.id ?? "",
                                    LotNo = item.lotnum ?? "",
                                    Qty = item.quantityshiprecv ?? 0
                                };
                                detailList.Add(stockRow);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ParseDetailResult: JSON解析エラー - {ex.Message}");
                    }
                    break;
                }
            }

            return detailList;
        }

        // ✅ 新增：加载模拟数据（原 _allBottomData 初始化逻辑移到这里）
        private void LoadMockData()
        {
            _allBottomData.Clear();
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

            // 如果是模拟数据，更新头部信息
            if (_detailInfo != null)
            {
                _detailInfo.ItemCount = _allBottomData.Count;
                _detailInfo.TotalQty = _allBottomData.Sum(x => x.Qty);
            }
        }

        // ✅ 新增：构建完整的UI（原 BuildUI 中 UI 构建逻辑移到这里）
        private async Task BuildCompleteUI()
        {
            // 计算总页数
            totalPages = (int)Math.Ceiling((double)_allBottomData.Count / pageSize);
            if (totalPages == 0) totalPages = 1;

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

            // ✅ 修改：从实际数据构建顶部已存在批次列表
            var topExistLotList = _allBottomData
                .GroupBy(x => new { x.ItemCode, x.LotNo })
                .Select(g => new StockInRow
                {
                    ItemCode = g.Key.ItemCode,
                    LotNo = g.Key.LotNo,
                    Qty = g.Sum(x => x.Qty)
                })
                .Take(5)
                .ToList();

            // 如果没有数据，使用默认示例
            if (topExistLotList.Count == 0)
            {
                topExistLotList = new List<StockInRow>()
                {
                    new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260620", Qty = 50 },
                    new StockInRow{ ItemCode = "部品A-1010", LotNo = "LOT20260615", Qty = 30 }
                };
            }

            // ✅ 修改：从实际数据构建已注册批次列表
            var registeredLotList = _allBottomData
                .GroupBy(x => x.LotNo)
                .Select(g => new StockInRow
                {
                    LotNo = g.Key,
                    Qty = g.Sum(x => x.Qty)
                })
                .Take(5)
                .ToList();

            if (registeredLotList.Count == 0)
            {
                registeredLotList = new List<StockInRow>()
                {
                    new StockInRow{ LotNo = "LOT20260708", Qty = 80 },
                    new StockInRow{ LotNo = "LOT20260709", Qty = 40 }
                };
            }

            // 从 _detailInfo 获取显示信息
            string displayPoNo = _detailInfo?.PoNo ?? "PO-2026-0114";
            string displaySupplier = _detailInfo?.SupplierName ?? "東亜電子部品(株)";
            string displayPlanDate = _detailInfo?.ArrivalPlanDate ?? "2026-07-08";

            pageHeaderInfo = BuildStockInHeader(displayPoNo, displaySupplier, displayPlanDate, topExistLotList);
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
            saveBtn.Clicked += OnSaveButtonClicked;

            // 滚动容器存入全局字段，供分页刷新使用
            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            _scrollContainer.Children.Add(pageHeaderInfo);
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

            System.Diagnostics.Debug.WriteLine("BuildCompleteUI: UI構築完了");
        }

        // ==================== 以下方法保持不变 ====================

        private Border BuildStockInHeader(string poNumber, string supplier, string planDate, List<StockInRow> topLotRows)
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
            innerGrid.Add(new Label { Text = "仕入先", FontSize = 12, TextColor = Colors.Gray });
            innerGrid.Add(new Label { Text = "入荷予定日", FontSize = 12, TextColor = Colors.Gray }, 1, 0);
            var vendorBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(0, 0, 2, 15)
            };
            var vendorLabel = new Label { Text = supplier, FontSize = 14, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            vendorBorder.Content = vendorLabel;
            innerGrid.Add(vendorBorder, 0, 1);
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
            var qtyEntry = new Entry { Text = "120", Keyboard = Keyboard.Numeric, BackgroundColor = Colors.White, };
            qtyRow.Add(qtyEntry, 0, 0);
            qtyRow.Add(new Label { Text = "個", VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            layout.Children.Add(new Label { Text = "入庫数量", FontSize = 12, TextColor = Colors.Gray });
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

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_orderId))
                {
                    await DisplayAlert("エラー", "注文IDが指定されていません。", "OK");
                    return;
                }

                var saveParam = new StockInSaveParam
                {
                    OrderId = _orderId
                };

                var request = new RequestData<StockInSaveParam, EvangJsonModel>(RESTLET_SAVE_STOCKIN);
                request.Info = saveParam;

                var saveResult = await this.Post<StockInSaveParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);

                if (saveResult == null)
                {
                    await DisplayAlert("エラー", "サーバーからの応答がありません。", "OK");
                    return;
                }

                if (!saveResult.Success)
                {
                    await DisplayAlert("エラー", saveResult.ErrorMessage ?? "入庫保存に失敗しました。", "OK");
                    return;
                }

                string receiptNumber = "";
                if (saveResult.SubData != null && saveResult.SubData.Count > 0)
                {
                    foreach (var subData in saveResult.SubData)
                    {
                        if (subData.SubName == "RECEIPT_INFO")
                        {
                            var receiptInfo = BaseUtils.JsonToClass<ReceiptInfo>(subData.SubJson!);
                            if (receiptInfo != null)
                            {
                                receiptNumber = receiptInfo.receiptNumber ?? "";
                            }
                            break;
                        }
                    }
                }

                await DisplayAlert("成功", $"入庫保存が完了しました。\n入库单号: {receiptNumber}", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("エラー", $"保存処理中にエラーが発生しました: {ex.Message}", "OK");
            }
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
            nextButton.Clicked += OnNextButtonClicked;

            buttonRow.Children.Add(prevButton);
            buttonRow.Children.Add(pageLabel);
            buttonRow.Children.Add(nextButton);

            Grid.SetRow(buttonRow, 0);
            paginationLayout.Children.Add(buttonRow);
        }

        private VerticalStackLayout BuildBottomRegisteredTable(List<StockInRow> rows, int totalCount)
        {
            var container = new VerticalStackLayout { Spacing = 4 };
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

        private void LoadPage(int pageIndex)
        {
            if (_allBottomData == null || pageIndex < 0 || pageIndex >= totalPages)
                return;

            currentPage = pageIndex;
            var pageData = _allBottomData.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            var newBottomTable = BuildBottomRegisteredTable(pageData, _allBottomData.Count);

            if (_scrollContainer != null && _scrollContainer.Children.Count > 3)
            {
                _scrollContainer.Children[3] = newBottomTable;
            }

            UpdatePaginationControls();
        }

        private void UpdatePaginationControls()
        {
            if (paginationLayout == null || pageLabel == null ||
                prevButton == null || nextButton == null)
                return;

            pageLabel.Text = $"{currentPage + 1} / {totalPages}";

            prevButton.IsEnabled = currentPage > 0;
            nextButton.IsEnabled = currentPage < totalPages - 1;

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

        // ==================== 数据模型 ====================

        public class StockInRow
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }

        // ✅ 新增：入库明细请求参数
        public class StockInDetailParam : EvangJsonModel
        {
            public string? OrderId { get; set; }
        }

        // ✅ 新增：入库保存请求参数（原在底部，移到此处统一管理）
        public class StockInSaveParam : EvangJsonModel
        {
            public string? OrderId { get; set; }
        }

        // ✅ 新增：明细项（从API返回）
        public class DetailItem
        {
            public string? po_id { get; set; }
            public string? entityName { get; set; }
            public string? scheduledDate { get; set; }
            public string? tranid { get; set; }
            public string? itemName { get; set; }
            public int? quantityshiprecv { get; set; }
            public string? house_name { get; set; }
            public string? id { get; set; }
            public string? itemid { get; set; }
            public string? fullname { get; set; }
            public string? standard { get; set; }
            public string? type { get; set; }
            public string? lotnum { get; set; }
        }

        // ✅ 修改：ReceiptInfo 移到此处统一管理（原在底部）
        public class ReceiptInfo
        {
            public string? receiptNumber { get; set; }
            public string? ReceiptId { get; set; }
            public string? Status { get; set; }
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