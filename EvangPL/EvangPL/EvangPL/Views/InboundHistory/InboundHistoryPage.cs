using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EvangPL.Views.InboundHistory
{
    /// <summary>
    /// 画面14: 入庫履歴レポート ViewModel
    /// </summary>
    public class InboundHistory : EvangContentVM
    {
        static InboundHistory()
        {
            // 下線を除去
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

            // 下線を除去
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

            // 下線を除去
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
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

        private Picker? locationPicker;
        private Entry? itemKeywordEntry;
        private Button? searchButton;
        private VerticalStackLayout? contentLayout;

        // 結果一覧セクション
        private VerticalStackLayout? resultSection;
        private Grid? paginationLayout;
        private Button? prevButton;
        private Button? nextButton;
        private Label? pageLabel;
        private Label? totalLabel;

        // ==========================================
        // データ・状態管理
        // ==========================================
        private List<InboundRecord>? allInboundData;
        private int currentPage = 0;
        private int pageSize = 5;
        private int totalPages = 0;

        private VerticalStackLayout? tableContainer;

        private List<LocationData> localist;
        private int? locationId = 1;

        public InboundHistory() : base("strInboundHistory")
        {
            Title = "入庫履歴レポート";
            BuildUI();
        }

        /// <summary>
        /// UI全体の構築
        /// </summary>
        private async void BuildUI()
        {
            await getdata();
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                }
            };

            // 検索条件エリア
            var filterFrame = CreateFilterFrame();
            mainGrid.Add(filterFrame, 0, 0);

            // 結果表示エリア（ScrollView）
            var scrollView = CreateContentContainer();
            mainGrid.Add(scrollView, 0, 1);

            Content = new Border
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Content = mainGrid
            };
        }

        /// <summary>
        /// 結果表示用コンテナの作成
        /// </summary>
        private ScrollView CreateContentContainer()
        {
            contentLayout = new VerticalStackLayout
            {
                Padding = new Thickness(2),
                Margin = new Thickness(0)
            };

            return new ScrollView
            {
                Content = contentLayout
            };
        }

        /// <summary>
        /// 検索条件フレームの作成
        /// </summary>
        private Grid CreateFilterFrame()
        {
            var searchFilterGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#f5f5f5"),
                //Padding = new Thickness(10),
                //Margin = new Thickness(10, 0, 10, 0)
            };

            var filterLayout = new VerticalStackLayout
            {
                Spacing = 8
            };

            // --- 対象期間 & ロケーション ---
            var row1Grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10
            };

            var dateLayout = new VerticalStackLayout { Spacing = 4 };
            dateLayout.Children.Add(new Label { Text = "対象期間:", FontSize = 13, TextColor = Colors.Gray });

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
                Margin = new Thickness(8, 0)
            };
            var endBorder = CreateInputBorder(endDatePicker);
            Grid.SetColumn(endBorder, 2);
            dateRangeGrid.Children.Add(endBorder);

            dateLayout.Children.Add(dateRangeGrid);
            row1Grid.Children.Add(dateLayout);
            Grid.SetColumn(dateLayout, 0);

            var locLayout = new VerticalStackLayout { Spacing = 4 };
            locLayout.Children.Add(new Label { Text = "ロケーション:", FontSize = 13, TextColor = Colors.Gray });

            locationPicker = new Picker
            {
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 36,
                Title = "WH1",
                Margin = new Thickness(8, 0)
            };
            for (int i = 0; i < localist.Count; i++)
            {
                locationPicker.Items.Add(localist[i].name);
            }
            //locationPicker.Items.Add("WH1");
            //locationPicker.Items.Add("WH2");
            locationPicker.SelectedIndex = 0;
            locationPicker.SelectedIndexChanged += (s, e) =>
            {
                if (locationPicker.SelectedIndex >= 0)
                    locationPicker.Title = locationPicker.SelectedItem?.ToString();
                else
                    locationPicker.Title = "ロケーションを選択";
                if (locationPicker.SelectedIndex != -1)
                {
                    string selectedName = locationPicker.Items[locationPicker.SelectedIndex];
                    var loc = localist.FirstOrDefault(l => l.name == selectedName);

                    if (loc != null)
                    {
                        locationId = loc.id;
                    }
                }
            };

            var locBorder = CreateInputBorder(locationPicker);
            locLayout.Children.Add(locBorder);
            row1Grid.Children.Add(locLayout);
            Grid.SetColumn(locLayout, 1);

            filterLayout.Children.Add(row1Grid);

            var itemLayout = new VerticalStackLayout { Spacing = 4 };
            itemLayout.Children.Add(new Label { Text = "品目 (スキャン可):", FontSize = 13, TextColor = Colors.Gray });

            itemKeywordEntry = new Entry
            {
                Placeholder = "検索キーワードを入力",
                BackgroundColor = Colors.Transparent,
                HeightRequest = 36,
                TextColor = Colors.Black,
                PlaceholderColor = Colors.Gray,
                Margin = new Thickness(10, 0)
            };
            var itemBorder = CreateInputBorder(itemKeywordEntry);

            var itemRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },   // 入力欄
                    new ColumnDefinition { Width = 50 }                  // スキャンアイコン幅
                },
                ColumnSpacing = 10
            };
            itemRow.Add(itemBorder, 0, 0);

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
                            itemKeywordEntry.Text = scannedCode;
                        }
                    });
                });
            };
            itemScanBorder.GestureRecognizers.Add(itemScanTap);
            itemRow.Add(itemScanBorder, 1, 0);

            itemLayout.Children.Add(itemRow);

            filterLayout.Children.Add(itemLayout);

            // --- 検索ボタン ---
            searchButton = new Button
            {
                Text = "検　索",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                HeightRequest = 45,
                CornerRadius = 5,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 5, 0, 0)
            };
            searchButton.Clicked += async (sender, e) => await OnbtnSearchClicked(sender, e);
            filterLayout.Children.Add(searchButton);

            // --- 結果一覧タイトル ---
            var resultTitle = new Label
            {
                Text = "結果一覧",
                FontSize = 15,
                TextColor = Color.FromArgb("#1f3854"),
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            filterLayout.Children.Add(resultTitle);

            // --- ページングコントロール ---
            CreatePaginationControls();
            filterLayout.Children.Add(paginationLayout!);

            searchFilterGrid.Children.Add(filterLayout);
            return searchFilterGrid;
        }

        /// <summary>
        /// 角丸ボーダー付きの入力枠を作成するヘルパーメソッド
        /// </summary>
        private Border CreateInputBorder(View content)
        {
            return new Border
            {
                Stroke = Color.FromArgb("#cccccc"),       // 薄いグレーの枠線
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 }, // 角丸
                BackgroundColor = Colors.White,           
                Padding = 0,
                Content = content,
                HeightRequest = 40                        // 全体の高さを統一
            };
        }

        /// <summary>
        /// ページングコントロールの作成
        /// </summary>
        private void CreatePaginationControls()
        {
            paginationLayout = new Grid
            {
                IsVisible = false,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center,
                RowSpacing = 3,
                Padding = new Thickness(10, 0)
            };

            var buttonRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 15,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center
            };

            // 前へボタン
            prevButton = new Button
            {
                Text = "◀ 前へ",
                FontSize = 12,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                CornerRadius = 5,
                HeightRequest = 35,
                MinimumHeightRequest = 30,
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Fill,
                Padding = new Thickness(2)
            };
            prevButton.Clicked += OnPrevButtonClicked;
            Grid.SetColumn(prevButton, 0);

            // ページ数ラベル
            pageLabel = new Label
            {
                FontSize = 12,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(0, 5, 0, 0)
            };
            Grid.SetColumn(pageLabel, 1);

            // 次へボタン
            nextButton = new Button
            {
                Text = "次へ ▶",
                FontSize = 12,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                CornerRadius = 5,
                HeightRequest = 35,
                MinimumHeightRequest = 30,
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Fill,
                Padding = new Thickness(2)
            };
            nextButton.Clicked += OnNextButtonClicked;
            Grid.SetColumn(nextButton, 2);

            buttonRow.Children.Add(prevButton);
            buttonRow.Children.Add(pageLabel);
            buttonRow.Children.Add(nextButton);

            // 合計件数ラベル
            totalLabel = new Label
            {
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            };

            Grid.SetRow(buttonRow, 0);
            Grid.SetRow(totalLabel, 1);

            paginationLayout.Children.Add(buttonRow);
            paginationLayout.Children.Add(totalLabel);
        }

        // ==========================================
        // 検索処理
        // ==========================================

        /// <summary>
        /// 検索ボタンクリック時処理
        /// </summary>
        //private async Task OnbtnSearchClicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        ClearContentContainer();
        //        HidePagination();
        //        ShowMessage("検索中...", Colors.Gray);

        //        var searchInfo = new InboundInfo(
        //            itemKeywordEntry?.Text?.Trim() ?? "",      // ItemId / Keyword
        //            locationPicker?.SelectedIndex >= 0
        //                ? locationPicker.SelectedItem?.ToString() ?? "" : "", // Status ->
        //            startDatePicker?.Date.ToString("yyyy-MM-dd") ?? "",       // StartDate
        //            endDatePicker?.Date.ToString("yyyy-MM-dd") ?? ""         // EndDate
        //        );

        //        var request = new RequestData<InboundInfo, EvangJsonModel>("GetInboundList");
        //        request.Info = searchInfo;

        //        var result = await this.Post<InboundInfo, EvangJsonModel, InboundRecord, EvangJsonModel>(request);

        //        if (result == null || result.SubData == null || result.SubData.Count == 0 || result.SubData[0]?.SubJson == null)
        //        {
        //            ShowMessage("検索条件に一致する入庫履歴はありません。", Colors.Gray);
        //            HidePagination();
        //            return;
        //        }

        //        var dbJson = result.SubData[0].SubJson;
        //        var dynamicList = BaseUtils.JsonToClass<List<dynamic>>(dbJson);

        //        if (dynamicList == null || dynamicList.Count == 0)
        //        {
        //            ShowMessage("検索条件に一致する入庫履歴はありません。", Colors.Gray);
        //            HidePagination();
        //            return;
        //        }

        //      
        //        List<InboundRecord> orderList = new List<InboundRecord>();
        //        foreach (var item in dynamicList)
        //        {
        //            orderList.Add(new InboundRecord(
        //                GetJsonStringValue(item, "SlipNo"),       // 伝票No
        //                GetJsonStringValue(item, "ItemName"),     // 品目
        //                GetJsonStringValue(item, "Supplier"),     // 取引先
        //                GetJsonStringValue(item, "Status"),       // 状態
        //                GetJsonIntValue(item, "Quantity")         // 数量
        //            ));
        //        }

        //       
        //        ShowData(orderList);
        //    }
        //    catch (Exception ex)
        //    {
        //        ShowMessage($"予期しないエラーが発生しました: {ex.Message}", Colors.Red);
        //    }
        //}

       

        //private string GetJsonStringValue(JsonElement jsonElement, string propertyName)
        //{
        //    try
        //    {
        //        if (jsonElement.TryGetProperty(propertyName, out JsonElement propertyValue))
        //        {
        //            return propertyValue.GetString() ?? string.Empty;
        //        }
        //        return string.Empty;
        //    }
        //    catch { return string.Empty; }
        //}

        //private int GetJsonIntValue(JsonElement jsonElement, string propertyName)
        //{
        //    try
        //    {
        //        if (jsonElement.TryGetProperty(propertyName, out JsonElement propertyValue))
        //        {
        //            if (propertyValue.ValueKind == JsonValueKind.Number)
        //                return propertyValue.GetInt32();
        //            else if (propertyValue.ValueKind == JsonValueKind.String)
        //            {
        //                int.TryParse(propertyValue.GetString(), out int val);
        //                return val;
        //            }
        //        }
        //        return 0;
        //    }
        //    catch { return 0; }
        //}

        private async Task OnbtnSearchClicked(object sender, EventArgs e)
        {
            try
            {
                var searchobj = new SearchParam();
                string startDate = startDatePicker?.Date.ToString("yyyy/MM/dd") ?? "";
                string endDate = endDatePicker?.Date.ToString("yyyy/MM/dd") ?? "";
                if (string.Compare(startDate, endDate) > 0)
                {
                    await DisplayAlert("エーラ", "対象期間FROMは対象期間TOより大きくすることはできません。", "OK");
                    return;
                }
                //string location = locationPicker?.SelectedIndex >= 0
                //    ? locationPicker.SelectedItem?.ToString() ?? string.Empty
                //    : string.Empty;
                string itemKeyword = itemKeywordEntry?.Text?.Trim() ?? string.Empty;
                searchobj.FromDate = startDate;
                searchobj.EndDate = endDate;
                searchobj.LocationId = locationId;
                searchobj.Keyword = itemKeyword;
                // ローディング表示
                ClearContentContainer();
                HidePagination();
                ShowMessage("検索中...", Colors.Gray);

                // TODO: API呼び出し
                //await Task.Delay(800);
                await getsearchdata(searchobj);
                var orderList = allInboundData;

                if (orderList == null || orderList.Count == 0)
                {
                    ShowMessage("検索条件に一致する入庫履歴はありません。", Colors.Gray);
                    HidePagination();
                    return;
                }

                ShowData(orderList);
            }
            catch (Exception ex)
            {
                ShowMessage($"予期しないエラーが発生しました: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 検索結果データの表示
        /// </summary>
        private void ShowData(List<InboundRecord> dataList)
        {
            ClearContentContainer();

            if (dataList == null || dataList.Count == 0)
            {
                ShowMessage("検索条件に一致する入庫履歴はありません。", Colors.Gray);
                HidePagination();
                return;
            }

            allInboundData = dataList;
            currentPage = 0;
            totalPages = (int)Math.Ceiling((double)dataList.Count / pageSize);

            ShowPagination();
            LoadPage(currentPage);
        }

        // ==========================================
        // ページング処理
        // ==========================================

        /// <summary>
        /// 指定ページのデータを読み込んで表示
        /// </summary>
        private void LoadPage(int pageIndex)
        {
            if (allInboundData == null || pageIndex < 0 || pageIndex >= totalPages)
                return;

            currentPage = pageIndex;
            var pageData = allInboundData.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            if (tableContainer == null)
            {
                tableContainer = new VerticalStackLayout { Spacing = 0 };
                contentLayout?.Children.Add(tableContainer);
            }

            // テーブル内容を更新
            UpdateTableContent(pageData);

            UpdatePaginationControls();
        }

        /// <summary>
        /// テーブル形式のUI生成・更新
        /// </summary>
        private void UpdateTableContent(List<InboundRecord> data)
        {
            if (tableContainer == null) return;
            tableContainer.Children.Clear();

            // ヘッダー行
            var headerGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#e0e0e0"), // 灰色背景
                Padding = new Thickness(5, 8),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }, // 伝票No
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }, // 品目
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }, // 取引先
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },   // 状態
                    new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) }  // 数量
                }
            };

            headerGrid.Children.Add(CreateHeaderLabel("伝票No.", 0));
            headerGrid.Children.Add(CreateHeaderLabel("品目", 1));
            headerGrid.Children.Add(CreateHeaderLabel("取引先", 2));
            headerGrid.Children.Add(CreateHeaderLabel("状態", 3));
            headerGrid.Children.Add(CreateHeaderLabel("数量", 4));

            tableContainer.Children.Add(headerGrid);

            // データ行
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var rowGrid = new Grid
                {
                    BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f9f9f9"), 
                    Padding = new Thickness(5, 8),
                    ColumnDefinitions = headerGrid.ColumnDefinitions // 列定義を共有
                };

                // 伝票No
                rowGrid.Children.Add(CreateCellLabel(item.SlipNo, 0, Colors.Black));

                // 品目
                rowGrid.Children.Add(CreateCellLabel(item.ItemName, 1, Colors.Black));

                // 取引先
                rowGrid.Children.Add(CreateCellLabel(item.Supplier, 2, Colors.Black));

                // 状態 (色分け)
                var statusColor = GetStatusColor(item.Status);
                rowGrid.Children.Add(CreateCellLabel(item.Status, 3, statusColor));

                // 数量
                rowGrid.Children.Add(CreateCellLabel(item.Quantity.ToString(), 4, Colors.Black, TextAlignment.End));

                // 区切り線
                var separator = new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = Color.FromArgb("#eeeeee"),
                    VerticalOptions = LayoutOptions.End
                };
                // Grid全体に線を引くのは難しいので、各セルの下に置くか、Borderで囲むのが一般的ですが、
                // ここでは簡易的にRowの下にBoxViewを追加するのではなく、GridのPaddingで調整します。
                // より正確な線が欲しい場合は、rowGridをBorderで囲んでください。

                tableContainer.Children.Add(rowGrid);

                // 簡易的な下線
                var line = new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#ddd") };
                tableContainer.Children.Add(line);
            }
        }

        private Label CreateHeaderLabel(string text, int col)
        {
            var label = new Label
            {
                Text = text,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                VerticalTextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(label, col);
            return label;
        }

        private Label CreateCellLabel(string text, int col, Color textColor, TextAlignment align = TextAlignment.Start)
        {
            var label = new Label
            {
                Text = text,
                FontSize = 11, // 少し小さくして収まりをよくする
                TextColor = textColor,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = align,
                LineBreakMode = LineBreakMode.TailTruncation // はみ出したら...にする
            };
            Grid.SetColumn(label, col);
            return label;
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "入庫済み" => Color.FromArgb("#4CAF50"), // 緑
                "一部入庫" => Color.FromArgb("#FF9800"), // オレンジ
                "未入庫" => Color.FromArgb("#F44336"),   // 赤
                _ => Colors.Gray
            };
        }

        /// <summary>
        /// ページングコントロールの状態更新
        /// </summary>
        private void UpdatePaginationControls()
        {
            if (paginationLayout == null || pageLabel == null || totalLabel == null ||
                prevButton == null || nextButton == null || allInboundData == null)
                return;

            pageLabel.Text = $"{currentPage + 1} / {totalPages}";
            //totalLabel.Text = $"合計: {allInboundData.Count}";

            prevButton.IsEnabled = currentPage > 0;
            nextButton.IsEnabled = currentPage < totalPages - 1;

            prevButton.TextColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.TextColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
        }

        private void ShowPagination()
        {
            if (paginationLayout != null)
                paginationLayout.IsVisible = true;
        }

        private void HidePagination()
        {
            if (paginationLayout != null)
                paginationLayout.IsVisible = false;
        }

        private void OnPrevButtonClicked(object sender, EventArgs e)
        {
            if (currentPage > 0)
                LoadPage(currentPage - 1);
        }

        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            if (currentPage < totalPages - 1)
                LoadPage(currentPage + 1);
        }

        // ==========================================
        // ユーティリティ
        // ==========================================

        /// <summary>
        /// コンテンツエリアのクリア
        /// </summary>
        private void ClearContentContainer()
        {
            contentLayout?.Children.Clear();
            tableContainer = null;
        }

        /// <summary>
        /// メッセージ表示
        /// </summary>
        private void ShowMessage(string message, Color color)
        {
            ClearContentContainer();

            var messageLabel = new Label
            {
                Text = message,
                FontSize = 13,
                TextColor = color,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };

            contentLayout?.Children.Add(messageLabel);
        }

        /// <summary>
        /// 仮データ生成
        /// </summary>
        private List<InboundRecord> GetMockData()
        {
            return new List<InboundRecord>
            {
                new InboundRecord("PO-0114", "部品A-1010", "東陽電子部品", "入庫済み", 320),
                new InboundRecord("PO-0115", "部品B-2020", "関東マテリアル", "一部入庫", 150),
                new InboundRecord("RMA-0032", "部品C-3030", "大和精密工業", "入庫済み", 40),
                new InboundRecord("PO-0116", "部品D-4040", "北陸金属工業", "未入庫", 80),
                new InboundRecord("PO-0117", "部品E-5050", "松本電機", "入庫済み", 60),
                new InboundRecord("PO-0118", "部品F-6060", "関西物流", "入庫済み", 200),
                new InboundRecord("PO-0119", "部品G-7070", "九州部品", "一部入庫", 90),
                new InboundRecord("PO-0120", "部品H-8080", "北海道商事", "入庫済み", 110),
                new InboundRecord("PO-0121", "部品I-9090", "四国電材", "未入庫", 55),
                new InboundRecord("PO-0122", "部品J-1010", "沖縄貿易", "入庫済み", 300),
                new InboundRecord("PO-0123", "部品K-1111", "東北産業", "入庫済み", 75)
            };
        }

        public class SearchParam : EvangJsonModel
        {
            public string? Kbn { get; set; }
            public string? FromDate { get; set; }
            public string? EndDate { get; set; }
            public int? LocationId { get; set; }
            public string? Keyword { get; set; }
        }

        public class LocationData : EvangJsonModel
        {
            public int? id { get; set; }
            public string? name { get; set; }
        }

        private async Task getdata()
        {
            var searchParam = new SearchParam();
            searchParam.Kbn = "loca";
            var request = new RequestData<SearchParam, EvangJsonModel>("GetItemrecept");
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
                }
            }
        }

        private async Task getsearchdata(SearchParam searchobj)
        {
            var searchParam = searchobj;
            searchParam.Kbn = "data";
            var request = new RequestData<SearchParam, EvangJsonModel>("GetItemrecept");
            request.Info = searchParam;
            var resultList = await this.Post<SearchParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
            if (resultList == null || resultList.SubData == null)
                return;
            foreach (var item in resultList.SubData)
            {
                switch (item.SubName)
                {
                    case "PH_DATA":
                        if (item == null || item.SubJson == null)
                            return;
                        allInboundData = BaseUtils.JsonToClass<List<InboundRecord>>(item.SubJson);
                        break;
                }
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
}