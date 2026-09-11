using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using EvangPL.Components;
using EvangPL.Utils;
using MauiIcons.Core;
using MauiIcons.Fluent;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Text.Json;
using PickingDetailInfo = EvangPL.Utils.PickingDetailInfo;

namespace EvangPL.Views.PickingDetail
{
    /// <summary>
    /// 出荷明細画面（入庫明細画面 InputDetail をベースに出荷用に改造）
    /// </summary>
    public class PickingDetail : EvangContentVM
    {
        // ==================== UIコントロール参照 ====================
        private Border? pageHeaderInfo;
        private PickingDetailInfo? _detailInfo;
        private VerticalStackLayout? _scrollContainer;
        private ContentView? _detailInputAreaHost;      // 明細登録エリア（選択中包裹専用）
        private ContentView? _bottomPendingTableHost;   // 底部：全包裹の明細一覧

        // ==================== データソース ====================
        // ① 出荷対象の包裹一覧（ヘッダー上部の選択テーブルの対象）
        private List<PackageItem> _packageItems = new List<PackageItem>();
        // ② 入力中の明細（まだ保存していない）
        private readonly List<PendingDetailItem> _pendingDetails = new List<PendingDetailItem>();

        // ==================== 選択状態 ====================
        private PackageItem? _selectedPackage = null;   // 現在選択中の包裹

        // ==================== 入力コントロール ====================
        private Entry? _entryLocation;
        private Entry? _entryLot;
        private Entry? _entryQty;

        // ==================== 色定数 ====================
        private static readonly Color InputBorderColor = Color.FromArgb("#cdd2dc");
        private static readonly Color InputBackgroundColor = Colors.White;
        private const int InputCornerRadius = 6;
        private static readonly Color SelectedRowColor = Color.FromArgb("#d7e8fa");

        // ==================== ポップアップ関連 ====================
        private Frame? _popupFrame;
        private BoxView? _popupMaskLayer;
        private Entry? _entryPackageNo;
        private Entry? _entryItemCode;
        private Entry? _entryPopupQty;
        private Button? _btnAddItem;
        private Button? _btnFinishPopup;
        private Button? _btnClosePopup;
        private CollectionView? _popupList;
        public ObservableCollection<PackageItem> AddedPackageList { get; set; } = new ObservableCollection<PackageItem>();

        // ==================== コンストラクター ====================
        public PickingDetail() : base("strPickingDetail")
        {
            _detailInfo = new PickingDetailInfo
            {
                OrderNo = "SO-2026-0987",
                CustomerName = "山田工業(株)",
                ScheduleDate = "2026-07-08",
                ItemCount = 4,
                TotalQty = 210,
                Status = "未出荷"
            };
            InitializeMockData();
            BuildUI();
        }

        public PickingDetail(PickingDetailInfo detailInfo) : base("strPickingDetail")
        {
            _detailInfo = detailInfo;
            InitializeMockData();
            BuildUI();
        }

        // ==================== モックデータ初期化 ====================
        private void InitializeMockData()
        {
            _packageItems = new List<PackageItem>
            {
                new PackageItem
                {
                    PackageNo = "BOX-0009",
                    ItemCode = "部品B-2020",
                    Qty = 20,
                    Customer = _detailInfo?.CustomerName ?? "山田工業(株)",
                    ShipDate = _detailInfo?.ScheduleDate ?? "2026-07-08",
                    Details = new List<PackageDetail>()
                },
                new PackageItem
                {
                    PackageNo = "BOX-0010",
                    ItemCode = "部品C-3030",
                    Qty = 10,
                    Customer = _detailInfo?.CustomerName ?? "山田工業(株)",
                    ShipDate = _detailInfo?.ScheduleDate ?? "2026-07-08",
                    Details = new List<PackageDetail>()
                }
            };
        }

        // ==================== UI構築 ====================
        private async void BuildUI()
        {
            await BuildCompleteUI();
        }

        private async Task BuildCompleteUI()
        {
            var mainGrid = new Grid
            {
                RowDefinitions = { new RowDefinition { Height = GridLength.Star } },
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // 1. ヘッダー（顧客/出荷予定日 + 包裹選択テーブル）
            pageHeaderInfo = BuildHeader();

            // 2. 明細登録エリア（選択なしの場合は非表示）
            var detailInputArea = BuildDetailInputArea();
            _detailInputAreaHost = new ContentView { Content = detailInputArea };

            // 3. 底部：全包裹の明細一覧
            _bottomPendingTableHost = new ContentView { Content = BuildBottomPendingTable() };

            // 4. 保存ボタン（クリックでポップアップを表示）
            var saveBtn = new Button
            {
                Text = "保存",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                Margin = new Thickness(10, 5, 10, 10),
                CornerRadius = 6
            };
            saveBtn.Clicked += OnSaveButtonClicked;   // ★ 内部でポップアップ表示に変更

            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            _scrollContainer.Children.Add(pageHeaderInfo);          // [0]
            _scrollContainer.Children.Add(_detailInputAreaHost);   // [1]
            _scrollContainer.Children.Add(_bottomPendingTableHost); // [2]
            _scrollContainer.Children.Add(saveBtn);                 // [3]

            var scrollView = new ScrollView
            {
                Content = _scrollContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            mainGrid.Add(scrollView, 0, 0);

            // ★ ポップアップを最前面に追加（GridのRowSpanで全画面カバー）
            BuildPopup(mainGrid);

            Content = mainGrid;
        }

        // ==================== ポップアップ構築（StockOutDetail を模倣） ====================
        private void BuildPopup(Grid parentGrid)
        {
            // ---- 入力コントロール ----
            _entryPackageNo = new Entry { Placeholder = "梱包No.(スキャン可)", BackgroundColor = Colors.White };
            _entryItemCode = new Entry { Placeholder = "品目(スキャン可)", BackgroundColor = Colors.White };
            _entryPopupQty = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "数量", BackgroundColor = Colors.White };

            // ---- 追加ボタン ----
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

            // ---- 完了ボタン ----
            _btnFinishPopup = new Button
            {
                Text = "完了",
                BackgroundColor = Color.FromArgb("#255499"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Margin = new Thickness(0, 20, 0, 0)
            };
            _btnFinishPopup.Clicked += (s, e) => ClosePopup();

            // ---- 閉じるボタン ----
            _btnClosePopup = new Button
            {
                Text = "×",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Gray,
                WidthRequest = 45,
                FontSize = 24
            };
            _btnClosePopup.Clicked += (s, e) => ClosePopup();

            // ---- タイトル行 ----
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

            // ---- ★ 梱包No. 行（Entry + バーコードアイコン） ----
            var packageRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };
            packageRow.Add(_entryPackageNo, 0, 0);
            packageRow.Add(BuildBarcodeIcon(), 1, 0);

            // ---- ★ 品目 行（Entry + バーコードアイコン） ----
            var itemRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };
            itemRow.Add(_entryItemCode, 0, 0);
            itemRow.Add(BuildBarcodeIcon(), 1, 0);

            // ---- 数量行（バーコードなし） ----
            // _entryPopupQty はそのまま使用

            // ---- 表頭 ----
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
            var qtyHeader = new Label { Text = "数量", FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End };
            Grid.SetColumn(qtyHeader, 2);
            tableHeader.Children.Add(qtyHeader);

            // ---- ポップアップ内リスト ----
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

            // ---- ポップアップ内容レイアウト ----
            var popupContent = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 15,
                BackgroundColor = Colors.White,
                Children =
                {
                    titleRow,
                    packageRow,       // ★ バーコード付き
                    itemRow,          // ★ バーコード付き
                    _entryPopupQty,   // 数量（バーコードなし）
                    _btnAddItem,
                    tableHeader,
                    _popupList,
                    _btnFinishPopup
                }
            };

            // ---- ポップアップ Frame ----
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

            // ---- 半透明マスク ----
            _popupMaskLayer = new BoxView
            {
                BackgroundColor = Colors.Black.WithAlpha(0.5f),
                InputTransparent = false
            };
            _popupMaskLayer.SetBinding(BoxView.IsVisibleProperty, new Binding(nameof(_popupFrame.IsVisible), source: _popupFrame));

            // ---- Grid にオーバーレイとして追加 ----
            Grid.SetRowSpan(_popupMaskLayer, 1);
            Grid.SetColumnSpan(_popupMaskLayer, 1);
            Grid.SetRowSpan(_popupFrame, 1);
            Grid.SetColumnSpan(_popupFrame, 1);

            parentGrid.Children.Add(_popupMaskLayer);
            parentGrid.Children.Add(_popupFrame);
        }

        // ==================== ポップアップ制御 ====================
        private void ClosePopup()
        {
            if (_popupFrame != null) _popupFrame.IsVisible = false;
        }

        // ==================== ポップアップの「+ この内容を追加」 ====================
        private async void OnPopupAddClick(object? sender, EventArgs e)
        {
            if (!int.TryParse(_entryPopupQty?.Text?.Trim(), out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "数量は1以上を入力してください。", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(_entryPackageNo?.Text) || string.IsNullOrWhiteSpace(_entryItemCode?.Text))
            {
                await DisplayAlert("エラー", "梱包No、品目は必須です。", "OK");
                return;
            }

            AddedPackageList.Add(new PackageItem
            {
                PackageNo = _entryPackageNo.Text!.Trim(),
                ItemCode = _entryItemCode.Text!.Trim(),
                Qty = qty
            });

            _entryPackageNo.Text = string.Empty;
            _entryItemCode.Text = string.Empty;
            _entryPopupQty.Text = string.Empty;

            if (_popupList != null && AddedPackageList.Count > 0)
            {
                _popupList.ScrollTo(AddedPackageList[AddedPackageList.Count - 1], position: ScrollToPosition.End);
            }
        }

        // ==================== 保存ボタン（ポップアップを開く） ====================
        private void OnSaveButtonClicked(object? sender, EventArgs e)
        {
            // 開く前にデータをクリア
            AddedPackageList.Clear();
            if (_entryPackageNo != null) _entryPackageNo.Text = string.Empty;
            if (_entryItemCode != null) _entryItemCode.Text = string.Empty;
            if (_entryPopupQty != null) _entryPopupQty.Text = string.Empty;

            if (_popupFrame != null) _popupFrame.IsVisible = true;
        }

        // ==================== ヘッダー（顧客/出荷予定日 + 包裹選択テーブル） ====================
        private Border BuildHeader()
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

            string customer = _detailInfo?.CustomerName ?? "";
            string shipDate = _detailInfo?.ScheduleDate ?? "";

            var customerBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(0, 0, 2, 15)
            };
            customerBorder.Content = new Label { Text = customer, FontSize = 14, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
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
            dateBorder.Content = new Label { Text = shipDate, FontSize = 15, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            innerGrid.Add(dateBorder, 1, 1);

            // 包裹選択テーブル（タップで選択）
            var packageTable = BuildPackageSelectionTable();
            Grid.SetRow(packageTable, 2);
            Grid.SetColumnSpan(packageTable, 2);
            innerGrid.Add(packageTable);

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

        // ==================== 包裹選択テーブル ====================
        private Border BuildPackageSelectionTable()
        {
            var headers = new List<string> { "アイテム", "出荷済みロット", "数量" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(2, GridUnitType.Star),
                new GridLength(2, GridUnitType.Star),
                new GridLength(1, GridUnitType.Star)
            };

            var tableGrid = new Grid();
            foreach (var width in columnWidths)
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });

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

            for (int r = 0; r < _packageItems.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var item = _packageItems[r];
                bool isSelected = _selectedPackage != null && _selectedPackage.PackageNo == item.PackageNo;
                var rowBg = isSelected ? SelectedRowColor : Colors.White;

                var pkgLabel = new Label { Text = item.ItemCode, FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };
                var itemLabel = new Label { Text = item.PackageNo, FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };
                var qtyLabel = new Label { Text = item.Qty.ToString(), FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };

                tableGrid.Add(pkgLabel, 0, dataRowIndex);
                tableGrid.Add(itemLabel, 1, dataRowIndex);
                tableGrid.Add(qtyLabel, 2, dataRowIndex);

                // 行全体をタップ可能にする
                var capturedItem = item;
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnPackageRowSelected(capturedItem);
                pkgLabel.GestureRecognizers.Add(tapGesture);
                itemLabel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnPackageRowSelected(capturedItem)) });
                qtyLabel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnPackageRowSelected(capturedItem)) });
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

        // ==================== 包裹行選択イベント ====================
        private void OnPackageRowSelected(PackageItem item)
        {
            _selectedPackage = item;
            RefreshHeaderAndDetailArea();
        }

        // ==================== ヘッダーと明細登録エリアを再構築して差し替え ====================
        private void RefreshHeaderAndDetailArea()
        {
            var newHeader = BuildHeader();
            var newDetailInputArea = BuildDetailInputArea();

            if (_scrollContainer != null && _scrollContainer.Children.Count > 1)
            {
                _scrollContainer.Children[0] = newHeader;
                _scrollContainer.Children[1] = newDetailInputArea;
            }
            pageHeaderInfo = newHeader;
            RefreshBottomPendingTable();
        }

        // ==================== 明細登録エリア ====================
        private View BuildDetailInputArea()
        {
            if (_selectedPackage == null)
            {
                return new ContentView { IsVisible = false };
            }

            var currentPackage = _selectedPackage;

            var border = new Border
            {
                Stroke = Color.FromArgb("#b4cee8"),
                Background = Color.FromArgb("#e6f0fa"),
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(5),
                StrokeThickness = 2
            };

            var layout = new VerticalStackLayout { Spacing = 10 };
            layout.Children.Add(new Label
            {
                Text = $"明細登録（{currentPackage.PackageNo}）",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold
            });

            // 1. 出荷元ロケーション行（Entry + バーコードアイコン）
            layout.Children.Add(new Label { Text = "出荷元ロケーション (スキャン可)", FontSize = 12, TextColor = Colors.Gray });
            var locRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };
            _entryLocation = new Entry { Placeholder = "スキャンまたは入力", BackgroundColor = Colors.Transparent };
            locRow.Add(WrapInputControl(_entryLocation), 0, 0);
            locRow.Add(BuildBarcodeIcon(), 1, 0);
            layout.Children.Add(locRow);

            // 2. ロット行（Entry + バーコードアイコン）
            layout.Children.Add(new Label { Text = "ロット (スキャン可)", FontSize = 12, TextColor = Colors.Gray });
            var lotRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };
            _entryLot = new Entry { Placeholder = "スキャンまたは入力", BackgroundColor = Colors.Transparent };
            lotRow.Add(WrapInputControl(_entryLot), 0, 0);
            lotRow.Add(BuildBarcodeIcon(), 1, 0);
            layout.Children.Add(lotRow);

            // 3. 数量
            layout.Children.Add(new Label { Text = "数量", FontSize = 12, TextColor = Colors.Gray });
            var qtyRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 60 }
                }
            };
            _entryQty = new Entry { Placeholder = "数量を入力", Keyboard = Keyboard.Numeric, BackgroundColor = Colors.Transparent };
            qtyRow.Add(WrapInputControl(_entryQty), 0, 0);
            qtyRow.Add(new Label { Text = "個", VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            layout.Children.Add(qtyRow);

            // 4. 選択中包裹に紐づく明細プレビュー表（登録済み明細）
            var detailTable = BuildEditableDetailTableForCurrentPackage();
            layout.Children.Add(detailTable);

            // 5. 「+明細を追加」ボタン
            var addBtn = new Button
            {
                Text = "+ 明細を追加",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#245a96"),
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 3,
                FontAttributes = FontAttributes.Bold
            };
            addBtn.Clicked += OnAddDetailClicked;
            layout.Children.Add(addBtn);

            border.Content = layout;
            return border;
        }

        // ==================== 現在選択中の包裹の明細一覧（ロット/数量/❌） ====================
        private Border BuildEditableDetailTableForCurrentPackage()
        {
            var currentPackage = _selectedPackage;
            if (currentPackage == null || currentPackage.Details.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "登録済み明細(0件)",
                    FontSize = 12,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var border = new Border
                {
                    Background = Colors.White,
                    Padding = new Thickness(8),
                    Stroke = InputBorderColor,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 4 }
                };
                border.Content = emptyLabel;
                return border;
            }

            var items = currentPackage.Details;
            var headers = new List<string> { "ロット", "数量", "" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(3, GridUnitType.Star),
                new GridLength(2, GridUnitType.Star),
                new GridLength(1, GridUnitType.Star)
            };

            var tableGrid = new Grid();
            foreach (var width in columnWidths)
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });

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

            for (int r = 0; r < items.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var detail = items[r];
                tableGrid.Add(new Label { Text = detail.DetailNo, FontSize = 11, Padding = new Thickness(4) }, 0, dataRowIndex);
                tableGrid.Add(new Label { Text = $"{detail.DetailQty}個", FontSize = 11, Padding = new Thickness(4) }, 1, dataRowIndex);

                var deleteLabel = new Label
                {
                    Text = "❌",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Center
                };
                var capturedDetail = detail;
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnDeleteDetail(capturedDetail);
                deleteLabel.GestureRecognizers.Add(tapGesture);
                tableGrid.Add(deleteLabel, 2, dataRowIndex);
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

        // ==================== 明細追加イベント ====================
        private async void OnAddDetailClicked(object? sender, EventArgs e)
        {
            if (_selectedPackage == null)
            {
                await DisplayAlert("エラー", "梱包が選択されていません。", "OK");
                return;
            }

            var location = _entryLocation?.Text?.Trim();
            var detailNo = _entryLot?.Text?.Trim();
            var qtyText = _entryQty?.Text?.Trim();

            if (string.IsNullOrEmpty(location))
            {
                await DisplayAlert("エラー", "出荷元ロケーションを入力してください。", "OK");
                return;
            }
            if (string.IsNullOrEmpty(detailNo))
            {
                await DisplayAlert("エラー", "ロットを入力してください。", "OK");
                return;
            }
            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "数量は1以上の整数で入力してください。", "OK");
                return;
            }

            _selectedPackage.Details.Add(new PackageDetail
            {
                DetailNo = detailNo,
                DetailQty = qty,
                Location = location
            });

            // 入力クリア
            if (_entryLocation != null) _entryLocation.Text = string.Empty;
            if (_entryLot != null) _entryLot.Text = string.Empty;
            if (_entryQty != null) _entryQty.Text = string.Empty;

            RefreshHeaderAndDetailArea();
            RefreshBottomPendingTable();
        }

        // ==================== 明細削除イベント ====================
        private void OnDeleteDetail(PackageDetail detail)
        {
            if (_selectedPackage == null) return;
            _selectedPackage.Details.Remove(detail);
            RefreshHeaderAndDetailArea();
            RefreshBottomPendingTable();
        }

        // ==================== 底部：全包裹の明細一覧 ====================
        private View BuildBottomPendingTable()
        {
            var allDetails = _packageItems
                .SelectMany(p => p.Details.Select(d => new { Package = p, Detail = d }))
                .ToList();

            if (allDetails.Count == 0)
            {
                return new ContentView { IsVisible = false };
            }

            var container = new VerticalStackLayout { Spacing = 4 };
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({allDetails.Count}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });

            var headers = new List<string> { "品目", "ロット", "数量" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(3, GridUnitType.Star),
                new GridLength(3, GridUnitType.Star),
                new GridLength(1, GridUnitType.Star)
            };

            var tableGrid = new Grid();
            foreach (var width in columnWidths)
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });

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

            for (int r = 0; r < allDetails.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var item = allDetails[r];
                tableGrid.Add(new Label { Text = item.Package.ItemCode, FontSize = 11, Padding = new Thickness(4) }, 0, dataRowIndex);
                tableGrid.Add(new Label { Text = item.Detail.DetailNo, FontSize = 11, Padding = new Thickness(4) }, 1, dataRowIndex);
                tableGrid.Add(new Label { Text = $"{item.Detail.DetailQty}個", FontSize = 11, Padding = new Thickness(4) }, 2, dataRowIndex);
            }

            var tableBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { },
                Background = Colors.White
            };
            tableBorder.Content = tableGrid;
            container.Children.Add(tableBorder);
            return container;
        }

        // ==================== 底部テーブルのリフレッシュ ====================
        private void RefreshBottomPendingTable()
        {
            if (_bottomPendingTableHost != null)
            {
                _bottomPendingTableHost.Content = BuildBottomPendingTable();
            }
        }

        // ==================== ヘルパー：入力コントロールをBorderでラップ ====================
        private Border WrapInputControl(View control)
        {
            return new Border
            {
                Stroke = InputBorderColor,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = InputCornerRadius },
                BackgroundColor = InputBackgroundColor,
                Padding = new Thickness(8, 0),
                Content = control
            };
        }

        // ==================== バーコードアイコン描画 ====================
        private Border BuildBarcodeIcon()
        {
            var barsLayout = new HorizontalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            double[] barWidths = { 2, 4, 2, 6, 2, 4, 2 };
            foreach (var w in barWidths)
            {
                barsLayout.Children.Add(new BoxView
                {
                    Color = Color.FromArgb("#1e3a5f"),
                    WidthRequest = w,
                    HeightRequest = 22,
                    VerticalOptions = LayoutOptions.Center
                });
            }

            return new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                BackgroundColor = Colors.White,
                Padding = new Thickness(8, 6),
                WidthRequest = 50,
                HeightRequest = 45,
                HorizontalOptions = LayoutOptions.End,
                Content = barsLayout
            };
        }

        // ==================== データモデル ====================
        public class PackageDetail
        {
            public string DetailNo { get; set; } = string.Empty;
            public int DetailQty { get; set; }
            public string Location { get; set; } = string.Empty;
        }

        public class PackageItem
        {
            public string PackageNo { get; set; } = string.Empty;
            public string ItemCode { get; set; } = string.Empty;
            public int Qty { get; set; }
            public string Customer { get; set; } = string.Empty;
            public string ShipDate { get; set; } = string.Empty;
            public List<PackageDetail> Details { get; set; } = new List<PackageDetail>();
        }

        public class PendingDetailItem
        {
            public string PackageNo { get; set; } = string.Empty;
            public string ItemCode { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string LotNo { get; set; } = string.Empty;
            public int Qty { get; set; }
        }
    }
}