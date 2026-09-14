using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json;
using EvangPL.Views.StockAdjust;

namespace EvangPL.Views.InventoryAdjustment
{
    public class InventoryAdjustment : EvangContentVM
    {
        // ==========================================
        // Android原生の下線を消去するためのHandler登録
        // ==========================================
        static InventoryAdjustment()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
        }

        // ==========================================
        // UI コントロール
        // ==========================================
        private Entry? itemEntry;               // ★ Picker → Entry に変更
        private Border? scanButtonBorder;
        private Picker? locationPicker;
        private Entry? currentStockEntry;
        private Entry? differenceEntry;
        private Border? _diffBorder;
        private Entry? adjustedStockEntry;
        private Picker? reasonPicker;
        private Button? registerButton;

        private VerticalStackLayout? _lotSectionContainer;

        private Entry? _lotEntry;
        private Entry? _qtyEntry;

        private readonly List<PendingLotItem> _pendingLots = new List<PendingLotItem>();

        private ContentView? _pendingLotTableHost;
        private ContentView? _bottomPendingTableHost;

        private readonly List<ItemMaster> _itemMasters = new List<ItemMaster>
        {
            new ItemMaster { ItemCode = "部品E-5050 / 洗浄前基板", IsLotItem = true  },
            new ItemMaster { ItemCode = "部品F-6060 / 洗浄後基板", IsLotItem = false }
        };

        private Grid? mainGrid;
        private int _currentStockValue = 480;

        private readonly StockAdjustItem? _editItem;
        private bool IsEditMode => _editItem != null;

        private static readonly Color InputBorderColor = Color.FromArgb("#cdd2dc");

        // ★ 差異入力の有効/無効時の色
        private static readonly Color DiffEnabledBg = Colors.White;
        private static readonly Color DiffDisabledBg = Color.FromArgb("#e0e0e0");
        private static readonly Color DiffEnabledFg = Colors.Black;
        private static readonly Color DiffDisabledFg = Colors.Gray;

        public InventoryAdjustment() : this(null)
        {
        }

        public InventoryAdjustment(StockAdjustItem? editItem) : base("strInventoryAdjustment")
        {
            _editItem = editItem;

            Title = IsEditMode
                ? "棚卸調整 - 編集"
                : "棚卸調整 - 新規登録";

            BuildUI();
            InitializeMockData();

            if (IsEditMode)
            {
                LoadEditData();
            }
        }

        private void BuildUI()
        {
            mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star }
                },
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Padding = new Thickness(0)
            };

            var formContainer = new VerticalStackLayout
            {
                Spacing = 8,
                BackgroundColor = Colors.White,
                Padding = new Thickness(12)
            };

            // === 1. 品目 (スキャン可) ★ Entry に変更 ===
            var itemLabel = new Label { Text = "品目(スキャン可)", FontSize = 12, TextColor = Colors.Gray };

            var itemRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                },
                ColumnSpacing = 10
            };

            itemEntry = new Entry
            {
                Placeholder = "品目コードを入力またはスキャン",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,
                FontSize = 13,
                Margin = new Thickness(10, 0),
                VerticalOptions = LayoutOptions.Center
            };
            // ★ 入力変化で IsLotItem 判定 → Lot セクション表示切替
            itemEntry.TextChanged += OnItemTextChanged;

            var itemBorder = CreateInputBorder(itemEntry, Colors.White);

            scanButtonBorder = BuildBarcodeIcon();
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnScanClicked;
            scanButtonBorder.GestureRecognizers.Add(tapGesture);

            itemRow.Add(itemBorder, 0, 0);
            itemRow.Add(scanButtonBorder, 1, 0);

            formContainer.Children.Add(itemLabel);
            formContainer.Children.Add(itemRow);

            // === 2. ロケーション + 現在庫数 ===
            var locStockGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 10 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                Margin = new Thickness(0, 5, 0, 0)
            };

            var locLayout = new VerticalStackLayout { Spacing = 2 };
            locLayout.Children.Add(new Label { Text = "ロケーション", FontSize = 11, TextColor = Colors.Gray });

            locationPicker = new Picker
            {
                Title = "WH1-A-05",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,
                FontSize = 13,
                Margin = new Thickness(10, 0)
            };
            locationPicker.Items.Add("WH1-A-05");
            locationPicker.Items.Add("WH1-A-06");
            locationPicker.Items.Add("WH2-B-01");
            locationPicker.SelectedIndexChanged += async (s, e) => await OnLocationChanged(s, e);

            var locBorder = CreateInputBorder(locationPicker, Colors.White);
            locLayout.Children.Add(locBorder);

            var stockLayout = new VerticalStackLayout { Spacing = 2 };
            stockLayout.Children.Add(new Label { Text = "現在庫数", FontSize = 11, TextColor = Colors.Gray });

            currentStockEntry = new Entry
            {
                Text = _currentStockValue.ToString(),
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.DimGray,
                HeightRequest = 35,
                FontSize = 13,
                IsReadOnly = true,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            var stockBorder = CreateInputBorder(currentStockEntry, Color.FromArgb("#e0e0e0"));
            stockLayout.Children.Add(stockBorder);

            locStockGrid.Add(locLayout, 0, 0);
            locStockGrid.Add(stockLayout, 2, 0);
            formContainer.Children.Add(locStockGrid);

            // === 3. 差異 + 調整後数量 ===
            var diffAdjGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 10 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                Margin = new Thickness(0, 5, 0, 0)
            };

            var diffLayout = new VerticalStackLayout { Spacing = 2 };
            diffLayout.Children.Add(new Label { Text = "差異", FontSize = 11, TextColor = Colors.Gray });

            differenceEntry = new Entry
            {
                Text = "-20",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            differenceEntry.TextChanged += OnDifferenceTextChanged;

            _diffBorder = CreateInputBorder(differenceEntry, DiffEnabledBg);
            diffLayout.Children.Add(_diffBorder);

            var adjLayout = new VerticalStackLayout { Spacing = 2 };
            adjLayout.Children.Add(new Label { Text = "調整後数量", FontSize = 11, TextColor = Colors.Gray });

            adjustedStockEntry = new Entry
            {
                Text = CalculateAdjustedStock().ToString(),
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.DimGray,
                HeightRequest = 35,
                FontSize = 13,
                IsReadOnly = true,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            var adjBorder = CreateInputBorder(adjustedStockEntry, Color.FromArgb("#e0e0e0"));
            adjLayout.Children.Add(adjBorder);

            diffAdjGrid.Add(diffLayout, 0, 0);
            diffAdjGrid.Add(adjLayout, 2, 0);
            formContainer.Children.Add(diffAdjGrid);

            // === 4. 調整理由 ===
            var reasonLabel = new Label { Text = "調整理由", FontSize = 12, TextColor = Colors.Gray, Margin = new Thickness(0, 5, 0, 0) };

            reasonPicker = new Picker
            {
                Title = "破損",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,
                FontSize = 13,
                Margin = new Thickness(10, 0)
            };
            reasonPicker.Items.Add("破損");
            reasonPicker.Items.Add("棚卸差異");
            reasonPicker.Items.Add("その他");

            reasonPicker.SelectedIndexChanged += (s, e) =>
            {
                if (reasonPicker.SelectedIndex >= 0)
                    reasonPicker.Title = reasonPicker.SelectedItem?.ToString();
                else
                    reasonPicker.Title = "調整理由を選択";
            };

            var reasonBorder = CreateInputBorder(reasonPicker, Colors.White);

            formContainer.Children.Add(reasonLabel);
            formContainer.Children.Add(reasonBorder);

            // ==========================================
            // ★ ロット関連セクション
            // ==========================================
            _lotSectionContainer = new VerticalStackLayout
            {
                Spacing = 8,
                IsVisible = false
            };

            _lotSectionContainer.Children.Add(new Label
            {
                Text = "ロット / シリアル (スキャン可)",
                FontSize = 12,
                TextColor = Colors.Gray,
                Margin = new Thickness(0, 5, 0, 0)
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
            _lotEntry = new Entry
            {
                Placeholder = "ロット/シリアルをスキャンまたは入力",
                BackgroundColor = Colors.Transparent,
                HeightRequest = 35,
                FontSize = 13,
                Margin = new Thickness(10, 0),
                VerticalOptions = LayoutOptions.Center
            };
            var lotBorder = CreateInputBorder(_lotEntry, Colors.White);
            lotRow.Add(lotBorder, 0, 0);

            var lotScanBorder = BuildBarcodeIcon();
            lotRow.Add(lotScanBorder, 1, 0);
            _lotSectionContainer.Children.Add(lotRow);

            _lotSectionContainer.Children.Add(new Label
            {
                Text = "移動数量",
                FontSize = 12,
                TextColor = Colors.Gray,
                Margin = new Thickness(0, 5, 0, 0)
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
            _qtyEntry = new Entry
            {
                Placeholder = "数量を入力",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.Transparent,
                HeightRequest = 35,
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            _qtyEntry.TextChanged += OnQtyEntryTextChanged;

            var qtyBorder = CreateInputBorder(_qtyEntry, Colors.White);
            qtyRow.Add(qtyBorder, 0, 0);
            qtyRow.Add(new Label
            {
                Text = "個",
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }, 1, 0);
            _lotSectionContainer.Children.Add(qtyRow);

            _pendingLotTableHost = new ContentView { Content = BuildEditableLotTableForPending() };
            _lotSectionContainer.Children.Add(_pendingLotTableHost);

            var addLotBtn = new Button
            {
                Text = "+ロットを追加",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#245a96"),
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 3,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 5
            };
            addLotBtn.Clicked += OnAddLotButtonClicked;
            _lotSectionContainer.Children.Add(addLotBtn);

            _bottomPendingTableHost = new ContentView
            {
                IsVisible = false,
                Content = BuildBottomPendingTable()
            };
            _lotSectionContainer.Children.Add(_bottomPendingTableHost);

            formContainer.Children.Add(_lotSectionContainer);

            // === 5. 登録ボタン ===
            registerButton = new Button
            {
                Text = "調整を登録",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                HeightRequest = 35,
                CornerRadius = 5,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            registerButton.Clicked += async (s, e) => await OnRegisterClicked(s, e);
            formContainer.Children.Add(registerButton);

            var scrollView = new ScrollView
            {
                Content = formContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            mainGrid.Add(scrollView, 0, 0);

            Content = new Border
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Content = mainGrid
            };
        }

        // ==========================================
        // ★ 品目入力変化時（Entry 版）
        //    入力テキストと _itemMasters の ItemCode を部分一致で照合
        // ==========================================
        private void OnItemTextChanged(object? sender, TextChangedEventArgs e)
        {
            var matched = GetMatchedItemMaster();

            // IsLotItem に応じて Lot セクション表示/非表示
            if (_lotSectionContainer != null)
            {
                _lotSectionContainer.IsVisible = matched?.IsLotItem ?? false;
            }

            // 非ロット品 or 未一致なら未保存ロットをクリア
            if (matched == null || !matched.IsLotItem)
            {
                _pendingLots.Clear();
                RefreshPendingLotTable();
                RefreshBottomPendingTable();
            }

            RecalculateDifference();
        }

        // ★ 入力テキストから ItemMaster を引く（部分一致）
        private ItemMaster? GetMatchedItemMaster()
        {
            var text = itemEntry?.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return null;

            return _itemMasters.FirstOrDefault(m =>
                !string.IsNullOrEmpty(m.ItemCode) &&
                m.ItemCode.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        private Border CreateInputBorder(View content, Color backgroundColor)
        {
            return new Border
            {
                Stroke = Color.FromArgb("#cccccc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                BackgroundColor = backgroundColor,
                Padding = 0,
                Content = content,
                HeightRequest = 35
            };
        }

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
                VerticalOptions = LayoutOptions.Center,
                Content = barsLayout
            };
        }

        #region Logic & Events

        private void InitializeMockData() { }

        private int CalculateAdjustedStock()
        {
            int diff = 0;
            if (int.TryParse(differenceEntry?.Text, out int parsedDiff))
            {
                diff = parsedDiff;
            }
            return _currentStockValue + diff;
        }

        // ==========================================
        // ★ 差異を自動計算
        // ==========================================
        private void RecalculateDifference()
        {
            var matchedItem = GetMatchedItemMaster();
            if (differenceEntry == null) return;

            bool isLotItem = matchedItem != null && matchedItem.IsLotItem;

            if (isLotItem)
            {
                int sum = _pendingLots
                    .Where(p => p.ItemCode == matchedItem!.ItemCode)
                    .Sum(p => p.Qty);

                if (int.TryParse(_qtyEntry?.Text?.Trim(), out int pendingQty) && pendingQty > 0)
                {
                    sum += pendingQty;
                }

                differenceEntry.IsReadOnly = true;
                differenceEntry.TextColor = DiffDisabledFg;
                if (_diffBorder != null)
                {
                    _diffBorder.BackgroundColor = DiffDisabledBg;
                }

                differenceEntry.Text = sum.ToString();
            }
            else
            {
                differenceEntry.IsReadOnly = false;
                differenceEntry.TextColor = DiffEnabledFg;
                if (_diffBorder != null)
                {
                    _diffBorder.BackgroundColor = DiffEnabledBg;
                }
            }

            if (adjustedStockEntry != null)
            {
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();
            }
        }

        private void OnQtyEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            RecalculateDifference();
        }

        private void OnDifferenceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (adjustedStockEntry != null)
            {
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();
            }
        }

        private async Task OnLocationChanged(object sender, EventArgs e)
        {
            if (locationPicker == null) return;

            if (locationPicker.SelectedIndex >= 0)
                locationPicker.Title = locationPicker.SelectedItem?.ToString();
            else
                locationPicker.Title = "ロケーションを選択";

            if (locationPicker.SelectedIndex < 0)
            {
                await Task.CompletedTask;
                return;
            }

            string selectedLocation = locationPicker.SelectedItem?.ToString() ?? "";

            if (selectedLocation.Contains("A-05")) _currentStockValue = 480;
            else if (selectedLocation.Contains("A-06")) _currentStockValue = 120;
            else _currentStockValue = 0;

            if (currentStockEntry != null)
                currentStockEntry.Text = _currentStockValue.ToString();

            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();

            await Task.CompletedTask;
        }

        private void LoadEditData()
        {
            if (_editItem == null) return;

            // 品目：Entry にテキストをセット（TextChanged 経由で IsLotItem 判定される）
            if (itemEntry != null && !string.IsNullOrWhiteSpace(_editItem.ItemCode))
            {
                var matched = _itemMasters.FirstOrDefault(m => m.ItemCode == _editItem.ItemCode);
                if (matched == null)
                {
                    // 仮データに無い場合は追加してから設定（判定できるようにする）
                    var newItem = new ItemMaster { ItemCode = _editItem.ItemCode, IsLotItem = true };
                    _itemMasters.Add(newItem);
                }
                itemEntry.Text = _editItem.ItemCode;
            }

            if (differenceEntry != null)
                differenceEntry.Text = _editItem.DiffQty.ToString();

            if (reasonPicker != null && !string.IsNullOrWhiteSpace(_editItem.AdjustReason))
            {
                if (!reasonPicker.Items.Contains(_editItem.AdjustReason))
                    reasonPicker.Items.Add(_editItem.AdjustReason);

                reasonPicker.SelectedItem = _editItem.AdjustReason;
                reasonPicker.Title = _editItem.AdjustReason;
            }

            if (currentStockEntry != null)
                currentStockEntry.Text = _currentStockValue.ToString();

            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();

            RecalculateDifference();
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            await DisplayAlert("スキャン", "バーコードスキャナーを起動します (実装待ち)", "OK");
        }

        private async Task OnRegisterClicked(object sender, EventArgs e)
        {
            string message = IsEditMode
                ? "在庫調整を更新しました (ダミー)"
                : "在庫調整を登録しました (ダミー)";

            await DisplayAlert("完了", message, "OK");
        }

        // ==================== 「+ロットを追加」 ====================
        private async void OnAddLotButtonClicked(object? sender, EventArgs e)
        {
            var matchedItem = GetMatchedItemMaster();
            if (matchedItem == null || !matchedItem.IsLotItem)
            {
                await DisplayAlert("エラー", "ロット対象の品目を入力してください。", "OK");
                return;
            }

            var lotNo = _lotEntry?.Text?.Trim();
            var qtyText = _qtyEntry?.Text?.Trim();

            if (string.IsNullOrEmpty(lotNo))
            {
                await DisplayAlert("エラー", "ロット/シリアルを入力またはスキャンしてください。", "OK");
                return;
            }
            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "数量を正しく入力してください。", "OK");
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
            RecalculateDifference();
        }

        private Border BuildEditableLotTableForPending()
        {
            return BuildEditableLotTableInternal(_pendingLots, showItemColumn: false);
        }

        private void OnDeletePendingLot(PendingLotItem item)
        {
            _pendingLots.Remove(item);
            RefreshPendingLotTable();
            RefreshBottomPendingTable();
            RecalculateDifference();
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

        #endregion

        // ==========================================
        // データモデル
        // ==========================================
        public class ItemMaster
        {
            public string ItemCode { get; set; } = "";
            public bool IsLotItem { get; set; }
        }

        public class PendingLotItem
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }
    }
}