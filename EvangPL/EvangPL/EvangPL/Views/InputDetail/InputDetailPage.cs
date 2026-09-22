using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using MauiIcons.Core;
using MauiIcons.Fluent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics.Text;
using System.Collections.Generic;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using InputDetailInfo = EvangPL.Utils.InputDetailInfo;

namespace EvangPL.Views.InputDetail
{
    public class InputDetail : EvangContentVM
    {
        private Border? pageHeaderInfo;
        private InputDetailInfo paramInfoToNext;

        // 全局滚动容器引用
        private VerticalStackLayout? _scrollContainer;
        private InputDetailInfo? _detailInfo;

        // ✅ RESTlet名（URL自体は Menu.cs の GetBaseMasterData() で LocalMemory.restlets に
        //    一元登録済み。ここではキー名のみを保持し、値(URL)はそちらを参照する。
        private const string RESTLET_GET_DETAIL = "GetStockInDetail";
        private const string RESTLET_SAVE_STOCKIN = "SaveStockIn";

        // ✅ RESTletに渡すActionType（検索/保存の識別用）
        private const string ACTION_SEARCH = "SEARCH";
        private const string ACTION_SAVE = "SAVE";

        // ✅ 3つの独立したデータソース
        //    ①PO自身の未入庫明細行（＝ヘッダー上部の選択テーブルの対象）
        private List<PoLineItem> _poLines = new List<PoLineItem>();
        //    ②実際に入庫済みの実績（現状ヘッダーには表示していないが、RESTletからは取得したままにしている）
        private List<ReceiptHistoryRow> _receiptHistory = new List<ReceiptHistoryRow>();
        //    ③入庫先ロケーション候補
        private List<LocationItem> _locationList = new List<LocationItem>();

        // ✅ [追加] 会計プリファレンス「受領書での超過を許可」(Allow Overage in Receipts / OVERRECEIPTS)。
        //    trueの場合、入庫数量が残数量(remainingQty)を超えていてもエラーにしない。
        //    RESTletのSEARCH結果（SubData: "PREFERENCES"）から取得する。取得できない場合は安全側(false)。
        private bool _allowOverReceipt = false;

        // ✅ [修正] ロット/シリアル入力の要否は、品目タイプ文字列(itemtype)の白名単一致ではなく、
        //    品目マスタの islotitem / isserialitem フラグ（PoLineItem.isLotItem / isSerialItem）で判定する。
        //    itemtypeの表記揺れ・想定外の値に左右されず、より確実に判定できるため。
        //    詳細は RequiresLotOrSerial() を参照。

        // ✅ 修正：「前明細/次明細」のページングを廃止。
        //    代わりに、ヘッダー上部の「未受領PO明細」テーブルで行をタップして選択する方式に変更。
        //    選択されていない（null）間は、明細登録エリアと登録済みロット表は非表示（空白）にする。
        private PoLineItem? _selectedPoLine = null;

        // ✅ 明細登録エリアの入力コントロール（Addボタン押下時に値を読むため保持）
        private Picker? _locationPicker;
        private Entry? _lotEntry;
        private Entry? _qtyEntry;

        // ✅ まだ保存していない「入力中の明細」リスト（+ロットを追加 で貯める。保存時にまとめてRESTletへ送る）
        //    複数の品目にまたがるので、各要素が対象品目(ItemId/ItemCode)を持つ
        private readonly List<PendingLotItem> _pendingLots = new List<PendingLotItem>();

        // ✅ 「明細登録」エリア内：選択中の品目に絞った小さいプレビュー表（ロット/数量のみ）
        private ContentView? _pendingLotTableHost;
        // ✅ ページ最下部：全品目分をまとめた「登録済み明細(N件)」表（品目/ロット/数量）
        private ContentView? _bottomPendingTableHost;

        // ✅ 入力コントロール（Picker/Entry）の統一スタイル用定数
        private static readonly Color InputBorderColor = Color.FromArgb("#cdd2dc");
        private static readonly Color InputBackgroundColor = Colors.White;
        private static readonly Color InputDisabledBackgroundColor = Color.FromArgb("#eceef1");
        private const int InputCornerRadius = 6;

        // ✅ 選択中の行をハイライトする背景色
        private static readonly Color SelectedRowColor = Color.FromArgb("#d7e8fa");

        public InputDetail() : base("strInputDetail")
        {
            // ✅ RESTlet URLは Menu.cs 側で一元登録済みのため、ここでは登録処理を行わない。
            _detailInfo = new InputDetailInfo();
            BuildUI();
        }

        private string? _orderId;
        public InputDetail(InputDetailInfo detailInfo) : base("strInputDetail")
        {
            // ✅ RESTlet URLは Menu.cs 側で一元登録済みのため、ここでは登録処理を行わない。
            _detailInfo = detailInfo;
            if (_detailInfo != null)
            {
                _orderId = _detailInfo.OrderId;
            }
            BuildUI();
        }

        // 読み込み中の中間表示は無し。データ取得→完全なUI構築を直接行う。
        private async void BuildUI()
        {
            try
            {
                await LoadDataFromApi();
            }
            catch (Exception ex)
            {
                // ✅ API呼び出しで例外が発生してもモックへのフォールバックは行わない。0件のまま画面を構築する。
                System.Diagnostics.Debug.WriteLine($"BuildUI Error: {ex.Message}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("エラー", $"初期データの読み込み中にエラーが発生しました: {ex.Message}", "OK"));
            }

            await BuildCompleteUI();
        }

        // ✅ 从后端API加载数据（PO头信息 / PO未入库明细行 / 入库实绩 / ロケーション候補 / 会計プリファレンス）
        private async Task LoadDataFromApi()
        {
            if (string.IsNullOrEmpty(_orderId))
            {
                System.Diagnostics.Debug.WriteLine("LoadDataFromApi: _orderId が空のため検索を行いません。呼び出し元でOrderIdが正しく渡っているか確認してください。");
                return;
            }

            try
            {
                var request = new RequestData<StockInDetailParam, EvangJsonModel>(RESTLET_GET_DETAIL);
                request.Info = new StockInDetailParam
                {
                    OrderId = _orderId,
                    ActionType = ACTION_SEARCH,
                    // ✅ 修正：前画面（InboundSearch等）から受け取った「入庫区分」をSEARCH時にもRESTletへ渡す。
                    //    これが無いと、RESTlet側は常にデフォルトの「発注入庫(PurchOrd)」として検索してしまい、
                    //    振替入庫(TrnfrOrd)・返品入庫(RtnAuth)のPO明細が0件になってしまう。
                    InboundType = _detailInfo?.InboundType
                };

                ResponseData<EvangJsonModel, EvangJsonModel>? result = null;
                try
                {
                    result = await this.Post<StockInDetailParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                }
                finally
                {
                    // ⚠️ TODO: ここでローディング遮罩(オーバーレイ)を確実に閉じる。
                    // EvangContentVM / Post 内部で ShowLoading() が呼ばれているなら、
                    // 対応する HideLoading() をここで必ず呼ぶこと（例外・エラー時も含めて）。
                    // 例: HideLoading();  もしくは  await HideLoadingAsync();
                }

                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadDataFromApi: サーバーからの応答がありません。");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", "サーバーからの応答がありません。", "OK"));
                    return;
                }

                if (!result.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: API Error - {result.ErrorMessage}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", result.ErrorMessage ?? "データ取得に失敗しました。", "OK"));
                    return;
                }

                ParseSearchResult(result);

                if (_detailInfo != null)
                {
                    // ✅ ヘッダー情報（仕入先/入荷予定日/PO番号）はPO明細行の先頭から取得
                    var firstLine = _poLines.FirstOrDefault();
                    if (firstLine != null)
                    {
                        if (!string.IsNullOrEmpty(firstLine.tranid))
                            _detailInfo.PoNo = firstLine.tranid;
                        if (!string.IsNullOrEmpty(firstLine.entityName))
                            _detailInfo.SupplierName = firstLine.entityName;
                        if (!string.IsNullOrEmpty(firstLine.scheduledDate))
                            _detailInfo.ArrivalPlanDate = firstLine.scheduledDate;
                    }

                    _detailInfo.ItemCount = _poLines.Count;
                    _detailInfo.TotalQty = _poLines.Sum(x => x.remainingQty);

                    if (string.IsNullOrEmpty(_detailInfo.Status))
                    {
                        _detailInfo.Status = "未入库";
                    }
                }

                System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: データ読み込み成功 - 未入庫明細{_poLines.Count}件 / 入庫実績{_receiptHistory.Count}件 / ロケーション{_locationList.Count}件 / 超過受領許可={_allowOverReceipt}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: API呼び出しエラー - {ex.Message}");

                // ⚠️ TODO: 例外時もローディング遮罩を確実に閉じる（上のfinallyで既にカバーされていれば不要）
                // HideLoading();

                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("エラー", $"データ取得中にエラーが発生しました: {ex.Message}", "OK"));
            }
        }

        // ✅ PO_LINES / RECEIPT_HISTORY / LOCATION_LIST / PREFERENCES の4ブロックをそれぞれ独立してパースする
        private void ParseSearchResult(ResponseData<EvangJsonModel, EvangJsonModel> result)
        {
            _poLines.Clear();
            _receiptHistory.Clear();
            _locationList.Clear();
            _allowOverReceipt = false; // ✅ [追加] 毎回リセット（取得できなければ安全側=false のまま）

            if (result.SubData == null || result.SubData.Count == 0)
                return;

            foreach (var subData in result.SubData)
            {
                try
                {
                    if (subData.SubName == "PO_LINES")
                    {
                        _poLines = BaseUtils.JsonToClass<List<PoLineItem>>(subData.SubJson!) ?? new List<PoLineItem>();
                        System.Diagnostics.Debug.WriteLine($"ParseSearchResult: PO_LINES 解析成功 - {_poLines.Count}件");
                    }
                    else if (subData.SubName == "RECEIPT_HISTORY")
                    {
                        _receiptHistory = BaseUtils.JsonToClass<List<ReceiptHistoryRow>>(subData.SubJson!) ?? new List<ReceiptHistoryRow>();
                        System.Diagnostics.Debug.WriteLine($"ParseSearchResult: RECEIPT_HISTORY 解析成功 - {_receiptHistory.Count}件");
                    }
                    else if (subData.SubName == "LOCATION_LIST")
                    {
                        System.Diagnostics.Debug.WriteLine($"ParseSearchResult: LOCATION_LIST 受信JSON = {subData.SubJson}");
                        _locationList = BaseUtils.JsonToClass<List<LocationItem>>(subData.SubJson!) ?? new List<LocationItem>();
                        System.Diagnostics.Debug.WriteLine($"ParseSearchResult: LOCATION_LIST 解析成功 - {_locationList.Count}件");
                    }
                    // ✅ [追加] 会計プリファレンス「受領書での超過を許可」(OVERRECEIPTS)。
                    //    RESTlet側で新たに追加してもらうSubData（詳細はRESTlet側の対応が必要）。
                    else if (subData.SubName == "PREFERENCES")
                    {
                        var pref = BaseUtils.JsonToClass<PreferenceInfo>(subData.SubJson!);
                        _allowOverReceipt = pref?.AllowOverReceipt ?? false;
                        System.Diagnostics.Debug.WriteLine($"ParseSearchResult: PREFERENCES 解析成功 - AllowOverReceipt={_allowOverReceipt}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ParseSearchResult: JSON解析エラー({subData.SubName}) - [{ex.GetType().Name}] {ex.Message}");
                }
            }
        }

        // ✅ 构建完整的UI
        private async Task BuildCompleteUI()
        {
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star }
                },
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // ✅ ヘッダー（仕入先/入荷予定日 + タップ選択可能な「未受領PO明細」テーブル）
            pageHeaderInfo = BuildHeader();

            // ✅ 明細登録エリア：品目が未選択の場合は空（＝表示なし）
            var detailInputArea = BuildDetailInputArea();

            // ✅ 底部：全品目分の未保存明細一覧
            _bottomPendingTableHost = new ContentView { Content = BuildBottomPendingTable() };

            var saveBtn = new Button
            {
                Text = "保存",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                Margin = new Thickness(10, 5, 10, 10),
                CornerRadius = 6
            };
            saveBtn.Clicked += OnSaveButtonClicked;

            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            _scrollContainer.Children.Add(pageHeaderInfo);          // [0]
            _scrollContainer.Children.Add(detailInputArea);         // [1]
            _scrollContainer.Children.Add(_bottomPendingTableHost); // [2]
            _scrollContainer.Children.Add(saveBtn);                 // [3]

            var scrollView = new ScrollView
            {
                Content = _scrollContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            mainGrid.Add(scrollView, 0, 0);
            Content = mainGrid;

            System.Diagnostics.Debug.WriteLine("BuildCompleteUI: UI構築完了");
        }

        // ==================== ヘッダー（仕入先/入荷予定日 + PO明細選択テーブル） ====================

        private Border BuildHeader()
        {
            var innerGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition()
                },
                ColumnDefinitions =
                 {
                    new ColumnDefinition(),
                    new ColumnDefinition()
                 },
                Padding = new Thickness(5, 5, 5, 8),
                BackgroundColor = Color.FromArgb("#edeff3")
            };

            // ✅ 新增：返品入庫(RtnAuth)の場合は「仕入先」ではなく「顧客」ラベルを表示する。
            //    InboundType に「返品」が含まれるかどうかで判定（他の入庫区分は従来どおり「仕入先」）。
            bool isReturnReceipt = _detailInfo?.InboundType != null && _detailInfo.InboundType.IndexOf("返品") >= 0;
            string partnerLabelText = isReturnReceipt ? "顧客" : "仕入先";

            innerGrid.Add(new Label { Text = partnerLabelText, FontSize = 12, TextColor = Colors.Gray });
            innerGrid.Add(new Label { Text = "入荷予定日", FontSize = 12, TextColor = Colors.Gray }, 1, 0);

            string supplier = _detailInfo?.SupplierName ?? "";
            string planDate = _detailInfo?.ArrivalPlanDate ?? "";

            var vendorBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(0, 0, 2, 15)
            };
            var vendorLabel = new Label { Text = supplier, FontSize = 14, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            vendorBorder.Content = vendorLabel;
            innerGrid.Add(vendorBorder, 0, 1);
            var dateBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(2, 0, 0, 15)
            };
            var dateLabel = new Label { Text = planDate, FontSize = 15, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            dateBorder.Content = dateLabel;
            innerGrid.Add(dateBorder, 1, 1);

            // ✅ 修正：「入庫済みロット」実績表 → タップで選択できる「未受領PO明細」テーブルに変更
            var poLineTable = BuildPoLineSelectionTable();
            Grid.SetRow(poLineTable, 2);
            Grid.SetColumnSpan(poLineTable, 2);
            innerGrid.Add(poLineTable);

            var headerBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(1)
            };
            headerBorder.Content = innerGrid;

            return headerBorder;
        }

        // ✅ 新增：「未受領PO明細」テーブル（品目 / 残数量 / 発注数量）。
        //    行をタップすると OnPoLineRowSelected が呼ばれ、その品目向けの明細登録エリアが開く。
        //    選択中の行は背景色をハイライトする。
        private Border BuildPoLineSelectionTable()
        {
            var headers = new List<string> { "品目", "残数量", "発注数量" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(2, GridUnitType.Star),
                new GridLength(1, GridUnitType.Star),
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

            for (int r = 0; r < _poLines.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var line = _poLines[r];
                bool isSelected = _selectedPoLine != null && _selectedPoLine.itemId == line.itemId;
                var rowBg = isSelected ? SelectedRowColor : Colors.White;

                var itemLabel = new Label { Text = line.itemCode, FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };
                var remainLabel = new Label { Text = line.remainingQty.ToString(), FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };
                var orderedLabel = new Label { Text = line.orderedQty.ToString(), FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };

                tableGrid.Add(itemLabel, 0, dataRowIndex);
                tableGrid.Add(remainLabel, 1, dataRowIndex);
                tableGrid.Add(orderedLabel, 2, dataRowIndex);

                // ✅ 行全体をタップ可能にする（3つのLabelそれぞれにジェスチャーを付与）
                var capturedLine = line; // クロージャ用
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnPoLineRowSelected(capturedLine);
                itemLabel.GestureRecognizers.Add(tapGesture);
                remainLabel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnPoLineRowSelected(capturedLine)) });
                orderedLabel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnPoLineRowSelected(capturedLine)) });
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

        // ✅ 新增：PO明細テーブルの行がタップされた時。選択品目を切り替えて、
        //    ヘッダー（選択ハイライト反映）と明細登録エリアの両方を再構築して差し替える。
        private void OnPoLineRowSelected(PoLineItem line)
        {
            _selectedPoLine = line;
            RefreshHeaderAndDetailArea();
        }

        // ✅ 新增：ヘッダーと明細登録エリアを再構築し、_scrollContainer の該当インデックスに差し替える
        private void RefreshHeaderAndDetailArea()
        {
            var newHeader = BuildHeader();
            var newDetailInputArea = BuildDetailInputArea();

            if (_scrollContainer != null && _scrollContainer.Children.Count > 1)
            {
                _scrollContainer.Children[0] = newHeader;
                _scrollContainer.Children[1] = newDetailInputArea;
            }
            pageHeaderInfo = newHeader;
        }

        // ✅ MauiIcons.Fluent フォントに依存しない「条码アイコン」を自前で描画するヘルパー
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
                Content = barsLayout
            };
        }

        // ✅ Picker / Entry を統一スタイルの Border で包むヘルパー
        //    disabled=true の場合、枠と背景をグレーアウトして「編集不可」を視覚的に示す
        private Border WrapInputControl(View control, bool showDropdownArrow = false, bool disabled = false)
        {
            View content = control;

            if (showDropdownArrow)
            {
                var innerGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };
                innerGrid.Add(control, 0, 0);

                var arrowLabel = new Label
                {
                    Text = "▼",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6b727c"),
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(6, 0, 2, 0),
                    InputTransparent = true
                };
                innerGrid.Add(arrowLabel, 1, 0);

                content = innerGrid;
            }

            return new Border
            {
                Stroke = InputBorderColor,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = InputCornerRadius },
                BackgroundColor = disabled ? InputDisabledBackgroundColor : InputBackgroundColor,
                Padding = new Thickness(8, 0),
                Content = content,
                Opacity = disabled ? 0.7 : 1.0
            };
        }

        // ==================== 明細登録エリア ====================

        // ✅ [修正] 選択中の品目がロット/シリアル入力必須かどうかを判定する。
        //    従来は itemtype 文字列を固定の白名单(LotOrSerialItemTypes)と比較していたが、
        //    itemtype の表記揺れ・想定外の値（NetSuite側の仕様変更や取得漏れ等）が原因で、
        //    本来ロット管理対象の品目が「管理対象外」と誤判定されるケースがあった。
        //    そのため、品目マスタが直接持つ islotitem / isserialitem フラグ
        //    （RESTletのSQLで取得し、PoLineItem.isLotItem / isSerialItem に格納済み）で
        //    判定する方式に変更。どちらか一方でもtrueであれば「ロット/シリアル入力必須」とする。
        private static bool RequiresLotOrSerial(PoLineItem? item)
        {
            if (item == null)
            {
                // データが取得できていない場合は、既存動作を壊さないよう「必須」扱いにしておく
                return true;
            }
            return item.isLotItem || item.isSerialItem;
        }

        // ✅ 修正：戻り値を View に変更。
        //    品目が未選択（_selectedPoLine == null）の場合は、明細登録エリアと登録済みロット表を
        //    まとめて非表示（IsVisible=false の空 ContentView、レイアウト上も場所を取らない）にする。
        private View BuildDetailInputArea()
        {
            if (_selectedPoLine == null)
            {
                return new ContentView { IsVisible = false };
            }

            var currentItem = _selectedPoLine;
            // ✅ [追加] この品目がロット/シリアル管理対象かどうか
            bool requiresLot = RequiresLotOrSerial(currentItem);
            // ✅ [追加] 発注入庫(PO Item Receipt)の場合のみ、PO明細行が持つロケーション/残数量を
            //    ロケーション欄・数量欄に自動セットする（振替入庫/返品入庫では従来どおり空欄のまま）。
            bool isPurchaseOrderReceipt = _detailInfo?.InboundType != null && _detailInfo.InboundType.IndexOf("発注") >= 0;

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

            var locRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 50 } }
            };
            locRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.Children.Add(new Label { Text = "入庫先ロケーション (スキャン可)", FontSize = 12, TextColor = Colors.Gray });

            var locationNames = _locationList
                .Select(l => l.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            // ✅ [デバッグ用] ロケーション一覧が空の場合はここで気づけるようにログを残す
            System.Diagnostics.Debug.WriteLine($"BuildDetailInputArea: locationNames件数={locationNames.Count}, isPurchaseOrderReceipt={isPurchaseOrderReceipt}, currentItem.locationName='{currentItem?.locationName}', isLotItem={currentItem?.isLotItem}, isSerialItem={currentItem?.isSerialItem}");

            // ✅ [修正] Picker の SelectedIndex を ItemsSource より先に設定すると、MAUIのPickerで
            //    選択が反映されないケースがあるため、まず ItemsSource を設定してからオブジェクト生成後に
            //    SelectedItem（文字列そのもの）で選択する方式に変更。SelectedIndexより確実。
            _locationPicker = new Picker
            {
                Title = "選択",
                BackgroundColor = Colors.Transparent,
                ItemsSource = locationNames
            };

            // ✅ [変更] 発注入庫の場合、PO明細行が持つロケーション名と一致するものを初期選択にする。
            //    見つからない場合（振替/返品入庫、またはPO側にロケーション未設定など）は
            //    従来どおり先頭にフォールバックする（候補が1件も無い場合は未選択のまま）。
            if (isPurchaseOrderReceipt && currentItem != null && !string.IsNullOrEmpty(currentItem.locationName)
                && locationNames.Contains(currentItem.locationName))
            {
                _locationPicker.SelectedItem = currentItem.locationName;
            }
            else if (locationNames.Count > 0)
            {
                _locationPicker.SelectedIndex = 0;
            }
            locRow.Add(WrapInputControl(_locationPicker, showDropdownArrow: true), 0, 1);
            locRow.Add(BuildBarcodeIcon(), 1, 1);
            layout.Children.Add(locRow);

            // ✅ [変更] ロット/シリアル欄のラベル：品目タイプに応じて注記を追加
            layout.Children.Add(new Label
            {
                Text = requiresLot ? "ロット / シリアル (スキャン可)" : "ロット / シリアル",
                FontSize = 12,
                TextColor = requiresLot ? Colors.Gray : Color.FromArgb("#a3a9b3")
            });

            var lotRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 50 } }
            };
            lotRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ✅ [変更] requiresLot=false の場合は Entry / バーコードアイコンを両方とも編集不可にする
            _lotEntry = new Entry
            {
                Placeholder = requiresLot ? "ロット/シリアルをスキャンまたは入力" : "入力不要（管理対象外の品目）",
                BackgroundColor = Colors.Transparent,
                IsEnabled = requiresLot
            };
            lotRow.Add(WrapInputControl(_lotEntry, disabled: !requiresLot), 0, 1);

            var lotBarcodeIcon = BuildBarcodeIcon();
            lotBarcodeIcon.IsEnabled = requiresLot;
            lotBarcodeIcon.Opacity = requiresLot ? 1.0 : 0.5;
            lotRow.Add(lotBarcodeIcon, 1, 1);
            layout.Children.Add(lotRow);

            var qtyRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 60 } }
            };
            // ✅ [変更] 発注入庫の場合、PO明細行の残数量(remainingQty)を数量欄の初期値としてセットする。
            //    ユーザーはそのまま使うことも、必要に応じて手動で変更することもできる。
            _qtyEntry = new Entry
            {
                Placeholder = "数量を入力",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.Transparent,
                Text = (isPurchaseOrderReceipt && currentItem != null) ? currentItem.remainingQty.ToString() : ""
            };
            qtyRow.Add(WrapInputControl(_qtyEntry), 0, 0);
            qtyRow.Add(new Label { Text = "個", VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            layout.Children.Add(new Label { Text = "入庫数量", FontSize = 12, TextColor = Colors.Gray });
            layout.Children.Add(qtyRow);

            // ✅ 選択中品目に絞った、未保存分のプレビュー表（ロット/数量のみ、品目列なし）
            _pendingLotTableHost = new ContentView { Content = BuildEditableLotTableForCurrentItem() };
            layout.Children.Add(_pendingLotTableHost);

            var addLotBtn = new Button
            {
                Text = "+ロットを追加",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#245a96"),
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 3,
                FontAttributes = FontAttributes.Bold
            };
            addLotBtn.Clicked += OnAddLotButtonClicked;
            layout.Children.Add(addLotBtn);

            border.Content = layout;
            return border;
        }

        // ✅ 「+ロットを追加」押下時。入力欄の値を検証し、選択中の品目に紐づけて _pendingLots に積む
        private async void OnAddLotButtonClicked(object? sender, EventArgs e)
        {
            var currentItem = _selectedPoLine;
            if (currentItem == null)
            {
                await DisplayAlert("エラー", "対象のアイテムがありません。上の一覧から品目を選択してください。", "OK");
                return;
            }

            // ✅ [追加] この品目がロット/シリアル管理対象かどうか
            bool requiresLot = RequiresLotOrSerial(currentItem);

            var selectedLocationName = _locationPicker?.SelectedItem as string;
            var lotNo = _lotEntry?.Text?.Trim() ?? "";
            var qtyText = _qtyEntry?.Text?.Trim();

            if (string.IsNullOrEmpty(selectedLocationName))
            {
                await DisplayAlert("エラー", "入庫先ロケーションを選択してください。", "OK");
                return;
            }

            // ✅ [変更] ロット/シリアル管理対象の品目のみ、ロット/シリアル未入力をエラーにする。
            //    管理対象外の品目（非在庫品目/値引き/通常在庫品目/アセンブリ等）は空のまま保存してよい。
            if (requiresLot && string.IsNullOrEmpty(lotNo))
            {
                await DisplayAlert("エラー", "ロット/シリアルを入力またはスキャンしてください。", "OK");
                return;
            }
            if (!requiresLot)
            {
                // 念のため：編集不可のはずだが、万一値が入っていても管理対象外品目には送らない
                lotNo = "";
            }

            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "入庫数量を正しく入力してください。", "OK");
                return;
            }

            // ✅ [変更] 数量チェック：
            //    ①同一品目について、既に_pendingLotsに積んである数量も合算したうえで残数量と比較する
            //      （1回の入力だけでなく、複数回「+ロットを追加」した合計が残数量を超えないようにする）
            //    ②会計プリファレンス「受領書での超過を許可」(OVERRECEIPTS)がtrueの場合はこのチェックをスキップする
            if (!_allowOverReceipt)
            {
                var alreadyPendingQty = _pendingLots
                    .Where(p => p.ItemId == currentItem.itemId)
                    .Sum(p => p.Qty);
                var totalQty = alreadyPendingQty + qty;

                if (totalQty > currentItem.remainingQty)
                {
                    string msg = alreadyPendingQty > 0
                        ? $"入庫数量の合計（既存{alreadyPendingQty}個＋今回{qty}個＝{totalQty}個）が残数量（{currentItem.remainingQty}個）を超えています。"
                        : $"入庫数量が残数量（{currentItem.remainingQty}個）を超えています。";
                    await DisplayAlert("エラー", msg, "OK");
                    return;
                }
            }

            var matchedLocation = _locationList.FirstOrDefault(l => l.Name == selectedLocationName);

            _pendingLots.Add(new PendingLotItem
            {
                ItemId = currentItem.itemId,
                ItemCode = currentItem.itemCode,
                LocationId = matchedLocation?.Id ?? "",
                LocationName = selectedLocationName,
                LotNo = lotNo,
                Qty = qty
            });

            if (_lotEntry != null) _lotEntry.Text = string.Empty;
            if (_qtyEntry != null) _qtyEntry.Text = string.Empty;

            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // ✅ 明細登録エリア内の小プレビュー表（選択中の品目のみ）を再構築して差し替える
        private void RefreshPendingLotTable()
        {
            if (_pendingLotTableHost != null)
            {
                _pendingLotTableHost.Content = BuildEditableLotTableForCurrentItem();
            }
        }

        // ✅ 底部の全品目一覧表を再構築して差し替える
        private void RefreshBottomPendingTable()
        {
            if (_bottomPendingTableHost != null)
            {
                _bottomPendingTableHost.Content = BuildBottomPendingTable();
            }
        }

        // ✅ ❌タップで該当行を _pendingLots から削除
        private void OnDeletePendingLot(PendingLotItem item)
        {
            _pendingLots.Remove(item);
            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // ✅ 選択中品目に絞った未保存プレビュー表（品目列なし：ロット/数量/❌）
        private Border BuildEditableLotTableForCurrentItem()
        {
            var currentItem = _selectedPoLine;
            var itemsForCurrent = currentItem == null
                ? new List<PendingLotItem>()
                : _pendingLots.Where(p => p.ItemId == currentItem.itemId).ToList();

            return BuildEditableLotTableInternal(itemsForCurrent, showItemColumn: false);
        }

        // ✅ 修正：_pendingLots が0件（＝初期表示、まだ何も登録していない状態）の場合は
        //    「登録済み明細(0件)」ごと非表示にする（見出しだけ出るのを防ぐ）
        private View BuildBottomPendingTable()
        {
            if (_pendingLots.Count == 0)
            {
                return new ContentView { IsVisible = false };
            }

            var container = new VerticalStackLayout { Spacing = 4 };
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({_pendingLots.Count}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });
            container.Children.Add(BuildEditableLotTableInternal(_pendingLots, showItemColumn: true));
            return container;
        }

        // ✅ _pendingLots から「削除ボタン付き」の一覧テーブルを作る共通実装
        //    showItemColumn=false: ロット/数量/❌（明細登録エリア内の選択中品目プレビュー用）
        //    showItemColumn=true : 品目/ロット/数量/❌（底部の全品目一覧用）
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
                // ✅ ロット未入力（管理対象外品目）の場合は "-" を表示
                var lotDisplayText = string.IsNullOrEmpty(item.LotNo) ? "-" : item.LotNo;
                tableGrid.Add(new Label { Text = lotDisplayText, FontSize = 11, Padding = new Thickness(4) }, col++, dataRowIndex);
                tableGrid.Add(new Label { Text = $"{item.Qty}個", FontSize = 11, Padding = new Thickness(4) }, col++, dataRowIndex);

                var deleteLabel = new Label
                {
                    Text = "❌",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Center
                };
                var capturedItem = item; // ✅ クロージャ用（実体参照なのでインデックスずれの心配がない）
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

        // ✅ 入力中の _pendingLots（全品目分）をまとめてRESTletへ送る。ActionType="SAVE" を明示
        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_orderId))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", "注文IDが指定されていません。", "OK"));
                    return;
                }

                if (_pendingLots.Count == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", "登録する明細がありません。「+ロットを追加」で入力してください。", "OK"));
                    return;
                }

                // ✅ [追加] 保存直前の最終防衛ライン：品目ごとに _pendingLots の合計数量が
                //    残数量(remainingQty)を超えていないか再チェックする（超過許可がfalseの場合のみ）。
                //    通常は「+ロットを追加」時点でチェック済みだが、
                //    ・PO明細の残数量がAPI再取得等で変わっていた
                //    ・将来的な実装変更で他の経路から_pendingLotsに追加された
                //    といったケースに備えた保険的チェック。
                if (!_allowOverReceipt)
                {
                    var overLimitGroups = _pendingLots
                        .GroupBy(p => p.ItemId)
                        .Select(g => new
                        {
                            ItemId = g.Key,
                            ItemCode = g.First().ItemCode,
                            TotalQty = g.Sum(x => x.Qty),
                            RemainingQty = _poLines.FirstOrDefault(l => l.itemId == g.Key)?.remainingQty
                        })
                        .Where(x => x.RemainingQty.HasValue && x.TotalQty > x.RemainingQty.Value)
                        .ToList();

                    if (overLimitGroups.Count > 0)
                    {
                        var firstOver = overLimitGroups.First();
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("エラー",
                                $"品目「{firstOver.ItemCode}」の入庫数量合計（{firstOver.TotalQty}個）が残数量（{firstOver.RemainingQty}個）を超えています。数量を見直してください。",
                                "OK"));
                        return;
                    }
                }

                var lotsToSave = _pendingLots.Select(p => new LotSaveItem
                {
                    ItemId = p.ItemId,
                    ItemCode = p.ItemCode,
                    LotNumber = p.LotNo,
                    Quantity = p.Qty,
                    LocationId = p.LocationId
                }).ToList();

                var saveParam = new StockInSaveParam
                {
                    OrderId = _orderId,
                    ActionType = ACTION_SAVE,
                    // ✅ 明細ごとにロケーションを持たせているので、ヘッダー代表値としては先頭行を入れておく
                    Location = lotsToSave.FirstOrDefault()?.LocationId,
                    Lots = lotsToSave,
                    // ✅ 画面遷移時に受け取った「入庫区分」をそのままRESTletへ渡す（画面上には表示しない）
                    InboundType = _detailInfo?.InboundType
                };

                var request = new RequestData<StockInSaveParam, EvangJsonModel>(RESTLET_SAVE_STOCKIN);
                request.Info = saveParam;

                ResponseData<EvangJsonModel, EvangJsonModel>? saveResult = null;
                try
                {
                    saveResult = await this.Post<StockInSaveParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                }
                finally
                {
                    // ⚠️ TODO: ここでローディング遮罩(オーバーレイ)を確実に閉じる。
                    // EvangContentVM / Post 内部で ShowLoading() が呼ばれているなら、
                    // 対応する HideLoading() をここで必ず呼ぶこと（例外・エラー時も含めて）。
                    // 例: HideLoading();  もしくは  await HideLoadingAsync();
                }

                if (saveResult == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", "サーバーからの応答がありません。", "OK"));
                    return;
                }

                if (!saveResult.Success)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", saveResult.ErrorMessage ?? "入庫保存に失敗しました。", "OK"));
                    return;
                }

                string receiptNumber = "";
                if (saveResult.SubData != null && saveResult.SubData.Count > 0)
                {
                    foreach (var subData in saveResult.SubData)
                    {
                        if (subData.SubName == "RECEIPT_INFO")
                        {
                            var receiptInfo = BaseUtils.JsonToClass<ReceiptInfo>(subData.SubJson!);
                            if (receiptInfo != null)
                            {
                                receiptNumber = receiptInfo.receiptNumber ?? "";
                            }
                            break;
                        }
                    }
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("成功", $"入庫保存が完了しました。\n入庫伝票番号: {receiptNumber}", "OK"));

                // ✅ [追加] 一覧画面（StockIn）に「この伝票の受領処理が完了した」ことを伝えておく。
                //    一覧画面へ戻った際に再度このカードをタップしても、
                //    「対象の入庫明細が見つかりません」という誤解を招くAlertが表示されないようにするため。
                EvangPL.Views.StockIn.StockIn.MarkOrderAsCompleted(_orderId);

                // ✅ 保存成功後：入力中リストをクリアし、選択状態もリセットしてから
                //    サーバーの最新明細/実績を再検索してUIを更新（＝再び未選択の空白状態から始まる）
                _pendingLots.Clear();
                _selectedPoLine = null;
                await LoadDataFromApi();
                await BuildCompleteUI();
            }
            catch (Exception ex)
            {
                // ⚠️ TODO: 例外時もローディング遮罩を確実に閉じる（上のfinallyで既にカバーされていれば不要）
                // HideLoading();

                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("エラー", $"保存処理中にエラーが発生しました: {ex.Message}", "OK"));
            }
        }

        // ==================== 数据模型 ====================

        // ✅ 入力中（未保存）の1行分の明細（どの品目向けかを保持する）
        public class PendingLotItem
        {
            public string ItemId { get; set; } = "";
            public string ItemCode { get; set; } = "";
            public string LocationId { get; set; } = "";
            public string LocationName { get; set; } = "";
            public string LotNo { get; set; } = "";
            public int Qty { get; set; }
        }

        // 入库明细请求参数（検索）
        public class StockInDetailParam : EvangJsonModel
        {
            public string? OrderId { get; set; }
            public string ActionType { get; set; } = "SEARCH";
            // ✅ 新增：入库区分（発注入庫(PO Item Receipt) / 返品入庫(Return Receipt) / 振替入庫(Transfer Receipt)）。
            //    前画面（InboundSearch等）から受け取った値をそのままRESTletへ渡す。
            //    これが無いと RESTlet 側は常にデフォルト（発注入庫）として検索してしまい、
            //    振替入庫・返品入庫のPO明細が正しく検索できない。
            public string? InboundType { get; set; }
        }

        // 入库保存请求参数
        public class StockInSaveParam : EvangJsonModel
        {
            public string? OrderId { get; set; }
            public string ActionType { get; set; } = "SAVE";
            public string? Location { get; set; }
            public List<LotSaveItem>? Lots { get; set; }
            // ✅ 新增：入库区分（発注入庫(PO Item Receipt) / 返品入庫(Return Receipt) / 振替入庫(Transfer Receipt)）
            //    画面上には表示しないが、前画面から受け取った値をそのままRESTletへ渡し、
            //    RESTlet側での受領処理の分岐に使用する。
            public string? InboundType { get; set; }
        }

        // ✅ ItemId（NetSuite内部ID）を持つ。RESTlet側の突合はItemCode(テキスト)ではなく
        //    ItemId(内部ID)で行うため、これが無いと複数品目のPOでロットが誤った行に設定される。
        public class LotSaveItem
        {
            public string? ItemId { get; set; }
            public string? ItemCode { get; set; }
            public string? LotNumber { get; set; }
            public int Quantity { get; set; }
            public string? LocationId { get; set; }
        }

        // ✅ PO自身の未入庫明細行（RESTletの PO_LINES から取得。ヘッダー上部の選択テーブルの対象）
        //    プロパティ名はRESTletが返すJSONのキー名と完全に一致させている（大文字/小文字を含む）
        public class PoLineItem
        {
            public string po_id { get; set; } = "";
            public string entityName { get; set; } = "";
            public string scheduledDate { get; set; } = "";
            public string tranid { get; set; } = "";
            public string lineId { get; set; } = "";
            public string itemId { get; set; } = "";
            public string itemCode { get; set; } = "";
            public string itemName { get; set; } = "";
            // ✅ NetSuiteのitemtype値（例: InvtPart / LotNumberedInventoryItem /
            //    SerializedInventoryItem / NonInvtPart / Discount / Assembly 等）。
            //    現状はロット/シリアル要否の判定には使用していない（isLotItem/isSerialItemを使用）。
            //    表示・ログ用途として引き続き保持。
            public string itemType { get; set; } = "";
            // ✅ [追加] 品目マスタの「ロット番号品目」チェックボックス(islotitem)。
            //    ロット/シリアル入力の要否判定の主キーとして使用する（RequiresLotOrSerial参照）。
            public bool isLotItem { get; set; }
            // ✅ [追加] 品目マスタの「シリアル番号品目」チェックボックス(isserialitem)。
            //    ロット/シリアル入力の要否判定の主キーとして使用する（RequiresLotOrSerial参照）。
            public bool isSerialItem { get; set; }
            // ✅ [追加] PO明細（行レベル。未設定時はヘッダーのロケーション）を発注入庫時の
            //    ロケーション自動セットに使用する
            public string locationId { get; set; } = "";
            public string locationName { get; set; } = "";
            public int orderedQty { get; set; }
            public int receivedQty { get; set; }
            public int remainingQty { get; set; }
        }

        // ✅ 実際に入庫済みの実績（RESTletの RECEIPT_HISTORY から取得。現状は表示に使用していないが、
        //    RESTletからのデータ取得自体は継続している）
        public class ReceiptHistoryRow
        {
            public string itemId { get; set; } = "";
            public string itemCode { get; set; } = "";
            public string lotnum { get; set; } = "";
            public int qty { get; set; }
        }

        // 入庫先ロケーション（GetStockInDetail RESTlet の LOCATION_LIST から取得）
        public class LocationItem
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
        }

        public class ReceiptInfo
        {
            public string? receiptNumber { get; set; }
            public string? ReceiptId { get; set; }
            public string? Status { get; set; }
        }

        // ✅ [追加] 会計プリファレンス情報（GetStockInDetail RESTlet の PREFERENCES から取得）
        public class PreferenceInfo
        {
            // 「受領書での超過を許可」(Allow Overage in Receipts / OVERRECEIPTS)
            public bool AllowOverReceipt { get; set; }
        }
    }
}