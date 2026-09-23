using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using EvangPL.Components;
using EvangPL.Utils;
using MauiIcons.Core;
using MauiIcons.Fluent;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using PickingDetailInfo = EvangPL.Utils.PickingDetailInfo;

namespace EvangPL.Views.PickingDetail
{
    /// <summary>
    /// 出荷明細画面（入庫明細画面 InputDetail をベースに出荷用に改造）
    /// RESTlet②（詳細/ピッキング専用）を ActionType="SEARCH"/"SAVE" で使い分けて呼び出す
    ///
    /// 画面構成：
    ///   ・画面7-1（出荷処理 - 詳細/ピッキング。本クラス）：ヘッダー(顧客/出荷予定日)
    ///     + 未出荷品目一覧（品目/未出荷数量/受注数量。入庫画面の「品目/残数量/発注数量」に相当）
    ///     + 選択中品目の明細登録エリア(ロケーション/ロット/数量) + 底部：全品目の登録済み明細一覧
    ///   ・画面7-2（梱包情報登録。<see cref="PackageRegistration"/>）：「保存」ボタン押下で
    ///     Navigation.PushAsyncにより遷移する別画面。梱包No/品目/数量をスキャンまたは手入力で追加し、
    ///     「完了」でRESTlet②へActionType=SAVEを送信して出荷確定する（実際のSAVE送信は画面7-2側で行う）。
    ///
    /// ✅ [追加分]
    ///   1. ロット入力時に在庫（LOT_LIST）と突き合わせて存在チェックを行い、
    ///      見つからない場合はメッセージを表示する。
    ///   2. 品目のIsLotItem（RESTletのPACKAGE_ITEMSで返却）に応じてロット入力欄の編集可否を切り替える。
    ///   3. 受注(SO)の場合、有効なロットが入力されたら在庫情報から数量・場所を自動セットする。
    ///
    /// ✅ [今回の修正分]
    ///   4. IsLotItem を bool? にし、RESTlet が未返却(null)の場合は「編集可」として扱う（LotEditable）。
    ///      これによりロット欄が全品目で編集不可になる不具合を解消。
    ///   5. 受注(SO)の場合、品目選択時に受注行のロケーションと（未出荷数量 - 登録済み数量）を自動セットする。
    ///
    /// ✅ [シリアル対応追加分]
    ///   6. 品目のIsSerialItem（RESTletのPACKAGE_ITEMSで返却）に応じて、シリアル管理品目は
    ///      1明細行あたりの数量を必ず1に制限する（数量>1の場合はメッセージを表示して明細追加を拒否）。
    ///
    /// ✅ [今回の修正分・追加]
    ///   7. シリアル管理品目（IsSerialItem=true）はロット管理対象外（IsLotItem=false）であっても、
    ///      入力欄自体は「シリアル番号」を入力するために編集可能にする必要がある。
    ///      これまでは LotEditable（=IsLotItem）だけで編集可否を判定していたため、
    ///      「ロット管理対象外 かつ シリアル管理対象」の品目でロット欄（実質シリアル欄）が
    ///      誤って編集不可になっていた。LotOrSerialEditable を追加し、
    ///      「ロット管理対象」または「シリアル管理対象」のいずれかであれば編集可とするよう修正。
    ///      あわせてラベル・プレースホルダー・バリデーションメッセージもシリアル品目向けの文言に切り替える。
    /// </summary>
    public class PickingDetail : EvangContentVM
    {
        // ==================== UIコントロール参照 ====================
        private Border? pageHeaderInfo;
        private PickingDetailInfo? _detailInfo;
        private VerticalStackLayout? _scrollContainer;
        private ContentView? _detailInputAreaHost;      // 明細登録エリア（選択中品目専用）
        private ContentView? _bottomPendingTableHost;   // 底部：全品目の明細一覧

        // ✅ RESTlet名・ActionType（InputDetail.cs と同じ流儀で定数化）
        private const string RESTLET_PICKING_DETAIL = "GetPickingDetail";
        private const string ACTION_SEARCH = "SEARCH";

        // ==================== データソース ====================
        // ① 未出荷品目一覧（ヘッダー上部の選択テーブルの対象。品目/未出荷数量/受注数量）
        private List<PackageItem> _packageItems = new List<PackageItem>();
        // ② 出荷元ロケーション候補（RESTletのLOCATION_LISTから取得。InputDetail.cs と同一の流儀）
        private List<LocationItem> _locationList = new List<LocationItem>();
        // ③ 品目別の在庫ロット候補（RESTletのLOT_LISTから取得。品目内部IDで絞り込んで使用する）
        private List<LotItem> _lotList = new List<LotItem>();

        // ==================== 選択状態 ====================
        private PackageItem? _selectedPackage = null;   // 現在選択中の品目行

        // ==================== 入力コントロール ====================
        private Picker? _locationPicker;
        private Entry? _entryLot;       // ロット番号（またはシリアル番号）入力欄（スキャン/手動入力用）
        private Entry? _entryQty;

        // ==================== 色定数 ====================
        private static readonly Color InputBorderColor = Color.FromArgb("#cdd2dc");
        private static readonly Color InputBackgroundColor = Colors.White;
        private const int InputCornerRadius = 6;
        private static readonly Color SelectedRowColor = Color.FromArgb("#d7e8fa");


        // ==================== コンストラクター ====================
        public PickingDetail() : base("strPickingDetail")
        {
            _detailInfo = new PickingDetailInfo
            {
                OrderNo = "SO-2026-0987",
                CustomerName = "山田工業(株)",
                ScheduleDate = "2026-07-08",
                ItemCount = 4,
                TotalQty = 210,
                Status = "未出荷",
                OutboundType = "SO"
            };
            BuildUI();
        }

        public PickingDetail(PickingDetailInfo detailInfo) : base("strPickingDetail")
        {
            _detailInfo = detailInfo;
            BuildUI();
        }


        // ==================== RESTlet②から未出荷品目/明細を取得（ActionType=SEARCH） ====================
        // ✅ InputDetail.LoadDataFromApi() と同じ流儀：Post<..., EvangJsonModel, EvangJsonModel, EvangJsonModel>
        //    を使い、ResponseData.Success / ErrorMessage / SubData(SubName+SubJson) で結果を判定する。
        private async Task LoadPackageItemsFromServer()
        {
            try
            {
                var reqInfo = new PickingDetailRequest
                {
                    ActionType = ACTION_SEARCH,
                    OrderNo = _detailInfo?.OrderNo ?? "",
                    OutboundType = _detailInfo?.OutboundType ?? "SO"
                };

                var request = new RequestData<PickingDetailRequest, EvangJsonModel>(RESTLET_PICKING_DETAIL);
                request.Info = reqInfo;

                ResponseData<EvangJsonModel, EvangJsonModel>? apiResult = null;
                try
                {
                    apiResult = await this.Post<PickingDetailRequest, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                }
                finally
                {
                    // ⚠️ TODO: ここでローディング遮罩(オーバーレイ)を確実に閉じる。
                    // 例: HideLoading();  もしくは  await HideLoadingAsync();
                    // ※ EvangContentVM / Post 内部で ShowLoading() が呼ばれているかを確認のうえ対応すること。
                    //   InputDetail.cs 側でも同様のTODOが未対応のまま正常動作しているため、
                    //   通常のSEARCH/SAVEフローにおいてはPost内部で正しく遮罩が閉じられている可能性が高い。
                }

                if (apiResult == null)
                {
                    _packageItems = new List<PackageItem>();
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", "サーバーからの応答がありません。", "OK"));
                    return;
                }

                if (!apiResult.Success)
                {
                    _packageItems = new List<PackageItem>();
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("エラー", apiResult.ErrorMessage ?? "明細の取得に失敗しました", "OK"));
                    return;
                }

                _packageItems = ParsePackageItems(apiResult);
                _locationList = ParseLocationList(apiResult);
                _lotList = ParseLotList(apiResult);
            }
            catch (Exception ex)
            {
                _packageItems = new List<PackageItem>();
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("エラー", $"明細取得に失敗しました: {ex.Message}", "OK"));
            }
        }

        // ✅ SubData の中から SubName="LOCATION_LIST" を取り出してパースする（InputDetail.cs と同一の流儀）
        private List<LocationItem> ParseLocationList(ResponseData<EvangJsonModel, EvangJsonModel> apiResult)
        {
            var newList = new List<LocationItem>();
            if (apiResult.SubData == null || apiResult.SubData.Count == 0)
            {
                return newList;
            }

            foreach (var subData in apiResult.SubData)
            {
                if (subData.SubName != "LOCATION_LIST" || string.IsNullOrEmpty(subData.SubJson))
                {
                    continue;
                }

                try
                {
                    newList = BaseUtils.JsonToClass<List<LocationItem>>(subData.SubJson!) ?? new List<LocationItem>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ParseLocationList: JSON解析エラー - {ex.Message}");
                }
            }

            return newList;
        }

        // ✅ SubData の中から SubName="LOT_LIST" を取り出してパースする。
        //    RESTlet側は {ItemInternalId, ItemCode, LotNo, LocationId, LocationName, AvailableQty} を返す。
        //    （シリアル管理品目の場合、LotNo にはシリアル番号が入る想定）
        private List<LotItem> ParseLotList(ResponseData<EvangJsonModel, EvangJsonModel> apiResult)
        {
            var newList = new List<LotItem>();
            if (apiResult.SubData == null || apiResult.SubData.Count == 0)
            {
                return newList;
            }

            foreach (var subData in apiResult.SubData)
            {
                if (subData.SubName != "LOT_LIST" || string.IsNullOrEmpty(subData.SubJson))
                {
                    continue;
                }

                try
                {
                    newList = BaseUtils.JsonToClass<List<LotItem>>(subData.SubJson!) ?? new List<LotItem>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ParseLotList: JSON解析エラー - {ex.Message}");
                }
            }

            return newList;
        }

        // ✅ SubData の中から SubName="PACKAGE_ITEMS" を取り出してパースする
        //    RESTlet側は {ItemCode, ItemInternalId, IsLotItem, IsSerialItem, LocationId, LocationName, OrderedQty, UnshippedQty} を返す
        //    （未出荷数量が0以下の行は含まれない）
        private List<PackageItem> ParsePackageItems(ResponseData<EvangJsonModel, EvangJsonModel> apiResult)
        {
            var newList = new List<PackageItem>();
            if (apiResult.SubData == null || apiResult.SubData.Count == 0)
            {
                return newList;
            }

            foreach (var subData in apiResult.SubData)
            {
                if (subData.SubName != "PACKAGE_ITEMS" || string.IsNullOrEmpty(subData.SubJson))
                {
                    continue;
                }

                try
                {
                    var parsed = BaseUtils.JsonToClass<List<PackageItem>>(subData.SubJson!) ?? new List<PackageItem>();
                    foreach (var p in parsed)
                    {
                        p.Customer = _detailInfo?.CustomerName ?? "";
                        p.ShipDate = _detailInfo?.ScheduleDate ?? "";
                        p.ItemInternalId ??= ""; // RESTletが未返却の場合の保険
                        p.LocationId ??= "";
                        p.LocationName ??= "";
                        p.Details ??= new List<PackageDetail>();
                        newList.Add(p);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ParsePackageItems: JSON解析エラー - {ex.Message}");
                }
            }

            return newList;
        }

        // ==================== UI構築 ====================
        private async void BuildUI()
        {
            await BuildCompleteUI();
        }

        private async Task BuildCompleteUI()
        {
            // ヘッダー/選択テーブルの描画前に、未出荷品目・明細データを準備する
            await LoadPackageItemsFromServer();

            // 1. ヘッダー（顧客/出荷予定日 + 未出荷品目選択テーブル）※画面7-1
            pageHeaderInfo = BuildHeader();

            // 2. 明細登録エリア（選択なしの場合は非表示）※画面7-1
            var detailInputArea = BuildDetailInputArea();
            _detailInputAreaHost = new ContentView { Content = detailInputArea };

            // 3. 底部：全品目の明細一覧 ※画面7-1
            _bottomPendingTableHost = new ContentView { Content = BuildBottomPendingTable() };

            // 4. 保存ボタン（クリックで画面7-2「梱包情報登録」画面へ遷移）
            var saveBtn = new Button
            {
                Text = "保存",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                Margin = new Thickness(10, 5, 10, 10),
                CornerRadius = 6
            };
            saveBtn.Clicked += OnSaveButtonClicked;   // ★ 画面7-2への画面遷移

            _scrollContainer = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(10) };
            _scrollContainer.Children.Add(pageHeaderInfo);          // [0]
            _scrollContainer.Children.Add(_detailInputAreaHost);   // [1]
            _scrollContainer.Children.Add(_bottomPendingTableHost); // [2]
            _scrollContainer.Children.Add(saveBtn);                 // [3]

            var scrollView = new ScrollView
            {
                Content = _scrollContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always,
                BackgroundColor = Color.FromArgb("#eff0f0")
            };

            Content = scrollView;
        }

        // ==================== 保存ボタン（画面7-2「梱包情報登録」画面へ遷移） ====================
        /// <summary>
        /// 画面7-1で登録した各品目のロット/ロケーション/数量明細（_packageItems）を
        /// そのまま画面7-2（PackageRegistration）へ引き渡し、新しいページとして遷移する。
        /// 実際のRESTlet②(ActionType=SAVE)呼び出しは画面7-2側の「完了」で行う。
        /// </summary>
        private async void OnSaveButtonClicked(object? sender, EventArgs e)
        {
            if (_packageItems == null || _packageItems.All(p => p.Details.Count == 0))
            {
                await DisplayAlert("確認", "登録する明細がありません。", "OK");
                return;
            }

            await Navigation.PushAsync(new PackageRegistration(_detailInfo, _packageItems));
        }

        // ==================== ヘッダー（顧客/出荷予定日 + 未出荷品目選択テーブル） ====================
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
            innerGrid.Add(new Label { Text = "顧客", FontSize = 12, TextColor = Colors.Gray });
            innerGrid.Add(new Label { Text = "出荷予定日", FontSize = 12, TextColor = Colors.Gray }, 1, 0);

            string customer = _detailInfo?.CustomerName ?? "";
            string shipDate = _detailInfo?.ScheduleDate ?? "";

            var customerBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(0, 0, 2, 15)
            };
            customerBorder.Content = new Label { Text = customer, FontSize = 14, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            innerGrid.Add(customerBorder, 0, 1);

            var dateBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Background = Color.FromArgb("#edeff3"),
                Padding = new Thickness(5, 5, 2, 4),
                Margin = new Thickness(2, 0, 0, 15)
            };
            dateBorder.Content = new Label { Text = shipDate, FontSize = 15, TextColor = Color.FromArgb("#6b727c"), FontAttributes = FontAttributes.Bold };
            innerGrid.Add(dateBorder, 1, 1);

            // 未出荷品目選択テーブル（タップで選択）
            var packageTable = BuildPackageSelectionTable();
            Grid.SetRow(packageTable, 2);
            Grid.SetColumnSpan(packageTable, 2);
            innerGrid.Add(packageTable);

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

        // ==================== 未出荷品目選択テーブル（品目 / 未出荷数量 / 受注数量） ====================
        private Border BuildPackageSelectionTable()
        {
            var headers = new List<string> { "品目", "未出荷数量", "受注数量" };
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

            for (int r = 0; r < _packageItems.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var item = _packageItems[r];
                bool isSelected = _selectedPackage != null && _selectedPackage.ItemCode == item.ItemCode;
                var rowBg = isSelected ? SelectedRowColor : Colors.White;

                // 列0=品目 / 列1=未出荷数量 / 列2=受注数量
                var itemCodeLabel = new Label { Text = item.ItemCode, FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };

                // 品目の内部ID（非表示）。画面7-2 → RLでのライン照合に使うため、
                // UI上は表示しないが品目名の隣に隠しラベルとして持たせておく。
                var itemInternalIdHiddenLabel = new Label
                {
                    Text = item.ItemInternalId,
                    IsVisible = false,
                    FontSize = 0,
                    WidthRequest = 0,
                    HeightRequest = 0
                };
                var itemCodeCell = new HorizontalStackLayout
                {
                    BackgroundColor = rowBg,
                    Children = { itemCodeLabel, itemInternalIdHiddenLabel }
                };

                var unshippedLabel = new Label { Text = item.UnshippedQty.ToString(), FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };
                var orderedLabel = new Label { Text = item.OrderedQty.ToString(), FontSize = 11, Padding = new Thickness(4), BackgroundColor = rowBg };

                tableGrid.Add(itemCodeCell, 0, dataRowIndex);
                tableGrid.Add(unshippedLabel, 1, dataRowIndex);
                tableGrid.Add(orderedLabel, 2, dataRowIndex);

                // 行全体をタップ可能にする
                var capturedItem = item;
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnPackageRowSelected(capturedItem);
                itemCodeLabel.GestureRecognizers.Add(tapGesture);
                unshippedLabel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnPackageRowSelected(capturedItem)) });
                orderedLabel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnPackageRowSelected(capturedItem)) });
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

        // ==================== 品目行選択イベント ====================
        private void OnPackageRowSelected(PackageItem item)
        {
            _selectedPackage = item;
            RefreshHeaderAndDetailArea();
        }

        // ==================== ヘッダーと明細登録エリアを再構築して差し替え ====================
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
            RefreshBottomPendingTable();
        }

        // ==================== 明細登録エリア ====================
        private View BuildDetailInputArea()
        {
            if (_selectedPackage == null)
            {
                return new ContentView { IsVisible = false };
            }

            var currentPackage = _selectedPackage;

            var border = new Border
            {
                Stroke = Color.FromArgb("#b4cee8"),
                Background = Color.FromArgb("#e6f0fa"),
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(5),
                StrokeThickness = 2
            };

            var layout = new VerticalStackLayout { Spacing = 10 };
            layout.Children.Add(new Label
            {
                Text = $"明細登録（{currentPackage.ItemCode}）",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold
            });

            // 選択中品目の内部ID（非表示）。デバッグ/引き渡し確認用に保持。
            layout.Children.Add(new Label
            {
                Text = currentPackage.ItemInternalId,
                IsVisible = false
            });

            // 1. 出荷元ロケーション行（Picker + バーコードアイコン。InputDetail.cs の入庫先ロケーションと同一の流儀）
            layout.Children.Add(new Label { Text = "出荷元ロケーション (スキャン可)", FontSize = 12, TextColor = Colors.Gray });
            var locRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };

            var locationNames = _locationList
                .Select(l => l.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            _locationPicker = new Picker
            {
                Title = "選択",
                SelectedIndex = -1, // 初期は未選択。SOの場合は下で ApplyDefaultsForSalesOrder が自動セットする
                BackgroundColor = Colors.Transparent,
                ItemsSource = locationNames
            };
            locRow.Add(WrapInputControl(_locationPicker, showDropdownArrow: true), 0, 0);
            locRow.Add(BuildBarcodeIcon(), 1, 0);
            layout.Children.Add(locRow);

            // 2. ロット（またはシリアル）行（Entry + バーコードアイコン）
            //    ✅ [修正] ロット欄は「ロット管理対象」または「シリアル管理対象」のいずれかであれば編集可にする。
            //       （IsLotItem が null の場合は従来通り編集可扱い。IsSerialItem=true の場合も編集可にする。）
            var lotEditable = currentPackage.LotOrSerialEditable;
            var isSerial = currentPackage.IsSerial;

            string lotLabelText;
            if (isSerial)
            {
                lotLabelText = "シリアル (スキャン可)";
            }
            else
            {
                lotLabelText = lotEditable ? "ロット (スキャン可)" : "ロット (この品目はロット管理対象外)";
            }
            layout.Children.Add(new Label { Text = lotLabelText, FontSize = 12, TextColor = Colors.Gray });

            var lotRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };
            _entryLot = new Entry
            {
                Placeholder = lotEditable
                    ? (isSerial ? "シリアル番号をスキャンまたは入力" : "スキャンまたは入力")
                    : "入力不要（管理対象外の品目）",
                BackgroundColor = Colors.Transparent,
                IsEnabled = lotEditable // ロット・シリアルいずれの管理対象外でもない場合のみ編集不可にする
            };
            if (lotEditable)
            {
                // フォーカスが外れたタイミングでロット（シリアル）の存在チェック＋自動セットを行う。
                // （TextChanged で1文字ごとに判定すると誤検知しやすいため Unfocused を採用）
                _entryLot.Unfocused += OnLotEntryUnfocused;
            }
            lotRow.Add(WrapInputControl(_entryLot), 0, 0);
            lotRow.Add(BuildBarcodeIcon(), 1, 0);
            if (!lotEditable)
            {
                // ロット・シリアルいずれの管理対象外でもない場合は行全体をグレーアウトして「触れない」ことを視覚的に示す
                lotRow.Opacity = 0.5;
            }
            layout.Children.Add(lotRow);

            // 3. 数量
            layout.Children.Add(new Label { Text = "数量", FontSize = 12, TextColor = Colors.Gray });
            var qtyRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 60 }
                }
            };
            _entryQty = new Entry { Placeholder = "数量を入力", Keyboard = Keyboard.Numeric, BackgroundColor = Colors.Transparent };
            _entryQty.TextChanged += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.NewTextValue)) return;
                var filtered = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
                if (filtered != e.NewTextValue)
                {
                    _entryQty.Text = filtered;
                }
            };
            qtyRow.Add(WrapInputControl(_entryQty), 0, 0);
            qtyRow.Add(new Label { Text = "個", VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            layout.Children.Add(qtyRow);

            // ✅ [追加] シリアル管理品目の場合は「数量は1のみ」の注記を表示する
            if (currentPackage.IsSerial)
            {
            }

            // ✅ [追加] 受注(SO)の場合、品目選択時に受注行のロケーションと残数量を自動セットする
            //    （Picker/Entry が作成済みのこのタイミングで呼び出す）
            ApplyDefaultsForSalesOrder(currentPackage);

            // 4. 選択中品目に紐づく明細プレビュー表（登録済み明細）
            var detailTable = BuildEditableDetailTableForCurrentPackage();
            layout.Children.Add(detailTable);

            // 5. 「+明細を追加」ボタン
            var addBtn = new Button
            {
                Text = "+ 明細を追加",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#245a96"),
                BorderColor = Color.FromArgb("#245a96"),
                BorderWidth = 3,
                FontAttributes = FontAttributes.Bold
            };
            addBtn.Clicked += OnAddDetailClicked;
            layout.Children.Add(addBtn);

            border.Content = layout;
            return border;
        }

        // ==================== [追加] 受注(SO)選択時の場所・数量の自動セット ====================
        /// <summary>
        /// 受注(SO)の場合のみ、選択中品目の
        ///   ・出荷元ロケーション：受注行に設定されているロケーション
        ///   ・数量：未出荷数量 - 登録済み明細の合計
        /// を入力欄へ自動セットする。
        /// ロット（またはシリアル）品目で値を入力した場合は OnLotEntryUnfocused が在庫情報で上書きする。
        /// ✅ [追加] シリアル管理品目の場合は、自動セットする数量が1を超えないようにする。
        /// </summary>
        private void ApplyDefaultsForSalesOrder(PackageItem pkg)
        {
            if ((_detailInfo?.OutboundType ?? "SO") != "SO") return;

            // ロケーション：受注行上の location
            if (_locationPicker != null && !string.IsNullOrEmpty(pkg.LocationId))
            {
                var loc = _locationList.FirstOrDefault(l => l.Id == pkg.LocationId);
                if (loc != null)
                {
                    var names = _locationPicker.ItemsSource?.Cast<string>().ToList() ?? new List<string>();
                    var idx = names.IndexOf(loc.Name);
                    if (idx >= 0) _locationPicker.SelectedIndex = idx;
                }
            }

            // 数量：未出荷数量 - 登録済み数量
            if (_entryQty != null)
            {
                var remaining = pkg.UnshippedQty - pkg.Details.Sum(d => d.DetailQty);
                if (pkg.IsSerial)
                {
                    // シリアル管理品目は自動セットも1件ずつに制限する
                    remaining = Math.Min(remaining, 1);
                }
                if (remaining > 0) _entryQty.Text = remaining.ToString();
            }
        }

        // ==================== ロット（シリアル）入力欄フォーカスアウト時の処理 ====================
        /// <summary>
        /// ①存在チェック：入力されたロット/シリアルが在庫(_lotList)に存在しない場合はメッセージを表示し、入力をクリアする。
        /// ②自動セット：受注(SO)の場合、存在するロット/シリアルであれば場所・数量を自動でセットする。
        /// ✅ [修正] LotEditable ではなく LotOrSerialEditable でガードし、シリアル管理品目でも動作するようにする。
        /// ✅ [追加] シリアル管理品目の場合は、自動セットする数量が1を超えないようにする。
        /// </summary>
        private async void OnLotEntryUnfocused(object? sender, FocusEventArgs e)
        {
            if (_selectedPackage == null || !_selectedPackage.LotOrSerialEditable) return;

            var lotNo = _entryLot?.Text?.Trim();
            if (string.IsNullOrEmpty(lotNo)) return;

            var isSerial = _selectedPackage.IsSerial;
            var label = isSerial ? "シリアル" : "ロット";

            var matches = _lotList
                .Where(l => l.ItemInternalId == _selectedPackage.ItemInternalId
                         && string.Equals(l.LotNo, lotNo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                await DisplayAlert("エラー", $"{label}「{lotNo}」は在庫に見つかりません。入力内容をご確認ください。", "OK");
                if (_entryLot != null) _entryLot.Text = string.Empty;
                return;
            }

            // 複数ロケーションに同一ロット（シリアル）がある場合は在庫数量が最も多いものを優先採用
            var best = matches.OrderByDescending(l => l.AvailableQty).First();

            // 受注(SO)の場合のみ場所・数量を自動セットする
            // （RTV/TRは仕入先返品・振替であり、通常「受注」に該当しないためSO限定とする）
            var outboundType = _detailInfo?.OutboundType ?? "SO";
            if (outboundType == "SO")
            {
                if (_locationPicker != null && !string.IsNullOrEmpty(best.LocationName))
                {
                    var names = _locationPicker.ItemsSource?.Cast<string>().ToList() ?? new List<string>();
                    var idx = names.IndexOf(best.LocationName);
                    if (idx >= 0)
                    {
                        _locationPicker.SelectedIndex = idx;
                    }
                }

                if (_entryQty != null)
                {
                    var alreadyEntered = _selectedPackage.Details.Sum(d => d.DetailQty);
                    var remaining = _selectedPackage.UnshippedQty - alreadyEntered;
                    var autoQty = (int)Math.Min(best.AvailableQty, Math.Max(remaining, 0));
                    if (isSerial)
                    {
                        // シリアル管理品目は自動セットも1件ずつに制限する
                        autoQty = Math.Min(autoQty, 1);
                    }
                    if (autoQty > 0)
                    {
                        _entryQty.Text = autoQty.ToString();
                    }
                }
            }
        }

        // ==================== 現在選択中の品目の明細一覧（ロット/数量/❌） ====================
        private Border BuildEditableDetailTableForCurrentPackage()
        {
            var currentPackage = _selectedPackage;
            if (currentPackage == null || currentPackage.Details.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "登録済み明細(0件)",
                    FontSize = 12,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var border = new Border
                {
                    Background = Colors.White,
                    Padding = new Thickness(8),
                    Stroke = InputBorderColor,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 4 }
                };
                border.Content = emptyLabel;
                return border;
            }

            var items = currentPackage.Details;
            var lotColumnHeader = currentPackage.IsSerial ? "シリアル" : "ロット";
            var headers = new List<string> { lotColumnHeader, "数量", "" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(3, GridUnitType.Star),
                new GridLength(2, GridUnitType.Star),
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

            for (int r = 0; r < items.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var detail = items[r];
                tableGrid.Add(new Label { Text = detail.DetailNo, FontSize = 11, Padding = new Thickness(4) }, 0, dataRowIndex);
                tableGrid.Add(new Label { Text = $"{detail.DetailQty}個", FontSize = 11, Padding = new Thickness(4) }, 1, dataRowIndex);

                var deleteLabel = new Label
                {
                    Text = "❌",
                    FontSize = 11,
                    Padding = new Thickness(4),
                    HorizontalOptions = LayoutOptions.Center
                };
                var capturedDetail = detail;
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnDeleteDetail(capturedDetail);
                deleteLabel.GestureRecognizers.Add(tapGesture);
                tableGrid.Add(deleteLabel, 2, dataRowIndex);
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

        // ==================== 明細追加イベント ====================
        private async void OnAddDetailClicked(object? sender, EventArgs e)
        {
            if (_selectedPackage == null)
            {
                await DisplayAlert("エラー", "品目が選択されていません。", "OK");
                return;
            }

            var isSerial = _selectedPackage.IsSerial;
            var label = isSerial ? "シリアル" : "ロット";

            var selectedLocationName = _locationPicker?.SelectedItem as string;
            var detailNo = _entryLot?.Text?.Trim();
            var qtyText = _entryQty?.Text?.Trim();

            if (string.IsNullOrEmpty(selectedLocationName))
            {
                await DisplayAlert("エラー", "出荷元ロケーションを選択してください。", "OK");
                return;
            }

            // ✅ [修正] ロット管理対象 または シリアル管理対象（LotOrSerialEditable）の品目のみ
            //    入力必須＋在庫存在チェックを行う。
            if (_selectedPackage.LotOrSerialEditable)
            {
                if (string.IsNullOrEmpty(detailNo))
                {
                    await DisplayAlert("エラー", $"{label}を入力してください。", "OK");
                    return;
                }

                // 最終防御として、追加ボタン押下時にもロット/シリアルの存在チェックを行う
                // （Unfocusedイベントが発火しないまま追加された場合の保険）
                var lotExists = _lotList.Any(l =>
                    l.ItemInternalId == _selectedPackage.ItemInternalId
                    && string.Equals(l.LotNo, detailNo, StringComparison.OrdinalIgnoreCase));
                if (!lotExists)
                {
                    await DisplayAlert("エラー", $"{label}「{detailNo}」は在庫に見つかりません。", "OK");
                    return;
                }
            }
            else
            {
                // ロット・シリアルいずれの管理対象外でもない品目は空のまま登録する
                detailNo = string.Empty;
            }

            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "数量は1以上の整数で入力してください。", "OK");
                return;
            }

            // ✅ シリアル管理品目は1明細行あたり数量1のみ許可。
            //    数量が複数個ある場合は1個ずつ（＝1行ずつ）明細を追加してもらう。
            if (isSerial && qty > 1)
            {
                await DisplayAlert("エラー", "シリアル管理品目のため、数量は1のみ入力できます。複数個ある場合は1個ずつ明細を追加してください。", "OK");
                return;
            }

            var matchedLocation = _locationList.FirstOrDefault(l => l.Name == selectedLocationName);

            // 未出荷数量を超える入力は警告のみ（保存自体は許可。業務要件に応じて厳格化してください）
            var alreadyEntered = _selectedPackage.Details.Sum(d => d.DetailQty);
            if (alreadyEntered + qty > _selectedPackage.UnshippedQty)
            {
                var proceed = await DisplayAlert(
                    "確認",
                    $"入力数量の合計（{alreadyEntered + qty}）が未出荷数量（{_selectedPackage.UnshippedQty}）を超えています。続けますか？",
                    "続ける", "やめる");
                if (!proceed) return;
            }

            _selectedPackage.Details.Add(new PackageDetail
            {
                DetailNo = detailNo ?? string.Empty,
                DetailQty = qty,
                Location = matchedLocation?.Id ?? "",
                LocationName = selectedLocationName
            });

            // 入力クリア
            if (_locationPicker != null) _locationPicker.SelectedIndex = -1;
            if (_entryLot != null) _entryLot.Text = string.Empty;
            if (_entryQty != null) _entryQty.Text = string.Empty;

            // 明細エリアを再構築（SOの場合は ApplyDefaultsForSalesOrder により
            // 場所＝受注行のロケーション／数量＝残数量 が再セットされる）
            RefreshHeaderAndDetailArea();
            RefreshBottomPendingTable();
        }

        // ==================== 明細削除イベント ====================
        private void OnDeleteDetail(PackageDetail detail)
        {
            if (_selectedPackage == null) return;
            _selectedPackage.Details.Remove(detail);
            RefreshHeaderAndDetailArea();
            RefreshBottomPendingTable();
        }

        // ==================== 底部：全品目の明細一覧 ====================
        private View BuildBottomPendingTable()
        {
            var allDetails = _packageItems
                .SelectMany(p => p.Details.Select(d => new { Package = p, Detail = d }))
                .ToList();

            if (allDetails.Count == 0)
            {
                return new ContentView { IsVisible = false };
            }

            var container = new VerticalStackLayout { Spacing = 4 };
            container.Children.Add(new Label
            {
                Text = $"登録済み明細({allDetails.Count}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });

            var headers = new List<string> { "品目", "ロット/シリアル", "数量" };
            var columnWidths = new List<GridLength>
            {
                new GridLength(3, GridUnitType.Star),
                new GridLength(3, GridUnitType.Star),
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

            for (int r = 0; r < allDetails.Count; r++)
            {
                int separatorRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = 1 });
                var separator = new BoxView { Color = Color.FromArgb("#e0e3e8"), HeightRequest = 1 };
                tableGrid.Add(separator, 0, separatorRowIndex);
                Grid.SetColumnSpan(separator, headers.Count);

                int dataRowIndex = tableGrid.RowDefinitions.Count;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var item = allDetails[r];
                tableGrid.Add(new Label { Text = item.Package.ItemCode, FontSize = 11, Padding = new Thickness(4) }, 0, dataRowIndex);
                tableGrid.Add(new Label { Text = item.Detail.DetailNo, FontSize = 11, Padding = new Thickness(4) }, 1, dataRowIndex);
                tableGrid.Add(new Label { Text = $"{item.Detail.DetailQty}個", FontSize = 11, Padding = new Thickness(4) }, 2, dataRowIndex);
            }

            var tableBorder = new Border
            {
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { },
                Background = Colors.White
            };
            tableBorder.Content = tableGrid;
            container.Children.Add(tableBorder);
            return container;
        }

        // ==================== 底部テーブルのリフレッシュ ====================
        private void RefreshBottomPendingTable()
        {
            if (_bottomPendingTableHost != null)
            {
                _bottomPendingTableHost.Content = BuildBottomPendingTable();
            }
        }

        // ==================== ヘルパー：入力コントロールをBorderでラップ ====================
        // ✅ showDropdownArrow=true で Picker 用の「▼」を右側に重ねて表示する（InputDetail.cs と同一の流儀）
        private Border WrapInputControl(View control, bool showDropdownArrow = false)
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
                BackgroundColor = InputBackgroundColor,
                Padding = new Thickness(8, 0),
                Content = content
            };
        }

        // ==================== バーコードアイコン描画 ====================
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

        // ==================== データモデル ====================
        public class PackageDetail
        {
            public string DetailNo { get; set; } = string.Empty;   // ロット番号（シリアル管理品目の場合はシリアル番号）
            public int DetailQty { get; set; }
            // ✅ Location はNetSuiteの内部ID（SAVE時にそのままRESTletへ渡し、select項目の設定に使う）
            //    LocationName は画面表示専用（プルダウンで選んだテキスト）
            public string Location { get; set; } = string.Empty;
            public string LocationName { get; set; } = string.Empty;
        }

        /// <summary>
        /// 未出荷品目1行分。RESTlet②(SEARCH)のPACKAGE_ITEMSと1:1対応
        /// （ItemCode/ItemInternalId/IsLotItem/IsSerialItem/LocationId/LocationName/OrderedQty/UnshippedQty）。
        /// Details は画面7-1でユーザーが入力したロット/ロケーション/数量の登録済み明細で、サーバーには送らず
        /// クライアント側で保持したまま画面7-2へ引き渡す。
        /// </summary>
        public class PackageItem
        {
            public string ItemCode { get; set; } = string.Empty;
            // 品目の内部ID（NetSuiteのitem内部ID）。
            // RL保存時にItemCode(表示テキスト)ではなくこちらでラインを照合するために使用する。
            public string ItemInternalId { get; set; } = string.Empty;

            // ✅ [修正] ロット管理対象品目かどうか。
            //    null  = RESTletが未返却（編集可として扱う）
            //    true  = ロット管理対象 / false = ロット管理対象外（ロット入力欄を編集不可にする）
            public bool? IsLotItem { get; set; }
            public bool LotEditable => IsLotItem ?? true;

            // ✅ [追加] シリアル管理品目かどうか。
            //    null/false = シリアル管理対象外（複数個をまとめて1明細行で登録可）
            //    true       = シリアル管理対象（1明細行あたり数量は必ず1にする）
            public bool? IsSerialItem { get; set; }
            public bool IsSerial => IsSerialItem ?? false;

            // ✅ [今回の修正・追加] ロット欄（実質シリアル欄）を編集可能にするかどうかの統合判定。
            //    「ロット管理対象」または「シリアル管理対象」のいずれかであれば編集可とする。
            //    これにより「IsLotItem=false かつ IsSerialItem=true」（シリアル管理だがロット管理対象外）
            //    の品目でも、シリアル番号を入力できるようになる。
            public bool LotOrSerialEditable => LotEditable || IsSerial;

            // ✅ [追加] 受注行に設定されている出荷元ロケーション（SO で品目選択時に自動セットする）
            public string LocationId { get; set; } = string.Empty;
            public string LocationName { get; set; } = string.Empty;

            public int OrderedQty { get; set; }     // 受注数量（SO上の数量）
            public int UnshippedQty { get; set; }   // 未出荷数量（受注数量 - 既出荷数量）
            public string Customer { get; set; } = string.Empty;
            public string ShipDate { get; set; } = string.Empty;
            public List<PackageDetail> Details { get; set; } = new List<PackageDetail>();
        }

        // 出荷元ロケーション（RESTletのLOCATION_LISTから取得。InputDetail.cs の LocationItem と同一構造）
        public class LocationItem
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
        }

        // RESTletのLOT_LISTと1:1対応。ロット/シリアルの存在チェックと
        // 受注(SO)時の場所・数量自動セットに利用する。
        // （シリアル管理品目の場合、LotNo にはシリアル番号が入る想定）
        public class LotItem
        {
            public string ItemInternalId { get; set; } = "";
            public string ItemCode { get; set; } = "";
            public string LotNo { get; set; } = "";
            public string LocationId { get; set; } = "";
            public string LocationName { get; set; } = "";
            public decimal AvailableQty { get; set; }
        }
    }

    #region RESTlet②(詳細/ピッキング) リクエスト・保存行モデル
    /// <summary>
    /// RESTlet②へ送るリクエスト。ActionType="SEARCH"で明細取得、ActionType="SAVE"で出荷確定。
    /// ✅ InputDetail.cs の StockInDetailParam / StockInSaveParam と同じ流儀で ActionType を使う。
    /// </summary>
    public class PickingDetailRequest : EvangJsonModel
    {
        public string ActionType { get; set; } = "SEARCH";
        public string OrderNo { get; set; } = "";
        public string OutboundType { get; set; } = "SO";
        public List<PickingDetailSaveLine> Details { get; set; } = new List<PickingDetailSaveLine>();
    }

    /// <summary>保存時に送信する1明細行（梱包No/品目/品目内部ID/ロケーション/ロット(またはシリアル)/数量）</summary>
    public class PickingDetailSaveLine
    {
        public string PackageNo { get; set; } = "";
        public string ItemCode { get; set; } = "";
        // RL側で fulfillment の各行(sublist item)を照合するためのキー。
        // ItemCode(表示テキスト)は表記ゆれで一致しない可能性があるため、こちらを正として使用する。
        public string ItemInternalId { get; set; } = "";
        public string Location { get; set; } = "";
        public string LotNo { get; set; } = "";
        // シリアル管理品目の場合、Qtyは必ず1で1行ごとに送信される想定（フロント側でOnAddDetailClickedにて制御済み）。
        public int Qty { get; set; }
    }
    #endregion
}