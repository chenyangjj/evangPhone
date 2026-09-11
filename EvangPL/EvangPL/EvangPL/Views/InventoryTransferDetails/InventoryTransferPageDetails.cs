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
using EvangPL.Views.InventoryTransfer;

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

        // グローバルスクロールコンテナ参照 + 全明細データソース
        private VerticalStackLayout? _scrollContainer;
        private readonly List<StockInRow> _allBottomData = new List<StockInRow>();

        // ★ 入力中（未保存）のロットリスト
        private readonly List<PendingLotItem> _pendingLots = new List<PendingLotItem>();

        // ★ 入力コントロール参照
        private Picker? _locationPicker;
        private Entry? _lotEntry;
        private Entry? _qtyEntry;

        // ★ プレビュー表と底部表のホスト
        private ContentView? _pendingLotTableHost;
        private ContentView? _bottomPendingTableHost;

        // ★ 色定数
        private static readonly Color InputBorderColor = Color.FromArgb("#cdd2dc");
        private static readonly Color InputBackgroundColor = Colors.White;
        private const int InputCornerRadius = 6;

        private readonly TransferRecord? _editRecord;
        private bool IsEditMode => _editRecord != null;


        public InventoryTransferPageDetails() : this(null)
        {
        }
        public InventoryTransferPageDetails(TransferRecord? editRecord) : base("strInventoryTransfer")
        {
            _editRecord = editRecord;
            Title = IsEditMode ? "在庫振替 - 編集" : "在庫振替 - 新規登録";  // ★ タイトル設定
            BuildUI();
        }

        private void BuildUI()
        {

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
                new StockInRow{ ItemCode = "部品D-1010", LotNo = "LOT20260708", Qty = 40 }
            });

            totalPages = (int)Math.Ceiling((double)_allBottomData.Count / pageSize);

            var mainGrid = new Grid
            {
                RowDefinitions = { new RowDefinition { Height = GridLength.Star } },
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            var detailInputArea = BuildDetailInputArea(registeredLotList);
            CreatePaginationControls();

            // ★ 修正：外側 host の IsVisible で表示/非表示を制御、Content は更新メソッドで動的に設定
            _bottomPendingTableHost = new ContentView
            {
                IsVisible = false,   // 初期 0 件 → 非表示
                Content = BuildBottomPendingTable()
            };

            var saveBtn = new Button
            {
                Text = "保存",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                Margin = new Thickness(10, 5, 10, 10),
                CornerRadius = 6
            };

            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            _scrollContainer.Children.Add(detailInputArea);            // [0]
            _scrollContainer.Children.Add(paginationLayout);           // [1]
            _scrollContainer.Children.Add(_bottomPendingTableHost);    // [2]
            _scrollContainer.Children.Add(saveBtn);                    // [3]

            var scrollView = new ScrollView
            {
                Content = _scrollContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            mainGrid.Add(scrollView, 0, 0);
            Content = mainGrid;

            UpdatePaginationControls();
        }

        // ==================== 明細登録エリア ====================
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

            // ============ 第一行：入庫先ロケーション（★ バーコード付き） ============
            var locRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 50 } }
            };
            locRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.Children.Add(new Label { Text = "入庫先ロケーション (スキャン可)", FontSize = 12, TextColor = Colors.Gray });

            _locationPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = 0,
                BackgroundColor = Colors.White,
                ItemsSource = new List<string> { "WH1-A-03" }
            };
            locRow.Add(WrapInputControl(_locationPicker), 0, 1);
            locRow.Add(BuildBarcodeIcon(), 1, 1);
            layout.Children.Add(locRow);

            // ============ 移動元 / 移動先 ============
            var innerGrid = new Grid
            {
                RowDefinitions = { new RowDefinition(), new RowDefinition() },
                ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() }
            };
            innerGrid.Add(new Label { Text = "移動元ロケーション", FontSize = 12, TextColor = Colors.Gray });
            innerGrid.Add(new Label { Text = "移動先ロケーション", FontSize = 12, TextColor = Colors.Gray }, 1, 0);

            // 移動元（編集モード時は一覧の Source を初期選択）
            var fromItems = new List<string> { "WH1-A-03" };
            if (!string.IsNullOrEmpty(_editRecord?.Source) && !fromItems.Contains(_editRecord!.Source))
                fromItems.Add(_editRecord.Source);

            var fromPicker = new Picker
            {
                Title = "選択",
                BackgroundColor = Colors.White,
                ItemsSource = fromItems
            };
            fromPicker.SelectedItem = _editRecord?.Source ?? fromItems.FirstOrDefault();
            innerGrid.Add(WrapInputControl(fromPicker), 0, 1);

            // 移動先（編集モード時は一覧の Dest を初期選択）
            var toItems = new List<string> { "WH2-C-01" };
            if (!string.IsNullOrEmpty(_editRecord?.Dest) && !toItems.Contains(_editRecord!.Dest))
                toItems.Add(_editRecord.Dest);

            var toPicker = new Picker
            {
                Title = "選択",
                BackgroundColor = Colors.White,
                ItemsSource = toItems
            };
            toPicker.SelectedItem = _editRecord?.Dest ?? toItems.FirstOrDefault();
            innerGrid.Add(WrapInputControl(toPicker), 1, 1);
            layout.Children.Add(innerGrid);

            // ============ 第三行：ロット / シリアル（★ バーコード付き） ============
            layout.Children.Add(new Label { Text = "ロット / シリアル (スキャン可)", FontSize = 12, TextColor = Colors.Gray });

            var lotRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 50 } }
            };
            lotRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _lotEntry = new Entry { Placeholder = "ロット/シリアルをスキャンまたは入力", BackgroundColor = Colors.Transparent };
            lotRow.Add(WrapInputControl(_lotEntry), 0, 1);
            lotRow.Add(BuildBarcodeIcon(), 1, 1);
            layout.Children.Add(lotRow);

            // ============ 数量 ============
            var qtyRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 60 } }
            };
            _qtyEntry = new Entry { Placeholder = "数量を入力", Keyboard = Keyboard.Numeric, BackgroundColor = Colors.Transparent };
            qtyRow.Add(WrapInputControl(_qtyEntry), 0, 0);
            qtyRow.Add(new Label { Text = "個", VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            layout.Children.Add(new Label { Text = "移動数量", FontSize = 12, TextColor = Colors.Gray });
            layout.Children.Add(qtyRow);

            // ============ 登録済みロット（未保存分のプレビュー、❌削除付き） ============
            _pendingLotTableHost = new ContentView { Content = BuildEditableLotTableForPending() };
            layout.Children.Add(_pendingLotTableHost);

            // ============ 「+ロットを追加」ボタン ============
            var addLotBtn = new Button
            {
                Text = "+ロットを追加",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#245a96"),
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 3,
                FontAttributes = FontAttributes.Bold
            };
            addLotBtn.Clicked += OnAddLotButtonClicked;
            layout.Children.Add(addLotBtn);

            border.Content = layout;
            return border;
        }

        // ==================== 「+ロットを追加」押下時 ====================
        private async void OnAddLotButtonClicked(object? sender, EventArgs e)
        {
            // --- 入力値取得 ---
            var locationName = _locationPicker?.SelectedItem as string;
            var lotNo = _lotEntry?.Text?.Trim();
            var qtyText = _qtyEntry?.Text?.Trim();

            // --- 検証（InputDetail と同じ順序・メッセージ） ---
            if (string.IsNullOrEmpty(locationName))
            {
                await DisplayAlert("エラー", "入庫先ロケーションを選択してください。", "OK");
                return;
            }
            if (string.IsNullOrEmpty(lotNo))
            {
                await DisplayAlert("エラー", "ロット/シリアルを入力またはスキャンしてください。", "OK");
                return;
            }
            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "入庫数量を正しく入力してください。", "OK");
                return;
            }

            // --- 1 行を _pendingLots に追加 ---
            _pendingLots.Add(new PendingLotItem
            {
                ItemCode = locationName,   // 現状は入庫先ロケーション名を格納
                LotNo = lotNo,
                Qty = qty
            });

            // --- 入力欄クリア ---
            if (_lotEntry != null) _lotEntry.Text = string.Empty;
            if (_qtyEntry != null) _qtyEntry.Text = string.Empty;

            // --- ★ 両方の表を再構築 ---
            RefreshPendingLotTable();      // 明細登録エリア内のプレビュー表
            RefreshBottomPendingTable();   // ページ下部の「登録済み明細」表
        }

        // ==================== 未保存プレビュー表（登録済みロット） ====================
        private Border BuildEditableLotTableForPending()
        {
            return BuildEditableLotTableInternal(_pendingLots, showItemColumn: false);
        }

        // ==================== ❌タップで該当行を削除 ====================
        private void OnDeletePendingLot(PendingLotItem item)
        {
            _pendingLots.Remove(item);
            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // 上のプレビュー表を再構築
        private void RefreshPendingLotTable()
        {
            if (_pendingLotTableHost != null)
            {
                _pendingLotTableHost.Content = BuildEditableLotTableForPending();
            }
        }

        // 下の「登録済み明細」表を再構築
        // ★ InputDetail と同じく、0件かどうかで表示/非表示を切り替える
        private void RefreshBottomPendingTable()
        {
            if (_bottomPendingTableHost == null) return;

            if (_pendingLots.Count == 0)
            {
                // 0件 → 表そのものを隠す
                _bottomPendingTableHost.IsVisible = false;
            }
            else
            {
                // 1件以上 → 先に IsVisible=true、その後 Content を更新
                // （先に Content を入れてから IsVisible=true にすると、Android で反映されないケースがある）
                _bottomPendingTableHost.IsVisible = true;
                _bottomPendingTableHost.Content = BuildBottomPendingTable();
            }
        }

        // ==================== 削除ボタン付き一覧テーブルの共通実装 ====================
        private Border BuildEditableLotTableInternal(List<PendingLotItem> items, bool showItemColumn)
        {
            List<string> headers;
            List<GridLength> columnWidths;
            if (showItemColumn)
            {
                headers = new List<string> { "品目", "ロット", "数量", "" };
                columnWidths = new List<GridLength>
                {
                    new GridLength(3, GridUnitType.Star),
                    new GridLength(3, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star)
                };
            }
            else
            {
                headers = new List<string> { "登録済みロット", "数量", "" };
                columnWidths = new List<GridLength>
                {
                    new GridLength(3, GridUnitType.Star),
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(1, GridUnitType.Star)
                };
            }

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

                var item = items[r];
                int col = 0;
                if (showItemColumn)
                {
                    tableGrid.Add(new Label { Text = item.ItemCode, FontSize = 11, Padding = new Thickness(4) }, col++, dataRowIndex);
                }
                tableGrid.Add(new Label { Text = item.LotNo, FontSize = 11, Padding = new Thickness(4) }, col++, dataRowIndex);
                tableGrid.Add(new Label { Text = $"{item.Qty}個", FontSize = 11, Padding = new Thickness(4) }, col++, dataRowIndex);

                var deleteLabel = new Label
                {
                    Text = "❌",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Center
                };
                var capturedItem = item;
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnDeletePendingLot(capturedItem);
                deleteLabel.GestureRecognizers.Add(tapGesture);
                tableGrid.Add(deleteLabel, col, dataRowIndex);
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

        // ==================== 底部：全品目一覧（登録済み明細） ====================
        // ★ 修正：IsVisible=false の ContentView を返さない（外側 host で表示制御を行う）
        private View BuildBottomPendingTable()
        {
            // ★ 0件なら「登録済み明細(0件)」ごと非表示にする
            if (_pendingLots.Count == 0)
            {
                return new ContentView { IsVisible = false };
            }

            var container = new VerticalStackLayout { Spacing = 4 };
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({_pendingLots.Count}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });
            container.Children.Add(BuildEditableLotTableInternal(_pendingLots, showItemColumn: true));
            return container;
        }

        // ==================== ページ送り部分 ====================
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
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
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

        #region 分页逻辑
        private void OnPrevButtonClicked(object sender, EventArgs e)
        {
            if (currentPage > 0) LoadPage(currentPage - 1);
        }

        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            if (currentPage < totalPages - 1) LoadPage(currentPage + 1);
        }

        private void LoadPage(int pageIndex)
        {
            if (_allBottomData == null || pageIndex < 0 || pageIndex >= totalPages)
                return;

            currentPage = pageIndex;

            // ★ 保持：_bottomPendingTableHost は上書きしない
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
        public class StockInRow
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }

        public class PendingLotItem
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }
    }
}