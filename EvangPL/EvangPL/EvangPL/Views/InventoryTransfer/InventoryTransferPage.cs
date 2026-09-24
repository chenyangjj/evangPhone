using EvangPL.Components;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System;

namespace EvangPL.Views.InventoryTransfer
{
    /// <summary>
    /// 画面9: 在庫振替 - 一覧 (Inventory Transfer List)
    /// レイアウト: 検索エリア + 新規登録ボタン + ページング + 振替レコードカード
    /// </summary>
    public class InventoryTransfer : EvangContentVM
    {
        // ==========================================
        // Androidネイティブの下線消去用Handler登録
        // ==========================================
        static InventoryTransfer()
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

            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
        }

        // ==========================================
        // UIコントロール宣言
        // ==========================================
        private DatePicker? startDatePicker;
        private DatePicker? endDatePicker;
        private Picker? sourceLocationPicker;
        private Picker? destLocationPicker;
        private Entry? itemKeywordEntry;
        private Button? newRegisterButton;

        private VerticalStackLayout? contentLayout;

        // ページング関連
        private Grid? paginationLayout;
        private Button? prevButton;
        private Button? nextButton;
        private Label? pageLabel;

        // ==========================================
        // データ・状態管理
        // ==========================================
        private List<TransferRecord>? allData;
        private int currentPage = 0;
        private int pageSize = 4;
        private int totalPages = 0;

        private CancellationTokenSource? _keywordCts;
        private SearchParam SearchCondition;
        private List<LocationData> localist;

        public static bool NeedRefreshAfterSave = false;

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (NeedRefreshAfterSave)
            {
                NeedRefreshAfterSave = false;
                _ = GetMockData("data");
            }
        }

        public InventoryTransfer() : base("strInventoryTransfer", null)
        {
            SearchCondition = new SearchParam();
            Title = "在庫振替 - 一覧";
            BuildUI();
        }

        /// <summary>
        /// UI全体の構築
        /// </summary>
        private async void BuildUI()
        {
            await GetLocaData("locadata");
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                },
                BackgroundColor = Color.FromArgb("#eff1f5")
            };

            // 上部: 検索条件 + ボタン + ページング
            var topSection = CreateTopSection();
            mainGrid.Add(topSection, 0, 0);

            contentLayout = new VerticalStackLayout
            {
                Padding = new Thickness(15, 10, 15, 20),
                Spacing = 10
            };

            var scrollView = new ScrollView
            {
                Content = contentLayout
            };

            mainGrid.Add(scrollView, 0, 1);

            Content = new Border
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Content = mainGrid
            };
            await GetMockData("data");
        }

        /// <summary>
        /// 上部セクション（検索条件、新規登録ボタン、ページング）の生成
        /// </summary>
        private VerticalStackLayout CreateTopSection()
        {
            var layout = new VerticalStackLayout
            {
                Padding = new Thickness(15, 10),
                Spacing = 10,
                BackgroundColor = Colors.Transparent
            };

            // --- Row 1: ロケーション選択 ---
            var row1 = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
                ColumnSpacing = 10
            };

            // 移動元ロケーション
            var srcLayout = new VerticalStackLayout { Spacing = 4 };
            srcLayout.Children.Add(new Label { Text = "移動元ロケーション", FontSize = 11, TextColor = Colors.Gray });
            sourceLocationPicker = new Picker { Title = "すべて", BackgroundColor = Colors.Transparent, HeightRequest = 35 };
            for (int i = 0; i < localist.Count; i++)
            {
                sourceLocationPicker.Items.Add(localist[i].name);
            }
            sourceLocationPicker.SelectedIndex = 0;
            sourceLocationPicker.SelectedIndexChanged += async (sender, e) =>
            {
                if (sourceLocationPicker.SelectedIndex >= 0)
                    sourceLocationPicker.Title = sourceLocationPicker.SelectedItem?.ToString();
                else
                    sourceLocationPicker.Title = "移動元を選択";

                if (sourceLocationPicker.SelectedIndex != -1)
                {
                    string selectedName = sourceLocationPicker.Items[sourceLocationPicker.SelectedIndex];
                    var loc = localist.FirstOrDefault(l => l.name == selectedName);
                    if (loc != null)
                        SearchCondition.SourceId = loc.id;
                }
                await GetMockData("data");
            };
            srcLayout.Children.Add(CreateInputBorder(sourceLocationPicker));
            row1.Add(srcLayout, 0, 0);

            // 移動先ロケーション
            var dstLayout = new VerticalStackLayout { Spacing = 4 };
            dstLayout.Children.Add(new Label { Text = "移動先ロケーション", FontSize = 11, TextColor = Colors.Gray });
            destLocationPicker = new Picker { Title = "すべて", BackgroundColor = Colors.Transparent, HeightRequest = 35 };
            for (int j = 0; j < localist.Count; j++)
            {
                destLocationPicker.Items.Add(localist[j].name);
            }
            destLocationPicker.SelectedIndex = 0;
            destLocationPicker.SelectedIndexChanged += async (sender, e) =>
            {
                if (destLocationPicker.SelectedIndex >= 0)
                    destLocationPicker.Title = destLocationPicker.SelectedItem?.ToString();
                else
                    destLocationPicker.Title = "移動先を選択";

                if (destLocationPicker.SelectedIndex != -1)
                {
                    string selectedName = destLocationPicker.Items[destLocationPicker.SelectedIndex];
                    var loc = localist.FirstOrDefault(l => l.name == selectedName);
                    if (loc != null)
                        SearchCondition.DestId = loc.id;
                }
                await GetMockData("data");
            };
            dstLayout.Children.Add(CreateInputBorder(destLocationPicker));
            row1.Add(dstLayout, 1, 0);

            layout.Children.Add(row1);

            // --- Row 2: 対象期間 & 品目キーワード ---
            var row2 = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
                ColumnSpacing = 10
            };

            // 対象期間
            var dateLayout = new VerticalStackLayout { Spacing = 4 };
            dateLayout.Children.Add(new Label { Text = "対象期間:", FontSize = 11, TextColor = Colors.Gray });

            var dateRangeGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 5,
                VerticalOptions = LayoutOptions.Center
            };

            startDatePicker = new DatePicker
            {
                Date = DateTime.Today.AddDays(-7),
                BackgroundColor = Colors.Transparent,
                HeightRequest = 36,
                Format = "MM/dd",
                TextColor = Colors.Black,
                FontSize = 10,
                Margin = new Thickness(8, 0)
            };
            var startBorder = CreateInputBorder(startDatePicker);
            Grid.SetColumn(startBorder, 0);
            dateRangeGrid.Children.Add(startBorder);

            var separator = new Label
            {
                Text = "~",
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                FontSize = 14,
                TextColor = Colors.Gray
            };
            Grid.SetColumn(separator, 1);
            dateRangeGrid.Children.Add(separator);

            endDatePicker = new DatePicker
            {
                Date = DateTime.Today,
                BackgroundColor = Colors.Transparent,
                HeightRequest = 36,
                Format = "MM/dd",
                TextColor = Colors.Black,
                FontSize = 10,
                Margin = new Thickness(8, 0)
            };
            var endBorder = CreateInputBorder(endDatePicker);
            Grid.SetColumn(endBorder, 2);
            dateRangeGrid.Children.Add(endBorder);

            dateLayout.Children.Add(dateRangeGrid);
            row2.Add(dateLayout, 0, 0);

            // 日付変更時に検索条件を更新
            async Task UpdateDateRange()
            {
                SearchCondition.DateRangeFrom = startDatePicker.Date.ToString("yyyy/MM/dd");
                SearchCondition.DateRangeTo = endDatePicker.Date.ToString("yyyy/MM/dd");
                await GetMockData("data");
            }
            startDatePicker.DateSelected += (s, e) => UpdateDateRange();
            endDatePicker.DateSelected += (s, e) => UpdateDateRange();

            // 品目キーワード
            var kwLayout = new VerticalStackLayout { Spacing = 4 };
            kwLayout.Children.Add(new Label { Text = "品目キーワード", FontSize = 11, TextColor = Colors.Gray });
            itemKeywordEntry = new Entry
            {
                Placeholder = "検索キーワード",
                BackgroundColor = Colors.Transparent,
                HeightRequest = 35,
                TextColor = Colors.Black,
                FontSize = 13,
                PlaceholderColor = Colors.Gray
            };
            itemKeywordEntry.TextChanged += async (s, e) =>
            {
                SearchCondition.Keyword = e.NewTextValue;
                _keywordCts?.Cancel();
                _keywordCts = new CancellationTokenSource();
                var token = _keywordCts.Token;

                try
                {
                    await Task.Delay(1000, token);
                    if (token.IsCancellationRequested) return;
                    await GetMockData("data");
                }
                catch (TaskCanceledException) { }
            };
            var kwRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },   // 入力欄
                    new ColumnDefinition { Width = 50 }                  // スキャンアイコン幅
                },
                ColumnSpacing = 10
            };
            kwRow.Add(CreateInputBorder(itemKeywordEntry), 0, 0);

            var itemScanBorder = BuildBarcodeIcon();
            var itemScanTap = new TapGestureRecognizer();
            itemScanTap.Tapped += async (s, e) =>
            {
                await ScanHelper.ScanAsync(Navigation, (scannedCode) =>
                {
                    if (string.IsNullOrEmpty(scannedCode)) return;

                    Dispatcher.Dispatch(() =>
                    {
                        if (itemKeywordEntry != null)
                        {
                            // スキャン結果を品目キーワード欄へ反映
                            // TextChanged が発火して 1秒デバウンス後に自動検索が走る
                            itemKeywordEntry.Text = scannedCode;
                        }
                    });
                });
            };
            itemScanBorder.GestureRecognizers.Add(itemScanTap);
            kwRow.Add(itemScanBorder, 1, 0);

            kwLayout.Children.Add(kwRow);
            row2.Add(kwLayout, 1, 0);

            layout.Children.Add(row2);

            // --- Row 3: 新規登録ボタン ---
            newRegisterButton = new Button
            {
                Text = "+ 新規登録",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                HeightRequest = 40,
                CornerRadius = 6,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14
            };
            newRegisterButton.Clicked += OnNewRegisterClicked;
            layout.Children.Add(newRegisterButton);

            // --- Row 4: ページング ---
            CreatePaginationControls();
            layout.Children.Add(paginationLayout!);

            return layout;
        }

        /// <summary>
        /// 入力枠の生成
        /// </summary>
        private Border CreateInputBorder(View content)
        {
            return new Border
            {
                Stroke = Color.FromArgb("#cccccc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                BackgroundColor = Colors.White,
                Padding = new Thickness(8, 0),
                Content = content,
                HeightRequest = 38
            };
        }

        /// <summary>
        /// ページングコントロールの生成
        /// </summary>
        private void CreatePaginationControls()
        {
            paginationLayout = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                Margin = new Thickness(0, 5, 0, 0)
            };

            prevButton = new Button
            {
                Text = "◀ 前へ",
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#e0e0e0"),
                TextColor = Colors.Gray,
                CornerRadius = 5,
                HeightRequest = 35,
                IsEnabled = false
            };
            prevButton.Clicked += OnPrevClicked;
            paginationLayout.Add(prevButton, 0, 0);

            pageLabel = new Label
            {
                Text = "1 / 1",
                FontSize = 13,
                TextColor = Colors.Black,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            paginationLayout.Add(pageLabel, 1, 0);

            nextButton = new Button
            {
                Text = "次へ ▶",
                FontSize = 12,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#1f3854"),
                BorderColor = Color.FromArgb("#1f3854"),
                BorderWidth = 1,
                CornerRadius = 5,
                HeightRequest = 35
            };
            nextButton.Clicked += OnNextClicked;
            paginationLayout.Add(nextButton, 2, 0);
        }

        // ==========================================
        // イベントハンドラ
        // ==========================================

        private async void OnNewRegisterClicked(object sender, EventArgs e)
        {
            try
            {
                var detailPage = new EvangPL.Views.InventoryTransferPageDetails.InventoryTransferPageDetails();
                await Navigation.PushAsync(detailPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
            }
        }

        private void OnPrevClicked(object sender, EventArgs e)
        {
            if (currentPage > 0) LoadPage(currentPage - 1);
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            if (currentPage < totalPages - 1) LoadPage(currentPage + 1);
        }

        // ==========================================
        // データ表示ロジック
        // ==========================================

        private void ShowData(List<TransferRecord> data)
        {
            allData = data;
            currentPage = 0;
            totalPages = (int)Math.Ceiling((double)data.Count / pageSize);
            if (totalPages == 0) totalPages = 1;

            LoadPage(0);
        }

        private void LoadPage(int pageIndex)
        {
            if (allData == null) return;

            currentPage = pageIndex;
            var pageData = allData.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            UpdateListUI(pageData);
            UpdatePaginationUI();
        }

        /// <summary>
        /// リストUIの更新（カード形式で描画）
        /// </summary>
        private void UpdateListUI(List<TransferRecord> data)
        {
            if (contentLayout == null) return;
            contentLayout.Children.Clear();

            if (data.Count == 0)
            {
                contentLayout.Children.Add(new Label
                {
                    Text = "検索条件に一致するデータはありません。",
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            foreach (var item in data)
            {
                var card = CreateTransferCard(item);
                contentLayout.Children.Add(card);
            }
        }

        /// <summary>
        /// 在庫振替レコードカードの生成
        /// StockAdjustと同じFrame+Gridレイアウトを採用（主従構造対応）
        /// </summary>
        /// <summary>
        /// 在庫振替レコードカードの生成
        /// StockAdjustと同じFrame+Gridレイアウトを採用（主従構造対応）
        /// </summary>
        private Frame CreateTransferCard(TransferRecord record)
        {
            var cardFrame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                HasShadow = false
            };

            var mainStack = new StackLayout { Spacing = 6 };

            // ===== ヘッダー行: 振替No + 登録日 =====
            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var lblTransferNo = new Label
            {
                Text = record.Id,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1f3854"),
                VerticalTextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(lblTransferNo, 0);

            var lblDate = new Label
            {
                Text = $"登録日: {record.RegisterDate}",
                FontSize = 12,
                TextColor = Colors.Gray,
                VerticalTextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(lblDate, 1);

            headerGrid.Children.Add(lblTransferNo);
            headerGrid.Children.Add(lblDate);
            mainStack.Children.Add(headerGrid);

            // ===== 移動元 → 移動先（セパレータの上に配置） =====
            var lblRoute = new Label
            {
                Text = $"移動元: {record.Source} → 移動先: {record.Dest}",
                FontSize = 12,
                TextColor = Colors.DimGray
            };
            mainStack.Children.Add(lblRoute);

            // ===== セパレータ =====
            mainStack.Children.Add(new BoxView
            {
                HeightRequest = 1,
                Color = Color.FromArgb("#eeeeee"),
                Margin = new Thickness(0, 4, 0, 4)
            });

            // ===== 明細リスト（品目コード + 数量） =====
            if (record.Details != null && record.Details.Count > 0)
            {
                foreach (var detail in record.Details)
                {
                    var detailGrid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        RowSpacing = 2,
                        Padding = new Thickness(0, 2)
                    };

                    // 品目コード
                    var lblItem = new Label
                    {
                        Text = $"品目: {detail.ItemCode}",
                        FontSize = 12,
                        TextColor = Colors.Black
                    };
                    Grid.SetColumn(lblItem, 0);

                    // 数量（「数量:」プレフィックス付き）
                    var lblQty = new Label
                    {
                        Text = $"数量: {detail.Qty}",
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#2e7d32"),
                        HorizontalTextAlignment = TextAlignment.End
                    };
                    Grid.SetColumn(lblQty, 1);

                    detailGrid.Children.Add(lblItem);
                    detailGrid.Children.Add(lblQty);

                    mainStack.Children.Add(detailGrid);
                }
            }
            else
            {
                // 明細がない場合のフォールバック表示
                mainStack.Children.Add(new Label
                {
                    Text = "品目: （明細なし）",
                    FontSize = 13,
                    TextColor = Colors.Gray
                });
            }

            cardFrame.Content = mainStack;

            // タップイベント（詳細画面へ遷移）
            //var tap = new TapGestureRecognizer();
            //tap.Tapped += async (_, _) =>
            //{
            //    try
            //    {
            //        var editPage = new EvangPL.Views.InventoryTransferPageDetails.InventoryTransferPageDetails(record);
            //        await Navigation.PushAsync(editPage);
            //    }
            //    catch (Exception ex)
            //    {
            //        await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
            //    }
            //};
            //cardFrame.GestureRecognizers.Add(tap);

            return cardFrame;
        }

        /// <summary>
        /// ページングUIの状態更新
        /// </summary>
        private void UpdatePaginationUI()
        {
            if (pageLabel == null || prevButton == null || nextButton == null) return;

            pageLabel.Text = $"{currentPage + 1} / {totalPages}";

            prevButton.IsEnabled = currentPage > 0;
            nextButton.IsEnabled = currentPage < totalPages - 1;

            // ボタンのスタイル切り替え
            prevButton.BackgroundColor = prevButton.IsEnabled ? Colors.White : Color.FromArgb("#e0e0e0");
            prevButton.TextColor = prevButton.IsEnabled ? Color.FromArgb("#1f3854") : Colors.Gray;
            prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#1f3854") : Colors.Transparent;
            prevButton.BorderWidth = prevButton.IsEnabled ? 1 : 0;

            nextButton.BackgroundColor = nextButton.IsEnabled ? Colors.White : Color.FromArgb("#e0e0e0");
            nextButton.TextColor = nextButton.IsEnabled ? Color.FromArgb("#1f3854") : Colors.Gray;
            nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#1f3854") : Colors.Transparent;
            nextButton.BorderWidth = nextButton.IsEnabled ? 1 : 0;
        }

        // ==========================================
        // データ取得ロジック
        // ==========================================

        /// <summary>
        /// 在庫振替データの取得
        /// </summary>
        private async Task GetMockData(string kbn)
        {
            try
            {
                if (string.IsNullOrEmpty(SearchCondition.DateRangeFrom))
                    SearchCondition.DateRangeFrom = startDatePicker.Date.ToString("yyyy/MM/dd");

                if (string.IsNullOrEmpty(SearchCondition.DateRangeTo))
                    SearchCondition.DateRangeTo = endDatePicker.Date.ToString("yyyy/MM/dd");

                if (string.Compare(SearchCondition.DateRangeFrom, SearchCondition.DateRangeTo) > 0)
                {
                    await DisplayAlert("エラー", "対象期間FROMは対象期間TOより大きくすることはできません。", "OK");
                    return;
                }

                var searchParam = SearchCondition;
                searchParam.Kbn = kbn;
                var request = new RequestData<SearchParam, EvangJsonModel>("GetTransfer");
                request.Info = searchParam;
                var resultList = await this.Post<SearchParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                if (resultList == null || resultList.SubData == null)
                    return;

                foreach (var item in resultList.SubData)
                {
                    switch (item.SubName)
                    {
                        case "PH_LOCATION":
                            if (item == null || item.SubJson == null) return;
                            localist = BaseUtils.JsonToClass<List<LocationData>>(item.SubJson);
                            break;
                        case "PH_DATA":
                            if (item == null || item.SubJson == null) return;
                            allData = BaseUtils.JsonToClass<List<TransferRecord>>(item.SubJson);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                allData = new List<TransferRecord>();
                System.Diagnostics.Debug.WriteLine($"GetMockData Error: {ex.Message}");
            }
            ShowData(allData);
        }

        /// <summary>
        /// ロケーションマスタデータの取得
        /// </summary>
        private async Task GetLocaData(string kbn)
        {
            try
            {
                var searchParam = SearchCondition;
                searchParam.Kbn = kbn;
                var request = new RequestData<SearchParam, EvangJsonModel>("GetTransfer");
                request.Info = searchParam;
                var resultList = await this.Post<SearchParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                if (resultList == null || resultList.SubData == null)
                    return;

                foreach (var item in resultList.SubData)
                {
                    switch (item.SubName)
                    {
                        case "PH_LOCATION":
                            if (item == null || item.SubJson == null) return;
                            localist = BaseUtils.JsonToClass<List<LocationData>>(item.SubJson);
                            break;
                        case "PH_DATA":
                            if (item == null || item.SubJson == null) return;
                            allData = BaseUtils.JsonToClass<List<TransferRecord>>(item.SubJson);
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

        /// <summary>
        /// 白黒のバーコードアイコン（他画面と統一）
        /// </summary>
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
    }

    // ==========================================
    // データモデル定義
    // ==========================================

    /// <summary>
    /// 在庫振替 検索条件モデル
    /// </summary>
    public class SearchParam : EvangJsonModel
    {
        public string Kbn { get; set; } = "";
        public int? SourceId { get; set; }
        public int? DestId { get; set; }
        public string DateRangeFrom { get; set; } = "";
        public string DateRangeTo { get; set; } = "";
        public string Keyword { get; set; } = "";
    }

    /// <summary>
    /// 在庫振替 レコードモデル
    /// </summary>
    public class TransferRecord : EvangJsonModel
    {
        public string Id { get; set; } = "";
        public string Source { get; set; } = "";
        public string Dest { get; set; } = "";
        public string RegisterDate { get; set; } = "";
        public int InId { get; set; }
        public int SourceId { get; set; }
        public int DestId { get; set; }

        /// <summary>
        /// 振替明細リスト（品目コード・数量の配列）
        /// </summary>
        public List<TransferDetailItem> Details { get; set; } = new();
    }

    /// <summary>
    /// 在庫振替 明細アイテム
    /// </summary>
    public class TransferDetailItem : EvangJsonModel
    {
        public string ItemCode { get; set; } = "";
        public int Qty { get; set; }
    }

    /// <summary>
    /// ロケーション マスタモデル
    /// </summary>
    public class LocationData : EvangJsonModel
    {
        public int? id { get; set; }
        public string? name { get; set; }
    }
}