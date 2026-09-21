using CommunityToolkit.Mvvm.Messaging;
using EvangPL.Views.StockAdjust;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.Dialog;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Message;
using Microsoft.Maui.Controls.Shapes;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace EvangPL.Views.InventoryAdjustment
{
    public class InventoryAdjustment : EvangContentVM
    {
        private const string RESTLET_STOCK_ADJUST = "SaveAdjust";
        private const string KBN_GETDATA = "getdata";
        private const string KBN_SAVE = "savedata";
        private const string DEFAULT_LOCATION_ID = "1";

        private const string REASON_BREAKAGE = "破損";
        private const string REASON_STOCK_DIFF = "棚卸差異";
        private const string REASON_OTHER = "その他";

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

        private readonly List<StockAdjustItem> _adjustItems = new List<StockAdjustItem>();
        private StockAdjustItem? _selectedItem = null;
        private bool _isNewDetail = false;
        private bool _isCurrentDetailSaved = false;

        private bool _hasUnsavedLotChanges = false;

        private ContentView? _detailSelectionTableHost;
        private Label? _headerInfoLabel;

        private readonly Dictionary<int, List<PendingLotItem>> _lotsPerItem = new Dictionary<int, List<PendingLotItem>>();
        private readonly List<PendingLotItem> _savedNewLots = new List<PendingLotItem>();
        private int _nextNewLineNo = 9000;

        private Entry? itemEntry;
        private Border? _itemBorder;
        private Border? scanButtonBorder;
        private Picker? locationPicker;
        private Border? _locBorder;
        private Entry? currentStockEntry;
        private Entry? differenceEntry;
        private Border? _diffBorder;
        private Entry? adjustedStockEntry;
        private Picker? reasonPicker;
        private Button? registerButton;
        private Button? saveDetailButton;

        private VerticalStackLayout? _lotSectionContainer;
        private Entry? _lotEntry;
        private Entry? _qtyEntry;

        private ContentView? _pendingLotTableHost;
        private ContentView? _allLotsSummaryHost;

        private List<ItemMaster>? _currentItems;
        private CancellationTokenSource? _itemSearchCts;
        private bool _suppressItemSearch;
        private bool _suppressLocationChanged;
        private const int ItemSearchDebounceMs = 300;
        private List<LocationItem> _locationList = new List<LocationItem>();
        private Grid? mainGrid;
        private int _currentStockValue = 0;

        private bool IsEditMode => _adjustItems.Count > 0 && _adjustItems.Any(i => i.Id > 0);

        private static readonly Color DiffEnabledBg = Colors.White;
        private static readonly Color DiffDisabledBg = Color.FromArgb("#e0e0e0");
        private static readonly Color DiffEnabledFg = Colors.Black;
        private static readonly Color DiffDisabledFg = Colors.Gray;
        private static readonly Color DisabledBg = Color.FromArgb("#e0e0e0");
        private static readonly Color DisabledFg = Colors.Gray;
        private static readonly Color SelectedRowColor = Color.FromArgb("#d7e8fa");

        private List<PendingLotItem> GetLotsForSelectedItem()
        {
            if (_selectedItem == null) return new List<PendingLotItem>();
            if (!_lotsPerItem.ContainsKey(_selectedItem.LineNo))
                _lotsPerItem[_selectedItem.LineNo] = new List<PendingLotItem>();
            return _lotsPerItem[_selectedItem.LineNo];
        }

        public InventoryAdjustment() : this((List<StockAdjustItem>?)null) { }

        public InventoryAdjustment(StockAdjustItem? singleItem) : this(
            singleItem != null ? new List<StockAdjustItem> { singleItem } : null)
        { }

        public InventoryAdjustment(List<StockAdjustItem>? items) : base("strInventoryAdjustment")
        {
            if (items != null && items.Count > 0)
            {
                _adjustItems = items;
            }
            else
            {
                _nextNewLineNo++;
                _adjustItems.Add(new StockAdjustItem
                {
                    Id = 0,
                    LineNo = _nextNewLineNo,
                    AdjustNo = "",
                    RegisterDate = "",
                    ItemCode = "",
                    LocationId = 0,
                    LocationName = "",
                    DiffQty = 0,
                    AdjustReason = ""
                });
            }

            Title = IsEditMode
                ? $"棚卸調整 - 編集 ({_adjustItems.FirstOrDefault()?.AdjustNo})"
                : "棚卸調整 - 新規登録";

            BuildUI();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                if (_adjustItems.Count > 0)
                    OnDetailRowSelected(_adjustItems[0]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 初期化エラー: {ex}");
            }
        }

        private Border BuildDetailSelectionTable()
        {
            var headers = new List<string> { "品目コード", "場所", "差異", "理由", "" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(2, GridUnitType.Star),
                new GridLength(2, GridUnitType.Star),
                new GridLength(1, GridUnitType.Star),
                new GridLength(2, GridUnitType.Star),
                new GridLength(35, GridUnitType.Absolute)
            };

            var tableGrid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            foreach (var width in columnWidths)
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });

            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < headers.Count; c++)
            {
                tableGrid.Add(new Label
                {
                    Text = headers[c],
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    BackgroundColor = Color.FromArgb("#dbe2ec"),
                    Padding = new Thickness(4, 2)
                }, c, 0);
            }

            for (int r = 0; r < _adjustItems.Count; r++)
            {
                int sepRow = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, sepRow);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRow = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var item = _adjustItems[r];
                bool isSelected = _selectedItem != null && _selectedItem.LineNo == item.LineNo;
                var rowBg = isSelected ? SelectedRowColor : Colors.White;

                var lblCode = new Label { Text = item.ItemCode ?? "", FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg, VerticalOptions = LayoutOptions.Fill };
                var lblLoc = new Label { Text = item.LocationName ?? "", FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg, VerticalOptions = LayoutOptions.Fill };
                var lblDiff = new Label
                {
                    Text = $"{(item.DiffQty > 0 ? "+" : "")}{item.DiffQty}",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    BackgroundColor = rowBg,
                    TextColor = item.DiffQty >= 0 ? Color.FromArgb("#2e7d32") : Color.FromArgb("#c62828"),
                    VerticalOptions = LayoutOptions.Fill
                };
                var lblReason = new Label { Text = item.AdjustReason ?? "", FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg, VerticalOptions = LayoutOptions.Fill };

                tableGrid.Add(lblCode, 0, dataRow);
                tableGrid.Add(lblLoc, 1, dataRow);
                tableGrid.Add(lblDiff, 2, dataRow);
                tableGrid.Add(lblReason, 3, dataRow);

                var deleteLabel = new Label
                {
                    Text = "❌",
                    FontSize = 11,
                    Padding = new Thickness(0),
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    BackgroundColor = rowBg
                };
                var capturedForDelete = item;
                var deleteTap = new TapGestureRecognizer();
                deleteTap.Tapped += async (s, e) => await OnDeleteDetailRow(capturedForDelete);
                deleteLabel.GestureRecognizers.Add(deleteTap);
                tableGrid.Add(deleteLabel, 4, dataRow);

                var captured = item;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) => OnDetailRowSelected(captured);
                lblCode.GestureRecognizers.Add(tap);
                lblLoc.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnDetailRowSelected(captured)) });
                lblDiff.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnDetailRowSelected(captured)) });
                lblReason.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnDetailRowSelected(captured)) });
            }

            var border = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new Rectangle(),
                Background = Colors.White,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(0)
            };
            border.Content = tableGrid;
            return border;
        }

        private async Task OnDeleteDetailRow(StockAdjustItem item)
        {
            bool confirm = await DisplayAlert("確認", $"明細「{item.ItemCode}」を削除しますか？", "はい", "いいえ");
            if (!confirm) return;

            _adjustItems.Remove(item);
            if (_lotsPerItem.ContainsKey(item.LineNo))
                _lotsPerItem.Remove(item.LineNo);

            _savedNewLots.RemoveAll(l => l.LineNo == item.LineNo);

            if (_adjustItems.Count == 0)
            {
                _selectedItem = null;
                _isNewDetail = false;
                _isCurrentDetailSaved = false;
                _hasUnsavedLotChanges = false;
                if (_detailSelectionTableHost != null)
                    _detailSelectionTableHost.Content = BuildDetailSelectionTable();
                ClearForm();
                RefreshAllLotsSummaryTable();
                return;
            }

            if (_selectedItem != null && _selectedItem.LineNo == item.LineNo)
                OnDetailRowSelected(_adjustItems[0]);
            else
            {
                if (_detailSelectionTableHost != null)
                    _detailSelectionTableHost.Content = BuildDetailSelectionTable();
                RefreshAllLotsSummaryTable();
            }
        }

        private void ClearForm()
        {
            if (itemEntry != null) itemEntry.Text = "";
            if (currentStockEntry != null) currentStockEntry.Text = "0";
            if (differenceEntry != null) differenceEntry.Text = "0";
            if (adjustedStockEntry != null) adjustedStockEntry.Text = "0";
            if (reasonPicker != null)
            {
                reasonPicker.SelectedIndex = -1;
                reasonPicker.Title = "調整理由を選択";
            }
            _currentItems = null;
            _currentStockValue = 0;
            UpdateFormEditability();
        }

        private void RemoveUnsavedNewDetailIfNeeded()
        {
            if (_selectedItem != null && _selectedItem.Id == 0 && !_isCurrentDetailSaved)
            {
                _adjustItems.Remove(_selectedItem);
                if (_lotsPerItem.ContainsKey(_selectedItem.LineNo))
                    _lotsPerItem.Remove(_selectedItem.LineNo);

                if (_detailSelectionTableHost != null)
                    _detailSelectionTableHost.Content = BuildDetailSelectionTable();
            }
        }

        private void RevertUnsavedLotChangesIfNeeded()
        {
            if (_selectedItem == null) return;
            if (!_hasUnsavedLotChanges) return;
            if (!_isCurrentDetailSaved) return; 

            var lineNo = _selectedItem.LineNo;

            
            var savedLots = _savedNewLots
                .Where(l => l.LineNo == lineNo)
                .Select(l => new PendingLotItem
                {
                    ItemCode = l.ItemCode,
                    LotNo = l.LotNo,
                    Qty = l.Qty,
                    IsNew = l.IsNew,
                    LineNo = l.LineNo
                })
                .ToList();

            _lotsPerItem[lineNo] = savedLots;
            _hasUnsavedLotChanges = false;
        }

        private async void OnDetailRowSelected(StockAdjustItem item)
        {
            if (_selectedItem != null && _selectedItem.LineNo != item.LineNo)
            {
                
                RevertUnsavedLotChangesIfNeeded();
                RemoveUnsavedNewDetailIfNeeded();
            }

            _selectedItem = item;
            _isNewDetail = (item.Id == 0);
            _isCurrentDetailSaved = !_isNewDetail || !string.IsNullOrWhiteSpace(item.ItemCode);
            _hasUnsavedLotChanges = false; 

            if (_detailSelectionTableHost != null)
                _detailSelectionTableHost.Content = BuildDetailSelectionTable();

            await LoadSelectedItemToFormAsync(item);
        }

        private async Task LoadSelectedItemToFormAsync(StockAdjustItem item)
        {
            UpdateFormEditability();

            if (string.IsNullOrWhiteSpace(item.ItemCode))
            {
                if (itemEntry != null) itemEntry.Text = "";
                if (currentStockEntry != null) currentStockEntry.Text = "0";
                if (differenceEntry != null) differenceEntry.Text = "0";
                if (adjustedStockEntry != null) adjustedStockEntry.Text = "0";

                ApplyItemDetail(null);
                ApplyReasonFromMemo(null);

                _suppressLocationChanged = true;
                if (locationPicker != null)
                {
                    if (locationPicker.Items.Count > 0)
                    {
                        locationPicker.SelectedIndex = 0;
                        locationPicker.Title = locationPicker.Items[0];
                    }
                    else
                    {
                        locationPicker.SelectedIndex = -1;
                        locationPicker.Title = "ロケーションを選択";
                    }
                }
                _suppressLocationChanged = false;

                RefreshPendingLotTable();
                RefreshAllLotsSummaryTable();
                return;
            }

            _suppressItemSearch = true;
            itemEntry.Text = item.ItemCode;
            _suppressItemSearch = false;

            List<ItemMaster>? detail = null;
            try
            {
                string? id = item.Id > 0 ? item.Id.ToString() : null;
                string? lineNo = item.LineNo > 0 ? item.LineNo.ToString() : null;
                string? locId = item.LocationId > 0 ? item.LocationId.ToString() : null;
                detail = await SearchItemMasterAsync(id, lineNo, item.ItemCode, locId, item.DiffQty, "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 明細データ読込エラー: {ex}");
            }

            ApplyItemDetail(detail);

            if (differenceEntry != null && !(_currentItems?.FirstOrDefault()?.IsLotItem ?? false))
                differenceEntry.Text = item.DiffQty.ToString();

            ApplyReasonFromMemo(item.AdjustReason);
            FillLocationPickerWithSuppress();

            if (currentStockEntry != null)
                currentStockEntry.Text = _currentStockValue.ToString();
            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();

            RefreshPendingLotTable();
            RefreshAllLotsSummaryTable();
            RecalculateDifference();
        }

        private void UpdateFormEditability()
        {
            bool canEditItemAndLoc = !IsEditMode || _isNewDetail;

            if (itemEntry != null)
            {
                itemEntry.IsReadOnly = !canEditItemAndLoc;
                itemEntry.TextColor = canEditItemAndLoc ? Colors.Black : DisabledFg;
            }
            if (_itemBorder != null)
                _itemBorder.BackgroundColor = canEditItemAndLoc ? Colors.White : DisabledBg;
            if (scanButtonBorder != null)
            {
                scanButtonBorder.BackgroundColor = canEditItemAndLoc ? Colors.White : DisabledBg;
                scanButtonBorder.Opacity = canEditItemAndLoc ? 1.0 : 0.5;
                scanButtonBorder.InputTransparent = !canEditItemAndLoc;
            }
            if (locationPicker != null)
            {
                locationPicker.IsEnabled = canEditItemAndLoc;
                locationPicker.TextColor = canEditItemAndLoc ? Colors.Black : DisabledFg;
            }
            if (_locBorder != null)
                _locBorder.BackgroundColor = canEditItemAndLoc ? Colors.White : DisabledBg;
        }

        private void OnAddDetailRowClicked(object? sender, EventArgs e)
        {
            RevertUnsavedLotChangesIfNeeded();
            RemoveUnsavedNewDetailIfNeeded();

            _nextNewLineNo++;
            var newItem = new StockAdjustItem
            {
                Id = 0,
                LineNo = _nextNewLineNo,
                AdjustNo = _adjustItems.FirstOrDefault()?.AdjustNo ?? "",
                RegisterDate = _adjustItems.FirstOrDefault()?.RegisterDate ?? "",
                ItemCode = "",
                LocationId = 0,
                LocationName = "",
                DiffQty = 0,
                AdjustReason = ""
            };

            _adjustItems.Add(newItem);
            _lotsPerItem[newItem.LineNo] = new List<PendingLotItem>();

            if (_detailSelectionTableHost != null)
                _detailSelectionTableHost.Content = BuildDetailSelectionTable();

            OnDetailRowSelected(newItem);
        }

        private async void OnSaveDetailClicked(object? sender, EventArgs e)
        {
            if (_selectedItem == null)
            {
                await DisplayAlert("エラー", "明細を選択してください。", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(itemEntry?.Text))
            {
                await DisplayAlert("エラー", "品目コードを入力してください。", "OK");
                return;
            }

            if (!int.TryParse(differenceEntry?.Text?.Trim(), out int diff))
            {
                await DisplayAlert("エラー", "差異数量を正しく入力してください。", "OK");
                return;
            }

            bool isLotItem = _currentItems?.FirstOrDefault()?.IsLotItem ?? false;
            if (isLotItem)
            {
                var lots = GetLotsForSelectedItem();
                int lotSum = lots.Sum(p => p.Qty ?? 0);

                if (lots.Count == 0)
                {
                    await DisplayAlert("エラー", "ロット管理品目の場合、ロットを1件以上追加してください。", "OK");
                    return;
                }
                if (lotSum == 0)
                {
                    await DisplayAlert("エラー", "ロット数量の合計が0です。数量を入力してください。", "OK");
                    return;
                }
                if (lotSum != diff)
                {
                    await DisplayAlert("エラー", $"ロット数量の合計({lotSum})が差異数量({diff})と一致しません。\n一致するように修正してください。", "OK");
                    return;
                }
            }
            else
            {
                if (diff == 0)
                {
                    await DisplayAlert("エラー", "差異数量が0のため保存できません。", "OK");
                    return;
                }
            }

            if (reasonPicker?.SelectedItem == null)
            {
                await DisplayAlert("エラー", "調整理由を選択してください。", "OK");
                return;
            }

            _selectedItem.DiffQty = diff;
            _selectedItem.AdjustReason = reasonPicker?.SelectedItem?.ToString() ?? "";

            var locName = locationPicker?.SelectedItem?.ToString() ?? "";
            _selectedItem.LocationName = locName;
            var matchedLoc = _locationList.FirstOrDefault(l => l.Name == locName);
            if (matchedLoc != null)
                _selectedItem.LocationId = matchedLoc.Id;

            _selectedItem.ItemCode = itemEntry?.Text?.Trim() ?? "";

            var currentLineNo = _selectedItem.LineNo;
            var currentItemCode = _selectedItem.ItemCode;

            _savedNewLots.RemoveAll(l => l.LineNo == currentLineNo);

            var currentLots = GetLotsForSelectedItem();
            foreach (var lot in currentLots.Where(p => p.IsNew))
            {
                _savedNewLots.Add(new PendingLotItem
                {
                    ItemCode = currentItemCode,
                    LotNo = lot.LotNo,
                    Qty = lot.Qty,
                    IsNew = true,
                    LineNo = currentLineNo
                });
            }

            _isCurrentDetailSaved = true;
            _hasUnsavedLotChanges = false; 

            if (_detailSelectionTableHost != null)
                _detailSelectionTableHost.Content = BuildDetailSelectionTable();

            RefreshAllLotsSummaryTable();

            await DisplayAlert("完了", "明細の値を保存しました。", "OK");
        }

        private void BuildUI()
        {
            mainGrid = new Grid
            {
                RowDefinitions = { new RowDefinition { Height = GridLength.Star } },
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Padding = new Thickness(0)
            };

            var formContainer = new VerticalStackLayout
            {
                Spacing = 8,
                BackgroundColor = Colors.White,
                Padding = new Thickness(12)
            };

            if (IsEditMode && _adjustItems.Count > 0)
            {
                var master = _adjustItems[0];
                _headerInfoLabel = new Label
                {
                    Text = $"調整No: {master.AdjustNo}  登録日: {master.RegisterDate}",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#333333"),
                    Margin = new Thickness(0, 0, 0, 4)
                };
                formContainer.Children.Add(_headerInfoLabel);
            }

            _detailSelectionTableHost = new ContentView();
            _detailSelectionTableHost.Content = BuildDetailSelectionTable();
            formContainer.Children.Add(_detailSelectionTableHost);

            var addDetailBtn = new Button
            {
                Text = "+ 明細を追加",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#245a96"),
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 2,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 5,
                HeightRequest = 35,
                Margin = new Thickness(0, 0, 0, 4)
            };
            addDetailBtn.Clicked += OnAddDetailRowClicked;
            formContainer.Children.Add(addDetailBtn);

            formContainer.Children.Add(new BoxView
            {
                HeightRequest = 2,
                Color = Color.FromArgb("#245a96"),
                Margin = new Thickness(0, 4, 0, 8)
            });

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

            _itemBorder = CreateInputBorder(itemEntry, Colors.White);
            scanButtonBorder = BuildBarcodeIcon();

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnScanClicked;
            scanButtonBorder.GestureRecognizers.Add(tapGesture);

            itemRow.Add(_itemBorder, 0, 0);
            itemRow.Add(scanButtonBorder, 1, 0);
            formContainer.Children.Add(itemLabel);
            formContainer.Children.Add(itemRow);

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

            _lotSectionContainer = new VerticalStackLayout { Spacing = 8, IsVisible = false };
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

            formContainer.Children.Add(_lotSectionContainer);

            _allLotsSummaryHost = new ContentView();
            formContainer.Children.Add(_allLotsSummaryHost);
            RefreshAllLotsSummaryTable();

            saveDetailButton = new Button
            {
                Text = "現在の明細を保存",
                BackgroundColor = Color.FromArgb("#e8a030"),
                TextColor = Colors.White,
                HeightRequest = 35,
                CornerRadius = 5,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            saveDetailButton.Clicked += OnSaveDetailClicked;
            formContainer.Children.Add(saveDetailButton);

            registerButton = new Button
            {
                Text = "調整を登録",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                HeightRequest = 35,
                CornerRadius = 5,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 5, 0, 0)
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

            UpdateFormEditability();
        }

        private void RefreshAllLotsSummaryTable()
        {
            if (_allLotsSummaryHost == null) return;

            if (_savedNewLots.Count == 0)
            {
                _allLotsSummaryHost.Content = null;
                _allLotsSummaryHost.IsVisible = false;
                return;
            }

            var container = new VerticalStackLayout { Spacing = 4, Margin = new Thickness(0, 10, 0, 0) };
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({_savedNewLots.Count}件)",
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

            for (int r = 0; r < _savedNewLots.Count; r++)
            {
                int sepRow = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, sepRow);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRow = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var lot = _savedNewLots[r];
                tableGrid.Add(new Label { Text = lot.ItemCode, FontSize = 11, Padding = new Thickness(4) }, 0, dataRow);
                tableGrid.Add(new Label { Text = lot.LotNo, FontSize = 11, Padding = new Thickness(4) }, 1, dataRow);
                tableGrid.Add(new Label { Text = $"{lot.Qty}個", FontSize = 11, Padding = new Thickness(4) }, 2, dataRow);
            }

            var tableBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Background = Colors.White
            };
            tableBorder.Content = tableGrid;
            container.Children.Add(tableBorder);

            _allLotsSummaryHost.Content = container;
            _allLotsSummaryHost.IsVisible = true;
        }

        private async void OnItemTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (!_isNewDetail && IsEditMode) return;
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

                string? id = _selectedItem != null && _selectedItem.Id > 0 ? _selectedItem.Id.ToString() : null;
                string? lineNo = _selectedItem != null && _selectedItem.LineNo > 0 ? _selectedItem.LineNo.ToString() : null;
                string? locId = GetCurrentLocationId();
                int? diffQty = _selectedItem?.DiffQty ?? 0;

                var items = await SearchItemMasterAsync(id, lineNo, text, locId, diffQty, "", cts.Token);
                if (cts.IsCancellationRequested) return;
                ApplyItemDetail(items);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested) return;
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] 品目検索エラー: {ex}");
                ApplyItemDetail(null);
            }
        }

        protected virtual async Task<List<ItemMaster>?> SearchItemMasterAsync(
            string? id, string? lineNo, string itemCode,
            string? locationId, int? diffQty, string? memo,
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
                finally { }

                if (result == null) return null;
                if (!result.Success) return null;
                if (result.SubData == null || result.SubData.Count == 0) return null;

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
                                MainThread.BeginInvokeOnMainThread(() => FillLocationPickerWithSuppress());
                        }
                    }
                    else if (sub.SubName == "PH_DATA")
                    {
                        items = BaseUtils.JsonToClass<List<ItemMaster>>(sub.SubJson!);

                        var lots = GetLotsForSelectedItem();

                        var savedLocalLots = lots.Where(p => p.IsNew).ToList();

                        lots.Clear();

                        if (items != null && items.Count > 0)
                        {
                            var itemCodeNow = itemEntry?.Text?.Trim() ?? string.Empty;
                            foreach (var it in items)
                            {
                                if (string.IsNullOrWhiteSpace(it.LotNo)) continue;
                                lots.Add(new PendingLotItem
                                {
                                    ItemCode = itemCodeNow,
                                    LotNo = it.LotNo,
                                    Qty = it.LotQuantity,
                                    IsNew = false,
                                    LineNo = _selectedItem?.LineNo ?? 0
                                });
                            }
                        }

                        foreach (var localLot in savedLocalLots)
                        {
                            if (!lots.Any(l => l.LotNo == localLot.LotNo && l.ItemCode == localLot.ItemCode))
                                lots.Add(localLot);
                        }

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            RefreshPendingLotTable();
                            RecalculateDifference();
                        });
                    }
                }

                if (items == null || items.Count == 0) return null;
                return items;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryAdjustment] SearchItemMasterAsync 例外: {ex.Message}");
                return null;
            }
        }

        private void ApplyItemDetail(List<ItemMaster>? items)
        {
            _currentItems = items;
            var first = items?.FirstOrDefault();
            bool isLotItem = first?.IsLotItem ?? false;

            ApplyReasonFromMemo(first?.Memo);

            int stock = 0;
            if (first != null)
                int.TryParse(first.HandQuantity?.ToString(), out stock);

            if (_lotSectionContainer != null)
                _lotSectionContainer.IsVisible = isLotItem;

            RefreshPendingLotTable();

            _currentStockValue = stock;
            if (currentStockEntry != null)
                currentStockEntry.Text = stock.ToString();

            RecalculateDifference();
        }

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
                target = trimmed;
            else
                target = REASON_OTHER;

            if (!reasonPicker.Items.Contains(target))
                reasonPicker.Items.Add(target);

            reasonPicker.SelectedItem = target;
            reasonPicker.Title = target;
        }

        private string? GetCurrentLocationId()
        {
            var selectedName = locationPicker?.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedName))
            {
                if (_isNewDetail || !IsEditMode) return DEFAULT_LOCATION_ID;
                return null;
            }
            var matched = _locationList.FirstOrDefault(l => l.Name == selectedName);
            return matched?.Id.ToString();
        }

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

                if (!_isNewDetail && IsEditMode && _selectedItem != null && _selectedItem.LocationId > 0)
                {
                    string targetId = _selectedItem.LocationId.ToString();
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
                    if (!found) locationPicker.SelectedIndex = 0;
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
                    if (!found) locationPicker.SelectedIndex = 0;
                }

                if (locationPicker.SelectedIndex >= 0)
                    locationPicker.Title = locationPicker.SelectedItem?.ToString();
            }
            finally
            {
                _suppressLocationChanged = false;
            }
        }

        private async Task OnLocationChanged(object sender, EventArgs e)
        {
            if (locationPicker == null) return;
            if (_suppressLocationChanged) return;
            if (!_isNewDetail && IsEditMode) return;

            if (locationPicker.SelectedIndex >= 0)
                locationPicker.Title = locationPicker.SelectedItem?.ToString();
            else
                locationPicker.Title = "ロケーションを選択";

            if (locationPicker.SelectedIndex < 0) return;

            var currentItemCode = itemEntry?.Text?.Trim();
            if (!string.IsNullOrEmpty(currentItemCode))
            {
                try
                {
                    string? id = _selectedItem != null && _selectedItem.Id > 0 ? _selectedItem.Id.ToString() : null;
                    string? lineNo = _selectedItem != null && _selectedItem.LineNo > 0 ? _selectedItem.LineNo.ToString() : null;
                    string? newLocId = GetCurrentLocationId();
                    int? diffQty = _selectedItem?.DiffQty ?? 0;

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
                diff = parsedDiff;
            return _currentStockValue + diff;
        }

        private void RecalculateDifference()
        {
            if (differenceEntry == null) return;

            bool isLotItem = _currentItems?.FirstOrDefault()?.IsLotItem ?? false;
            var lots = GetLotsForSelectedItem();

            if (isLotItem)
            {
                int sum = lots.Sum(p => p.Qty ?? 0);
                if (int.TryParse(_qtyEntry?.Text?.Trim(), out int pendingQty) && pendingQty > 0)
                    sum += pendingQty;

                differenceEntry.IsReadOnly = true;
                differenceEntry.TextColor = DiffDisabledFg;
                if (_diffBorder != null) _diffBorder.BackgroundColor = DiffDisabledBg;
                differenceEntry.Text = sum.ToString();
            }
            else
            {
                differenceEntry.IsReadOnly = false;
                differenceEntry.TextColor = DiffEnabledFg;
                if (_diffBorder != null) _diffBorder.BackgroundColor = DiffEnabledBg;
            }

            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();
        }

        private void OnQtyEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            RecalculateDifference();
        }

        private void OnDifferenceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            if (!_isNewDetail && IsEditMode) return;
            await DisplayAlert("スキャン", "バーコードスキャナーを起動します (実装待ち)", "OK");
        }

        private async Task OnRegisterClicked(object sender, EventArgs e)
        {
            RevertUnsavedLotChangesIfNeeded();
            RemoveUnsavedNewDetailIfNeeded();

            var validItems = _adjustItems.Where(i => !string.IsNullOrWhiteSpace(i.ItemCode)).ToList();

            if (validItems.Count == 0)
            {
                await DisplayAlert("エラー", "有効な明細がありません。", "OK");
                return;
            }

            foreach (var item in validItems)
            {
                if (string.IsNullOrWhiteSpace(item.AdjustReason))
                {
                    await DisplayAlert("エラー", $"調整理由が未選択の明細があります（{item.ItemCode}）。", "OK");
                    return;
                }
            }

            if (registerButton != null) registerButton.IsEnabled = false;

            try
            {
                foreach (var item in validItems)
                {
                    bool hasLotsInMemory = _lotsPerItem.ContainsKey(item.LineNo)
                                           && _lotsPerItem[item.LineNo].Count > 0;

                    if (!hasLotsInMemory && item.Id > 0 && !string.IsNullOrWhiteSpace(item.ItemCode))
                    {
                        string? locId = item.LocationId > 0 ? item.LocationId.ToString() : null;

                        var previousSelected = _selectedItem;
                        _selectedItem = item;

                        try
                        {
                            await SearchItemMasterAsync(
                                item.Id.ToString(),
                                item.LineNo.ToString(),
                                item.ItemCode,
                                locId,
                                item.DiffQty,
                                item.AdjustReason);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[InventoryAdjustment] Lotのプリロードに失敗しました: {ex.Message}");
                        }
                        finally
                        {
                            _selectedItem = previousSelected;
                        }
                    }
                }
                var batchParam = new StockAdjustBatchSaveParam
                {
                    Id = IsEditMode ? _adjustItems.FirstOrDefault()?.Id : null,
                    Kbn = KBN_SAVE,
                    Items = validItems.Select(item =>
                    {
                        var lots = _lotsPerItem.ContainsKey(item.LineNo)
                            ? _lotsPerItem[item.LineNo]
                            : new List<PendingLotItem>();

                        return new StockAdjustSaveParam
                        {
                            id = item.Id > 0 ? item.Id : null,
                            Kbn = KBN_SAVE,
                            LineNo = item.LineNo > 0 ? item.LineNo.ToString() : null,
                            Keyword = item.ItemCode,
                            LocationId = item.LocationId > 0 ? item.LocationId.ToString() : null,
                            LineSeq = item.LineSeq,
                            DiffQty = item.DiffQty,
                            Memo = item.AdjustReason,
                            Lots = lots.Count > 0
                                ? lots.Select(p => new LotSaveItem { LotNo = p.LotNo, Qty = p.Qty ?? 0 }).ToList()
                                : null
                        };
                    }).ToList()
                };

                var request = new RequestData<StockAdjustBatchSaveParam, EvangJsonModel>(RESTLET_STOCK_ADJUST);
                request.Info = batchParam;

                var (success, errorMsg, tranId) = await SafeSaveAdjustAsync(batchParam);

                if (!success)
                {
                    await DisplayAlert("エラー", $"保存に失敗しました。\n{errorMsg}", "OK");
                    return;
                }

                await DisplayAlert("完了",
                    IsEditMode ? $"{tranId}\n在庫調整を更新しました。" : $"{tranId}\n在庫調整を登録しました。",
                    "OK");
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

            var lots = GetLotsForSelectedItem();
            lots.Add(new PendingLotItem
            {
                ItemCode = itemCode,
                LotNo = lotNo,
                Qty = qty,
                IsNew = true,
                LineNo = _selectedItem?.LineNo ?? 0
            });

            // ★ 新增Lot也属于未保存修改
            _hasUnsavedLotChanges = true;

            if (_lotEntry != null) _lotEntry.Text = string.Empty;
            if (_qtyEntry != null) _qtyEntry.Text = string.Empty;

            RefreshPendingLotTable();
            RecalculateDifference();
        }

        private Border BuildEditableLotTableForPending()
        {
            var lots = GetLotsForSelectedItem();
            return BuildEditableLotTableInternal(lots, showItemColumn: false);
        }

        private void OnDeletePendingLot(PendingLotItem item)
        {
            var lots = GetLotsForSelectedItem();
            lots.Remove(item);

            _hasUnsavedLotChanges = true;

            RefreshPendingLotTable();
            RecalculateDifference();
        }

        private void RefreshPendingLotTable()
        {
            if (_pendingLotTableHost == null) return;
            var lots = GetLotsForSelectedItem();

            if (lots.Count == 0)
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
                int sepRow = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, sepRow);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRow = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var item = items[r];
                int col = 0;
                if (showItemColumn)
                    tableGrid.Add(new Label { Text = item.ItemCode, FontSize = 11, Padding = new Thickness(4) }, col++, dataRow);

                tableGrid.Add(new Label { Text = item.LotNo, FontSize = 11, Padding = new Thickness(4) }, col++, dataRow);
                tableGrid.Add(new Label { Text = $"{item.Qty}個", FontSize = 11, Padding = new Thickness(4) }, col++, dataRow);

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
                tableGrid.Add(deleteLabel, col, dataRow);
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

        public static LoadingDialog? loading;
        public static OAuth2Client oauth2_client = new();
        private async Task<(bool Success, string? ErrorMessage, string? TranId)> SafeSaveAdjustAsync(StockAdjustBatchSaveParam batchParam)
        {
            if (loading == null) loading = new LoadingDialog();
            await MainThread.InvokeOnMainThreadAsync(() => loading?.LoadingShow(this));

            try
            {
                var (token, errMsg) = await oauth2_client.GetValidAccessTokenAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(token))
                    return (false, errMsg ?? "認証トークンの取得に失敗しました。", null);

                var request = new RequestData<StockAdjustBatchSaveParam, EvangJsonModel>(RESTLET_STOCK_ADJUST) { Info = batchParam };
                var json = System.Text.Json.JsonSerializer.Serialize(request);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var response = await client.PostAsync(
                    LocalMemory.restlets[RESTLET_STOCK_ADJUST],
                    new StringContent(json, Encoding.UTF8, "application/json"),
                    cts.Token).ConfigureAwait(false);

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return (false, $"サーバーエラー ({(int)response.StatusCode}): {content}", null);

                var result = System.Text.Json.JsonSerializer.Deserialize<ResponseData<EvangJsonModel, EvangJsonModel>>(content);
                if (result == null || !result.Success)
                    return (false, result?.ErrorMessage ?? "NetSuite側で保存処理に失敗しました。", null);

                var tranId = result.SubData?.FirstOrDefault(s => s.SubName == "PH_TRANID")?.SubJson;
                return (true, null, tranId);
            }
            catch (OperationCanceledException)
            {
                WeakReferenceMessenger.Default.Send(new NetsuiteTimeoutMessage(oauth2_client.ClientId, LocalMemory.restlets[RESTLET_STOCK_ADJUST]));
                return (false, "リクエストがタイムアウトしました。ネットワーク接続を確認してください。", null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (loading != null && loading.LoadingClose())
                        loading = null;
                });
            }
        }

        #endregion

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
            public int LineNo { get; set; } = 0;
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
            public int? LineSeq { get; set; }
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

        public class StockAdjustBatchSaveParam : EvangJsonModel
        {
            [JsonPropertyName("Id")]
            public int? Id { get; set; }
            public string Kbn { get; set; } = "savedata";
            public List<StockAdjustSaveParam> Items { get; set; } = new();
        }
    }
}