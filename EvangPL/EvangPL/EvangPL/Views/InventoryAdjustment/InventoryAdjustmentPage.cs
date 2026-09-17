using Android.AdServices.Common;
using EvangPL.Components;
using EvangPL.Utils;
using EvangPL.Views.StockAdjust;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvangPL.Views.InventoryAdjustment
{
    public class InventoryAdjustment : EvangContentVM
    {
        // ==========================================
        // RESTlet 名（Menu.cs の LocalMemory.restlets に登録済みのキー）
        // 後端は1つの POST エントリで、Info.Kbn で検索/保存を切替
        // ==========================================
        private const string RESTLET_STOCK_ADJUST = "SaveAdjust";

        private const string KBN_GETDATA = "getdata";
        private const string KBN_SAVE = "savedata";

        /// <summary>新規モードで位置未選択時に使うデフォルト LocationId</summary>
        private const string DEFAULT_LOCATION_ID = "1";

        // ==========================================
        // 調整理由の固定候補（後端 API が無いため）
        // ==========================================
        private const string REASON_BREAKAGE = "破損";
        private const string REASON_STOCK_DIFF = "棚卸差異";
        private const string REASON_OTHER = "その他";

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
        private Border? _locBorder;
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
        // 検索まわり
        // ==========================================
        private List<ItemMaster>? _currentItems;

        private CancellationTokenSource? _itemSearchCts;

        private bool _suppressItemSearch;

        private bool _suppressLocationChanged;

        private const int ItemSearchDebounceMs = 300;

        private List<LocationItem> _locationList = new List<LocationItem>();

        private Grid? mainGrid;
        private int _currentStockValue = 0;

        private readonly StockAdjustItem? _editItem;
        private bool IsEditMode => _editItem != null;

        private static readonly Color DiffEnabledBg = Colors.White;
        private static readonly Color DiffDisabledBg = Color.FromArgb("#e0e0e0");
        private static readonly Color DiffEnabledFg = Colors.Black;
        private static readonly Color DiffDisabledFg = Colors.Gray;

        private static readonly Color DisabledBg = Color.FromArgb("#e0e0e0");
        private static readonly Color DisabledFg = Colors.Gray;

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

            _ = InitializeAsync();
        }

        // ==========================================
        // 画面初期化
        // ==========================================
        private async Task InitializeAsync()
        {
            try
            {
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

        // ==========================================
        // UI 構築
        // ==========================================
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

            if (IsEditMode)
            {
                itemEntry.IsReadOnly = true;
                itemEntry.TextColor = DisabledFg;
                itemBorder.BackgroundColor = DisabledBg;

                scanButtonBorder.BackgroundColor = DisabledBg;
                scanButtonBorder.Opacity = 0.5;

                itemLabel.Text = "品目";
            }
            else
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnScanClicked;
                scanButtonBorder.GestureRecognizers.Add(tapGesture);
            }

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

            _locBorder = CreateInputBorder(locationPicker, Colors.White);
            locLayout.Children.Add(_locBorder);

            if (IsEditMode)
            {
                locationPicker.IsEnabled = false;
                locationPicker.TextColor = DisabledFg;
                _locBorder.BackgroundColor = DisabledBg;
            }

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

            reasonPicker.Items.Add(REASON_BREAKAGE);
            reasonPicker.Items.Add(REASON_STOCK_DIFF);
            reasonPicker.Items.Add(REASON_OTHER);

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
            // ロット関連セクション
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

            _pendingLotTableHost = new ContentView { IsVisible = false };
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
                IsVisible = false
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
        // 品目入力変化時（デバウンス + 検索）
        // ==========================================
        private async void OnItemTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (IsEditMode) return;
            if (_suppressItemSearch) return;

            await SearchItemByKeywordAsync(e.NewTextValue);
        }

        private async Task SearchItemByKeywordAsync(string? keyword)
        {
            _itemSearchCts?.Cancel();
            _itemSearchCts?.Dispose();
            var cts = new CancellationTokenSource();
            _itemSearchCts = cts;

            var text = keyword?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                ApplyItemDetail(null);
                return;
            }

            try
            {
                await Task.Delay(ItemSearchDebounceMs, cts.Token);

                string? id = _editItem != null && _editItem.Id > 0 ? _editItem.Id.ToString() : null;
                string? lineNo = _editItem != null && _editItem.LineNo > 0 ? _editItem.LineNo.ToString() : null;

                string? locId = GetCurrentLocationId();

                int? diffQty = _editItem?.DiffQty ?? 0;

                System.Diagnostics.Debug.WriteLine(
                    $"[InventoryAdjustment] SearchItemByKeyword locId={locId ?? "null"}, diffQty={diffQty}, keyword={text}");

                var items = await SearchItemMasterAsync(id, lineNo, text, locId, diffQty, "", cts.Token);

                if (cts.IsCancellationRequested) return;
                ApplyItemDetail(items);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested) return;
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 品目検索エラー: {ex}");
                ApplyItemDetail(null);
            }
        }

        // ==========================================
        // ★★★ 品目詳細検索 API ★★★
        // 後端 RESTlet: Info.Kbn = "getdata"
        //   戻り SubData:
        //     [0] SubName='PH_LOCATION' → ロケーション一覧
        //     [1] SubName='PH_DATA' → 品目データ（GetDatas の結果・配列）
        // ==========================================
        protected virtual async Task<List<ItemMaster>?> SearchItemMasterAsync(
            string? id,
            string? lineNo,
            string itemCode,
            string? locationId,
            int? diffQty,
            string? memo,
            CancellationToken ct = default)
        {
            try
            {
                var request = new RequestData<StockAdjustSearchParam, EvangJsonModel>(RESTLET_STOCK_ADJUST);
                request.Info = new StockAdjustSearchParam
                {
                    Kbn = KBN_GETDATA,
                    Id = id,
                    LineNo = lineNo,
                    Keyword = itemCode,
                    LocationId = locationId,
                    DiffQty = diffQty,
                    Memo = memo
                };

                ResponseData<EvangJsonModel, EvangJsonModel>? result = null;
                try
                {
                    result = await this.Post<StockAdjustSearchParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                }
                finally
                {
                    // TODO: HideLoading();
                }

                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine("[InventoryAdjustment] サーバー応答なし");
                    return null;
                }

                if (!result.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] APIエラー: {result.ErrorMessage}");
                    return null;
                }

                if (result.SubData == null || result.SubData.Count == 0)
                    return null;

                List<ItemMaster>? items = null;

                foreach (var sub in result.SubData)
                {
                    if (sub.SubName == "PH_LOCATION")
                    {
                        var locations = BaseUtils.JsonToClass<List<LocationItem>>(sub.SubJson!);
                        if (locations != null && locations.Count > 0)
                        {
                            bool needFillPicker = _locationList.Count == 0
                                                  || locationPicker == null
                                                  || locationPicker.Items.Count == 0;

                            _locationList = locations;

                            if (needFillPicker)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    FillLocationPickerWithSuppress();
                                });
                            }
                        }
                    }
                    else if (sub.SubName == "PH_DATA")
                    {
                        // 品目データ → 配列をそのまま受け取る（マージしない）
                        items = BaseUtils.JsonToClass<List<ItemMaster>>(sub.SubJson!);

                        // ★ 修正：items が空でも必ずクリア（旧データの残留を防ぐ）
                        _pendingLots.Clear();

                        if (items != null && items.Count > 0)
                        {
                            var itemCodeNow = itemEntry?.Text?.Trim() ?? string.Empty;
                            foreach (var it in items)
                            {
                                if (string.IsNullOrWhiteSpace(it.LotNo))
                                    continue;
                                _pendingLots.Add(new PendingLotItem
                                {
                                    ItemCode = itemCodeNow,
                                    LotNo    = it.LotNo,
                                    Qty      = it.LotQuantity,
                                    IsNew    = false
                                });
                            }
                        }

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            RefreshPendingLotTable();
                            RefreshBottomPendingTable();
                            RecalculateDifference();
                        });
                    }
                }

                if (items == null || items.Count == 0)
                    return null;

                System.Diagnostics.Debug.WriteLine(
                    $"[InventoryAdjustment] 品目詳細取得成功: {itemCode} / 行数={items.Count}");

                return items;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] SearchItemMasterAsync 例外: {ex.Message}");
                return null;
            }
        }

        // ==========================================
        // 検索結果を画面へ反映
        // ==========================================
        private void ApplyItemDetail(List<ItemMaster>? items)
        {
            _currentItems = items;

            var first = items?.FirstOrDefault();
            bool isLotItem = first?.IsLotItem ?? false;

            ApplyReasonFromMemo(first?.Memo);

            int stock = 0;
            if (first != null)
            {
                int.TryParse(first.HandQuantity?.ToString(), out stock);
            }

            if (_lotSectionContainer != null)
            {
                _lotSectionContainer.IsVisible = isLotItem;
            }

            if (items == null || items.Count == 0 || !isLotItem)
            {
                _pendingLots.Clear();
                RefreshPendingLotTable();
                RefreshBottomPendingTable();
            }
            else
            {
                RefreshPendingLotTable();
                RefreshBottomPendingTable();
            }

            _currentStockValue = stock;
            if (currentStockEntry != null)
                currentStockEntry.Text = stock.ToString();

            RecalculateDifference();
        }

        // ==========================================
        // Memo → 調整理由 Picker 反映
        // ==========================================
        private void ApplyReasonFromMemo(string? memo)
        {
            if (reasonPicker == null) return;

            var trimmed = memo?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(trimmed))
            {
                reasonPicker.SelectedItem = null;
                reasonPicker.SelectedIndex = -1;
                reasonPicker.Title = "調整理由を選択";
                return;
            }

            string target;
            if (trimmed == REASON_BREAKAGE || trimmed == REASON_STOCK_DIFF)
            {
                target = trimmed;
            }
            else
            {
                target = REASON_OTHER;
            }

            if (!reasonPicker.Items.Contains(target))
                reasonPicker.Items.Add(target);

            reasonPicker.SelectedItem = target;
            reasonPicker.Title = target;
        }

        // ==========================================
        // ロケーション名 → LocationId 逆引き
        // ==========================================
        private string? GetCurrentLocationId()
        {
            var selectedName = locationPicker?.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedName))
            {
                if (!IsEditMode)
                    return DEFAULT_LOCATION_ID;

                return null;
            }

            var matched = _locationList.FirstOrDefault(l => l.Name == selectedName);
            return matched?.Id.ToString();
        }

        // ==========================================
        // ロケーション Picker を埋める（抑制付き）
        // ==========================================
        private void FillLocationPickerWithSuppress()
        {
            if (locationPicker == null) return;
            if (_locationList.Count == 0) return;

            _suppressLocationChanged = true;

            try
            {
                locationPicker.Items.Clear();
                foreach (var loc in _locationList)
                {
                    if (!string.IsNullOrEmpty(loc.Name))
                        locationPicker.Items.Add(loc.Name);
                }

                if (locationPicker.Items.Count == 0)
                {
                    locationPicker.SelectedIndex = -1;
                    return;
                }

                if (IsEditMode && _editItem != null && _editItem.LocationId > 0)
                {
                    string targetId = _editItem.LocationId.ToString();
                    bool found = false;

                    for (int i = 0; i < _locationList.Count; i++)
                    {
                        if (_locationList[i].Id.ToString() == targetId)
                        {
                            locationPicker.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        locationPicker.SelectedIndex = 0;
                }
                else
                {
                    bool found = false;
                    for (int i = 0; i < _locationList.Count; i++)
                    {
                        if (_locationList[i].Id.ToString() == DEFAULT_LOCATION_ID)
                        {
                            locationPicker.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        locationPicker.SelectedIndex = 0;
                }

                if (locationPicker.SelectedIndex >= 0)
                    locationPicker.Title = locationPicker.SelectedItem?.ToString();
            }
            finally
            {
                _suppressLocationChanged = false;
            }
        }

        // ==========================================
        // 編集モード：初期データロード（4項目検索）
        // ==========================================
        private async Task LoadEditDataAsync()
        {
            if (_editItem == null) return;

            if (itemEntry != null && !string.IsNullOrWhiteSpace(_editItem.ItemCode))
            {
                _suppressItemSearch = true;
                itemEntry.Text = _editItem.ItemCode;
                _suppressItemSearch = false;

                List<ItemMaster>? detail = null;
                try
                {
                    string? id = _editItem.Id > 0 ? _editItem.Id.ToString() : null;
                    string? lineNo = _editItem.LineNo > 0 ? _editItem.LineNo.ToString() : null;
                    string? locId = _editItem.LocationId > 0 ? _editItem.LocationId.ToString() : null;

                    detail = await SearchItemMasterAsync(id, lineNo, _editItem.ItemCode, locId, _editItem.DiffQty, "");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 編集データ読込エラー: {ex}");
                }

                ApplyItemDetail(detail);
            }

            if (differenceEntry != null && !(_currentItems?.FirstOrDefault()?.IsLotItem ?? false))
            {
                differenceEntry.Text = _editItem.DiffQty.ToString();
            }

            if (currentStockEntry != null)
                currentStockEntry.Text = _currentStockValue.ToString();

            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();

            RecalculateDifference();

            if (!string.IsNullOrWhiteSpace(_editItem.AdjustNo))
            {
                Title = $"棚卸調整 - 編集 ({_editItem.AdjustNo})";
            }
        }

        // ==========================================
        // ロケーション切替時：新しい LocationId で再検索
        // ==========================================
        private async Task OnLocationChanged(object sender, EventArgs e)
        {
            if (locationPicker == null) return;

            if (_suppressLocationChanged) return;

            if (IsEditMode) return;

            if (locationPicker.SelectedIndex >= 0)
                locationPicker.Title = locationPicker.SelectedItem?.ToString();
            else
                locationPicker.Title = "ロケーションを選択";

            if (locationPicker.SelectedIndex < 0)
            {
                await Task.CompletedTask;
                return;
            }

            var currentItemCode = itemEntry?.Text?.Trim();
            if (!string.IsNullOrEmpty(currentItemCode))
            {
                try
                {
                    string? id = _editItem != null && _editItem.Id > 0 ? _editItem.Id.ToString() : null;
                    string? lineNo = _editItem != null && _editItem.LineNo > 0 ? _editItem.LineNo.ToString() : null;
                    string? newLocId = GetCurrentLocationId();

                    int? diffQty = _editItem?.DiffQty ?? 0;

                    System.Diagnostics.Debug.WriteLine(
                        $"[InventoryAdjustment] OnLocationChanged newLocId={newLocId ?? "null"}, itemCode={currentItemCode}");

                    var detail = await SearchItemMasterAsync(id, lineNo, currentItemCode, newLocId, diffQty, "");

                    ApplyItemDetail(detail);
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
        // UI ヘルパー
        // ==========================================
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

        private void RecalculateDifference()
        {
            if (differenceEntry == null) return;

            bool isLotItem = _currentItems?.FirstOrDefault()?.IsLotItem ?? false;

            if (isLotItem)
            {
                int sum = _pendingLots.Sum(p => p.Qty ?? 0);

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

        private async void OnScanClicked(object sender, EventArgs e)
        {
            if (IsEditMode) return;

            await DisplayAlert("スキャン", "バーコードスキャナーを起動します (実装待ち)", "OK");
        }

        // ==========================================
        // 登録／更新（本実装）
        // ==========================================
        private async Task OnRegisterClicked(object sender, EventArgs e)
        {
            if (_currentItems == null || _currentItems.Count == 0)
            {
                await DisplayAlert("エラー", "品目を入力してください。", "OK");
                return;
            }

            var first = _currentItems.FirstOrDefault();
            bool isLotItem = first?.IsLotItem ?? false;

            int diffQty = 0;

            if (isLotItem)
            {
                if (_pendingLots.Count == 0)
                {
                    await DisplayAlert("エラー", "ロットを1件以上追加してください。", "OK");
                    return;
                }
                diffQty = _pendingLots.Sum(p => p.Qty ?? 0);
            }
            else
            {
                if (!int.TryParse(differenceEntry?.Text?.Trim(), out diffQty))
                {
                    await DisplayAlert("エラー", "差異を正しく入力してください。", "OK");
                    return;
                }
                if (diffQty == 0)
                {
                    await DisplayAlert("エラー", "差異が0のため保存できません。", "OK");
                    return;
                }
            }

            var reason = reasonPicker?.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(reason))
            {
                await DisplayAlert("エラー", "調整理由を選択してください。", "OK");
                return;
            }

            var request = new RequestData<StockAdjustSaveParam, EvangJsonModel>(RESTLET_STOCK_ADJUST);

            request.Info = new StockAdjustSaveParam
            {
                id         = _editItem != null && _editItem.Id > 0 ? _editItem.Id : null,
                Kbn        = KBN_SAVE,
                LineNo     = _editItem != null && _editItem.LineNo > 0 ? _editItem.LineNo.ToString() : null,
                Keyword    = itemEntry?.Text?.Trim(),
                LocationId = GetCurrentLocationId(),
                DiffQty    = diffQty,
                Memo       = reason,
                Lots       = isLotItem
                    ? _pendingLots.Select(p => new LotSaveItem
                    {
                        LotNo = p.LotNo,
                        Qty   = p.Qty ?? 0
                    }).ToList()
                    : null
            };

            if (registerButton != null) registerButton.IsEnabled = false;

            try
            {
                var successNo = "";
                var result = await this.Post<StockAdjustSaveParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);

                if (result == null || !result.Success)
                {
                    var err = result?.ErrorMessage ?? "サーバー応答なし";
                    System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 保存失敗: {err}");
                    await DisplayAlert("エラー", $"保存に失敗しました。\n{err}", "OK");
                    return;
                }
                foreach (var item in result.SubData)
                {
                    switch (item.SubName)
                    {
                        case "PH_TRANID":
                            if (item == null || item.SubJson == null)
                                return;
                            successNo = item.SubJson;
                            break;
                    }
                }
                
                await DisplayAlert("完了",
                    IsEditMode ? $"\n{successNo}在庫調整を更新しました。" : $"\n{successNo}在庫調整を登録しました。",
                    "OK");
                // ★ 保存成功後、一覧画面へ戻る
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 保存例外: {ex}");
                await DisplayAlert("エラー", "保存中にエラーが発生しました。", "OK");
            }
            finally
            {
                if (registerButton != null) registerButton.IsEnabled = true;
            }
        }

        // ==================== 「+ロットを追加」 ====================
        private async void OnAddLotButtonClicked(object? sender, EventArgs e)
        {
            bool isLotItem = _currentItems?.FirstOrDefault()?.IsLotItem ?? false;
            var itemCode = itemEntry?.Text?.Trim() ?? "";

            if (!isLotItem || string.IsNullOrEmpty(itemCode))
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
                ItemCode = itemCode,
                LotNo    = lotNo,
                Qty      = qty,
                IsNew    = true
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
            if (_pendingLotTableHost == null) return;

            if (_pendingLots.Count == 0)
            {
                _pendingLotTableHost.IsVisible = false;
                _pendingLotTableHost.Content = null;
            }
            else
            {
                _pendingLotTableHost.Content = BuildEditableLotTableForPending();
                _pendingLotTableHost.IsVisible = true;
            }
        }

        private void RefreshBottomPendingTable()
        {
            if (_bottomPendingTableHost == null) return;

            bool hasNew = _pendingLots.Any(p => p.IsNew);

            if (!hasNew)
            {
                _bottomPendingTableHost.IsVisible = false;
                _bottomPendingTableHost.Content = null;
            }
            else
            {
                _bottomPendingTableHost.Content = BuildBottomPendingTable();
                _bottomPendingTableHost.IsVisible = true;
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
            var newLots = _pendingLots.Where(p => p.IsNew).ToList();

            if (newLots.Count == 0)
            {
                return new ContentView { IsVisible = false };
            }

            var container = new VerticalStackLayout { Spacing = 4 };
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({newLots.Count}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });
            container.Children.Add(BuildEditableLotTableInternal(newLots, showItemColumn: true));
            return container;
        }

        #endregion

        // ==========================================
        // データモデル（後端 JSON に完全一致）
        // ==========================================
        public class ItemMaster
        {
            [JsonPropertyName("HandQuantity")]
            public int? HandQuantity { get; set; }

            [JsonPropertyName("IsLotItem")]
            public bool IsLotItem { get; set; }

            [JsonPropertyName("LotNo")]
            public string LotNo { get; set; } = "";

            [JsonPropertyName("LotQuantity")]
            public int? LotQuantity { get; set; }

            [JsonPropertyName("Memo")]
            public string? Memo { get; set; }
        }

        public class PendingLotItem
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int? Qty { get; set; }

            public bool IsNew { get; set; } = false;
        }

        public class StockAdjustSearchParam : EvangJsonModel
        {
            public string Kbn { get; set; } = "getdata";
            public string? Id { get; set; }
            public string? LineNo { get; set; }
            public string? Keyword { get; set; }
            public string? LocationId { get; set; }
            public int? DiffQty { get; set; }
            public string? Memo { get; set; }
        }

        public class StockAdjustSaveParam : EvangJsonModel
        {
            [JsonPropertyName("Id")]
            public int? id { get; set; }
            public string Kbn { get; set; } = "savedata";
            public string? LineNo { get; set; }
            public string? Keyword { get; set; }
            public string? LocationId { get; set; }
            public int? DiffQty { get; set; }
            public string? Memo { get; set; }
            public List<LotSaveItem>? Lots { get; set; }
        }

        public class LotSaveItem
        {
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }

        public class LocationItem
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";
        }
    }
}