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
        private Entry? itemEntry;
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

        // ==========================================
        // ★ 品目検索まわり
        // ==========================================
        /// <summary>現在選択中の品目マスタ（検索APIから取得した詳細データ）</summary>
        private ItemMaster? _currentItem;

        /// <summary>入力デバウンス／連続検索の競合防止用</summary>
        private CancellationTokenSource? _itemSearchCts;

        /// <summary>編集モードの初期ロード時など、TextChanged による自動検索を抑止するフラグ</summary>
        private bool _suppressItemSearch;

        /// <summary>入力が落ち着くまで待つ時間(ms)</summary>
        private const int ItemSearchDebounceMs = 300;

        private Grid? mainGrid;
        private int _currentStockValue = 0;

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

            // ★ 画面初期化（編集時はここで品目詳細を一度検索する）
            _ = InitializeAsync();
        }

        /// <summary>
        /// 画面初期化処理。
        /// 編集モードの場合は _editItem.ItemCode で一度だけ品目詳細を検索する。
        /// 新規モードの場合は何も検索しない（品目入力時に検索される）。
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // ロケーション候補の初期化（必要ならAPI化）
                await InitializeLocationsAsync();

                // 編集モード：既存データをロード
                if (IsEditMode)
                {
                    await LoadEditDataAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 初期化エラー: {ex}");
            }
        }

        private async Task InitializeLocationsAsync()
        {
            if (locationPicker == null) return;

            // TODO: ロケーション候補をAPIから取得する場合はここを差し替える
            // 現在は固定値
            locationPicker.Items.Clear();
            locationPicker.Items.Add("WH1-A-05");
            locationPicker.Items.Add("WH1-A-06");
            locationPicker.Items.Add("WH2-B-01");

            if (locationPicker.Items.Count > 0)
                locationPicker.SelectedIndex = 0;

            await Task.CompletedTask;
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

            // === 1. 品目 (スキャン可) ===
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
                Title = "ロケーションを選択",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,
                FontSize = 13,
                Margin = new Thickness(10, 0)
            };
            locationPicker.SelectedIndexChanged += async (s, e) => await OnLocationChanged(s, e);

            var locBorder = CreateInputBorder(locationPicker, Colors.White);
            locLayout.Children.Add(locBorder);

            var stockLayout = new VerticalStackLayout { Spacing = 2 };
            stockLayout.Children.Add(new Label { Text = "現在庫数", FontSize = 11, TextColor = Colors.Gray });

            currentStockEntry = new Entry
            {
                Text = "0",
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
                Text = "0",
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
                Text = "0",
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
                Title = "調整理由を選択",
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
        //    デバウンスして品目詳細APIを呼び出す
        // ==========================================
        private async void OnItemTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_suppressItemSearch) return;

            await SearchItemByKeywordAsync(e.NewTextValue);
        }

        /// <summary>
        /// 入力キーワードで品目詳細を検索し、結果をUIへ反映する。
        /// 連続入力時は古いリクエストをキャンセルする。
        /// </summary>
        private async Task SearchItemByKeywordAsync(string? keyword)
        {
            // 直前の検索をキャンセル
            _itemSearchCts?.Cancel();
            _itemSearchCts?.Dispose();
            var cts = new CancellationTokenSource();
            _itemSearchCts = cts;

            var text = keyword?.Trim();

            // 空入力 → 選択解除
            if (string.IsNullOrEmpty(text))
            {
                ApplyItemDetail(null);
                return;
            }

            try
            {
                // デバウンス（入力が落ち着くまで待つ）
                await Task.Delay(ItemSearchDebounceMs, cts.Token);

                // 現在選択中のロケーションを取得
                string? location = locationPicker?.SelectedItem?.ToString();

                // ★ 品目詳細検索API呼び出し（品目コード + ロケーション）
                var detail = await SearchItemMasterAsync(text, location, cts.Token);

                if (cts.IsCancellationRequested) return;

                ApplyItemDetail(detail);
            }
            catch (OperationCanceledException)
            {
                // 後続の入力に追い越された場合は何もしない
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested) return;

                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 品目検索エラー: {ex}");
                ApplyItemDetail(null);
            }
        }

        /// <summary>
        /// 検索結果（または null）を画面へ反映する。
        /// </summary>
        private void ApplyItemDetail(ItemMaster? item)
        {
            _currentItem = item;

            // IsLotItem に応じて Lot セクション表示/非表示
            if (_lotSectionContainer != null)
            {
                _lotSectionContainer.IsVisible = item?.IsLotItem ?? false;
            }

            // 非ロット品 or 未一致なら未保存ロットをクリア
            if (item == null || !item.IsLotItem)
            {
                _pendingLots.Clear();
                RefreshPendingLotTable();
                RefreshBottomPendingTable();
            }

            // 詳細データに現在庫が含まれていれば反映
            if (item?.CurrentStock is int stock && stock >= 0)
            {
                _currentStockValue = stock;
                if (currentStockEntry != null)
                    currentStockEntry.Text = stock.ToString();
            }
            else
            {
                _currentStockValue = 0;
                if (currentStockEntry != null)
                    currentStockEntry.Text = "0";
            }

            RecalculateDifference();
        }

        // ==========================================
        // ★★★ 品目詳細検索 API（ここを実APIに差し替える） ★★★
        // ==========================================
        /// <summary>
        /// 品目コード／品目名の一部から品目詳細を検索する。
        /// ロケーションを指定すると、そのロケーションの在庫数も返す想定。
        /// <para>
        /// 【実装メモ】<br/>
        /// 現在は未実装（常に null = 該当なし）を返しています。<br/>
        /// 実際のAPI／DBアクセスに置き換えてください。<br/>
        /// 例：<br/>
        /// <code>
        /// var res = await _itemApi.SearchAsync(keyword, location, ct);
        /// if (res == null) return null;
        /// return new ItemMaster
        /// {
        ///     ItemCode     = res.ItemCode,
        ///     ItemName     = res.ItemName,
        ///     IsLotItem    = res.IsLotItem,
        ///     CurrentStock = res.CurrentStock,
        /// };
        /// </code>
        /// </para>
        /// </summary>
        /// <param name="itemCode">入力された品目コード／品目名（部分一致想定）</param>
        /// <param name="location">現在選択中のロケーション（null の場合あり）</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>該当する品目詳細。該当なしの場合は null。</returns>
        protected virtual async Task<ItemMaster?> SearchItemMasterAsync(
            string itemCode,
            string? location,
            CancellationToken ct = default)
        {
            // TODO: ★ここに実API呼び出しを実装する★
            // 例：
            // var result = await _itemApi.SearchAsync(itemCode, location, ct);
            // return new ItemMaster
            // {
            //     ItemCode = result.ItemCode,
            //     ItemName = result.ItemName,
            //     IsLotItem = result.IsLotItem,
            //     CurrentStock = result.CurrentStock,
            // };

            await Task.CompletedTask;
            return null;
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
            if (differenceEntry == null) return;

            bool isLotItem = _currentItem?.IsLotItem ?? false;

            if (isLotItem)
            {
                int sum = _pendingLots
                    .Where(p => p.ItemCode == _currentItem!.ItemCode)
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

            // 品目が既に選択されている場合、ロケーション変更で再検索して在庫を更新
            if (_currentItem != null && !string.IsNullOrEmpty(_currentItem.ItemCode))
            {
                var location = locationPicker.SelectedItem?.ToString();
                try
                {
                    var detail = await SearchItemMasterAsync(_currentItem.ItemCode, location);
                    if (detail != null)
                    {
                        ApplyItemDetail(detail);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] ロケーション変更時の再検索エラー: {ex}");
                }
            }

            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();

            await Task.CompletedTask;
        }

        // ==========================================
        // ★ 編集モード：初期データロード
        //    品目は API から詳細を取得する
        // ==========================================
        private async Task LoadEditDataAsync()
        {
            if (_editItem == null) return;

            // 品目：Entry にテキストをセット → API で詳細検索
            if (itemEntry != null && !string.IsNullOrWhiteSpace(_editItem.ItemCode))
            {
                _suppressItemSearch = true;
                itemEntry.Text = _editItem.ItemCode;
                _suppressItemSearch = false;

                ItemMaster? detail = null;
                try
                {
                    string? location = locationPicker?.SelectedItem?.ToString();
                    detail = await SearchItemMasterAsync(_editItem.ItemCode, location);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 編集データ読込エラー: {ex}");
                }

                ApplyItemDetail(detail);
            }

            // 差異・理由を編集データで上書き（ロット品の場合、差異はロットから再計算されるため注意）
            if (differenceEntry != null && !(_currentItem?.IsLotItem ?? false))
            {
                differenceEntry.Text = _editItem.DiffQty.ToString();
            }

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

            // TODO: スキャン結果を itemEntry.Text にセットすると
            //       TextChanged → SearchItemByKeywordAsync が自動で走ります。
            // 例：
            // var scanned = await _scanner.ScanAsync();
            // if (!string.IsNullOrWhiteSpace(scanned)) itemEntry!.Text = scanned;
        }

        private async Task OnRegisterClicked(object sender, EventArgs e)
        {
            if (_currentItem == null)
            {
                await DisplayAlert("エラー", "品目を入力してください。", "OK");
                return;
            }

            string message = IsEditMode
                ? "在庫調整を更新しました (ダミー)"
                : "在庫調整を登録しました (ダミー)";

            await DisplayAlert("完了", message, "OK");
        }

        // ==================== 「+ロットを追加」 ====================
        private async void OnAddLotButtonClicked(object? sender, EventArgs e)
        {
            var matchedItem = _currentItem;
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
            public string ItemName { get; set; } = "";
            public bool IsLotItem { get; set; }
            public int? CurrentStock { get; set; }
        }

        public class PendingLotItem
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }
    }
}