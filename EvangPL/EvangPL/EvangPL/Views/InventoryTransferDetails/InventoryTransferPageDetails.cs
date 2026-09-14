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

        private VerticalStackLayout? _scrollContainer;

        // ★ 入力中（未保存）のロットリスト
        private readonly List<PendingLotItem> _pendingLots = new List<PendingLotItem>();

        // ★ 入力コントロール参照
        private Picker? _locationPicker;
        private Entry? _lotEntry;
        private Entry? _qtyEntry;

        // ★ プレビュー表と底部表のホスト
        private ContentView? _pendingLotTableHost;
        private ContentView? _bottomPendingTableHost;

        // ★ 品目入力（IsLotItem で Lot 関連の表示制御）
        private Entry? _itemEntry;
        private VerticalStackLayout? _lotSectionContainer;
        private readonly List<ItemMaster> _itemMasters = new List<ItemMaster>
        {
            new ItemMaster { ItemCode = "部品C-3030 / 強化ガラス基板", IsLotItem = true  },
            new ItemMaster { ItemCode = "部品D-1010 / 洗浄後基板",     IsLotItem = false }
        };

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
            Title = IsEditMode ? "在庫振替 - 編集" : "在庫振替 - 新規登録";
            BuildUI();
        }

        private void BuildUI()
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

            var detailInputArea = BuildDetailInputArea();

            _bottomPendingTableHost = new ContentView
            {
                IsVisible = false,
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
            // ★ 保存ボタンにクリックイベントを追加
            saveBtn.Clicked += OnSaveClicked;

            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            _scrollContainer.Children.Add(detailInputArea);            // [0]
            _scrollContainer.Children.Add(_bottomPendingTableHost);    // [1]
            _scrollContainer.Children.Add(saveBtn);                    // [2]

            var scrollView = new ScrollView
            {
                Content = _scrollContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            mainGrid.Add(scrollView, 0, 0);
            Content = mainGrid;
        }

        // ==================== 明細登録エリア ====================
        private Border BuildDetailInputArea()
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

            // ==================================================
            // ★ 常に表示：第一行 品目(スキャン可)
            // ==================================================
            layout.Children.Add(new Label { Text = "品目(スキャン可)", FontSize = 12, TextColor = Colors.Gray });

            var itemRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                },
                ColumnSpacing = 10
            };
            _itemEntry = new Entry
            {
                Placeholder = "品目コードを入力またはスキャン",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center
            };
            _itemEntry.TextChanged += OnItemTextChanged;

            itemRow.Add(WrapInputControl(_itemEntry), 0, 0);
            itemRow.Add(BuildBarcodeIcon(), 1, 0);
            layout.Children.Add(itemRow);

            // ==================================================
            // ★ 常に表示：第二行 移動元 / 移動先
            // ==================================================
            var innerGrid = new Grid
            {
                RowDefinitions = { new RowDefinition(), new RowDefinition() },
                ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() }
            };
            innerGrid.Add(new Label { Text = "移動元ロケーション", FontSize = 12, TextColor = Colors.Gray });
            innerGrid.Add(new Label { Text = "移動先ロケーション", FontSize = 12, TextColor = Colors.Gray }, 1, 0);

            var fromItems = new List<string> { "WH1-A-03", "WH2-A-01" };
            if (!string.IsNullOrEmpty(_editRecord?.Source) && !fromItems.Contains(_editRecord!.Source))
                fromItems.Add(_editRecord.Source);

            var fromPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = -1,
                BackgroundColor = Colors.White,
                ItemsSource = fromItems
            };
            if (IsEditMode)
                fromPicker.SelectedItem = _editRecord?.Source;
            innerGrid.Add(WrapInputControl(fromPicker), 0, 1);

            var toItems = new List<string> { "WH2-C-01", "WH1-C-04" };
            if (!string.IsNullOrEmpty(_editRecord?.Dest) && !toItems.Contains(_editRecord!.Dest))
                toItems.Add(_editRecord.Dest);

            var toPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = -1,
                BackgroundColor = Colors.White,
                ItemsSource = toItems
            };
            if (IsEditMode)
                toPicker.SelectedItem = _editRecord?.Dest;
            innerGrid.Add(WrapInputControl(toPicker), 1, 1);
            layout.Children.Add(innerGrid);

            // ==================================================
            // ★ IsLotItem=true のときだけ表示：入庫先 / ロット / 移動数量 / 表 / +ボタン
            // ==================================================
            _lotSectionContainer = new VerticalStackLayout
            {
                Spacing = 10,
                IsVisible = false
            };

            // --- 入庫先ロケーション ---
            _lotSectionContainer.Children.Add(new Label
            {
                Text = "入庫先ロケーション (スキャン可)",
                FontSize = 12,
                TextColor = Colors.Gray
            });

            var locRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                },
                ColumnSpacing = 10
            };
            var locationItems = new List<string> { "WH1-A-03", "WH2-C-01" };
            _locationPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = -1,
                BackgroundColor = Colors.White,
                ItemsSource = locationItems
            };
            locRow.Add(WrapInputControl(_locationPicker), 0, 0);
            locRow.Add(BuildBarcodeIcon(), 1, 0);
            _lotSectionContainer.Children.Add(locRow);

            // --- ロット / シリアル ---
            _lotSectionContainer.Children.Add(new Label
            {
                Text = "ロット / シリアル (スキャン可)",
                FontSize = 12,
                TextColor = Colors.Gray
            });

            var lotRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                },
                ColumnSpacing = 10
            };
            _lotEntry = new Entry { Placeholder = "ロット/シリアルをスキャンまたは入力", BackgroundColor = Colors.Transparent };
            lotRow.Add(WrapInputControl(_lotEntry), 0, 0);
            lotRow.Add(BuildBarcodeIcon(), 1, 0);
            _lotSectionContainer.Children.Add(lotRow);

            // --- 移動数量 ---
            _lotSectionContainer.Children.Add(new Label
            {
                Text = "移動数量",
                FontSize = 12,
                TextColor = Colors.Gray
            });

            var qtyRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 60 }
                },
                ColumnSpacing = 10
            };
            _qtyEntry = new Entry { Placeholder = "数量を入力", Keyboard = Keyboard.Numeric, BackgroundColor = Colors.Transparent };
            qtyRow.Add(WrapInputControl(_qtyEntry), 0, 0);
            qtyRow.Add(new Label { Text = "個", VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            _lotSectionContainer.Children.Add(qtyRow);

            // --- 未保存プレビュー表 ---
            _pendingLotTableHost = new ContentView { Content = BuildEditableLotTableForPending() };
            _lotSectionContainer.Children.Add(_pendingLotTableHost);

            // --- 「+ロットを追加」ボタン ---
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
            _lotSectionContainer.Children.Add(addLotBtn);

            layout.Children.Add(_lotSectionContainer);

            border.Content = layout;
            return border;
        }

        // ==================== 保存ボタン押下時 ====================
        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            try
            {
                // 必要であればここで保存処理（API 呼び出しなど）を実装
                // 今回はダミーでメッセージを表示してから一覧画面へ戻る
                await DisplayAlert("完了", "在庫振替を保存しました (ダミー)", "OK");

                // ★ 一覧画面へ戻る（Push 元へ Pop）
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
            }
        }

        // ==================== 品目入力変化時（Entry 版） ====================
        private void OnItemTextChanged(object? sender, TextChangedEventArgs e)
        {
            var matched = GetMatchedItemMaster();

            if (_lotSectionContainer != null)
            {
                _lotSectionContainer.IsVisible = matched?.IsLotItem ?? false;
            }

            if (matched == null || !matched.IsLotItem)
            {
                _pendingLots.Clear();
                RefreshPendingLotTable();
                RefreshBottomPendingTable();
            }
        }

        // ★ 入力テキストから ItemMaster を引く（部分一致）
        private ItemMaster? GetMatchedItemMaster()
        {
            var text = _itemEntry?.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return null;

            return _itemMasters.FirstOrDefault(m =>
                !string.IsNullOrEmpty(m.ItemCode) &&
                m.ItemCode.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        // ==================== 「+ロットを追加」押下時 ====================
        private async void OnAddLotButtonClicked(object? sender, EventArgs e)
        {
            var matchedItem = GetMatchedItemMaster();
            if (matchedItem == null || !matchedItem.IsLotItem)
            {
                await DisplayAlert("エラー", "ロット対象の品目を入力してください。", "OK");
                return;
            }

            var locationName = _locationPicker?.SelectedItem as string;
            var lotNo = _lotEntry?.Text?.Trim();
            var qtyText = _qtyEntry?.Text?.Trim();

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

            _pendingLots.Add(new PendingLotItem
            {
                ItemCode = matchedItem.ItemCode,
                LotNo = lotNo,
                Qty = qty
            });

            if (_lotEntry != null) _lotEntry.Text = string.Empty;
            if (_qtyEntry != null) _qtyEntry.Text = string.Empty;

            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // ==================== 未保存プレビュー表 ====================
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

        private void RefreshPendingLotTable()
        {
            if (_pendingLotTableHost != null)
            {
                _pendingLotTableHost.Content = BuildEditableLotTableForPending();
            }
        }

        private void RefreshBottomPendingTable()
        {
            if (_bottomPendingTableHost == null) return;

            if (_pendingLots.Count == 0)
            {
                _bottomPendingTableHost.IsVisible = false;
            }
            else
            {
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
        private View BuildBottomPendingTable()
        {
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
        public class PendingLotItem
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }

        // ★ 品目マスタ（IsLotItem で Lot 関連の表示制御）
        public class ItemMaster
        {
            public string ItemCode { get; set; } = "";
            public bool IsLotItem { get; set; }
        }
    }
}