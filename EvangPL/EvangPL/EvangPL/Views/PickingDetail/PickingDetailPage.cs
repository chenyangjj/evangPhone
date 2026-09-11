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
using System.Collections.ObjectModel;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using PickingDetailInfo = EvangPL.Utils.PickingDetailInfo;

namespace InventorySys.Views.PickingDetail
{
    public class PickingDetail : EvangContentVM
    {
        // ========== 主页面控件 ==========
        private Border? pageHeaderInfo;
        private PickingDetailInfo? _detailInfo;
        private Grid? _mainGrid;
        private VerticalStackLayout? _scrollContainer;
        private readonly List<StockInRow> _allBottomData = new List<StockInRow>();

        // ========== 分页参数 ==========
        private Grid? paginationLayout;
        private Button? prevButton;
        private Button? nextButton;
        private Label? pageLabel;
        private int currentPage = 0;
        private int pageSize = 5;
        private int totalPages = 0;

        // ========== 弹窗相关 ==========
        private Frame? _popupFrame;
        private BoxView? _maskLayer;
        private Entry? _entryPackageNo;
        private Entry? _entryItemCode;
        private Entry? _entryQty;
        private Button? _btnAddItem;
        private Button? _btnFinishPopup;
        private Button? _btnClosePopup;
        private CollectionView? _popupList;
        private Label? _popupCountLabel;

        // ========== 弹窗数据源 ==========
        public ObservableCollection<PackageItem> AddedPackageList { get; set; }

        // ========== 构造函数 ==========
        public PickingDetail() : base("strPickingDetail")
        {
            AddedPackageList = new ObservableCollection<PackageItem>();
            _detailInfo = new PickingDetailInfo
            {
                OrderNo = "SO-2026-0987",
                CustomerName = "山田工業(株)",
                ScheduleDate = "2026-07-08",
                ItemCount = 4,
                TotalQty = 210,
                Status = "未出荷"
            };
            BuildUI();
        }

        public PickingDetail(PickingDetailInfo detailInfo) : base("strPickingDetail")
        {
            AddedPackageList = new ObservableCollection<PackageItem>();
            _detailInfo = detailInfo;
            BuildUI();
        }

        // ========== 主UI构建 ==========
        private void BuildUI()
        {
            // 假数据
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

            totalPages = (int)Math.Ceiling((double)_allBottomData.Count / pageSize);

            // ========== 主Grid ==========
            _mainGrid = new Grid
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

            // ========== 头部信息 ==========
            string displayOrderNo = _detailInfo?.OrderNo ?? "SO-2026-0987";
            string displayCustomer = _detailInfo?.CustomerName ?? "山田工業(株)";
            string displayPlanDate = _detailInfo?.ScheduleDate ?? "2026-07-08";

            pageHeaderInfo = BuildStockInHeader(displayOrderNo, displayCustomer, displayPlanDate, topExistLotList);
            var detailInputArea = BuildDetailInputArea(registeredLotList);
            CreatePaginationControls();

            // 初始化加载第一页数据
            var firstPageData = _allBottomData.Take(pageSize).ToList();
            var bottomTable = BuildBottomRegisteredTable(firstPageData, _allBottomData.Count);

            // ========== 保存按钮（点击弹出弹窗） ==========
            var saveBtn = new Button
            {
                Text = "保存",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                Margin = new Thickness(10, 5, 10, 10),
                CornerRadius = 6
            };
            saveBtn.Clicked += OnSaveButtonClicked;

            // ========== 滚动容器 ==========
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

            _mainGrid.Add(scrollView, 0, 0);

            // ========== 构建弹窗（添加到主Grid最上层） ==========
            BuildPopup();

            Content = _mainGrid;

            // 初始化分页按钮状态
            UpdatePaginationControls();
        }

        // ========== 构建弹窗 ==========
        private void BuildPopup()
        {
            // ---- 弹窗输入控件 ----
            _entryPackageNo = new Entry { Placeholder = "梱包No.(スキャン可)", BackgroundColor = Colors.White };
            _entryItemCode = new Entry { Placeholder = "品目(スキャン可)", BackgroundColor = Colors.White };
            _entryQty = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "数量", BackgroundColor = Colors.White };

            // ---- 追加按钮 ----
            _btnAddItem = new Button
            {
                Text = "+ この内容を追加",
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#255499"),
                BorderColor = Color.FromArgb("#255499"),
                BorderWidth = 2,
                CornerRadius = 8,
                Padding = new Thickness(12)
            };
            _btnAddItem.Clicked += OnPopupAddClick;

            // ---- 完了按钮 ----
            _btnFinishPopup = new Button
            {
                Text = "完了",
                BackgroundColor = Color.FromArgb("#255499"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Margin = new Thickness(0, 20, 0, 0)
            };
            _btnFinishPopup.Clicked += (s, e) => ClosePopup();

            // ---- 关闭按钮 ----
            _btnClosePopup = new Button
            {
                Text = "×",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Gray,
                WidthRequest = 45,
                FontSize = 24
            };
            _btnClosePopup.Clicked += (s, e) => ClosePopup();

            // ---- 弹窗标题 ----
            var titleRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            var titleLabel = new Label
            {
                Text = "梱包を追加",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(titleLabel, 0);
            Grid.SetColumn(_btnClosePopup, 1);
            titleRow.Children.Add(titleLabel);
            titleRow.Children.Add(_btnClosePopup);

            // ---- 弹窗内列表表头 ----
            var tableHeader = new Grid
            {
                BackgroundColor = Color.FromArgb("#e6edf7"),
                Padding = new Thickness(8),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            tableHeader.Children.Add(new Label { Text = "梱包No.", FontAttributes = FontAttributes.Bold });
            tableHeader.Children.Add(new Label { Text = "品目", FontAttributes = FontAttributes.Bold });
            var qtyLabel = new Label { Text = "数量", FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End };
            Grid.SetColumn(qtyLabel, 2);
            tableHeader.Children.Add(qtyLabel);

            // ---- 弹窗内列表 ----
            _popupList = new CollectionView
            {
                ItemsSource = AddedPackageList,
                HeightRequest = 200,
                ItemTemplate = new DataTemplate(() =>
                {
                    var row = new Grid
                    {
                        Padding = new Thickness(8),
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto)
                        }
                    };
                    var lb1 = new Label { VerticalOptions = LayoutOptions.Center };
                    lb1.SetBinding(Label.TextProperty, nameof(PackageItem.PackageNo));
                    var lb2 = new Label { VerticalOptions = LayoutOptions.Center };
                    lb2.SetBinding(Label.TextProperty, nameof(PackageItem.ItemCode));
                    var lb3 = new Label { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
                    lb3.SetBinding(Label.TextProperty, nameof(PackageItem.Qty));
                    Grid.SetColumn(lb1, 0);
                    Grid.SetColumn(lb2, 1);
                    Grid.SetColumn(lb3, 2);
                    row.Children.Add(lb1);
                    row.Children.Add(lb2);
                    row.Children.Add(lb3);
                    return row;
                })
            };

            // ---- 弹窗内计数标签 ----
            _popupCountLabel = new Label
            {
                Text = "追加済み: 0件",
                FontSize = 12,
                TextColor = Colors.Gray,
                Margin = new Thickness(0, 5, 0, 0)
            };

            // ---- 弹窗内容布局 ----
            var popupContent = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 15,
                BackgroundColor = Colors.White,
                Children =
                {
                    titleRow,
                    _entryPackageNo,
                    _entryItemCode,
                    _entryQty,
                    _btnAddItem,
                    tableHeader,
                    _popupList,
                    _popupCountLabel,
                    _btnFinishPopup
                }
            };

            // ---- 弹窗Frame（默认隐藏） ----
            _popupFrame = new Frame
            {
                IsVisible = false,
                BackgroundColor = Colors.White,
                CornerRadius = 12,
                Padding = 0,
                MaximumWidthRequest = 580,
                Margin = new Thickness(20),
                Content = new ScrollView { Content = popupContent },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // ---- 半透明遮罩 ----
            _maskLayer = new BoxView
            {
                BackgroundColor = Colors.Black.WithAlpha(0.5f),
                InputTransparent = false,
                IsVisible = false
            };
            // 遮罩点击关闭弹窗
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => ClosePopup();
            _maskLayer.GestureRecognizers.Add(tapGesture);

            // ---- 添加到主Grid（覆盖全屏） ----
            Grid.SetRowSpan(_maskLayer, 1);
            Grid.SetColumnSpan(_maskLayer, 1);
            Grid.SetRowSpan(_popupFrame, 1);
            Grid.SetColumnSpan(_popupFrame, 1);

            _mainGrid?.Children.Add(_maskLayer);
            _mainGrid?.Children.Add(_popupFrame);
        }

        // ========== 弹窗控制方法 ==========
        private void ShowPopup()
        {
            if (_maskLayer != null) _maskLayer.IsVisible = true;
            if (_popupFrame != null) _popupFrame.IsVisible = true;
            UpdatePopupCount();
        }

        private void ClosePopup()
        {
            if (_maskLayer != null) _maskLayer.IsVisible = false;
            if (_popupFrame != null) _popupFrame.IsVisible = false;
        }

        private void UpdatePopupCount()
        {
            if (_popupCountLabel != null)
            {
                _popupCountLabel.Text = $"追加済み: {AddedPackageList.Count}件";
            }
        }

        // ========== 保存按钮点击事件 ==========
        private void OnSaveButtonClicked(object? sender, EventArgs e)
        {
            // 清空旧数据
            AddedPackageList.Clear();
            if (_entryPackageNo != null) _entryPackageNo.Text = string.Empty;
            if (_entryItemCode != null) _entryItemCode.Text = string.Empty;
            if (_entryQty != null) _entryQty.Text = string.Empty;
            UpdatePopupCount();
            ShowPopup();
        }

        // ========== 弹窗追加按钮点击事件 ==========
        private async void OnPopupAddClick(object? sender, EventArgs e)
        {
            try
            {
                string packageNo = _entryPackageNo?.Text?.Trim() ?? "";
                string itemCode = _entryItemCode?.Text?.Trim() ?? "";
                string qtyText = _entryQty?.Text?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(packageNo))
                {
                    await DisplayAlert("エラー", "梱包Noを入力してください。", "OK");
                    return;
                }
                if (string.IsNullOrWhiteSpace(itemCode))
                {
                    await DisplayAlert("エラー", "品目を入力してください。", "OK");
                    return;
                }
                if (!int.TryParse(qtyText, out int qty) || qty <= 0)
                {
                    await DisplayAlert("エラー", "数量は1以上の数値を入力してください。", "OK");
                    return;
                }

                AddedPackageList.Add(new PackageItem
                {
                    PackageNo = packageNo,
                    ItemCode = itemCode,
                    Qty = qty
                });

                // 清空输入框
                if (_entryPackageNo != null) _entryPackageNo.Text = string.Empty;
                if (_entryItemCode != null) _entryItemCode.Text = string.Empty;
                if (_entryQty != null) _entryQty.Text = string.Empty;

                UpdatePopupCount();

                // 滚动到最新项
                if (_popupList != null)
                {
                     _popupList.ScrollTo(AddedPackageList[AddedPackageList.Count - 1], position: ScrollToPosition.End);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("エラー", $"追加に失敗しました: {ex.Message}", "OK");
            }
        }

        // ========== 以下为原有的辅助方法（保持不变） ==========

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

    // ========== PackageItem 类 ==========
    public class PackageItem
    {
        public string PackageNo { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int Qty { get; set; }
    }
}