using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using EvangPL.Components;
using EvangPL.Utils;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using PickingDetailInfo = EvangPL.Utils.PickingDetailInfo;

namespace EvangPL.Views.PickingDetail
{
    /// <summary>
    /// 画面7-2：梱包情報登録
    /// 画面7-1（PickingDetail）で品目ごとに登録した明細（ロケーション/ロット/数量）を受け取り、
    /// ここで「梱包No.」と数量をスキャンまたは手入力で追加登録した上で、
    /// 「完了」押下時に画面7-1の明細と梱包Noを品目単位でマージし、
    /// RESTlet②（ActionType=SAVE）へ送信して出荷確定（Item Fulfillment作成）を行う。
    /// </summary>
    public class PackageRegistration : EvangContentVM
    {
        private const string RESTLET_PICKING_DETAIL = "GetPickingDetail";
        private const string ACTION_SAVE = "SAVE";

        private readonly PickingDetailInfo? _detailInfo;
        private readonly List<PickingDetail.PackageItem> _packageItems;

        // ==================== 入力コントロール ====================
        private Entry? _entryPackageNo;
        private Entry? _entryItemCode;        // ✅ 【修正】Picker → Entry（品目をテキスト入力に変更）
        private Entry? _entryQty;
        private CollectionView? _addedList;
        private Label? _addedCountLabel;

        // ✅ 品目候補リスト（初期値の自動入力に使用。入力補助が不要であれば未使用でもよい）
        private List<string> _itemCodeList = new List<string>();

        // ✅ [追加] ItemCode(表示テキスト) → ItemInternalId(内部ID) のマップ。
        //    画面7-1から引き継いだ _packageItems から構築する。
        private Dictionary<string, string> _itemInternalIdMap = new Dictionary<string, string>();

        // ✅ [追加] 現在入力中の品目の内部ID（非表示）。完了時にRESTletへ送信するItemInternalIdの元データ。
        private Label? _hiddenItemInternalIdLabel;

        public ObservableCollection<PackageBoxEntry> AddedPackageList { get; set; } = new ObservableCollection<PackageBoxEntry>();

        public PackageRegistration(PickingDetailInfo? detailInfo, List<PickingDetail.PackageItem> packageItems) : base("strPackageRegistration")
        {
            _detailInfo = detailInfo;
            _packageItems = packageItems ?? new List<PickingDetail.PackageItem>();

            // ✅ 品目リストを初期化（Detailsが1件以上ある品目のみ対象）
            _itemCodeList = _packageItems
                .Where(p => p.Details != null && p.Details.Count > 0)
                .Select(p => p.ItemCode)
                .Distinct()
                .ToList();

            // ✅ [追加] 品目コード → 内部IDのマップを構築（同一品目コードが複数あっても内部IDは先頭のものを採用）
            _itemInternalIdMap = _packageItems
                .Where(p => p.Details != null && p.Details.Count > 0)
                .GroupBy(p => p.ItemCode)
                .ToDictionary(g => g.Key, g => g.First().ItemInternalId ?? "");

            BuildUI();
        }

        // ==================== UI構築 ====================
        private void BuildUI()
        {
            var root = new VerticalStackLayout { Spacing = 15, Padding = new Thickness(20), BackgroundColor = Colors.White };

            // ---- ヘッダー行（← 戻る + 伝票番号） ----
            var backBtn = new Button
            {
                Text = "←",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#333333"),
                FontSize = 20,
                WidthRequest = 40,
                HorizontalOptions = LayoutOptions.Start
            };
            backBtn.Clicked += async (s, e) => await Navigation.PopAsync();

            var orderNoLabel = new Label
            {
                Text = _detailInfo?.OrderNo ?? "",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };

            var headerRow = new HorizontalStackLayout { Spacing = 10, Children = { backBtn, orderNoLabel } };
            root.Children.Add(headerRow);

            // ---- タイトル行（梱包を追加） ----
            var titleLabel = new Label
            {
                Text = "梱包を追加",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold
            };
            root.Children.Add(titleLabel);

            // ---- 梱包No. 行（Entry + バーコードアイコン） ----
            root.Children.Add(new Label { Text = "梱包No.(スキャン可)", FontSize = 12, TextColor = Colors.Gray });
            _entryPackageNo = new Entry {};
            var packageRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };
            packageRow.Add(WrapInputControl(_entryPackageNo), 0, 0);
            packageRow.Add(BuildBarcodeIcon(), 1, 0);
            root.Children.Add(packageRow);

            // ✅ 【修正】品目 行（Entry + バーコードアイコン。Pickerからテキスト入力に変更）----
            root.Children.Add(new Label { Text = "品目 (スキャン可)", FontSize = 12, TextColor = Colors.Gray });
            _entryItemCode = new Entry {};

            // ✅ [追加] 選択中品目の内部ID（非表示）。品目Entryの入力に連動して更新する。
            _hiddenItemInternalIdLabel = new Label { IsVisible = false };

            _entryItemCode.TextChanged += (s, e) =>
            {
                var code = _entryItemCode?.Text?.Trim();
                _hiddenItemInternalIdLabel.Text =
                    (!string.IsNullOrEmpty(code) && _itemInternalIdMap.TryGetValue(code, out var iid)) ? iid : "";
            };

            // ✅ 【修正】画面初期表示時に最初の品目コードを自動入力（従来のSelectedIndex=0相当）
            if (_itemCodeList.Count > 0)
            {
                var initialCode = _itemCodeList[0];
                _entryItemCode.Text = initialCode;
                // ✅ [追加] 初期入力に合わせて隠し内部IDラベルも同期しておく
                _hiddenItemInternalIdLabel.Text =
                    _itemInternalIdMap.TryGetValue(initialCode, out var initialIid) ? initialIid : "";
            }

            var itemRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };
            itemRow.Add(WrapInputControl(_entryItemCode), 0, 0);
            itemRow.Add(BuildBarcodeIcon(), 1, 0); // ✅ 品目もスキャン対応にするためバーコードアイコンを表示
            root.Children.Add(itemRow);
            root.Children.Add(_hiddenItemInternalIdLabel); // ✅ [追加] 非表示の内部IDラベルをツリーに追加

            // ---- 数量 ----
            root.Children.Add(new Label { Text = "数量", FontSize = 12, TextColor = Colors.Gray });
            _entryQty = new Entry {Keyboard = Keyboard.Numeric };
            root.Children.Add(WrapInputControl(_entryQty));

            // ---- 「この内容を追加」ボタン ----
            var addBtn = new Button
            {
                Text = "+ 梱包内容を追加",
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#255499"),
                BorderColor = Color.FromArgb("#255499"),
                BorderWidth = 2,
                CornerRadius = 8,
                Padding = new Thickness(12)
            };
            addBtn.Clicked += OnAddClick;
            root.Children.Add(addBtn);

            // ---- 追加済みの梱包 ----
            _addedCountLabel = new Label
            {
                Text = $"追加済みの梱包({AddedPackageList.Count}件)",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            };
            root.Children.Add(_addedCountLabel);

            var tableHeader = new Grid
            {
                BackgroundColor = Color.FromArgb("#e6edf7"),
                Padding = new Thickness(8),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            tableHeader.Add(new Label { Text = "梱包No.", FontAttributes = FontAttributes.Bold }, 0, 0);
            tableHeader.Add(new Label { Text = "品目", FontAttributes = FontAttributes.Bold }, 1, 0);
            var qtyHeader = new Label { Text = "数量", FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End };
            tableHeader.Add(qtyHeader, 2, 0);
            root.Children.Add(tableHeader);

            _addedList = new CollectionView
            {
                ItemsSource = AddedPackageList,
                ItemTemplate = new DataTemplate(() =>
                {
                    var row = new Grid
                    {
                        Padding = new Thickness(8),
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto),
                            new ColumnDefinition(GridLength.Auto)
                        }
                    };
                    var lb1 = new Label { VerticalOptions = LayoutOptions.Center };
                    lb1.SetBinding(Label.TextProperty, nameof(PackageBoxEntry.PackageNo));
                    var lb2 = new Label { VerticalOptions = LayoutOptions.Center };
                    lb2.SetBinding(Label.TextProperty, nameof(PackageBoxEntry.ItemCode));
                    var lb3 = new Label { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
                    lb3.SetBinding(Label.TextProperty, nameof(PackageBoxEntry.Qty));

                    // ✅ [追加] 内部ID（非表示）。行データとしては保持するがUIには出さない。
                    var lbHiddenInternalId = new Label { IsVisible = false };
                    lbHiddenInternalId.SetBinding(Label.TextProperty, nameof(PackageBoxEntry.ItemInternalId));

                    var deleteLabel = new Label
                    {
                        Text = "❌",
                        FontSize = 12,
                        VerticalOptions = LayoutOptions.Center,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    deleteLabel.SetBinding(BindingContextProperty, ".");

                    Grid.SetColumn(lb1, 0);
                    Grid.SetColumn(lb2, 1);
                    Grid.SetColumn(lb3, 2);
                    Grid.SetColumn(deleteLabel, 3);
                    row.Children.Add(lb1);
                    row.Children.Add(lb2);
                    row.Children.Add(lb3);
                    row.Children.Add(deleteLabel);
                    row.Children.Add(lbHiddenInternalId); // ✅ [追加]

                    var tap = new TapGestureRecognizer();
                    tap.Tapped += (s, e) =>
                    {
                        if (row.BindingContext is PackageBoxEntry entry)
                        {
                            AddedPackageList.Remove(entry);
                            RefreshAddedCount();
                        }
                    };
                    deleteLabel.GestureRecognizers.Add(tap);

                    return row;
                })
            };
            root.Children.Add(_addedList);

            // ---- 完了ボタン ----
            var finishBtn = new Button
            {
                Text = "完了",
                BackgroundColor = Color.FromArgb("#255499"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Margin = new Thickness(0, 10, 0, 0)
            };
            finishBtn.Clicked += async (s, e) => await OnFinishAndSaveAsync();
            root.Children.Add(finishBtn);

            var scrollView = new ScrollView
            {
                Content = root,
                BackgroundColor = Colors.White
            };
            Content = scrollView;
        }

        // ==================== 「+ この内容を追加」 ====================
        private async void OnAddClick(object? sender, EventArgs e)
        {
            var packageNo = _entryPackageNo?.Text?.Trim();
            // ✅ 【修正】Entryに入力された品目コードを取得
            var itemCode = _entryItemCode?.Text?.Trim();
            var qtyText = _entryQty?.Text?.Trim();

            if (string.IsNullOrEmpty(packageNo))
            {
                await DisplayAlert("エラー", "梱包Noは必須です。", "OK");
                return;
            }
            if (string.IsNullOrEmpty(itemCode))
            {
                await DisplayAlert("エラー", "品目を入力してください。", "OK");
                return;
            }
            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "数量は1以上を入力してください。", "OK");
                return;
            }

            // 画面7-1で登録した品目一覧に存在するかチェック
            var matchedItem = _packageItems.FirstOrDefault(p => p.ItemCode == itemCode);
            if (matchedItem == null)
            {
                var proceed = await DisplayAlert(
                    "確認",
                    $"品目[{itemCode}]は出荷対象の明細に見つかりません。続けますか？",
                    "続ける", "やめる");
                if (!proceed) return;
            }

            // ✅ [追加] 非表示ラベルに保持している内部IDを取得（万一未設定ならマップから再取得）
            var itemInternalId = _hiddenItemInternalIdLabel?.Text;
            if (string.IsNullOrEmpty(itemInternalId))
            {
                _itemInternalIdMap.TryGetValue(itemCode, out itemInternalId);
            }

            AddedPackageList.Add(new PackageBoxEntry
            {
                PackageNo = packageNo,
                ItemCode = itemCode,
                ItemInternalId = itemInternalId ?? "", // ✅ [追加]
                Qty = qty
            });

            if (_entryPackageNo != null) _entryPackageNo.Text = string.Empty;
            // ✅ 品目はクリアせず現在の入力を維持（連続登録の利便性向上）
            if (_entryQty != null) _entryQty.Text = string.Empty;

            RefreshAddedCount();

            if (_addedList != null && AddedPackageList.Count > 0)
            {
                _addedList.ScrollTo(AddedPackageList[AddedPackageList.Count - 1], position: ScrollToPosition.End);
            }
        }

        private void RefreshAddedCount()
        {
            if (_addedCountLabel != null)
            {
                _addedCountLabel.Text = $"追加済みの梱包({AddedPackageList.Count}件)";
            }
        }

        // ==================== 「完了」＝ RESTlet②へ ActionType=SAVE で送信 ====================
        private async Task OnFinishAndSaveAsync()
        {
            var allDetails = new List<PickingDetailSaveLine>();

            foreach (var item in _packageItems)
            {
                if (item.Details == null || item.Details.Count == 0) continue;

                var matchedPackage = AddedPackageList.FirstOrDefault(p => p.ItemCode == item.ItemCode);
                var packageNo = matchedPackage?.PackageNo ?? "";

                foreach (var d in item.Details)
                {
                    allDetails.Add(new PickingDetailSaveLine
                    {
                        PackageNo = packageNo,
                        ItemCode = item.ItemCode,
                        ItemInternalId = !string.IsNullOrEmpty(item.ItemInternalId)
                            ? item.ItemInternalId
                            : (matchedPackage?.ItemInternalId ?? ""),
                        Location = d.Location,
                        LotNo = d.DetailNo,
                        Qty = d.DetailQty
                    });
                }
            }

            if (allDetails.Count == 0)
            {
                await DisplayAlert("確認", "登録する明細がありません。", "OK");
                return;
            }

            var unassignedItems = allDetails.Where(d => string.IsNullOrEmpty(d.PackageNo))
                .Select(d => d.ItemCode).Distinct().ToList();
            if (unassignedItems.Count > 0)
            {
                var proceed = await DisplayAlert(
                    "確認",
                    $"梱包No.が未登録の品目があります（{string.Join(", ", unassignedItems)}）。続けますか？",
                    "続ける", "やめる");
                if (!proceed) return;
            }

            try
            {
                var reqInfo = new PickingDetailRequest
                {
                    ActionType = ACTION_SAVE,
                    OrderNo = _detailInfo?.OrderNo ?? "",
                    OutboundType = _detailInfo?.OutboundType ?? "SO",
                    Details = allDetails
                };

                var request = new RequestData<PickingDetailRequest, EvangJsonModel>(RESTLET_PICKING_DETAIL);
                request.Info = reqInfo;

                ResponseData<EvangJsonModel, EvangJsonModel>? saveResult = null;
                try
                {
                    saveResult = await this.Post<PickingDetailRequest, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                }
                finally
                {
                    // ⚠️ TODO: ローディング遮罩を確実に閉じる
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
                        DisplayAlert("エラー", saveResult.ErrorMessage ?? "保存に失敗しました", "OK"));
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("完了", "出荷処理が完了しました。", "OK"));

                // ✅ [修正] PopToRootAsync() → PopAsync() に変更。
                //    アプリの最初の画面まで戻ってしまう挙動をやめ、
                //    画面7-1（PickingDetail、この画面をPushしてきた呼び出し元）へ1つだけ戻る。
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("エラー", $"保存に失敗しました: {ex.Message}", "OK"));
            }
        }
        // ==================== ヘルパー：入力コントロールをBorderでラップ ====================
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
                Stroke = Color.FromArgb("#cdd2dc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                BackgroundColor = Colors.White,
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
    }

    /// <summary>画面7-2で追加登録した「梱包No.＋品目＋品目内部ID＋数量」の1行</summary>
    public class PackageBoxEntry
    {
        public string PackageNo { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        // ✅ [追加] 品目の内部ID（非表示項目由来）。将来的に梱包単位での照合が必要になった場合に備え保持。
        public string ItemInternalId { get; set; } = string.Empty;
        public int Qty { get; set; }
    }
}