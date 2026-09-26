using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json;
using System.Linq;

namespace EvangPL.Views.StockOut
{
    /// <summary>
    /// 出荷処理 - 検索結果画面 【文件名 StockOut.cs】
    /// UI样式参照截图：分页栏置顶、单据卡片、日文标签、状态色
    /// RESTlet①（一覧検索専用）を呼び出し、出荷区分（受注/仕入先返品/振替）を横断した検索結果を表示する
    /// </summary>
    public class StockOut : EvangContentVM
    {
        // UI控件缓存
        private Grid? mainGrid;
        private Grid? paginationGrid;
        private Label? pageInfoLabel;
        private Button? prevPageBtn;
        private Button? nextPageBtn;
        private StackLayout? listContainer;

        // 分页参数
        private int _currentPage = 1;
        private int _totalPage = 1;
        private const int PageSize = 4; // 一页展示4条，和截图效果一致

        // [追加] 本番/開発切替：true にすると内蔵のモックデータで動作確認できる
        private const bool UseMockData = false;

        // 查询条件实体（画面5から渡される）
        private StockOutPageInfo SearchCondition;

        public StockOut() : base("strStockOutSearch")
        {
            SearchCondition = new StockOutPageInfo();
            Title = "出荷処理-検索結果";
            BuildUI();
            // [変更] 検索条件は OutboundSearch から SetSearchCondition() 経由で渡される想定のため、
            //         このコンストラクタ単体でのロードは行わない（単独デバッグ時は SetSearchCondition を呼ぶこと）
        }

        // ==========================================
        // [追加] OutboundSearch（画面5）から検索条件を受け取るエントリポイント
        // 　　　　設定と同時に検索を実行する
        // ==========================================
        public void SetSearchCondition(StockOutPageInfo condition)
        {
            SearchCondition = condition ?? new StockOutPageInfo();
            _currentPage = SearchCondition.PageIndex > 0 ? SearchCondition.PageIndex : 1;
            _ = LoadStockOutData();
        }

        // ==========================================
        // [追加] 画面7-2（梱包情報登録）で保存完了後、この画面まで戻ってきた際に
        // 　　　　初期表示（1ページ目）の状態で再検索を行うためのエントリポイント。
        // 　　　　検索条件（SearchCondition）自体は変更せず、ページ位置のみ1ページ目に戻す。
        // ==========================================
        public async Task ReloadInitial()
        {
            _currentPage = 1;
            await LoadStockOutData();
        }

        #region 页面布局构建
        private void BuildUI()
        {
            mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = 60 },
                    new RowDefinition { Height = GridLength.Star }
                },
                ColumnDefinitions = { new ColumnDefinition() },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0
            };

            // 1. 顶部分页控件
            paginationGrid = CreatePaginationBar();
            Grid.SetRow(paginationGrid, 0);
            mainGrid.Children.Add(paginationGrid);

            // 2. 列表滚动区域
            listContainer = new StackLayout
            {
                Spacing = 8,
                Padding = new Thickness(10)
            };
            var scrollView = new ScrollView
            {
                Content = listContainer
            };
            Grid.SetRow(scrollView, 1);
            mainGrid.Children.Add(scrollView);

            Content = mainGrid;
        }

        /// <summary>
        /// 分页栏：前へ｜1/2｜次へ  和截图UI一致
        /// </summary>
        private Grid CreatePaginationBar()
        {
            paginationGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 15,
                Padding = new Thickness(10, 10),
                BackgroundColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };

            prevPageBtn = new Button
            {
                Text = "◀ 前へ",
                WidthRequest = 110,
                HeightRequest = 40,
                BackgroundColor = Color.FromArgb("#e6e6e6"),
                TextColor = Colors.Gray,
                BorderWidth = 0,
                VerticalOptions = LayoutOptions.Center
            };
            prevPageBtn.Clicked += async (s, e) => await PrevPage();

            pageInfoLabel = new Label
            {
                Text = "1/1",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            };

            nextPageBtn = new Button
            {
                Text = "次へ ▶",
                WidthRequest = 110,
                HeightRequest = 40,
                BackgroundColor = Colors.Transparent,
                BorderColor = Colors.DarkBlue,
                BorderWidth = 1,
                TextColor = Colors.DarkBlue,
                VerticalOptions = LayoutOptions.Center
            };
            nextPageBtn.Clicked += async (s, e) => await NextPage();

            paginationGrid.Add(prevPageBtn, 0, 0);
            paginationGrid.Add(pageInfoLabel, 1, 0);
            paginationGrid.Add(nextPageBtn, 2, 0);
            return paginationGrid;
        }
        #endregion

        #region 搜索分页逻辑
        private void CollectSearchCondition()
        {
            SearchCondition.PageIndex = _currentPage;
            SearchCondition.PageSize = PageSize;
        }

        /// <summary>
        /// 出荷対象データを取得する。
        /// RESTlet①（出荷処理-一覧検索専用）へ SearchCondition を渡し、
        /// 受注出荷/仕入先返品出荷/振替出荷を横断した検索結果を受け取る。
        /// </summary>
        private async Task LoadStockOutData()
        {
            try
            {
                CollectSearchCondition();

                List<StockOutItem> dataList;

                if (UseMockData)
                {
                    dataList = BuildMockData(out _totalPage);
                }
                else
                {
                    //====================RESTlet①呼び出し====================
                    var request = new RequestData<StockOutPageInfo, EvangJsonModel>("GetStockOutList");
                    request.Info = SearchCondition;
                    var apiResult = await this.Post<StockOutPageInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);

                    if (apiResult == null || apiResult.SubData == null || apiResult.SubData.Count == 0
                        || apiResult.SubData[0]?.SubJson == null)
                    {
                        ShowEmptyTip();
                        return;
                    }

                    string json = apiResult.SubData[0].SubJson;
                    using var jsonDoc = JsonDocument.Parse(json);
                    JsonElement rootEle = jsonDoc.RootElement;

                    // RESTlet側でエラーが起きた場合、Errorプロパティを返す取り決め
                    if (rootEle.TryGetProperty("Error", out JsonElement errEle) && errEle.ValueKind == JsonValueKind.String)
                    {
                        await DisplayAlert("エラー", errEle.GetString() ?? "検索に失敗しました", "OK");
                        ShowErrorTip();
                        return;
                    }

                    _totalPage = GetJsonIntValue(rootEle, "TotalPage");
                    if (_totalPage <= 0) _totalPage = 1;

                    dataList = new List<StockOutItem>();
                    if (rootEle.TryGetProperty("List", out JsonElement listEle) && listEle.GetArrayLength() > 0)
                    {
                        foreach (JsonElement itemEle in listEle.EnumerateArray())
                        {
                            var item = new StockOutItem
                            {
                                OrderNo = GetJsonStringValue(itemEle, "OrderNo"),
                                Status = GetJsonStringValue(itemEle, "Status"),
                                CustomerName = GetJsonStringValue(itemEle, "CustomerName"),
                                ScheduleDate = GetJsonStringValue(itemEle, "ScheduleDate"),
                                ItemCount = GetJsonIntValue(itemEle, "ItemCount"),
                                TotalQty = GetJsonIntValue(itemEle, "TotalQty"),
                                OutboundType = GetJsonStringValue(itemEle, "OutboundType")
                            };
                            dataList.Add(item);
                        }
                    }
                }

                if (_currentPage > _totalPage && _totalPage > 0)
                    _currentPage = _totalPage;

                RefreshPageUI();
                RenderCardList(dataList);
            }
            catch (Exception)
            {
                ShowErrorTip();
            }
        }

        /// <summary>
        /// [開発用] UseMockData = true の時のみ使用する内蔵データ（截图4条样本と一致）
        /// </summary>
        private List<StockOutItem> BuildMockData(out int totalPage)
        {
            var dataList = new List<StockOutItem>
            {
                new StockOutItem { OrderNo = "SO-2026-0987", Status = "未出荷", CustomerName = "山田工業(株)", ScheduleDate = "2026-07-08", ItemCount = 4, TotalQty = 210, OutboundType = "SO" },
                new StockOutItem { OrderNo = "SO-2026-0988", Status = "一部出荷", CustomerName = "中央物流サービス(株)", ScheduleDate = "2026-07-08", ItemCount = 2, TotalQty = 60, OutboundType = "SO" },
                new StockOutItem { OrderNo = "TR-0021", Status = "未出庫", CustomerName = "移動元:WH1 → 移動先:WH2", ScheduleDate = "2026-07-07", ItemCount = 6, TotalQty = 300, OutboundType = "TR" },
                new StockOutItem { OrderNo = "SO-2026-0989", Status = "一部出荷", CustomerName = "松本電機(株)", ScheduleDate = "2026-07-09", ItemCount = 3, TotalQty = 95, OutboundType = "SO" }
            };

            int totalRecordCount = dataList.Count;
            totalPage = (int)Math.Ceiling((double)totalRecordCount / PageSize);
            if (totalPage <= 0) totalPage = 1;

            return dataList
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private void RefreshPageUI()
        {
            if (pageInfoLabel != null)
                pageInfoLabel.Text = $"{_currentPage}/{_totalPage}";

            if (prevPageBtn != null)
            {
                prevPageBtn.IsEnabled = _currentPage > 1;
                prevPageBtn.BackgroundColor = _currentPage > 1 ? Colors.LightGray : Color.FromArgb("#e6e6e6");
                prevPageBtn.TextColor = _currentPage > 1 ? Colors.Black : Colors.Gray;
            }
            if (nextPageBtn != null)
            {
                nextPageBtn.IsEnabled = _currentPage < _totalPage;
                nextPageBtn.BorderColor = _currentPage < _totalPage ? Colors.DarkBlue : Colors.LightGray;
                nextPageBtn.TextColor = _currentPage < _totalPage ? Colors.DarkBlue : Colors.Gray;
            }
        }

        private async Task PrevPage()
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            await LoadStockOutData();
        }

        private async Task NextPage()
        {
            if (_currentPage >= _totalPage) return;
            _currentPage++;
            await LoadStockOutData();
        }
        #endregion

        #region Json工具函数
        private string GetJsonStringValue(JsonElement jsonElement, string propertyName)
        {
            try
            {
                if (jsonElement.TryGetProperty(propertyName, out JsonElement val))
                    return val.GetString() ?? string.Empty;
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int GetJsonIntValue(JsonElement jsonElement, string propertyName)
        {
            try
            {
                if (jsonElement.TryGetProperty(propertyName, out JsonElement val))
                {
                    if (val.ValueKind == JsonValueKind.Number)
                        return val.GetInt32();
                    int.TryParse(val.GetString(), out int num);
                    return num;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        #endregion

        #region 单据卡片渲染（严格匹配截图样式）
        private void RenderCardList(List<StockOutItem> dataList)
        {
            if (listContainer == null) return;
            listContainer.Children.Clear();

            if (dataList == null || dataList.Count == 0)
            {
                ShowEmptyTip();
                return;
            }

            foreach (var item in dataList)
            {
                var cardFrame = new Frame
                {
                    BackgroundColor = Colors.White,
                    CornerRadius = 8,
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                    HasShadow = false
                };

                var cardGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    RowSpacing = 4
                };

                // 伝票番号
                var lblOrderNo = new Label
                {
                    Text = item.OrderNo,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                // 状態タグ
                var statusTag = CreateStatusTag(item.Status);

                Grid.SetRow(lblOrderNo, 0);
                Grid.SetColumn(lblOrderNo, 0);
                cardGrid.Children.Add(lblOrderNo);

                Grid.SetRow(statusTag, 0);
                Grid.SetColumn(statusTag, 1);
                cardGrid.Children.Add(statusTag);

                // 顧客 / 移動元・移動先
                // ★修正: 振替出荷(TR)の場合、CustomerName には既に
                //   「移動元:xxx → 移動先:xxx」という形式の文字列が入っているため、
                //   「顧客: 」を重ねて付けると二重表示になってしまう
                //   （例: "顧客: 移動元: → 移動先:不良品"）。
                //   → TR の場合は「顧客: 」というラベルを付けず、そのまま表示する。
                var lblCustomer = new Label
                {
                    Text = item.OutboundType == "TR" ? item.CustomerName : $"顧客: {item.CustomerName}",
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                Grid.SetRow(lblCustomer, 1);
                Grid.SetColumnSpan(lblCustomer, 2);
                cardGrid.Children.Add(lblCustomer);

                // 出荷予定日
                var lblDate = new Label
                {
                    Text = $"出荷予定日: {item.ScheduleDate}",
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                Grid.SetRow(lblDate, 2);
                Grid.SetColumnSpan(lblDate, 2);
                cardGrid.Children.Add(lblDate);

                // 品目数 / 数量
                var lblSummary = new Label
                {
                    Text = $"品目数: {item.ItemCount} / 数量: {item.TotalQty}",
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                Grid.SetRow(lblSummary, 3);
                Grid.SetColumnSpan(lblSummary, 2);
                cardGrid.Children.Add(lblSummary);

                cardFrame.Content = cardGrid;

                // 卡片クリックイベント（跳转详情）
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                {
                    try
                    {
                        var detailInfo = new EvangPL.Utils.PickingDetailInfo
                        {
                            OrderNo = item.OrderNo,
                            CustomerName = item.CustomerName,
                            ScheduleDate = item.ScheduleDate,
                            ItemCount = item.ItemCount,
                            TotalQty = item.TotalQty,
                            Status = item.Status,
                            // [追加] 出荷区分を詳細画面へ引き継ぐ（PickingDetailInfoにOutboundTypeプロパティの追加が必要）
                            OutboundType = item.OutboundType
                        };

                        // ✅ 创建详情页实例并传入数据
                        var detailPage = new EvangPL.Views.PickingDetail.PickingDetail(detailInfo);
                        await Navigation.PushAsync(detailPage);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
                    }
                };
                cardFrame.GestureRecognizers.Add(tap);

                listContainer.Children.Add(cardFrame);
            }
        }

        /// <summary>
        /// 状態ラベル配色 和截图完全一致
        /// 未出荷：オレンジ / 一部出荷：濃青 / 未出庫：グレー
        /// </summary>
        private View CreateStatusTag(string statusText)
        {
            Color bgColor = Colors.Gray;
            switch (statusText)
            {
                case "未出荷":
                    bgColor = Color.FromArgb("#E68922");
                    break;
                case "一部出荷":
                    bgColor = Color.FromArgb("#255499");
                    break;
                case "未出庫":
                    bgColor = Color.FromArgb("#808080");
                    break;
            }

            var label = new Label
            {
                Text = statusText,
                TextColor = Colors.White,
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var frame = new Frame
            {
                Content = label,
                BackgroundColor = bgColor,
                CornerRadius = 12,
                Padding = new Thickness(12, 4),
                HorizontalOptions = LayoutOptions.End,
                HasShadow = false
            };
            return frame;
        }
        #endregion

        #region 空・エラーメッセージ
        private void ShowEmptyTip()
        {
            if (listContainer == null) return;
            listContainer.Children.Clear();
            listContainer.Children.Add(new Label
            {
                Text = "検索条件に一致するデータはありません。",
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
        }

        private void ShowErrorTip()
        {
            if (listContainer == null) return;
            listContainer.Children.Clear();
            listContainer.Children.Add(new Label
            {
                Text = "予期しないエラーが発生しました。管理者に連絡してください。",
                FontSize = 12,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
        }
        #endregion
    }

    #region 出荷用Model
    public class StockOutPageInfo : EvangJsonModel
    {
        // [追加] 出荷区分コード（"SO" / "RTV" / "TR"）。RESTlet①が分岐に使用する
        public string OutboundType { get; set; } = "SO";
        public string Keyword { get; set; } = "";
        public string Status { get; set; } = "";
        public string TargetDate { get; set; } = "";
        public string Customer { get; set; } = "";
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; }
    }

    public class StockOutItem
    {
        /// <summary>伝票番号 SO-xxxx</summary>
        public string OrderNo { get; set; } = "";
        /// <summary>ステータス：未出荷 / 一部出荷 / 未出庫</summary>
        public string Status { get; set; } = "";
        /// <summary>顧客名/倉庫移動情報</summary>
        public string CustomerName { get; set; } = "";
        /// <summary>出荷予定日</summary>
        public string ScheduleDate { get; set; } = "";
        /// <summary>品目数</summary>
        public int ItemCount { get; set; }
        /// <summary>総数量</summary>
        public int TotalQty { get; set; }
        /// <summary>[追加] 出荷区分コード（"SO" / "RTV" / "TR"）。詳細画面(RESTlet②)へ引き継ぐ</summary>
        public string OutboundType { get; set; } = "SO";
    }

    public class StockOutApiWrap
    {
        public List<StockOutItem> List { get; set; } = new();
        public int TotalPage { get; set; }
        public int TotalCount { get; set; }
    }
    #endregion
}