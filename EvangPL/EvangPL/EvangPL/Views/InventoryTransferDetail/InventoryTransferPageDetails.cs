//using Android.Webkit;
using EvangPL.Components;
using EvangPL.Utils;
using EvangPL.Views.InventoryTransfer;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EvangPL.Views.InventoryTransferPageDetails
{
    public class InventoryTransferPageDetails : EvangContentVM
    {
        // UI コントロール参照
        private VerticalStackLayout? _scrollContainer;
        private Entry? _itemEntry;
        private VerticalStackLayout? _lotSectionContainer;
        private Entry? _lotEntry;
        private Entry? _qtyEntry;
        private ContentView? _pendingLotTableHost;
        private ContentView? _bottomPendingTableHost;
        private ContentView? _topTableHost;

        private Border? _lotEntryBorder;
        private Border? _qtyEntryBorder;
        private Border? _lotBarcodeBorder;
        private HorizontalStackLayout? _lotBarcodeBars;
        private Button? _addLotButton;

        // 状態フィールド
        private readonly List<PendingLotItem> _pendingLots = new List<PendingLotItem>();
        private readonly List<TransferDetailItem> _details = new List<TransferDetailItem>();
        private int _editingDetailIndex = -1;
        private bool _suppressItemSearch;

        private CancellationTokenSource? _itemSearchCts;
        private CancellationTokenSource? _keywordCts;

        private readonly TransferRecord? _editRecord;
        private bool IsEditMode => _editRecord != null;

        private List<LocationData> localist = new List<LocationData>();
        private Picker? fromPicker;
        private Picker? toPicker;
        private bool islotflag;

        private string? _currentItemCode;
        private bool _currentItemIsLot;

        // 色定数
        private static readonly Color InputBorderColor = Color.FromArgb("#cdd2dc");
        private static readonly Color InputBackgroundColor = Colors.White;
        private const int InputCornerRadius = 6;
        private static readonly Color SelectedRowColor = Color.FromArgb("#e6f0fa");
        private static readonly Color DisabledBg = Color.FromArgb("#e0e0e0");
        private static readonly Color DisabledFg = Colors.Gray;
        private static readonly Color DisabledBorder = Color.FromArgb("#c0c0c0");

        private static readonly Color BarcodeBarColor = Color.FromArgb("#1e3a5f");
        private static readonly Color PrimaryBlue = Color.FromArgb("#245a96");
        private static readonly Color AccentOrange = Color.FromArgb("#e8a030");

        private const int ButtonHeight = 35;
        private const int ButtonCornerRadius = 5;

        // ==========================================
        // ★ リスト内の未保存空白行（新規入力用）を取得するヘルパー
        // ==========================================
        private TransferDetailItem? GetExistingBlankDetail()
        {
            return _details.FirstOrDefault(d => string.IsNullOrWhiteSpace(d.ItemCode));
        }

        // ==========================================
        // コンストラクタ
        // ==========================================
        public InventoryTransferPageDetails() : this(null)
        {
        }

        public InventoryTransferPageDetails(TransferRecord? editRecord) : base("strInventoryTransfer")
        {
            _editRecord = editRecord;
            Title = IsEditMode ? "在庫振替 - 編集" : "在庫振替 - 新規登録";

            if (!IsEditMode)
            {
                _details.Add(new TransferDetailItem
                {
                    ItemCode     = "",
                    FromLocation = null,
                    ToLocation   = null,
                    IsLotItem    = false,
                    Lots         = new List<PendingLotItem>()
                });
                _editingDetailIndex = 0;
            }

            BuildUI();
        }

        // ==========================================
        // 画面構築
        // ==========================================
        private async void BuildUI()
        {
            await GetLocaData("locadata");

            var mainGrid = new Grid
            {
                RowDefinitions = { new RowDefinition { Height = GridLength.Star } },
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            var locationArea = BuildLocationArea();
            var detailInputArea = BuildDetailInputArea();

            _bottomPendingTableHost = new ContentView
            {
                IsVisible = false,
                Content = BuildBottomPendingTable()
            };

            _topTableHost = new ContentView { IsVisible = true };

            var saveBtn = new Button
            {
                Text = "保存",
                BackgroundColor = PrimaryBlue,
                TextColor = Colors.White,
                HeightRequest = ButtonHeight,
                CornerRadius = ButtonCornerRadius,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(10, 5, 10, 10)
            };
            saveBtn.Clicked += OnSaveClicked;

            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            _scrollContainer.Children.Add(locationArea);
            _scrollContainer.Children.Add(_topTableHost);
            _scrollContainer.Children.Add(detailInputArea);
            _scrollContainer.Children.Add(_bottomPendingTableHost);
            _scrollContainer.Children.Add(saveBtn);

            var scrollView = new ScrollView
            {
                Content = _scrollContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            mainGrid.Add(scrollView, 0, 0);
            Content = mainGrid;

            SetLotInputEnabled(false);

            RefreshTopTable();
            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // ==========================================
        // ページ最上部：移動元 / 移動先エリア
        // ==========================================
        private Border BuildLocationArea()
        {
            var outerLayout = new VerticalStackLayout { Spacing = 10 };

            var titleRow = new HorizontalStackLayout { Spacing = 8 };
            titleRow.Children.Add(new BoxView
            {
                Color = PrimaryBlue,
                WidthRequest = 3,
                HeightRequest = 16,
                VerticalOptions = LayoutOptions.Center
            });
            titleRow.Children.Add(new Label
            {
                Text = "移動ロケーション",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#333333"),
                VerticalOptions = LayoutOptions.Center
            });
            outerLayout.Children.Add(titleRow);

            var innerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 8
            };

            // 左：移動元
            var fromLayout = new VerticalStackLayout { Spacing = 4 };
            fromLayout.Children.Add(new Label
            {
                Text = "移動元",
                FontSize = 11,
                TextColor = Colors.Gray
            });

            fromPicker = new Picker
            {
                Title = "選択してください",
                SelectedIndex = -1,
                BackgroundColor = Colors.White,
                ItemsSource = localist,
                ItemDisplayBinding = new Binding("name")
            };
            if (IsEditMode && _editRecord != null && localist != null)
            {
                var sourceLoc = localist.FirstOrDefault(x => x.id == _editRecord.SourceId);
                if (sourceLoc == null && _editRecord.SourceId != null)
                {
                    sourceLoc = new LocationData { id = _editRecord.SourceId, name = $"ID: {_editRecord.SourceId}" };
                    localist.Add(sourceLoc);
                }
                fromPicker.SelectedItem = sourceLoc;
            }
            var fromBorder = WrapInputControl(fromPicker);
            if (IsEditMode)
            {
                fromPicker.InputTransparent = true;
                fromBorder.BackgroundColor = DisabledBg;
            }
            fromLayout.Children.Add(fromBorder);
            innerGrid.Add(fromLayout, 0, 0);

            // 中央：矢印
            var arrowLabel = new Label
            {
                Text = "→",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = PrimaryBlue,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            innerGrid.Add(arrowLabel, 1, 0);

            // 右：移動先
            var toLayout = new VerticalStackLayout { Spacing = 4 };
            toLayout.Children.Add(new Label
            {
                Text = "移動先",
                FontSize = 11,
                TextColor = Colors.Gray
            });

            toPicker = new Picker
            {
                Title = "選択してください",
                SelectedIndex = -1,
                BackgroundColor = Colors.White,
                ItemsSource = localist,
                ItemDisplayBinding = new Binding("name")
            };
            if (IsEditMode && _editRecord != null && localist != null)
            {
                var destLoc = localist.FirstOrDefault(x => x.id == _editRecord.DestId);
                if (destLoc == null && _editRecord.DestId != null)
                {
                    destLoc = new LocationData { id = _editRecord.DestId, name = $"ID: {_editRecord.DestId}" };
                    localist.Add(destLoc);
                }
                toPicker.SelectedItem = destLoc;
            }
            var toBorder = WrapInputControl(toPicker);
            if (IsEditMode)
            {
                toPicker.InputTransparent = true;
                toBorder.BackgroundColor = DisabledBg;
            }
            toLayout.Children.Add(toBorder);
            innerGrid.Add(toLayout, 2, 0);

            outerLayout.Children.Add(innerGrid);

            return new Border
            {
                Stroke = InputBorderColor,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = Colors.White,
                Padding = new Thickness(12, 10),
                Content = outerLayout
            };
        }

        // ==========================================
        // 明細登録エリア
        // ==========================================
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

            // ロット関連
            _lotSectionContainer = new VerticalStackLayout
            {
                Spacing = 10,
                IsVisible = true
            };

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
            _lotEntry = new Entry
            {
                Placeholder = "ロット/シリアルをスキャンまたは入力",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                PlaceholderColor = Colors.Gray
            };
            _lotEntryBorder = WrapInputControl(_lotEntry);
            lotRow.Add(_lotEntryBorder, 0, 0);

            _lotBarcodeBars = new HorizontalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            double[] barWidths = { 2, 4, 2, 6, 2, 4, 2 };
            foreach (var w in barWidths)
            {
                _lotBarcodeBars.Children.Add(new BoxView
                {
                    Color = BarcodeBarColor,
                    WidthRequest = w,
                    HeightRequest = 22,
                    VerticalOptions = LayoutOptions.Center
                });
            }
            _lotBarcodeBorder = new Border
            {
                Stroke = InputBorderColor,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                BackgroundColor = Colors.White,
                Padding = new Thickness(8, 6),
                WidthRequest = 50,
                HeightRequest = 45,
                HorizontalOptions = LayoutOptions.End,
                Content = _lotBarcodeBars
            };
            lotRow.Add(_lotBarcodeBorder, 1, 0);
            _lotSectionContainer.Children.Add(lotRow);

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
            _qtyEntry = new Entry
            {
                Placeholder = "数量を入力",
                Keyboard = Microsoft.Maui.Keyboard.Numeric,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                PlaceholderColor = Colors.Gray
            };
            _qtyEntryBorder = WrapInputControl(_qtyEntry);
            qtyRow.Add(_qtyEntryBorder, 0, 0);
            qtyRow.Add(new Label
            {
                Text = "個",
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }, 1, 0);
            _lotSectionContainer.Children.Add(qtyRow);

            _pendingLotTableHost = new ContentView { Content = BuildEditableLotTableForPending() };
            _lotSectionContainer.Children.Add(_pendingLotTableHost);

            _addLotButton = new Button
            {
                Text = "+ロットを追加",
                BackgroundColor = Colors.Transparent,
                TextColor = PrimaryBlue,
                BorderColor = PrimaryBlue,
                BorderWidth = 3,
                HeightRequest = ButtonHeight,
                CornerRadius = ButtonCornerRadius,
                FontAttributes = FontAttributes.Bold
            };
            _addLotButton.Clicked += OnAddLotButtonClicked;
            _lotSectionContainer.Children.Add(_addLotButton);

            var saveDetailBtn = new Button
            {
                Text = "現在の明細を保存",
                BackgroundColor = AccentOrange,
                TextColor = Colors.White,
                HeightRequest = ButtonHeight,
                CornerRadius = ButtonCornerRadius,
                FontAttributes = FontAttributes.Bold
            };
            saveDetailBtn.Clicked += OnSaveDetailClicked;
            _lotSectionContainer.Children.Add(saveDetailBtn);

            layout.Children.Add(_lotSectionContainer);

            border.Content = layout;
            return border;
        }

        // ==========================================
        // lot 入力関連の有効/無効切替
        // ==========================================
        private void SetLotInputEnabled(bool enabled)
        {
            if (_lotEntry != null)
            {
                _lotEntry.IsEnabled = enabled;
                _lotEntry.TextColor = enabled ? Colors.Black : DisabledFg;
                _lotEntry.PlaceholderColor = enabled ? Colors.Gray : DisabledFg;
            }
            if (_lotEntryBorder != null)
            {
                _lotEntryBorder.BackgroundColor = enabled ? InputBackgroundColor : DisabledBg;
                _lotEntryBorder.Stroke = enabled ? InputBorderColor : DisabledBorder;
            }

            if (_lotBarcodeBorder != null)
            {
                _lotBarcodeBorder.InputTransparent = !enabled;
                _lotBarcodeBorder.BackgroundColor = enabled ? Colors.White : DisabledBg;
                _lotBarcodeBorder.Stroke = enabled ? InputBorderColor : DisabledBorder;
            }
            if (_lotBarcodeBars != null)
            {
                foreach (var child in _lotBarcodeBars.Children)
                {
                    if (child is BoxView bv)
                        bv.Color = enabled ? BarcodeBarColor : DisabledFg;
                }
            }

            if (_qtyEntry != null)
            {
                _qtyEntry.IsEnabled = true;
                _qtyEntry.TextColor = Colors.Black;
                _qtyEntry.PlaceholderColor = Colors.Gray;
            }
            if (_qtyEntryBorder != null)
            {
                _qtyEntryBorder.BackgroundColor = InputBackgroundColor;
                _qtyEntryBorder.Stroke = InputBorderColor;
            }

            if (_addLotButton != null)
            {
                _addLotButton.IsEnabled = enabled;
                _addLotButton.TextColor = enabled ? PrimaryBlue : DisabledFg;
                _addLotButton.BorderColor = enabled ? PrimaryBlue : DisabledFg;
                _addLotButton.Opacity = enabled ? 1.0 : 0.7;
            }
        }

        // ==========================================
        // ★ 明細保存ボタン押下時
        // ★ 位置チェックは「保存」ボタン押下時に一括で行うため、ここでは実施しない
        // ==========================================
        private async void OnSaveDetailClicked(object? sender, EventArgs e)
        {
            try
            {
                var itemCode = _itemEntry?.Text?.Trim();
                if (string.IsNullOrEmpty(itemCode))
                {
                    await DisplayAlert("エラー", "品目を入力してください。", "OK");
                    return;
                }

                var fromLoc = fromPicker?.SelectedItem as LocationData;
                var toLoc = toPicker?.SelectedItem as LocationData;

                if (_currentItemIsLot && _pendingLots.Count == 0)
                {
                    await DisplayAlert("エラー", "ロットを1件以上追加してください。", "OK");
                    return;
                }

                int nonLotQty = 0;
                if (!_currentItemIsLot)
                {
                    var qtyTextCheck = _qtyEntry?.Text?.Trim();
                    if (!int.TryParse(qtyTextCheck, out nonLotQty) || nonLotQty <= 0)
                    {
                        await DisplayAlert("エラー", "移動数量を正しく入力してください。", "OK");
                        return;
                    }
                }

                var detailLots = _pendingLots.Select(x => new PendingLotItem
                {
                    ItemCode = x.ItemCode,
                    LotNo    = x.LotNo,
                    Qty      = x.Qty
                }).ToList();

                if (!_currentItemIsLot && detailLots.Count == 0)
                {
                    detailLots.Add(new PendingLotItem
                    {
                        ItemCode = itemCode,
                        LotNo    = "",
                        Qty      = nonLotQty
                    });
                }

                var detail = new TransferDetailItem
                {
                    ItemCode     = itemCode,
                    FromLocation = fromLoc,
                    ToLocation   = toLoc,
                    IsLotItem    = _currentItemIsLot,
                    Lots         = detailLots
                };

                if (_editingDetailIndex >= 0 && _editingDetailIndex < _details.Count)
                {
                    _details[_editingDetailIndex] = detail;
                }
                else
                {
                    _details.Add(detail);
                }

                var nextBlankItem = GetExistingBlankDetail();
                if (nextBlankItem == null)
                {
                    var blank = new TransferDetailItem
                    {
                        ItemCode     = "",
                        FromLocation = null,
                        ToLocation   = null,
                        IsLotItem    = false,
                        Lots         = new List<PendingLotItem>()
                    };
                    _details.Add(blank);
                    nextBlankItem = blank;
                }

                _editingDetailIndex = _details.IndexOf(nextBlankItem);

                RefreshTopTable();
                RefreshBottomPendingTable();

                ClearForm();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveDetail] {ex}");
                await DisplayAlert("エラー", $"明細保存に失敗しました: {ex.Message}", "OK");
            }
        }

        // ==========================================
        // ★ フォームを初期状態に戻す
        // ==========================================
        private void ClearForm()
        {
            _suppressItemSearch = true;
            if (_itemEntry != null) _itemEntry.Text = "";
            _suppressItemSearch = false;

            if (_lotEntry != null) _lotEntry.Text = "";
            if (_qtyEntry != null) _qtyEntry.Text = "";

            _pendingLots.Clear();
            _currentItemCode  = null;
            _currentItemIsLot = false;
            islotflag         = false;

            if (_lotSectionContainer != null)
                _lotSectionContainer.IsVisible = true;
            SetLotInputEnabled(false);

            RefreshPendingLotTable();
            RefreshTopTable();
            RefreshBottomPendingTable();
        }

        // ==========================================
        // 顶部表格 再描画
        // ==========================================
        private void RefreshTopTable()
        {
            if (_topTableHost == null) return;

            _topTableHost.Content = BuildTopTable();
            _topTableHost.IsVisible = true;
        }

        // ==========================================
        // 顶部表格 ビルド
        // ==========================================
        private Border BuildTopTable()
        {
            var headers = new List<string> { "品目", "数量", "" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(3, GridUnitType.Star),
                new GridLength(2, GridUnitType.Star),
                new GridLength(1, GridUnitType.Star)
            };

            var tableGrid = new Grid();
            foreach (var w in columnWidths)
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = w });

            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < headers.Count; c++)
            {
                tableGrid.Add(new Label
                {
                    Text = headers[c],
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    BackgroundColor = Color.FromArgb("#dbe2ec"),
                    Padding = new Thickness(2),
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }, c, 0);
            }

            for (int r = 0; r < _details.Count; r++)
            {
                int sepIdx = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var sep = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(sep, 0, sepIdx);
                Grid.SetColumnSpan(sep, headers.Count);

                int rowIdx = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var detail = _details[r];
                bool isBlank = string.IsNullOrWhiteSpace(detail.ItemCode);
                var rowBg = (r == _editingDetailIndex) ? SelectedRowColor : Colors.White;

                var itemLabel = new Label
                {
                    Text = detail.ItemCode,
                    FontSize = 11,
                    Padding = new Thickness(4),
                    BackgroundColor = rowBg,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    VerticalTextAlignment = TextAlignment.Center
                };
                var qtyLabel = new Label
                {
                    Text = $"{detail.TotalQty}個",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    BackgroundColor = rowBg,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                var delLabel = new Label
                {
                    Text = isBlank ? "" : "❌",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    BackgroundColor = rowBg,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                var capIdx = r;
                var tapRow = new TapGestureRecognizer();
                tapRow.Tapped += (s, e) => OnTopTableRowSelected(capIdx);
                itemLabel.GestureRecognizers.Add(tapRow);
                qtyLabel.GestureRecognizers.Add(tapRow);

                if (!isBlank)
                {
                    var tapDel = new TapGestureRecognizer();
                    tapDel.Tapped += async (s, e) => await OnDeleteDetailAsync(capIdx);
                    delLabel.GestureRecognizers.Add(tapDel);
                }

                tableGrid.Add(itemLabel, 0, rowIdx);
                tableGrid.Add(qtyLabel, 1, rowIdx);
                tableGrid.Add(delLabel, 2, rowIdx);
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

        // ==========================================
        // 行タップ → フォームへ回显
        // ==========================================
        private void OnTopTableRowSelected(int index)
        {
            if (index < 0 || index >= _details.Count) return;

            _editingDetailIndex = index;
            LoadDetailToForm(_details[index]);
            RefreshTopTable();
            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // ==========================================
        // 明細をフォームへ展開
        // ==========================================
        private void LoadDetailToForm(TransferDetailItem detail)
        {
            _suppressItemSearch = true;
            if (_itemEntry != null) _itemEntry.Text = detail.ItemCode;
            _suppressItemSearch = false;

            if (fromPicker != null && detail.FromLocation != null)
            {
                fromPicker.SelectedItem = detail.FromLocation;
                fromPicker.Title = detail.FromLocation.name ?? "選択してください";
            }
            if (toPicker != null && detail.ToLocation != null)
            {
                toPicker.SelectedItem = detail.ToLocation;
                toPicker.Title = detail.ToLocation.name ?? "選択してください";
            }

            _pendingLots.Clear();

            if (detail.IsLotItem)
            {
                foreach (var l in detail.Lots)
                {
                    _pendingLots.Add(new PendingLotItem
                    {
                        ItemCode = l.ItemCode,
                        LotNo    = l.LotNo,
                        Qty      = l.Qty
                    });
                }
                if (_qtyEntry != null) _qtyEntry.Text = "";
            }
            else
            {
                if (_qtyEntry != null) _qtyEntry.Text = "";
            }

            _currentItemCode  = detail.ItemCode;
            _currentItemIsLot = detail.IsLotItem;
            islotflag         = detail.IsLotItem;

            if (_lotSectionContainer != null)
                _lotSectionContainer.IsVisible = true;
            SetLotInputEnabled(detail.IsLotItem);

            RefreshPendingLotTable();
        }

        // ==========================================
        // ★ 明細削除（確認ダイアログ付き）
        // ==========================================
        private async Task OnDeleteDetailAsync(int index)
        {
            if (index < 0 || index >= _details.Count) return;

            var target = _details[index];
            bool isBlank = string.IsNullOrWhiteSpace(target.ItemCode);

            if (isBlank) return;

            bool confirm = await DisplayAlert(
                "確認",
                $"明細「{target.ItemCode}」を削除しますか？",
                "はい",
                "いいえ");
            if (!confirm) return;

            _details.RemoveAt(index);

            if (GetExistingBlankDetail() == null)
            {
                _details.Add(new TransferDetailItem
                {
                    ItemCode     = "",
                    FromLocation = null,
                    ToLocation   = null,
                    IsLotItem    = false,
                    Lots         = new List<PendingLotItem>()
                });
            }

            var nextBlank = GetExistingBlankDetail();
            if (nextBlank != null)
            {
                _editingDetailIndex = _details.IndexOf(nextBlank);
                LoadDetailToForm(nextBlank);
            }
            else
            {
                _editingDetailIndex = -1;
                ClearForm();
            }

            RefreshTopTable();
            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // ==========================================
        // 保存ボタン押下時
        // ==========================================
        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            try
            {
                var selectedLocation = fromPicker?.SelectedItem as LocationData;
                if (selectedLocation == null || selectedLocation.id == null)
                {
                    await DisplayAlert("エラー", "移動元ロケーションを選択してください。", "OK");
                    return;
                }
                int? fromlocationId = selectedLocation.id;

                var selectedLocationto = toPicker?.SelectedItem as LocationData;
                if (selectedLocationto == null || selectedLocationto.id == null)
                {
                    await DisplayAlert("エラー", "移動先ロケーションを選択してください。", "OK");
                    return;
                }
                int? tolocationId = selectedLocationto.id;

                if (fromlocationId.Value == tolocationId.Value)
                {
                    await DisplayAlert("エラー", "移動元と移動先に同じロケーションは選択できません。", "OK");
                    return;
                }

                var currentItemCode = _itemEntry?.Text?.Trim();
                if (!string.IsNullOrEmpty(currentItemCode))
                {
                    var pendingLots = _pendingLots.Select(x => new PendingLotItem
                    {
                        ItemCode = x.ItemCode,
                        LotNo    = x.LotNo,
                        Qty      = x.Qty
                    }).ToList();

                    if (!_currentItemIsLot && pendingLots.Count == 0)
                    {
                        var qtyText = _qtyEntry?.Text?.Trim();
                        if (int.TryParse(qtyText, out int qtyVal) && qtyVal > 0)
                        {
                            pendingLots.Add(new PendingLotItem
                            {
                                ItemCode = currentItemCode,
                                LotNo    = "",
                                Qty      = qtyVal
                            });
                        }
                    }

                    var pendingDetail = new TransferDetailItem
                    {
                        ItemCode     = currentItemCode,
                        FromLocation = selectedLocation,
                        ToLocation   = selectedLocationto,
                        IsLotItem    = _currentItemIsLot,
                        Lots         = pendingLots
                    };

                    if (_editingDetailIndex >= 0 && _editingDetailIndex < _details.Count)
                        _details[_editingDetailIndex] = pendingDetail;
                    else
                        _details.Add(pendingDetail);

                    RefreshTopTable();
                    RefreshPendingLotTable();
                    RefreshBottomPendingTable();
                }

                var validDetails = _details
                    .Where(d => !string.IsNullOrWhiteSpace(d.ItemCode))
                    .ToList();

                if (validDetails.Count == 0)
                {
                    await DisplayAlert("エラー", "保存する明細がありません。", "OK");
                    return;
                }

                var detailParams = validDetails.Select(d => new StockTransferDetailParam
                {
                    ItemCode  = d.ItemCode,
                    IsLotItem = d.IsLotItem,
                    TotalQty  = d.TotalQty,
                    Lots      = d.IsLotItem
                        ? d.Lots.Select(l => new PendingLotItem
                        {
                            ItemCode = l.ItemCode,
                            LotNo    = l.LotNo,
                            Qty      = l.Qty
                        }).ToList()
                        : new List<PendingLotItem>()
                }).ToList();

                var request = new RequestData<StockTransferSaveParam, EvangJsonModel>("SaveTransfer");
                request.Info = new StockTransferSaveParam
                {
                    Kbn            = "savedata",
                    FromLocationId = fromlocationId,
                    ToLocationId   = tolocationId,
                    Details        = detailParams,
                };

                var successNo = "";
                var result = await this.Post<StockTransferSaveParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                if (result == null || !result.Success)
                {
                    var err = result?.ErrorMessage ?? "サーバー応答なし";
                    System.Diagnostics.Debug.WriteLine($"[InventoryTransfer] 保存失敗: {err}");
                    await DisplayAlert("エラー", $"保存に失敗しました。\n{err}", "OK");
                    return;
                }

                if (result.SubData != null)
                {
                    foreach (var item in result.SubData)
                    {
                        if (item?.SubName == "PH_TRANID" && item.SubJson != null)
                        {
                            successNo = item.SubJson;
                            break;
                        }
                    }
                }

                await DisplayAlert("完了", $"在庫振替\n{successNo}を保存しました", "OK");
                EvangPL.Views.InventoryTransfer.InventoryTransfer.NeedRefreshAfterSave = true;

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryTransfer] 保存例外: {ex}");
                await DisplayAlert("エラー", $"保存中にエラーが発生しました: {ex.Message}", "OK");
            }
        }

        // ==========================================
        // 品目入力変化時
        // ==========================================
        private async void OnItemTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_suppressItemSearch) return;

            _keywordCts?.Cancel();
            _keywordCts = new CancellationTokenSource();
            var token = _keywordCts.Token;

            try
            {
                await Task.Delay(400, token);
                if (token.IsCancellationRequested) return;
                await SearchItemByKeywordAsync(e.NewTextValue);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private async Task SearchItemByKeywordAsync(string? keyword)
        {
            _itemSearchCts?.Cancel();
            var cts = new CancellationTokenSource();
            _itemSearchCts = cts;

            var text = keyword?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                ApplyItemDetail(false);
                return;
            }

            try
            {
                islotflag = false;
                var searchParam = new StockTransferSearchParam
                {
                    Kbn = "islot",
                    Keyword = text
                };
                var request = new RequestData<StockTransferSearchParam, EvangJsonModel>("SaveTransfer");
                request.Info = searchParam;

                var result = await this.Post<StockTransferSearchParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                if (result == null || !result.Success)
                {
                    var err = result?.ErrorMessage ?? "サーバー応答なし";
                    System.Diagnostics.Debug.WriteLine($"[InventoryTransfer] 品目検索失敗: {err}");
                    ApplyItemDetail(false);
                    return;
                }

                if (result.SubData != null)
                {
                    foreach (var item in result.SubData)
                    {
                        if (item?.SubName == "PH_ISLOT" && item.SubJson == "1")
                        {
                            islotflag = true;
                            break;
                        }
                    }
                }

                if (cts.IsCancellationRequested) return;
                ApplyItemDetail(islotflag);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested) return;
                System.Diagnostics.Debug.WriteLine($"[InventoryTransfer] 品目検索エラー: {ex}");
                ApplyItemDetail(false);
            }
        }

        private void ApplyItemDetail(bool isLotItem)
        {
            _currentItemCode = _itemEntry?.Text?.Trim();
            _currentItemIsLot = isLotItem;

            if (_lotSectionContainer != null)
                _lotSectionContainer.IsVisible = true;
            SetLotInputEnabled(isLotItem);

            if (!isLotItem)
            {
                _pendingLots.Clear();
            }

            RefreshPendingLotTable();
        }

        // ==========================================
        // +ロットを追加
        // ==========================================
        private async void OnAddLotButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_currentItemIsLot || string.IsNullOrEmpty(_currentItemCode))
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
                    await DisplayAlert("エラー", "移動数量を正しく入力してください。", "OK");
                    return;
                }
                if (_pendingLots.Any(x => x.LotNo == lotNo))
                {
                    await DisplayAlert("エラー", "同じロットが既に追加されています。", "OK");
                    return;
                }

                _pendingLots.Add(new PendingLotItem
                {
                    ItemCode = _currentItemCode,
                    LotNo    = lotNo,
                    Qty      = qty
                });

                if (_lotEntry != null) _lotEntry.Text = string.Empty;
                if (_qtyEntry != null) _qtyEntry.Text = string.Empty;

                RefreshPendingLotTable();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddLot] {ex}");
                await DisplayAlert("エラー", $"ロット追加に失敗しました: {ex.Message}", "OK");
            }
        }

        // ==========================================
        // 中間表
        // ==========================================
        private Border BuildEditableLotTableForPending()
        {
            return BuildEditableLotTableInternal(_pendingLots, showItemColumn: false, onDelete: OnDeletePendingLot);
        }

        private void OnDeletePendingLot(PendingLotItem item)
        {
            _pendingLots.Remove(item);
            RefreshPendingLotTable();
        }

        private void RefreshPendingLotTable()
        {
            if (_pendingLotTableHost != null)
            {
                _pendingLotTableHost.Content = BuildEditableLotTableForPending();
            }
        }

        // ==========================================
        // 底部表
        // ==========================================
        private List<PendingLotItem> CollectBottomLots()
        {
            var result = new List<PendingLotItem>();

            for (int i = 0; i < _details.Count; i++)
            {
                result.AddRange(_details[i].Lots);
            }

            return result;
        }

        private void RefreshBottomPendingTable()
        {
            if (_bottomPendingTableHost == null) return;

            var allLots = CollectBottomLots();

            if (allLots.Count == 0)
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

        // ==========================================
        // ★ 底部表ビルド
        // onDelete = null を渡すことで ❌ 列を非表示にする
        // ==========================================
        private View BuildBottomPendingTable()
        {
            var allLots = CollectBottomLots();

            if (allLots.Count == 0)
            {
                return new ContentView { IsVisible = false };
            }

            var container = new VerticalStackLayout { Spacing = 4 };
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({allLots.Count}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });
            container.Children.Add(
                BuildEditableLotTableInternal(allLots, showItemColumn: true, onDelete: null));
            return container;
        }

        // ==========================================
        // 削除ボタン付き一覧テーブル
        // onDelete == null のときは ❌ 列を描画しない
        // ==========================================
        private Border BuildEditableLotTableInternal(
            List<PendingLotItem> items,
            bool showItemColumn,
            Action<PendingLotItem>? onDelete = null)
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
                    Padding = new Thickness(2),
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
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
                    tableGrid.Add(new Label
                    {
                        Text = item.ItemCode,
                        FontSize = 11,
                        Padding = new Thickness(4),
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        VerticalTextAlignment = TextAlignment.Center
                    }, col++, dataRowIndex);
                }

                tableGrid.Add(new Label
                {
                    Text = item.LotNo ?? "",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    VerticalTextAlignment = TextAlignment.Center
                }, col++, dataRowIndex);

                tableGrid.Add(new Label
                {
                    Text = $"{item.Qty}個",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }, col++, dataRowIndex);

                if (onDelete != null)
                {
                    var deleteLabel = new Label
                    {
                        Text = "❌",
                        FontSize = 11,
                        Padding = new Thickness(4),
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                    var capturedItem = item;
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) => onDelete(capturedItem);
                    deleteLabel.GestureRecognizers.Add(tapGesture);
                    tableGrid.Add(deleteLabel, col, dataRowIndex);
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

        // ==========================================
        // ヘルパー
        // ==========================================
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
                    Color = BarcodeBarColor,
                    WidthRequest = w,
                    HeightRequest = 22,
                    VerticalOptions = LayoutOptions.Center
                });
            }

            return new Border
            {
                Stroke = InputBorderColor,
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

        private async Task GetLocaData(string kbn)
        {
            try
            {
                var searchParam = new StockTransferSearchParam { Kbn = kbn };
                var request = new RequestData<StockTransferSearchParam, EvangJsonModel>("SaveTransfer");
                request.Info = searchParam;

                var resultList = await this.Post<StockTransferSearchParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                if (resultList == null || resultList.SubData == null)
                    return;

                foreach (var item in resultList.SubData)
                {
                    if (item?.SubName == "PH_LOCATION" && item.SubJson != null)
                    {
                        localist = BaseUtils.JsonToClass<List<LocationData>>(item.SubJson) ?? new List<LocationData>();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                localist = new List<LocationData>();
                System.Diagnostics.Debug.WriteLine($"GetLocaData Error: {ex.Message}");
            }
        }

        // ==========================================
        // 保存用パラメータ
        // ==========================================
        public class StockTransferSaveParam : EvangJsonModel
        {
            public string Kbn { get; set; } = "savedata";

            public int? FromLocationId { get; set; }
            public int? ToLocationId { get; set; }

            public List<StockTransferDetailParam> Details { get; set; } = new();
        }

        public class StockTransferDetailParam
        {
            public string ItemCode { get; set; } = "";
            public bool IsLotItem { get; set; }
            public int TotalQty { get; set; }
            public List<PendingLotItem> Lots { get; set; } = new();
        }

        // ==========================================
        // データモデル
        // ==========================================
        public class PendingLotItem
        {
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }

        public class TransferDetailItem
        {
            public string ItemCode { get; set; } = "";
            public LocationData? FromLocation { get; set; }
            public LocationData? ToLocation { get; set; }
            public bool IsLotItem { get; set; }
            public List<PendingLotItem> Lots { get; set; } = new List<PendingLotItem>();

            public int TotalQty => Lots.Sum(x => x.Qty);
        }

        public class StockTransferSearchParam : EvangJsonModel
        {
            public string Kbn { get; set; } = "";
            public int Id { get; set; }
            public string? Keyword { get; set; }
            public int? FromLocationId { get; set; }
            public int? ToLocationId { get; set; }
            public List<PendingLotItem>? Lots { get; set; }
        }

        public class LocationData : EvangJsonModel
        {
            public int? id { get; set; }
            public string? name { get; set; }
        }
    }
}