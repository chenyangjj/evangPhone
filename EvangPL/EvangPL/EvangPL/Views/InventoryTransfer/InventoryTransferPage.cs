using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using System;

namespace EvangPL.Views.InventoryTransfer
{
    /// <summary>
    /// 画面9: 在庫振替 - 一覧 (Inventory Transfer List)
    /// 注意：虽然菜单叫 InventoryAdjustment，但根据设计图这是“在庫振替”列表。
    /// 如果菜单跳转逻辑是 View = "InventoryAdjustment"，则文件名和类名需保持一致。
    /// </summary>
    public class InventoryTransfer : EvangContentVM
    {
        // ==========================================
        // [追加] Android原生の下線を消去するためのHandler登録
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
        // UI コントロール宣言
        // ==========================================
        private DatePicker? startDatePicker;
        private DatePicker? endDatePicker;
        private Picker? sourceLocationPicker;
        private Picker? destLocationPicker;
        private Entry? dateRangeEntry; // 設計図に合わせて単一Entryで表現 (またはDatePicker2つ)
        private Entry? itemKeywordEntry;
        private Button? newRegisterButton;

        private VerticalStackLayout? contentLayout;
        private VerticalStackLayout? listContainer;

        // ページング
        private Grid? paginationLayout;
        private Button? prevButton;
        private Button? nextButton;
        private Label? pageLabel;

        // ==========================================
        // データ・状態管理
        // ==========================================
        private List<TransferRecord>? allData;
        private int currentPage = 0;
        private int pageSize = 4; // 設計図のスクリーンサイズに合わせて調整
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
            //_ = GetMockData("data");

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
                    new RowDefinition { Height = GridLength.Auto }, // 検索エリア + ボタン + ページング
                    new RowDefinition { Height = GridLength.Star }  // リストエリア
                },
                BackgroundColor = Color.FromArgb("#eff1f5")
            };

            // 上部：検索条件 + ボタン + ページング
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
            //ShowData(allData);
        }

        /// <summary>
        /// 上部セクション（検索条件、新規登録ボタン、ページング）
        /// </summary>
        private VerticalStackLayout CreateTopSection()
        {
            var layout = new VerticalStackLayout
            {
                Padding = new Thickness(15, 10),
                Spacing = 10,
                BackgroundColor = Colors.White // 上部は白背景にするか、デザイン図に合わせてグレーのままか。デザイン図は全体グレーっぽいが、入力欄は白。
            };
            // デザイン図を見ると、背景は薄いグレー(#eff1f5)で、入力欄とカードが白。
            layout.BackgroundColor = Colors.Transparent;

            // --- Row 1: ロケーション選択 ---
            var row1 = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
                ColumnSpacing = 10
            };

            // 移動元
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
                {
                    sourceLocationPicker.Title = sourceLocationPicker.SelectedItem?.ToString();
                }
                else
                {
                    sourceLocationPicker.Title = "移動元を選択";
                }
                if (sourceLocationPicker.SelectedIndex != -1)
                {
                    string selectedName = sourceLocationPicker.Items[sourceLocationPicker.SelectedIndex];
                    var loc = localist.FirstOrDefault(l => l.name == selectedName);

                    if (loc != null)
                    {
                        SearchCondition.SourceId = loc.id;
                    }
                }
                await GetMockData("data");
            };
            srcLayout.Children.Add(CreateInputBorder(sourceLocationPicker));
            row1.Add(srcLayout, 0, 0);

            // 移動先
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
                {
                    destLocationPicker.Title = destLocationPicker.SelectedItem?.ToString();
                }
                else
                {
                    destLocationPicker.Title = "移動先を選択";
                }
                if (destLocationPicker.SelectedIndex != -1)
                {
                    string selectedName = destLocationPicker.Items[destLocationPicker.SelectedIndex];
                    var loc = localist.FirstOrDefault(l => l.name == selectedName);

                    if (loc != null)
                    {
                        SearchCondition.DestId = loc.id;
                    }
                }
                await GetMockData("data");
            };
            dstLayout.Children.Add(CreateInputBorder(destLocationPicker));
            row1.Add(dstLayout, 1, 0);

            layout.Children.Add(row1);

            // --- Row 2: 期間 & キーワード ---
            var row2 = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
                ColumnSpacing = 10
            };

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

            // 日付変更時に SearchCondition.DateRange を更新
            async Task UpdateDateRange()
            {
                SearchCondition.DateRangeFrom = startDatePicker.Date.ToString("yyyy/MM/dd");
                SearchCondition.DateRangeTo = endDatePicker.Date.ToString("yyyy/MM/dd");
                await GetMockData("data");
            }
            startDatePicker.DateSelected += (s, e) => UpdateDateRange();
            endDatePicker.DateSelected += (s, e) => UpdateDateRange();
            //UpdateDateRange(); // 初期値セット

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
                    await Task.Delay(1000, token);   // 1000ms
                    if (token.IsCancellationRequested) return;
                    await GetMockData("data");
                }
                catch (TaskCanceledException)
                {
                }
            };
            kwLayout.Children.Add(CreateInputBorder(itemKeywordEntry));
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
        /// 入力枠の作成
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
        /// ページングコントロール
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

        private void UpdateListUI(List<TransferRecord> data)
        {
            if (contentLayout == null) return;

            // 既存のリストをクリア (ページング部分以外のコンテンツをクリアする必要があるが、
            // ここでは contentLayout 全体をリスト専用としているため Clear でOK)
            // 注意: BuildUI で contentLayout を ScrollView の Content にしているため、ここにはカードだけを入れる
            contentLayout.Children.Clear();

            if (data.Count == 0)
            {
                contentLayout.Children.Add(new Label
                {
                    Text = "データがありません",
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(0, 20)
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
        /// 一覧カードの作成
        /// </summary>
        private Border CreateTransferCard(TransferRecord record)
        {
            var stack = new VerticalStackLayout
            {
                Spacing = 4,
                Padding = new Thickness(12, 10)
            };

            // TR-0031
            stack.Children.Add(new Label
            {
                Text = record.Id,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1f3854")
            });

            // 移動元: ... -> 移動先: ...
            stack.Children.Add(new Label
            {
                Text = $"移動元: {record.Source} → 移動先: {record.Dest}",
                FontSize = 12,
                TextColor = Colors.DimGray
            });

            // 対象品目数: 3
            stack.Children.Add(new Label
            {
                Text = $"対象品目数: {record.ItemCount}",
                FontSize = 12,
                TextColor = Colors.DimGray
            });

            // 登録日: 2026-07-06
            stack.Children.Add(new Label
            {
                Text = $"登録日: {record.RegisterDate}",
                FontSize = 12,
                TextColor = Colors.DimGray
            });

            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#dddddd"),
                StrokeThickness = 1,
                Content = stack,
                Padding = 0 // StackLayout に Padding を持たせているため
            };

            // タップイベント (詳細画面へ)
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                try
                {
                    var editPage = new EvangPL.Views.InventoryTransferPageDetails.InventoryTransferPageDetails(record);
                    await Navigation.PushAsync(editPage);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
                }
            };
            border.GestureRecognizers.Add(tap);

            return border;
        }

        private void UpdatePaginationUI()
        {
            if (pageLabel == null || prevButton == null || nextButton == null) return;

            pageLabel.Text = $"{currentPage + 1} / {totalPages}";

            prevButton.IsEnabled = currentPage > 0;
            nextButton.IsEnabled = currentPage < totalPages - 1;

            // ボタンの色調整
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
        // モックデータ
        // ==========================================

        private async Task GetMockData(string kbn)
        {
            try
            {
                if (string.IsNullOrEmpty(SearchCondition.DateRangeFrom))
                {
                    SearchCondition.DateRangeFrom = startDatePicker.Date.ToString("yyyy/MM/dd");
                }
                if (string.IsNullOrEmpty(SearchCondition.DateRangeTo))
                {
                    SearchCondition.DateRangeTo = endDatePicker.Date.ToString("yyyy/MM/dd");
                }
                if (string.Compare(SearchCondition.DateRangeFrom, SearchCondition.DateRangeTo) > 0)
                {
                    await DisplayAlert("エーラ", "対象期間FROMは対象期間TOより大きくすることはできません。", "OK");
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
                            if (item == null || item.SubJson == null)
                                return;
                            localist = BaseUtils.JsonToClass<List<LocationData>>(item.SubJson);
                            break;
                        case "PH_DATA":
                            if (item == null || item.SubJson == null)
                                return;
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
                            if (item == null || item.SubJson == null)
                                return;
                            localist = BaseUtils.JsonToClass<List<LocationData>>(item.SubJson);
                            break;
                        case "PH_DATA":
                            if (item == null || item.SubJson == null)
                                return;
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

    }

    public class SearchParam : EvangJsonModel
    {
        public string Kbn { get; set; } = "";
        public int? SourceId { get; set; }
        public int? DestId { get; set; }
        public string DateRangeFrom { get; set; } = "";
        public string DateRangeTo { get; set; } = "";
        public string Keyword { get; set; } = "";
    }

    // ==========================================
    // データモデル
    // ==========================================
    public class TransferRecord : EvangJsonModel
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Dest { get; set; }
        public int ItemCount { get; set; }
        public string RegisterDate { get; set; }
        public int InId { get; set; }
        public int SourceId { get; set; }
        public int DestId { get; set; }

        //public TransferRecord(string id, string source, string dest, int itemCount, string registerDate)
        //{
        //    Id = id;
        //    Source = source;
        //    Dest = dest;
        //    ItemCount = itemCount;
        //    RegisterDate = registerDate;
        //}
    }

    public class LocationData : EvangJsonModel
    {
        public int? id { get; set; }
        public string? name { get; set; }
    }
}