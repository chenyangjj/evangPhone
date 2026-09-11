using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using System.Diagnostics.Metrics;
using System.Text.Json;
using EvangPL.Views.StockIn;


namespace EvangPL.Views.InboundSearch
{

    /// <summary>
    /// 検索条件エンティティ（StockIn画面への受け渡し用）
    /// </summary>
    public class SearchCondition
    {
        public string? Keyword { get; set; }
        public string? InboundType { get; set; }
        public string? Status { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public List<OrderInfo>? SearchResult { get; set; }  // 検索結果
    }

    /// <summary>
    /// オーダー情報モデル（NetSuiteレスポンス構造に合わせて調整）
    /// </summary>
    public class OrderInfo
    {
        public string? orderId { get; set; }           // オーダーID
        public string? orderNumber { get; set; }       // オーダー番号
        public string? status { get; set; }            // ステータス
        public string? statusLabel { get; set; }        // ステータスラベル
        public string? inboundType { get; set; }       // 入庫区分
        public DateTime? scheduledDate { get; set; }   // 入庫予定日
        public string? supplierName { get; set; }      // サプライヤー名
        public decimal? totalQuantity { get; set; }    // 合計数量
        public string? itemCode { get; set; }          // 品目コード
        public string? itemName { get; set; }          // 品目名称
        public double? itemCount { get; set; }

    }





    public class InboundSearch : EvangContentVM
    {
        // ==========================================
        // [追加] Androidネイティブの下線を消去するためのHandler登録
        // ==========================================
        static InboundSearch()
        {
            // Entryの下線を透明にする
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

            // Pickerの下線を透明にする
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

            // DatePickerの下線を透明にする
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
        }

        private Picker? inboundTypePicker;      // 入庫区分
        private Picker? statusPicker;           // ステータス
        private DatePicker? datePicker;         // 入庫予定日
        private Entry? keywordEntry;            // 発注書番号 / 品目コード
        private Button? searchButton;           // 検索ボタン
        private Grid? filterFrame;
        private Grid? mainGrid;

        // ✅ RESTlet URL(GetOrderList)は Menu.cs の GetBaseMasterData() で
        //    LocalMemory.restlets に一元登録済み。ここでは登録処理を行わない。
        public InboundSearch() : base("strInboundSearch")
        {
            Title = "入庫処理";

            BuildUI();
        }

        private void BuildUI()
        {
            filterFrame = CreateFilterFrame();

            mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                },
                ColumnDefinitions = { new ColumnDefinition { } },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0
            };
            Grid.SetRow(filterFrame, 0);
            mainGrid.Children.Add(filterFrame);

            var resultContainer = new StackLayout
            {
                Spacing = 5,
                Padding = new Thickness(10),
            };

            var resultScrollView = new ScrollView
            {
                Content = resultContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            };

            mainGrid.Add(resultScrollView, 0, 1);
            Content = mainGrid;
        }

        private Grid CreateFilterFrame()
        {
            var searchFilterGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#f5f5f5"),
                Padding = new Thickness(10)
            };

            var filterLayout = new VerticalStackLayout
            {
                Spacing = 8
            };

            // === 入庫区分 ===
            var inboundTypeTitle = new Label
            {
                Text = "入庫区分",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black
            };
            filterLayout.Children.Add(inboundTypeTitle);

            // === 入庫区分 (ピッカー) ===
            // [修正] PickerをBorderで包んで角丸・下線なしにする
            inboundTypePicker = new Picker
            {
                Title = "発注入庫(PO Item Receipt)",
                BackgroundColor = Colors.Transparent, // 背景を透明に
                TextColor = Colors.Black,
                HeightRequest = 40,
                FontSize = 12,
                Margin = new Thickness(10, 0) // 内側の余白
            };
            inboundTypePicker.Items.Add("発注入庫(PO Item Receipt)");
            inboundTypePicker.Items.Add("返品入庫(Return Receipt)");
            inboundTypePicker.Items.Add("振替入庫(Transfer Receipt)");
            inboundTypePicker.SelectedIndexChanged += (sender, e) =>
            {
                if (inboundTypePicker.SelectedIndex >= 0)
                {
                    inboundTypePicker.Title = inboundTypePicker.SelectedItem?.ToString();
                }
                else
                {
                    inboundTypePicker.Title = "入庫区分を選択";
                }
            };

            var inboundTypeBorder = CreateInputBorder(inboundTypePicker);
            filterLayout.Children.Add(inboundTypeBorder);

            // === ステータス + 入庫予定日（2列レイアウト） ===
            var statusDateGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                Margin = new Thickness(0, 5, 0, 0)
            };

            // ステータス
            var statusLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            var statusLabel = new Label
            {
                Text = "ステータス",
                FontSize = 11,
                TextColor = Colors.Gray
            };
            statusLayout.Children.Add(statusLabel);

            // [修正] PickerをBorderで包む
            statusPicker = new Picker
            {
                Title = "未入庫",
                BackgroundColor = Colors.Transparent, // 背景を透明に
                TextColor = Colors.Black,
                HeightRequest = 40,
                FontSize = 12,
                Margin = new Thickness(10, 0)
            };
            statusPicker.Items.Add("未入庫");
            statusPicker.Items.Add("一部入庫");
            statusPicker.SelectedIndexChanged += (sender, e) =>
            {
                if (statusPicker.SelectedIndex >= 0)
                {
                    statusPicker.Title = statusPicker.SelectedItem?.ToString();
                }
                else
                {
                    statusPicker.Title = "ステータスを選択";
                }
            };

            var statusBorder = CreateInputBorder(statusPicker);
            statusLayout.Children.Add(statusBorder);
            statusDateGrid.Add(statusLayout, 0, 0);

            // 入庫予定日
            var dateLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            var dateLabel = new Label
            {
                Text = "入庫予定日",
                FontSize = 11,
                TextColor = Colors.Gray
            };
            dateLayout.Children.Add(dateLabel);

            // [修正] DatePickerをBorderで包む
            datePicker = new DatePicker
            {
                BackgroundColor = Colors.Transparent, // 背景を透明に
                HeightRequest = 40,
                FontSize = 12,
                Format = "yyyy-MM-dd",
                Margin = new Thickness(10, 0)
            };

            var dateBorder = CreateInputBorder(datePicker);
            dateLayout.Children.Add(dateBorder);
            statusDateGrid.Add(dateLayout, 1, 0);

            filterLayout.Children.Add(statusDateGrid);

            // === 発注書番号 / 品目コード ===
            var keywordLabel = new Label
            {
                Text = "発注書番号 / 品目コード",
                FontSize = 11,
                TextColor = Colors.Gray
            };
            filterLayout.Children.Add(keywordLabel);

            // [修正] EntryをBorderで包む
            keywordEntry = new Entry
            {
                Placeholder = "検索キーワードを入力",
                BackgroundColor = Colors.Transparent, // 背景を透明に
                HeightRequest = 40,
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(10, 0)
            };

            var keywordBorder = CreateInputBorder(keywordEntry);
            filterLayout.Children.Add(keywordBorder);

            // === 検索ボタン ===
            searchButton = new Button
            {
                Text = "検索",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                HeightRequest = 45,
                CornerRadius = 6,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            searchButton.Clicked += async (sender, e) => await OnbtnSearchClicked(sender, e);
            filterLayout.Children.Add(searchButton);

            searchFilterGrid.Children.Add(filterLayout);

            return searchFilterGrid;
        }

        // ==========================================
        // [追加] 角丸ボーダー付きの入力枠を作成するヘルパーメソッド
        // ==========================================
        /// <summary>
        /// 入力コントロールを角丸のBorderで囲みます
        /// </summary>
        /// <param name="content">内部のEntry, Picker, DatePicker</param>
        /// <returns>設定済みのBorder</returns>
        private Border CreateInputBorder(View content)
        {
            return new Border
            {
                Stroke = Color.FromArgb("#cccccc"),       // 薄いグレーの枠線
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 }, // 角丸
                BackgroundColor = Colors.White,           // 白い背景
                Padding = 0,
                Content = content,
                HeightRequest = 44                        // 全体の高さを統一 (内部40 + 上下余白)
            };
        }

        #region OnbtnSearchClicked
        /// <summary>
        /// TODO: 検索ロジック
        /// </summary>
        private async Task OnbtnSearchClicked(object sender, EventArgs e)
        {
            //await Task.CompletedTask;
            string viewName = "StockIn";
            try
            {

                var searchParam = new OrderSearchParam
                {
                    InboundType = inboundTypePicker?.SelectedIndex >= 0
                ? inboundTypePicker.SelectedItem?.ToString()
                : null,
                    Status = statusPicker?.SelectedIndex >= 0
                ? statusPicker.SelectedItem?.ToString()
                : null,
                    ScheduledDate = datePicker?.Date.ToString("yyyy-MM-dd"),
                    Keyword = string.IsNullOrWhiteSpace(keywordEntry?.Text)
                ? null
                : keywordEntry.Text.Trim()
                };
                var request = new RequestData<OrderSearchParam, EvangJsonModel>("GetOrderList");
                request.Info = searchParam;

                var result = await this.Post<OrderSearchParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                // 3. 返却結果をチェック
                if (result == null)
                {
                    await DisplayAlert("エラー", "検索結果を取得できません", "OK");
                    return;
                }

                if (!result.Success)
                {
                    await DisplayAlert("エラー", result.ErrorMessage ?? "検索に失敗しました", "OK");
                    return;
                }

                // 4. 検索結果を解析
                var orderList = ParseSearchResult(result);
                if (orderList == null || orderList.Count == 0)
                {
                    await DisplayAlert("お知らせ", "条件に一致する入庫データが見つかりません", "OK");
                    return;
                }

                // メニューと同様にフレームワークファクトリを使用し、手動での new StockIn() は禁止
                var pageObj = ClassMapping.CreatePageInstance(viewName);
                if (pageObj == null)
                {
                    await DisplayAlert("エラー", $"画面[{viewName}]を作成できません", "OK");
                    return;
                }

                if (pageObj is EvangContentVM vm)
                {
                    vm.IsFromMenu = true;
                    // ========== 新規追加：検索条件を渡す（コメント記載の方法を使用） ==========
                    // SearchConditionエンティティを作成して値を設定
                    var cond = new SearchCondition();
                    cond.Keyword = keywordEntry?.Text;
                    cond.InboundType = inboundTypePicker?.SelectedIndex >= 0
                        ? inboundTypePicker.SelectedItem?.ToString()
                        : null;
                    cond.Status = statusPicker?.SelectedIndex >= 0
                        ? statusPicker.SelectedItem?.ToString()
                        : null;
                    cond.ScheduledDate = datePicker?.Date;
                    cond.SearchResult = orderList;  // 検索結果も渡す
                    EvangPL.Views.StockIn.StockIn.PassedCondition = cond;
                    // コメント記載の方法に従って第2引数を渡す
                    await Navigation.PushAsync(vm);
                    // =========将来的に検索条件をここで渡す場合、第2引数にエンティティを指定========
                    // SearchCondition cond = new SearchCondition();
                    // cond.Keyword = keywordEntry.Text;
                    // await Navigation.PushAsync(vm, cond);
                    //await Navigation.PushAsync(vm);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                if (ex.InnerException != null)
                    errMsg += Environment.NewLine + ex.InnerException.Message;
                await DisplayAlert("エラー", errMsg, "OK");
            }
        }

        /// <summary>
        /// 検索条件オブジェクトを構築（RESTlet呼び出し用）
        /// </summary>
        private object BuildSearchCondition()
        {
            // NetSuite RESTletが期待するリクエスト形式に合わせて構築
            var condition = new
            {
                Info = new
                {
                    // 入庫区分
                    InboundType = inboundTypePicker?.SelectedIndex >= 0
                        ? inboundTypePicker.SelectedItem?.ToString()
                        : null,

                    // ステータス
                    Status = statusPicker?.SelectedIndex >= 0
                        ? statusPicker.SelectedItem?.ToString()
                        : null,

                    // 入庫予定日（yyyy-MM-dd形式）
                    ScheduledDate = datePicker?.Date != null
                        ? datePicker.Date.ToString("yyyy-MM-dd")
                        : null,

                    // 検索キーワード（発注書番号 / 品目コード）
                    Keyword = string.IsNullOrWhiteSpace(keywordEntry?.Text)
                        ? null
                        : keywordEntry.Text.Trim()
                }
            };

            return condition;
        }

        /// <summary>
        /// 検索結果を解析
        /// </summary>
        /// <param name="result">ResponseData型の結果</param>
        /// <returns>オーダーリスト</returns>
        private List<OrderInfo> ParseSearchResult(ResponseData<EvangJsonModel, EvangJsonModel> result)
        {
            var orderList = new List<OrderInfo>();

            if (result.SubData == null || result.SubData.Count == 0)
                return orderList;

            foreach (var subData in result.SubData)
            {
                if (subData.SubName == "ORDER_LIST" || subData.SubName == "orders")
                {
                    var orders = BaseUtils.JsonToClass<List<OrderInfo>>(subData.SubJson!);
                    if (orders != null)
                    {
                        orderList.AddRange(orders);
                    }
                }
            }

            return orderList;
        }




        /// <summary>
        /// オーダー検索リクエストパラメータ
        /// </summary>
        public class OrderSearchParam : EvangJsonModel
        {
            public string? InboundType { get; set; }
            public string? Status { get; set; }
            public string? ScheduledDate { get; set; }
            public string? Keyword { get; set; }
        }

        #endregion
    }
}