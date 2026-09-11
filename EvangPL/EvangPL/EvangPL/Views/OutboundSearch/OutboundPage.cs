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
                Title = "受注出荷(SO Item Fulfillment)",
                BackgroundColor = Colors.Transparent, // 背景を透明に
                TextColor = Colors.Black,
                HeightRequest = 40,
                FontSize = 12,
                Margin = new Thickness(10, 0) // 内側の余白
            };
            outboundTypePicker.Items.Add("受注出荷(SO Item Fulfillment)");
            outboundTypePicker.Items.Add("仕入先返品出荷(Return to Vendor)");
            outboundTypePicker.Items.Add("振替出荷(Transfer Shipment)");

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
                Title = "未出荷",
                BackgroundColor = Colors.Transparent, // 背景を透明に
                TextColor = Colors.Black,
                HeightRequest = 40,
                FontSize = 12,
                Margin = new Thickness(10, 0)
            };
            statusPicker.Items.Add("未出荷");
            statusPicker.Items.Add("一部出荷");

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
            string viewName = "StockOut";
            try
            {
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

                    // =========将来传递检索条件在这里，第二个参数传实体========
                    // SearchCondition cond = new SearchCondition();
                    // cond.Keyword = keywordEntry.Text;
                    // await Navigation.PushAsync(vm, cond);

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
    }
}