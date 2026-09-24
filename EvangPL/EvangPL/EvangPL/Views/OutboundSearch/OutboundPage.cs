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
// [追加] 検索条件を次画面(StockOut)へ渡すために名前空間を参照
using EvangPL.Views.StockOut;

namespace EvangPL.Views.OutboundSearch
{
    public class OutboundSearch : EvangContentVM
    {
        // ==========================================
        // [追加] Android原生の下線を消去するためのHandler登録
        // ==========================================
        static OutboundSearch()
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

        private Picker? outboundTypePicker;     // 出荷区分
        private Picker? statusPicker;           // ステータス
        private DatePicker? datePicker;         // 出荷予定日
        private Entry? keywordEntry;            // 受注番号 / 顧客名
        private Button? searchButton;           // 検索ボタン
        private Grid? filterFrame;
        private Grid? mainGrid;

        // ==========================================
        // [追加] Picker表示文言 → RESTletへ渡すコード値のマッピング
        // 　　　　（NetSuite側の分岐キーとして使用。並び順はPickerのItems.Add順と一致させること）
        // ==========================================
        private static readonly string[] OutboundTypeCodes = { "SO", "RTV", "TR" };
        // SO  = 受注出荷(SO Item Fulfillment)
        // RTV = 仕入先返品出荷(Return to Vendor)
        // TR  = 振替出荷(Transfer Shipment)

        private static readonly string[] StatusCodes = { "未出荷", "一部出荷" };

        public OutboundSearch() : base("strOutboundSearch")
        {
            Title = "出荷処理";
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

            // === 出荷区分 ===
            var outboundTypeTitle = new Label
            {
                Text = "出荷区分",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black
            };
            filterLayout.Children.Add(outboundTypeTitle);

            // === 出荷区分 (ピッカー) ===
            // [修正] PickerをBorderで包んで角丸・下線なしにする
            outboundTypePicker = new Picker
            {
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 40,
                FontSize = 12,
                Margin = new Thickness(10, 0)
            };
            outboundTypePicker.Items.Add("受注出荷(SO Item Fulfillment)");
            outboundTypePicker.Items.Add("仕入先返品出荷(Return to Vendor)");
            outboundTypePicker.Items.Add("振替出荷(Transfer Shipment)");
            outboundTypePicker.SelectedIndex = 0;

            // 選択時にタイトルを更新するロジック
            outboundTypePicker.SelectedIndexChanged += (sender, e) =>
            {
                if (outboundTypePicker.SelectedIndex >= 0)
                {
                    outboundTypePicker.Title = outboundTypePicker.SelectedItem?.ToString();
                }
                else
                {
                    outboundTypePicker.Title = "出荷区分を選択";
                }
            };

            var outboundTypeBorder = CreateInputBorder(outboundTypePicker);
            filterLayout.Children.Add(outboundTypeBorder);

            // === ステータス + 出荷予定日（2列レイアウト） ===
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
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 40,
                FontSize = 12,
                Margin = new Thickness(10, 0)
            };
            statusPicker.Items.Add("未出荷");
            statusPicker.Items.Add("一部出荷");
            statusPicker.SelectedIndex = 0;

            // 選択時にタイトルを更新するロジック
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

            // 出荷予定日
            var dateLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            var dateLabel = new Label
            {
                Text = "出荷予定日",
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

            // === 受注番号 / 顧客名 ===
            var keywordLabel = new Label
            {
                Text = "受注番号 / 顧客名",
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

            var kwRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },   // 入力欄
                    new ColumnDefinition { Width = 50 }                  // スキャンアイコン幅
                },
                ColumnSpacing = 8
            };
            kwRow.Add(CreateInputBorder(keywordEntry), 0, 0);

            var scanBorder = BuildBarcodeIcon();
            var scanTap = new TapGestureRecognizer();
            scanTap.Tapped += async (s, e) =>
            {
                await ScanHelper.ScanAsync(Navigation, (scannedCode) =>
                {
                    if (string.IsNullOrEmpty(scannedCode)) return;

                    Dispatcher.Dispatch(() =>
                    {
                        if (keywordEntry != null)
                        {
                            // スキャン結果を受注番号／顧客名欄へ反映
                            keywordEntry.Text = scannedCode;
                        }
                    });
                });
            };
            scanBorder.GestureRecognizers.Add(scanTap);
            kwRow.Add(scanBorder, 1, 0);

            filterLayout.Children.Add(kwRow);

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
        /// 検索ロジック：画面上の検索条件を組み立てて、出荷処理-一覧(StockOut)画面へ渡す
        /// </summary>
        private async Task OnbtnSearchClicked(object sender, EventArgs e)
        {
            string viewName = "StockOut";
            try
            {
                // [追加] 画面上の入力値から検索条件オブジェクトを組み立てる
                var condition = new StockOutPageInfo
                {
                    OutboundType = (outboundTypePicker?.SelectedIndex ?? 0) >= 0
                        && (outboundTypePicker?.SelectedIndex ?? -1) < OutboundTypeCodes.Length
                            ? OutboundTypeCodes[outboundTypePicker!.SelectedIndex]
                            : OutboundTypeCodes[0],
                    Status = (statusPicker?.SelectedIndex ?? -1) >= 0
                        && (statusPicker?.SelectedIndex ?? -1) < StatusCodes.Length
                            ? StatusCodes[statusPicker!.SelectedIndex]
                            : "",
                    TargetDate = datePicker?.Date.ToString("yyyy-MM-dd") ?? "",
                    Customer = keywordEntry?.Text?.Trim() ?? "",
                    Keyword = keywordEntry?.Text?.Trim() ?? "",
                    PageIndex = 1
                };

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

                    // [追加] 検索条件をStockOut画面へ設定（設定後、内部で自動的に検索がかかる）
                    if (vm is EvangPL.Views.StockOut.StockOut stockOutVm)
                    {
                        stockOutVm.SetSearchCondition(condition);
                    }

                    await Navigation.PushAsync(vm);
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
        #endregion

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