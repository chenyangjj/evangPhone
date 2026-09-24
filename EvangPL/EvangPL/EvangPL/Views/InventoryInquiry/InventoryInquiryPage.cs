using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
// using static Microsoft.Maui.Controls.Button;

namespace EvangPL.Views.InventoryInquiry
{
    /// <summary>
    /// 画面16: 在庫照会 ViewModel
    /// </summary>
    public class InventoryInquiry : EvangContentVM
    {
        // ==========================================
        // [追加] Android原生の下線を消去するためのHandler登録
        // ==========================================
        static InventoryInquiry()
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
        }

        // ==========================================
        // UI コントロール宣言
        // ==========================================
        private Entry? itemCodeEntry;           // 商品番号
        private Border? scanButtonBorder;       // スキャンボタン (Border+Image方式に変更)
        private Picker? locationPicker;         // 倉庫 (ロケーション)
        private Button? searchButton;           // 検索ボタン

        private VerticalStackLayout? contentLayout; // 結果表示エリア
        private VerticalStackLayout? tableContainer; // テーブルコンテナ

        // 合計表示用
        private Entry? totalStockEntry;         // 合計在庫数
        private Entry? allocatedQtyEntry;       // 引当済数量

        // ==========================================
        // データ・状態管理
        // ==========================================
        private List<InventoryRecord>? currentData;

        private List<LocationData> localist;
        private List<TotalData> totallist;
        private int? locationId = 0;

        public InventoryInquiry() : base("strInventoryInquiry")
        {
            Title = "在庫照会";
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
                    new RowDefinition { Height = GridLength.Auto }, // 検索条件
                    new RowDefinition { Height = GridLength.Star }  // 結果一覧
                },
                BackgroundColor = Color.FromArgb("#eff1f5")
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
                Padding = new Thickness(10),
                Spacing = 10
            };

            return new ScrollView
            {
                Content = contentLayout,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
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
                Padding = new Thickness(10)
            };

            var filterLayout = new VerticalStackLayout
            {
                Spacing = 8
            };

            // === 1. 商品番号 (スキャン可) ===
            var itemLabel = new Label { Text = "商品番号(スキャン可)", FontSize = 12, TextColor = Colors.Gray };

            var itemRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 40 } // ボタン幅
                },
                ColumnSpacing = 10,
                //HeightRequest = 40 // 行全体の高さを固定
            };

            itemCodeEntry = new Entry
            {
                Text = "", // 仮データ
                BackgroundColor = Colors.Transparent,
                HeightRequest = 35, // Borderの高さと合わせる
                FontSize = 13,
                IsReadOnly = false,
                Margin = new Thickness(10, 0),
                VerticalOptions = LayoutOptions.Center
            };
            var itemBorder = CreateInputBorder(itemCodeEntry, Colors.White);

            // --- スキャンボタン (Border + Image + TapGesture) ---
            var scanImage = new Image
            {
                Source = "scan.png", // バーコードアイコン
                Aspect = Aspect.AspectFit,
                WidthRequest = 33,
                HeightRequest = 31,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            scanButtonBorder = new Border
            {
                Stroke = Color.FromArgb("#cccccc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                BackgroundColor = Colors.White,
                Padding = 0,
                HeightRequest = 35,
                WidthRequest = 40,
                Content = scanImage,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnScanClicked;
            scanButtonBorder.GestureRecognizers.Add(tapGesture);

            itemRow.Add(itemBorder, 0, 0);
            itemRow.Add(scanButtonBorder, 1, 0);

            filterLayout.Children.Add(itemLabel);
            filterLayout.Children.Add(itemRow);

            // === 2. 倉庫 (ロケーション) ===
            var locLabel = new Label { Text = "倉庫", FontSize = 12, TextColor = Colors.Gray, Margin = new Thickness(0, 5, 0, 0) };

            locationPicker = new Picker
            {
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35, // 高さを統一
                FontSize = 13,
                Margin = new Thickness(10, 0),
                VerticalOptions = LayoutOptions.Center
            };
            for (int i = 0; i < localist.Count; i++)
            {
                locationPicker.Items.Add(localist[i].name);
            }
            //locationPicker.Items.Add("すべて");
            //locationPicker.Items.Add("WH1-A-03");
            //locationPicker.Items.Add("WH2-C-01");
            locationPicker.SelectedIndex = 0;
            locationPicker.SelectedIndexChanged += (sender, e) =>
            {
                if (locationPicker.SelectedIndex >= 0)
                {
                    locationPicker.Title = locationPicker.SelectedItem?.ToString();
                }
                else
                {
                    locationPicker.Title = "ロケーションを選択";
                }
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

            var locBorder = CreateInputBorder(locationPicker, Colors.White);

            filterLayout.Children.Add(locLabel);
            filterLayout.Children.Add(locBorder);

            // === 3. 検索ボタン ===
            searchButton = new Button
            {
                Text = "検　索",
                BackgroundColor = Color.FromArgb("#245a96"),
                TextColor = Colors.White,
                HeightRequest = 45,
                CornerRadius = 5,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            // 【確認】イベントハンドラが正しく登録されているか
            searchButton.Clicked += async (sender, e) => await OnbtnSearchClicked(sender, e);

            filterLayout.Children.Add(searchButton);

            searchFilterGrid.Children.Add(filterLayout);
            return searchFilterGrid;
        }

        /// <summary>
        /// 角丸ボーダー付きの入力枠を作成するヘルパーメソッド
        /// </summary>
        private Border CreateInputBorder(View content, Color backgroundColor)
        {
            return new Border
            {
                Stroke = Color.FromArgb("#cccccc"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                BackgroundColor = backgroundColor,
                Padding = 0,
                Content = content,
                HeightRequest = 35
            };
        }

        // ==========================================
        // イベント処理
        // ==========================================

        /// <summary>
        /// スキャンボタンクリックイベント
        /// </summary>
        private async void OnScanClicked(object sender, EventArgs e)
        {
            // TODO: バーコードスキャナーライブラリ呼び出し
            await DisplayAlert("スキャン", "バーコードスキャナーを起動します (実装待ち)", "OK");
        }

        /// <summary>
        /// 検索ボタンクリック時処理
        /// </summary>
        /// <summary>
        /// 検索ボタンクリック時処理
        /// </summary>
        private async Task OnbtnSearchClicked(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(">>> [DEBUG] OnbtnSearchClicked START");

            try
            {
                var searchobj = new SearchParam();
                string itemCode = itemCodeEntry?.Text?.Trim() ?? "";
                //string location = locationPicker?.SelectedIndex >= 0
                //    ? locationPicker.SelectedItem?.ToString() ?? ""
                //    : "";

                //System.Diagnostics.Debug.WriteLine($">>> [DEBUG] Input: Item={itemCode}, Loc={location}");

                if (string.IsNullOrEmpty(itemCode))
                {
                    await DisplayAlert("エラー", "商品番号を入力してください。", "OK");
                    return;
                }

                
                if (contentLayout == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> [ERROR] contentLayout is NULL! Cannot update UI.");
                    await DisplayAlert("System Error", "結果表示エリアが初期化されていません。", "OK");
                    return;
                }
                searchobj.LocationId = locationId;
                searchobj.Keyword = itemCode;

                // 3. ローディング表示
                ClearContentContainer();
                ShowMessage("検索中...", Colors.Gray);
                System.Diagnostics.Debug.WriteLine(">>> [DEBUG] Loading message shown.");

                // 4. API呼び出しシミュレート
                // 注意: 如果这里卡住，UI会冻结。Task.Delay 应该是非阻塞的。
                await getsearchdata(searchobj);
                System.Diagnostics.Debug.WriteLine(">>> [DEBUG] Delay finished.");

                // 5. データ取得
                var resultData = currentData;
                System.Diagnostics.Debug.WriteLine($">>> [DEBUG] Data count: {resultData?.Count ?? 0}");

                if (resultData == null || resultData.Count == 0)
                {
                    ShowMessage("該当する在庫データはありません。", Colors.Gray);
                    return;
                }

                // 6. 結果表示
                
                ShowResult(resultData, itemCode);

                System.Diagnostics.Debug.WriteLine(">>> [DEBUG] OnbtnSearchClicked END Successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> [EXCEPTION] {ex}");
                
                try
                {
                    ShowMessage($"エラー: {ex.Message}", Colors.Red);
                }
                catch
                {
                    
                    await DisplayAlert("Critical Error", ex.ToString(), "OK");
                }
            }
        }

        /// <summary>
        /// 検索結果の表示
        /// </summary>
        private void ShowResult(List<InventoryRecord> data, string searchedItemCode)
        {
            try
            {
                ClearContentContainer(); // 再度クリアして確実に初期化
                currentData = data;


                // --- 結果タイトル ---
                var resultTitle = new Label
                {
                    Text = $"検索結果: {searchedItemCode}",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Black,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                contentLayout?.Children.Add(resultTitle);

                // --- テーブル表示 ---
                tableContainer = new VerticalStackLayout { Spacing = 0 };

                // ヘッダー行
                var headerGrid = new Grid
                {
                    BackgroundColor = Color.FromArgb("#e0e0e0"),
                    Padding = new Thickness(5, 8),
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }, // ロケーション
                        new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }, // ロット
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }    // 在庫数量
                    }
                };

                headerGrid.Children.Add(CreateHeaderLabel("ロケーション", 0));
                headerGrid.Children.Add(CreateHeaderLabel("ロット", 1));
                headerGrid.Children.Add(CreateHeaderLabel("在庫数量", 2));
                tableContainer.Children.Add(headerGrid);

                // データ行
                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    var rowGrid = new Grid
                    {
                        BackgroundColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#f9f9f9"),
                        Padding = new Thickness(5, 8),
                        ColumnDefinitions = headerGrid.ColumnDefinitions
                    };

                    rowGrid.Children.Add(CreateCellLabel(item.Location, 0, Colors.Black));
                    rowGrid.Children.Add(CreateCellLabel(item.LotNo, 1, Colors.Black));
                    // 数量は右寄せ
                    rowGrid.Children.Add(CreateCellLabel(item.Quantity.ToString(), 2, Colors.Black, TextAlignment.End));

                    tableContainer.Children.Add(rowGrid);

                    // 区切り線
                    var line = new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#ddd") };
                    tableContainer.Children.Add(line);
                }

                contentLayout?.Children.Add(tableContainer);

                // --- 合計表示エリア ---
                CreateSummarySection();
            }
            catch (Exception ex)
            {
                ShowMessage($"表示エラー: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 合計在庫数・引当済数量の表示エリア作成
        /// </summary>
        private void CreateSummarySection()
        {
            double? totalStock = totallist[0].TotalStock;
            double? allocatedQty = totallist[0].AllocatedQty;

            var summaryGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 10 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                Margin = new Thickness(0, 15, 0, 0),
                ColumnSpacing = 10
            };

            // 左: 合計在庫数
            var totalLayout = new VerticalStackLayout { Spacing = 2 };
            var totalLabelRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }
            };
            totalLabelRow.Children.Add(new Label { Text = "合計在庫数", FontSize = 11, TextColor = Colors.Gray });
            var refLabel1 = new Label { Text = "参照", FontSize = 10, TextColor = Colors.LightGray, HorizontalTextAlignment = TextAlignment.End };
            Grid.SetColumn(refLabel1, 1);
            totalLabelRow.Children.Add(refLabel1);

            totalLayout.Children.Add(totalLabelRow);

            totalStockEntry = new Entry
            {
                Text = totalStock.ToString(),
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.DimGray,
                HeightRequest = 35,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                IsReadOnly = true,
                HorizontalTextAlignment = TextAlignment.Start,
                Margin = new Thickness(10, 0)
            };
            var totalBorder = CreateInputBorder(totalStockEntry, Color.FromArgb("#e0e0e0")); // グレー背景
            totalLayout.Children.Add(totalBorder);

            // 右: 引当済数量
            var allocLayout = new VerticalStackLayout { Spacing = 2 };
            var allocLabelRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }
            };
            allocLabelRow.Children.Add(new Label { Text = "引当済数量", FontSize = 11, TextColor = Colors.Gray });
            var refLabel2 = new Label { Text = "参照", FontSize = 10, TextColor = Colors.LightGray, HorizontalTextAlignment = TextAlignment.End };
            Grid.SetColumn(refLabel2, 1);
            allocLabelRow.Children.Add(refLabel2);

            allocLayout.Children.Add(allocLabelRow);

            allocatedQtyEntry = new Entry
            {
                Text = allocatedQty.ToString(),
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.DimGray,
                HeightRequest = 35,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                IsReadOnly = true,
                HorizontalTextAlignment = TextAlignment.Start,
                Margin = new Thickness(10, 0)
            };
            var allocBorder = CreateInputBorder(allocatedQtyEntry, Color.FromArgb("#e0e0e0")); // グレー背景
            allocLayout.Children.Add(allocBorder);

            summaryGrid.Add(totalLayout, 0, 0);
            summaryGrid.Add(allocLayout, 2, 0);

            contentLayout?.Children.Add(summaryGrid);
        }

        // ==========================================
        // ヘルパーメソッド
        // ==========================================

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
                FontSize = 11,
                TextColor = textColor,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = align,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            Grid.SetColumn(label, col);
            return label;
        }

        private void ClearContentContainer()
        {
            if (contentLayout != null)
            {
                contentLayout.Children.Clear();
            }
            tableContainer = null;
        }

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
        /// 仮データ生成 (画像の内容に合わせる)
        /// </summary>
        private List<InventoryRecord> GetMockData(string itemCode)
        {
            // 画像のデータを再現
            // 注意: itemCode が "部品A-1010" の場合のみデータを返すようにしても良いですが、
            // テスト用に常にデータを返します。
            return new List<InventoryRecord>
            {
                new InventoryRecord("WH1-A-03", "LOT20260701", 118),
                new InventoryRecord("WH1-A-03", "LOT20260702", 40),
                new InventoryRecord("WH2-C-01", "LOT20260620", 60)
            };
        }

        private async Task getdata()
        {
            var searchParam = new SearchParam();
            searchParam.Kbn = "loca";
            var request = new RequestData<SearchParam, EvangJsonModel>("GetInventdetail");
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
            var request = new RequestData<SearchParam, EvangJsonModel>("GetInventdetail");
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
                        currentData = BaseUtils.JsonToClass<List<InventoryRecord>>(item.SubJson);
                        var a = 1;
                        break;
                    case "PH_TOTALDATA":
                        if (item == null || item.SubJson == null)
                            return;
                        totallist = BaseUtils.JsonToClass<List<TotalData>>(item.SubJson);
                        var aa = 1; 
                        break;
                }
            }
        }
    }

    // ==========================================
    // データモデル
    // ==========================================

    /// <summary>
    /// 在庫レコード
    /// </summary>
    public class InventoryRecord
    {
        public string Location { get; set; }    // ロケーション
        public string LotNo { get; set; }       // ロット
        public double Quantity { get; set; }       // 在庫数量

        public InventoryRecord(string location, string lotNo, double quantity)
        {
            Location = location;
            LotNo = lotNo;
            Quantity = quantity;
        }
    }

    public class TotalData : EvangJsonModel
    {
        public double? TotalStock { get; set; }
        public double? AllocatedQty { get; set; }
    }

    public class LocationData : EvangJsonModel
    {
        public int? id { get; set; }
        public string? name { get; set; }
    }

    public class SearchParam : EvangJsonModel
    {
        public string? Kbn { get; set; }
        public int? LocationId { get; set; }
        public string? Keyword { get; set; }
    }
}