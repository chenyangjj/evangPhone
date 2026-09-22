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

        // ✅ [追加] 品目ごとに既に在庫として存在するロット/シリアル番号一覧。
        //    RESTletのSEARCH結果（SubData: "EXISTING_SERIALS"）から取得する。
        //    シリアル管理品目の受領時、この一覧に既にある番号を入力した場合は
        //    保存前にエラーとする（NetSuiteの一意性制約による保存時エラー
        //    「次のシリアル番号は在庫アイテム[XXX]に既に存在します」を事前に防ぐ）。
        //    ※ ロット管理品目の番号も一緒に含まれるが、ロットは追加受領が正常なユースケース
        //    （同じロット番号に数量を積み増す）のため、isSerialItem=trueの品目に対してのみ
        //    この一覧との重複チェックを行う。
        private List<ExistingSerialItem> _existingSerials = new List<ExistingSerialItem>();

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

        // ✅ [追加] 自前のローディング遮罩（EvangContentVM側の挙動に依存せず、確実にShow/Hideする）
        //    BuildCompleteUI() 内で mainGrid の最上層に重ねて配置する。
        private Grid? _loadingOverlay;
        // ✅ [追加] 複数箇所（初期ロード/保存）からShow/Hideが重なっても正しく管理するための参照カウント
        private int _loadingRefCount = 0;

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

        // ✅ 从后端API加载数据（PO头信息 / PO未入库明细行 / 入库实绩 / ロケーション候補 / 会計プリファレンス / 既存ロット/シリアル番号）
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
                    // ✅ [追加] 自前のローディング遮罩を表示。
                    //    ※注意：LoadDataFromApi() は BuildCompleteUI() より前に呼ばれるため、
                    //    このタイミングでは _loadingOverlay がまだ null（画面自体が未構築）であり、
                    //    実際には何も表示されない。初期表示時の"画面全体が薄暗く見える"症状の原因は、
                    //    このRESTlet呼び出し自体ではなく、他の箇所（画面遷移アニメーションや
                    //    EvangContentVM側の共通処理）にある可能性が高い。
                    ShowLoading();
                    result = await this.Post<StockInDetailParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                }
                finally
                {
                    // ✅ [修正] 確実にローディング遮罩を閉じる（例外・エラー時も含めて必ず通る）
                    HideLoading();
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

                System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: データ読み込み成功 - 未入庫明細{_poLines.Count}件 / 入庫実績{_receiptHistory.Count}件 / ロケーション{_locationList.Count}件 / 超過受領許可={_allowOverReceipt} / 既存ロット・シリアル{_existingSerials.Count}件");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDataFromApi: API呼び出しエラー - {ex.Message}");

                // ✅ [補足] ローディング遮罩は内側のtry/finally（HideLoading()）で既に閉じられているため、
                //    ここで改めて呼ぶ必要はない。

                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("エラー", $"データ取得中にエラーが発生しました: {ex.Message}", "OK"));
            }
        }

        // ✅ PO_LINES / RECEIPT_HISTORY / LOCATION_LIST / PREFERENCES / EXISTING_SERIALS の
        //    5ブロックをそれぞれ独立してパースする
        private void ParseSearchResult(ResponseData<EvangJsonModel, EvangJsonModel> result)
        {
            _poLines.Clear();
            _receiptHistory.Clear();
            _locationList.Clear();
            _allowOverReceipt = false; // ✅ [追加] 毎回リセット（取得できなければ安全側=false のまま）
            _existingSerials.Clear(); // ✅ [追加] 毎回リセット

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
                    // ✅ [追加] 既存ロット/シリアル番号一覧（シリアル管理品目の重複入力事前チェック用）
                    else if (subData.SubName == "EXISTING_SERIALS")
                    {
                        _existingSerials = BaseUtils.JsonToClass<List<ExistingSerialItem>>(subData.SubJson!) ?? new List<ExistingSerialItem>();
                        System.Diagnostics.Debug.WriteLine($"ParseSearchResult: EXISTING_SERIALS 解析成功 - {_existingSerials.Count}件");
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

            // ✅ [追加] ローディング遮罩を同じセル（0,0）に重ねて配置し、常に最上層に表示されるようにする。
            //    Grid.Add は後から追加した要素ほど上に描画されるため、scrollViewの後に追加すればよい。
            _loadingOverlay = BuildLoadingOverlay();
            mainGrid.Add(_loadingOverlay, 0, 0);

            Content = mainGrid;

            System.Diagnostics.Debug.WriteLine("BuildCompleteUI: UI構築完了");
        }

        // ==================== ローディング遮罩（自前実装） ====================

        // ✅ [追加] ローディング遮罩本体を作成（半透明背景＋中央にインジケータ）。
        //    EvangContentVM側の遮罩に依存せず、本画面だけで確実にShow/Hideを制御するために自前実装した。
        private Grid BuildLoadingOverlay()
        {
            var overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#80000000"), // 半透明の黒
                IsVisible = false,
                InputTransparent = false // ✅ ローディング中はタップを透過させず、下の操作をブロックする
            };

            var indicator = new ActivityIndicator
            {
                IsRunning = false,
                Color = Colors.White,
                WidthRequest = 50,
                HeightRequest = 50,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            overlay.Children.Add(indicator);
            return overlay;
        }

        // ✅ [追加] ローディング表示開始（参照カウント方式：初期ロードと保存が重なっても安全に管理できる）
        private void ShowLoading()
        {
            _loadingRefCount++;
            if (_loadingOverlay != null)
            {
                _loadingOverlay.IsVisible = true;
                if (_loadingOverlay.Children.FirstOrDefault() is ActivityIndicator indicator)
                {
                    indicator.IsRunning = true;
                }
            }
        }

        // ✅ [追加] ローディング表示終了（参照カウントが0になった時だけ実際に非表示にする）
        //    必ず try/finally 内から呼ぶことで、成功時・例外時のいずれでも確実に閉じるようにする。
        private void HideLoading()
        {
            _loadingRefCount = Math.Max(0, _loadingRefCount - 1);
            if (_loadingRefCount == 0 && _loadingOverlay != null)
            {
                _loadingOverlay.IsVisible = false;
                if (_loadingOverlay.Children.FirstOrDefault() is ActivityIndicator indicator)
                {
                    indicator.IsRunning = false;
                }
            }
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
            //    ★[追加] シリアル管理品目の場合は「数量は必ず1」であることを併記し、ユーザーに事前に伝える
            string lotLabelText;
            if (!requiresLot)
            {
                lotLabelText = "ロット / シリアル";
            }
            else if (currentItem.isSerialItem)
            {
                lotLabelText = "ロット / シリアル (スキャン可) ";
            }
            else
            {
                lotLabelText = "ロット / シリアル (スキャン可)";
            }

            layout.Children.Add(new Label
            {
                Text = lotLabelText,
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
            if (requiresLot)
            {
                _lotEntry.Unfocused += OnLotEntryUnfocused;
            }
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
            //    ★[追加] ただしシリアル管理品目は「1件＝数量1」固定のため、初期値も常に"1"にする
            //    （残数量が2以上あっても、シリアル番号ごとに1件ずつ「+明細を追加」してもらう運用のため）。
            string initialQtyText;
            if (currentItem != null && currentItem.isSerialItem)
            {
                initialQtyText = "1";
            }
            else if (isPurchaseOrderReceipt && currentItem != null)
            {
                initialQtyText = currentItem.remainingQty.ToString();
            }
            else
            {
                initialQtyText = "";
            }

            _qtyEntry = new Entry
            {
                Placeholder = "数量を入力",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.Transparent,
                Text = initialQtyText
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
                Text = "+明細を追加",
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

            // ✅ [追加] シリアル管理品目の場合、入力された番号が既にNetSuite上の在庫として
            //    存在していないか事前チェックする。ここでブロックしないと、保存時にNetSuite側の
            //    一意性制約で「次のシリアル番号は在庫アイテム[XXX]に既に存在します」というエラーになり、
            //    ユーザーは保存を押すまで気づけない。
            //    （既存番号一覧はRESTletのSEARCH結果 EXISTING_SERIALS から取得済み。詳細はフィールド定義を参照）
            if (currentItem.isSerialItem)
            {
                bool alreadyExistsInSystem = _existingSerials.Any(s =>
                    s.ItemId == currentItem.itemId &&
                    string.Equals(s.SerialNo, lotNo, StringComparison.OrdinalIgnoreCase));
                if (alreadyExistsInSystem)
                {
                    await DisplayAlert("エラー",
                        $"シリアル番号「{lotNo}」は品目「{currentItem.itemCode}」の在庫として既に存在しています。別の番号を入力してください。",
                        "OK");
                    return;
                }

                // ✅ 今回のセッション内（未保存分）で同じ番号を重複入力していないかもチェック
                bool alreadyPendingSameSerial = _pendingLots.Any(p =>
                    p.ItemId == currentItem.itemId &&
                    string.Equals(p.LotNo, lotNo, StringComparison.OrdinalIgnoreCase));
                if (alreadyPendingSameSerial)
                {
                    await DisplayAlert("エラー",
                        $"シリアル番号「{lotNo}」は既にこの明細内に入力済みです。別の番号を入力してください。",
                        "OK");
                    return;
                }
            }

            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "入庫数量を正しく入力してください。", "OK");
                return;
            }

            // ✅ [追加] シリアル管理品目の場合、1件のロット入力（＝1シリアル番号）につき数量は1のみ許可。
            //    NetSuiteの在庫詳細の仕様上、シリアル番号1つに数量2以上を割り当てることはできないため
            //    （割り当てると「在庫詳細の合計数量はXとなる必要があります」等のエラーになる）、
            //    複数個ある場合はシリアル番号ごとに「+明細を追加」を繰り返してもらう必要がある。
            if (currentItem.isSerialItem && qty > 1)
            {
                await DisplayAlert("エラー", "シリアル管理品目のため、数量は1のみ入力できます。複数個ある場合は、シリアル番号を入力しなおして「+明細を追加」を繰り返してください。", "OK");
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
            // ✅ [変更] シリアル管理品目は次の入力でも数量1を初期値に戻す（都度1に固定される想定のため）
            if (_qtyEntry != null) _qtyEntry.Text = currentItem.isSerialItem ? "1" : string.Empty;

            RefreshPendingLotTable();
            RefreshBottomPendingTable();
        }

        // ✅ [追加] ロット/シリアル入力欄のフォーカスアウト時チェック。
        //    シリアル管理品目のみ対象：
        //    ①既にNetSuite上の在庫として存在する番号（_existingSerials）
        //    ②今回のセッション内で未保存のまま既に入力済みの番号（_pendingLots）
        //    のいずれかと一致する場合は、その場でエラー表示して入力をクリアする。
        //    「+明細を追加」ボタン押下時・保存時のチェックと内容は同じだが、
        //    フォーカスが外れた時点（スキャン直後含む）でより早く気づけるようにするためのもの。
        private async void OnLotEntryUnfocused(object? sender, FocusEventArgs e)
        {
            var currentItem = _selectedPoLine;
            if (currentItem == null || !currentItem.isSerialItem) return;

            var lotNo = _lotEntry?.Text?.Trim();
            if (string.IsNullOrEmpty(lotNo)) return;

            bool alreadyExistsInSystem = _existingSerials.Any(s =>
                s.ItemId == currentItem.itemId &&
                string.Equals(s.SerialNo, lotNo, StringComparison.OrdinalIgnoreCase));

            bool alreadyPendingSameSerial = _pendingLots.Any(p =>
                p.ItemId == currentItem.itemId &&
                string.Equals(p.LotNo, lotNo, StringComparison.OrdinalIgnoreCase));

            if (alreadyExistsInSystem || alreadyPendingSameSerial)
            {
                var reason = alreadyExistsInSystem
                    ? "既に在庫として存在しています"
                    : "既にこの明細内に入力済みです";

                await DisplayAlert("エラー",
                    $"シリアル番号「{lotNo}」は品目「{currentItem.itemCode}」に{reason}。別の番号を入力してください。",
                    "OK");

                if (_lotEntry != null) _lotEntry.Text = string.Empty;
            }
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

                // ✅ [追加] 保存直前の最終防衛ライン（その1）：シリアル管理品目について、
                //    _pendingLots内の各行の数量が1になっているかを再チェックする。
                //    通常は「+ロットを追加」時点でチェック済みだが、将来的な実装変更等に備えた保険的チェック。
                var serialItemIds = _poLines.Where(l => l.isSerialItem).Select(l => l.itemId).ToHashSet();
                var invalidSerialLot = _pendingLots.FirstOrDefault(p => serialItemIds.Contains(p.ItemId) && p.Qty != 1);
                if (invalidSerialLot != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー",
                            $"品目「{invalidSerialLot.ItemCode}」はシリアル管理品目のため、数量は1のみ指定可能です（ロット「{invalidSerialLot.LotNo}」の数量: {invalidSerialLot.Qty}）。",
                            "OK"));
                    return;
                }

                // ✅ [追加] 保存直前の最終防衛ライン（その2）：品目ごとに _pendingLots の合計数量が
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

                // ✅ [追加] 保存直前の最終防衛ライン（その3）：シリアル管理品目について、
                //    _pendingLots内の番号が既存在庫（_existingSerials）と重複していないかを再チェックする。
                //    通常は「+ロットを追加」時点でチェック済みだが、
                //    ・他端末/他ユーザーが同じ番号で先に受領していた（本画面表示後にNetSuite側の在庫が変化した）
                //    ・将来的な実装変更で他の経路から_pendingLotsに追加された
                //    といったケースに備えた保険的チェック。ここで検知できなかった場合でも、
                //    RESTlet側のsave()実行時にNetSuiteの一意性制約で最終的にブロックされる。
                var duplicateSerialLot = _pendingLots.FirstOrDefault(p =>
                    serialItemIds.Contains(p.ItemId) &&
                    _existingSerials.Any(s => s.ItemId == p.ItemId && string.Equals(s.SerialNo, p.LotNo, StringComparison.OrdinalIgnoreCase)));
                if (duplicateSerialLot != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー",
                            $"品目「{duplicateSerialLot.ItemCode}」のシリアル番号「{duplicateSerialLot.LotNo}」は既に在庫として存在しています。「登録済み明細」から削除し、別の番号で登録しなおしてください。",
                            "OK"));
                    return;
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
                    // ✅ [追加] 保存中はローディング遮罩を表示し、二重タップ等を防止する。
                    //    こちらは BuildCompleteUI() 実行後（画面表示後）に呼ばれるため、
                    //    _loadingOverlay は生成済みで、実際に画面が暗転して表示される。
                    ShowLoading();
                    saveResult = await this.Post<StockInSaveParam, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                }
                finally
                {
                    // ✅ [修正] 確実にローディング遮罩を閉じる（例外・エラー時も含めて必ず通る）
                    HideLoading();
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

                // ✅ [変更] 保存成功後は本画面を再構築せず、呼び出し元の一覧画面（StockIn）へ戻る。
                //    一覧画面側の OnAppearing で、保存済みの検索条件を使ってサーバーへ再検索をかけ、
                //    最新のステータス/数量を反映する（詳細は StockIn.OnAppearing / RefreshSearchFromServer 参照）。
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Navigation.PopAsync();
                });
            }
            catch (Exception ex)
            {
                // ✅ [補足] ローディング遮罩は内側のtry/finally（HideLoading()）で既に閉じられているため、
                //    ここで改めて呼ぶ必要はない。

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
            //    ロット/シリアル入力の要否判定の主キー、「数量は1のみ」制限の判定、
            //    および既存シリアル番号重複チェック（_existingSerialsとの突合）に使用する
            //    （RequiresLotOrSerial / OnAddLotButtonClicked / OnSaveButtonClicked 参照）。
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

        // ✅ [追加] 既存ロット/シリアル番号（GetStockInDetail RESTlet の EXISTING_SERIALS から取得）
        //    シリアル管理品目の受領時、入力された番号がここに存在すれば
        //    NetSuiteの一意性制約に反するため、保存前にブロックする。
        public class ExistingSerialItem
        {
            public string ItemId { get; set; } = "";
            public string SerialNo { get; set; } = "";
        }
    }
}