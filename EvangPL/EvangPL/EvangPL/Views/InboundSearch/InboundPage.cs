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
    /// 搜索条件实体（用于传递给 StockIn 页面）
    /// </summary>
    public class SearchCondition
    {
        public string? Keyword { get; set; }
        public string? InboundType { get; set; }
        public string? Status { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public List<OrderInfo>? SearchResult { get; set; }  // 搜索结果
    }

    /// <summary>
    /// 订单信息模型（根据 NetSuite 响应结构调整）
    /// </summary>
    public class OrderInfo
    {
        public string? orderId { get; set; }           // 订单ID
        public string? orderNumber { get; set; }       // 订单编号
        public string? status { get; set; }            // 状态
        public string? statusLabel { get; set; }        // 状态标签
        public string? inboundType { get; set; }       // 入库类型
        public DateTime? scheduledDate { get; set; }   // 预计入库日期
        public string? supplierName { get; set; }      // 供应商名称
        public decimal? totalQuantity { get; set; }    // 总数量
        public string? itemCode { get; set; }          // 物料代码
        public string? itemName { get; set; }          // 物料名称
        
    }





    public class InboundSearch : EvangContentVM
    {
        // ==========================================
        // [追加] Android原生の下線を消去するためのHandler登録
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

        public InboundSearch() : base("strInboundSearch")
        {
            Title = "入庫処理";

            if (!LocalMemory.restlets.ContainsKey("GetOrderList"))
            {
                //LocalMemory.restlets.Add("GetOrderList",
                //    "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1799&deploy=1");
                LocalMemory.restlets.Add("GetOrderList",
                   "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2056&deploy=1");

            }

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
                // 3. 检查返回结果
                if (result == null)
                {
                    await DisplayAlert("错误", "无法获取搜索结果", "OK");
                    return;
                }

                if (!result.Success)
                {
                    await DisplayAlert("错误", result.ErrorMessage ?? "搜索失败", "OK");
                    return;
                }

                // 4. 解析搜索结果
                var orderList = ParseSearchResult(result);
                if (orderList == null || orderList.Count == 0)
                {
                    await DisplayAlert("提示", "没有找到符合条件的入库数据", "OK");
                    return;
                }

                // 和菜单完全一致，用框架工厂，禁止手动 new StockIn()
                var pageObj = ClassMapping.CreatePageInstance(viewName);
                if (pageObj == null)
                {
                    await DisplayAlert("エラー", $"画面[{viewName}]を作成できません", "OK");
                    return;
                }

                if (pageObj is EvangContentVM vm)
                {
                    vm.IsFromMenu = true;
                    // ========== 新增：传递检索条件（使用注释中的方式） ==========
                    // 创建 SearchCondition 实体并赋值
                    var cond = new SearchCondition();
                    cond.Keyword = keywordEntry?.Text;
                    cond.InboundType = inboundTypePicker?.SelectedIndex >= 0
                        ? inboundTypePicker.SelectedItem?.ToString()
                        : null;
                    cond.Status = statusPicker?.SelectedIndex >= 0
                        ? statusPicker.SelectedItem?.ToString()
                        : null;
                    cond.ScheduledDate = datePicker?.Date;
                    cond.SearchResult = orderList;  // 把搜索结果也传过去
                    EvangPL.Views.StockIn.StockIn.PassedCondition = cond;
                    // 按照注释中的方式传递第二个参数
                    await Navigation.PushAsync(vm);
                    // =========将来传递检索条件在这里，第二个参数传实体========
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
        /// 构建搜索条件对象（用于 RESTlet 调用）
        /// </summary>
        private object BuildSearchCondition()
        {
            // 按照 NetSuite RESTlet 期望的请求格式构建
            var condition = new
            {
                Info = new
                {
                    // 入库类型
                    InboundType = inboundTypePicker?.SelectedIndex >= 0
                        ? inboundTypePicker.SelectedItem?.ToString()
                        : null,

                    // 状态
                    Status = statusPicker?.SelectedIndex >= 0
                        ? statusPicker.SelectedItem?.ToString()
                        : null,

                    // 预计入库日期（yyyy-MM-dd 格式）
                    ScheduledDate = datePicker?.Date != null
                        ? datePicker.Date.ToString("yyyy-MM-dd")
                        : null,

                    // 搜索关键词（采购订单号 / 物料代码）
                    Keyword = string.IsNullOrWhiteSpace(keywordEntry?.Text)
                        ? null
                        : keywordEntry.Text.Trim()
                }
            };

            return condition;
        }

        /// <summary>
        /// 解析搜索结果
        /// </summary>
        /// <param name="result">ResponseData 类型的结果</param>
        /// <returns>订单列表</returns>
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
        /// 订单搜索请求参数
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