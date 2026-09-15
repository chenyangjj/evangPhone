using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json;
using System.Linq;
using EvangPL.Views.InboundSearch;

namespace EvangPL.Views.StockIn
{
    public class StockIn : EvangContentVM
    {
        public static SearchCondition? PassedCondition { get; set; }

        // UIコントロールキャッシュ
        private Grid? mainGrid;
        private Grid? paginationGrid;
        private Label? pageInfoLabel;
        private Button? prevPageBtn;
        private Button? nextPageBtn;
        private StackLayout? listContainer;

        // ページネーションパラメータ
        private int _currentPage = 1;
        private int _totalPage = 1;
        private const int PageSize = 4;

        // ✅ 検索画面から渡されたデータを格納
        private List<OrderInfo>? _searchResultData;
        private string? _keyword;
        private string? _inboundType;
        private string? _status;
        private DateTime? _scheduledDate;

        // ✅ データ読み込み済みフラグ（重複読み込み防止）
        private bool _isDataLoaded = false;

        // 検索条件エンティティ
        private StockInPageInfo SearchCondition;

        public StockIn() : base("strStockInSearch")
        {
            Title = "入庫処理-検索結果";
            SearchCondition = new StockInPageInfo();
            BuildUI();
        }



        protected override void OnAppearing()
        {
            base.OnAppearing();
            // ✅ OnAppearingで静的プロパティから読み取る
            if (!_isDataLoaded)
            {
                // 静的プロパティから渡されたデータを読み取る
                if (PassedCondition != null)
                {
                    _searchResultData = PassedCondition.SearchResult;
                    _keyword = PassedCondition.Keyword;
                    _inboundType = PassedCondition.InboundType;
                    _status = PassedCondition.Status;
                    _scheduledDate = PassedCondition.ScheduledDate;

                    // 使用後にクリア
                    PassedCondition = null;
                }

                _ = LoadStockInData();
                _isDataLoaded = true;
            }
        }

        #region 画面レイアウト構築
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

            paginationGrid = CreatePaginationBar();
            Grid.SetRow(paginationGrid, 0);
            mainGrid.Children.Add(paginationGrid);

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

        #region 検索・ページネーションロジック
        private void CollectSearchCondition()
        {
            SearchCondition.PageIndex = _currentPage;
            SearchCondition.PageSize = PageSize;
        }

        /// <summary>
        /// 入庫データを読み込む
        /// </summary>
        private async Task LoadStockInData()
        {
            try
            {
                CollectSearchCondition();
                List<StockInItem> dataList = new List<StockInItem>();

                // ✅ 検索画面から渡された実データを優先して使用
                if (_searchResultData != null && _searchResultData.Count > 0)
                {
                    // ✅ RESTlet側で伝票(orderId)ごとにGROUP BY集計済み。
                    //    各OrderInfoが1枚の伝票に相当し（itemCount/totalQuantityはSQL層で算出済み）、
                    //    C#側での再グルーピングは不要。
                    dataList = _searchResultData.Select(order => new StockInItem
                    {
                        OrderId = order.orderId ?? "",
                        OrderNo = order.orderNumber ?? order.orderId ?? "",
                        Status = order.status ?? "未入庫",
                        SupplierName = order.supplierName ?? "",
                        ScheduleDate = order.scheduledDate?.ToString("yyyy-MM-dd") ?? "",
                        ItemCount = (int)(order.itemCount ?? 0),       // ✅ RESTletの集計値を直接使用
                        TotalQty = (int)(order.totalQuantity ?? 0),    // ✅ RESTletの集計値を直接使用
                        InboundType = order.inboundType,
                        ItemCode = order.itemCode,
                        ItemName = order.itemName
                    }).ToList();

                    // ページネーション計算
                    int totalRecordCount = dataList.Count;
                    _totalPage = (int)Math.Ceiling((double)totalRecordCount / PageSize);

                    if (_currentPage > _totalPage && _totalPage > 0)
                        _currentPage = _totalPage;

                    var pagedData = dataList
                        .Skip((_currentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                    dataList = pagedData;
                }
                else
                {
                    // ✅ 実データがない場合はモックデータを使用（メニューから直接遷移した場合など）
                    dataList = GetMockData();
                }

                RefreshPageUI();
                RenderCardList(dataList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStockInData Error: {ex.Message}");
                ShowErrorTip();
            }
        }

        /// <summary>
        /// モックデータを取得（メニューから直接遷移した場合用）
        /// </summary>
        private List<StockInItem> GetMockData()
        {
            var allData = new List<StockInItem>
            {
                new StockInItem
                {
                    OrderId = "MOCK-0114",
                    OrderNo = "PO-2026-0114",
                    Status = "未入庫",
                    SupplierName = "東菱電子部品(株)",
                    ScheduleDate = "2026-07-08",
                    ItemCount = 5,
                    TotalQty = 320
                },
                new StockInItem
                {
                    OrderId = "MOCK-0115",
                    OrderNo = "PO-2026-0115",
                    Status = "一部入庫",
                    SupplierName = "関東マテリアル(株)",
                    ScheduleDate = "2026-07-09",
                    ItemCount = 3,
                    TotalQty = 150
                },
                new StockInItem
                {
                    OrderId = "MOCK-0032",
                    OrderNo = "RMA-0032",
                    Status = "未処理",
                    SupplierName = "大和精密工業(株)",
                    ScheduleDate = "",
                    ItemCount = 2,
                    TotalQty = 40
                },
                new StockInItem
                {
                    OrderId = "MOCK-0116",
                    OrderNo = "PO-2026-0116",
                    Status = "未入庫",
                    SupplierName = "北陸金属工業(株)",
                    ScheduleDate = "2026-07-10",
                    ItemCount = 2,
                    TotalQty = 80
                }
            };

            // ページネーション計算
            int totalRecordCount = allData.Count;
            _totalPage = (int)Math.Ceiling((double)totalRecordCount / PageSize);

            if (_currentPage > _totalPage && _totalPage > 0)
                _currentPage = _totalPage;

            return allData
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
                nextPageBtn.BackgroundColor = _currentPage < _totalPage ? Colors.Transparent : Color.FromArgb("#e6e6e6");
            }
        }

        private async Task PrevPage()
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            _isDataLoaded = false;  // 再読み込みを許可
            await LoadStockInData();
        }

        private async Task NextPage()
        {
            if (_currentPage >= _totalPage) return;
            _currentPage++;
            _isDataLoaded = false;  // 再読み込みを許可
            await LoadStockInData();
        }
        #endregion

        #region 伝票カード描画
        private void RenderCardList(List<StockInItem> dataList)
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
                var cardFrame = CreateCardFrame(item);
                listContainer.Children.Add(cardFrame);
            }
        }

        private Frame CreateCardFrame(StockInItem item)
        {
            // ✅ 新增：受領可能な品目が無い伝票（ItemCount<=0）かどうかを判定。
            //    この場合はカード自体をタップ不可にし、詳細画面へは遷移させない。
            bool isReceivable = item.ItemCount > 0;

            var cardFrame = new Frame
            {
                // ✅ 受領不可の場合は背景をやや灰色にして「押せない」ことを視覚的に示す
                BackgroundColor = isReceivable ? Colors.White : Color.FromArgb("#f2f2f2"),
                CornerRadius = 8,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                HasShadow = false,
                // ✅ 受領不可の場合は全体を薄く表示（disabled 感を出す）
                Opacity = isReceivable ? 1.0 : 0.55
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
            var statusTag = CreateStatusTag(item.Status);

            Grid.SetRow(lblOrderNo, 0);
            Grid.SetColumn(lblOrderNo, 0);
            cardGrid.Children.Add(lblOrderNo);

            Grid.SetRow(statusTag, 0);
            Grid.SetColumn(statusTag, 1);
            cardGrid.Children.Add(statusTag);

            // ✅ 第1行：仕入先（RMA/POによる分岐判断は廃止し、統一表示）
            bool isReturnReceipt = item.InboundType != null && item.InboundType.IndexOf("返品") >= 0;
            string partnerLabelText = isReturnReceipt ? "顧客" : "仕入先";

            var lblSupplier = new Label
            {
                Text = $"{partnerLabelText}: {item.SupplierName}",
                FontSize = 12,
                TextColor = Colors.Gray
            };
            Grid.SetRow(lblSupplier, 1);
            Grid.SetColumnSpan(lblSupplier, 2);
            cardGrid.Children.Add(lblSupplier);

            // ✅ 第2行：入荷予定日（検索で取得した実データ。「返品理由:納出荷」のハードコードは廃止）
            var lblDate = new Label
            {
                Text = $"入荷予定日: {item.ScheduleDate}",
                FontSize = 12,
                TextColor = Colors.Gray
            };
            Grid.SetRow(lblDate, 2);
            Grid.SetColumnSpan(lblDate, 2);
            cardGrid.Children.Add(lblDate);

            // 品目数 / 数量（集計後の実統計値）
            // ✅ 受領不可（ItemCount<=0）の場合は、件数表示の代わりに注意メッセージを赤字で表示する
            var lblSummary = new Label
            {
                Text = isReceivable
                    ? $"品目数: {item.ItemCount} / 数量: {item.TotalQty}"
                    : "受領可能な品目がありません",
                FontSize = 12,
                TextColor = isReceivable ? Colors.Gray : Color.FromArgb("#c0392b")
            };
            Grid.SetRow(lblSummary, 3);
            Grid.SetColumnSpan(lblSummary, 2);
            cardGrid.Children.Add(lblSummary);

            cardFrame.Content = cardGrid;

            // ✅ 受領可能な品目がある場合のみ、タップジェスチャーを付与する。
            //    isReceivable=false の場合はジェスチャー自体を付けないため、
            //    カードをタップしても何も起こらず、詳細画面へは遷移しない。
            if (isReceivable)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                {
                    // ✅ 二重タップ防止：判定中は一旦ジェスチャーを外す
                    cardFrame.GestureRecognizers.Clear();

                    try
                    {
                        // ✅ 新增：一覧の ItemCount（検索結果の集計値）は >0 でも、
                        //    実際に詳細画面用のRESTlet（GetStockInDetail）を叩くと
                        //    PO_LINES が0件で返ってくるケースがある（データの整合性ズレ等）。
                        //    詳細画面へ遷移する前にここで実データの有無を確認し、
                        //    無ければ遷移させず、メッセージのみ表示する（カードの見た目は変更しない）。
                        bool hasItems = await CheckHasReceivableItems(item.OrderId, item.InboundType);

                        if (!hasItems)
                        {
                            await DisplayAlert("お知らせ", "対象の入庫明細（品目）が見つかりませんでした。", "OK");

                            // ✅ 灰色化はせず、メッセージのみ表示。再タップできるようジェスチャーを戻す。
                            cardFrame.GestureRecognizers.Add(tap);
                            return;
                        }

                        var detailInfo = new InputDetailInfo
                        {
                            OrderId = item.OrderId,                      // ✅ 新規追加：伝票IDを渡す
                            PoNo = item.OrderNo,
                            SupplierName = item.SupplierName,
                            ArrivalPlanDate = item.ScheduleDate,
                            ItemCount = item.ItemCount,
                            TotalQty = item.TotalQty,
                            Status = item.Status,
                            InboundType = item.InboundType,
                            ItemCode = item.ItemCode,
                            ItemName = item.ItemName
                        };

                        var detailPage = new EvangPL.Views.InputDetail.InputDetail(detailInfo);
                        await Navigation.PushAsync(detailPage);

                        // ✅ 詳細画面から戻ってきた場合に備えて、正常だったのでジェスチャーを再度付与しておく
                        cardFrame.GestureRecognizers.Add(tap);
                    }
                    catch (Exception ex)
                    {
                        // ✅ 予期せぬエラー時もタップを再度可能にしておく（一時的な通信エラー等の可能性があるため）
                        cardFrame.GestureRecognizers.Add(tap);
                        await DisplayAlert("エラー", $"画面遷移に失敗しました: {ex.Message}", "OK");
                    }
                };
                cardFrame.GestureRecognizers.Add(tap);
            }

            return cardFrame;
        }

        // ✅ 新增：詳細画面用RESTlet（GetStockInDetail）を叩いて、対象伝票に
        //    実際に受領可能な品目（PO_LINES）が存在するかどうかを確認する。
        //    一覧のItemCount（検索結果の集計値）だけでは実データとズレる可能性があるための保険。
        private async Task<bool> CheckHasReceivableItems(string? orderId, string? inboundType)
        {
            if (string.IsNullOrEmpty(orderId))
                return false;

            try
            {
                var request = new RequestData<EvangPL.Views.InputDetail.InputDetail.StockInDetailParam, EvangJsonModel>("GetStockInDetail");
                request.Info = new EvangPL.Views.InputDetail.InputDetail.StockInDetailParam
                {
                    OrderId = orderId,
                    ActionType = "SEARCH",
                    InboundType = inboundType
                };

                var result = await this.Post<EvangPL.Views.InputDetail.InputDetail.StockInDetailParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);

                if (result == null || !result.Success || result.SubData == null)
                    return false;

                foreach (var subData in result.SubData)
                {
                    if (subData.SubName == "PO_LINES")
                    {
                        var lines = BaseUtils.JsonToClass<List<EvangPL.Views.InputDetail.InputDetail.PoLineItem>>(subData.SubJson!);
                        return lines != null && lines.Count > 0;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // ✅ 通信エラー等が発生した場合も「受領不可」として扱い、詳細画面へは進ませない
                System.Diagnostics.Debug.WriteLine($"CheckHasReceivableItems Error: {ex.Message}");
                return false;
            }
        }

        private View CreateStatusTag(string statusText)
        {
            Color bgColor = Colors.Gray;
            switch (statusText)
            {
                case "未入庫":
                    bgColor = Color.FromArgb("#E68922");
                    break;
                case "一部入庫":
                    bgColor = Color.FromArgb("#255499");
                    break;
                case "未処理":
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

    #region 入庫用Model
    public class StockInPageInfo : EvangJsonModel
    {
        public string Keyword { get; set; } = "";
        public string Status { get; set; } = "";
        public string TargetDate { get; set; } = "";
        public string Supplier { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class StockInItem
    {
        public string? OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string Status { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string ScheduleDate { get; set; } = "";
        public int ItemCount { get; set; }
        public int TotalQty { get; set; }
        // ✅ 新規追加フィールド：詳細画面への情報受け渡し用
        public string? InboundType { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
    }

    public class StockInApiWrap
    {
        public List<StockInItem> List { get; set; } = new();
        public int TotalPage { get; set; }
        public int TotalCount { get; set; }
    }
    #endregion
}