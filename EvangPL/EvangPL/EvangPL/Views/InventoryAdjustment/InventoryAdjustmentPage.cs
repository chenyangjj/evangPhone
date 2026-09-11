using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using System.Text.Json;

namespace EvangPL.Views.InventoryAdjustment
{
    public class InventoryAdjustment : EvangContentVM
    {
        // ==========================================
        // Android原生の下線を消去するためのHandler登録
        // ==========================================
        static InventoryAdjustment()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
        }

        // UI コントロール
        private Entry? itemEntry;
        private Border? scanButtonBorder;       // ← Button から Border に変更
        private Picker? locationPicker;
        private Entry? currentStockEntry;
        private Entry? differenceEntry;
        private Entry? adjustedStockEntry;
        private Picker? reasonPicker;
        private Button? registerButton;

        private Grid? mainGrid;

        private int _currentStockValue = 480;

        public InventoryAdjustment() : base("strInventoryAdjustment")
        {
            Title = "棚卸調整 - 新規登録";
            BuildUI();
            InitializeMockData();
        }

        private void BuildUI()
        {
            mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                },
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Padding = new Thickness(0)
            };

            var formContainer = new VerticalStackLayout
            {
                Spacing = 8,
                BackgroundColor = Colors.White,
                Padding = new Thickness(12)
            };

            // === 1. 品目 (スキャン可) ===
            var itemLabel = new Label { Text = "品目(スキャン可)", FontSize = 12, TextColor = Colors.Gray };

            var itemRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 40 }
                },
                ColumnSpacing = 10
            };

            itemEntry = new Entry
            {
                Text = "部品E-5050 / 洗浄前基板",
                BackgroundColor = Colors.Transparent,
                HeightRequest = 35,
                FontSize = 13,
                IsReadOnly = false,
                Margin = new Thickness(10, 0),
                VerticalOptions = LayoutOptions.Center
            };
            var itemBorder = CreateInputBorder(itemEntry, Colors.White);

            // --- スキャンボタン (Border + Image + TapGesture) ---
            var scanImage = new Image
            {
                Source = "scan.png",
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

            formContainer.Children.Add(itemLabel);
            formContainer.Children.Add(itemRow);

            // === 2. ロケーション + 現在庫数 (2列) ===
            var locStockGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 10 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                Margin = new Thickness(0, 5, 0, 0)
            };

            var locLayout = new VerticalStackLayout { Spacing = 2 };
            locLayout.Children.Add(new Label { Text = "ロケーション", FontSize = 11, TextColor = Colors.Gray });

            locationPicker = new Picker
            {
                Title = "WH1-A-05",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,
                FontSize = 13,
                Margin = new Thickness(10, 0)
            };
            locationPicker.Items.Add("WH1-A-05");
            locationPicker.Items.Add("WH1-A-06");
            locationPicker.Items.Add("WH2-B-01");
            locationPicker.SelectedIndexChanged += async (s, e) => await OnLocationChanged(s, e);

            var locBorder = CreateInputBorder(locationPicker, Colors.White);
            locLayout.Children.Add(locBorder);

            var stockLayout = new VerticalStackLayout { Spacing = 2 };
            stockLayout.Children.Add(new Label { Text = "現在庫数", FontSize = 11, TextColor = Colors.Gray });

            currentStockEntry = new Entry
            {
                Text = _currentStockValue.ToString(),
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.DimGray,
                HeightRequest = 35,
                FontSize = 13,
                IsReadOnly = true,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            var stockBorder = CreateInputBorder(currentStockEntry, Color.FromArgb("#e0e0e0"));
            stockLayout.Children.Add(stockBorder);

            locStockGrid.Add(locLayout, 0, 0);
            locStockGrid.Add(stockLayout, 2, 0);
            formContainer.Children.Add(locStockGrid);

            // === 3. 差異 + 調整後数量 (2列) ===
            var diffAdjGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 10 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                Margin = new Thickness(0, 5, 0, 0)
            };

            var diffLayout = new VerticalStackLayout { Spacing = 2 };
            diffLayout.Children.Add(new Label { Text = "差異", FontSize = 11, TextColor = Colors.Gray });

            differenceEntry = new Entry
            {
                Text = "-20",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.Transparent,
                HeightRequest = 35,
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            differenceEntry.TextChanged += OnDifferenceTextChanged;
            var diffBorder = CreateInputBorder(differenceEntry, Colors.White);
            diffLayout.Children.Add(diffBorder);

            var adjLayout = new VerticalStackLayout { Spacing = 2 };
            adjLayout.Children.Add(new Label { Text = "調整後数量", FontSize = 11, TextColor = Colors.Gray });

            adjustedStockEntry = new Entry
            {
                Text = CalculateAdjustedStock().ToString(),
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.DimGray,
                HeightRequest = 35,
                FontSize = 13,
                IsReadOnly = true,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            var adjBorder = CreateInputBorder(adjustedStockEntry, Color.FromArgb("#e0e0e0"));
            adjLayout.Children.Add(adjBorder);

            diffAdjGrid.Add(diffLayout, 0, 0);
            diffAdjGrid.Add(adjLayout, 2, 0);
            formContainer.Children.Add(diffAdjGrid);

            // === 4. 調整理由 ===
            var reasonLabel = new Label { Text = "調整理由", FontSize = 12, TextColor = Colors.Gray, Margin = new Thickness(0, 5, 0, 0) };

            reasonPicker = new Picker
            {
                Title = "破損",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black,
                HeightRequest = 35,          // ← 45 から 35 に統一
                FontSize = 13,               // ← 14 から 13 に統一
                Margin = new Thickness(10, 0)
            };
            reasonPicker.Items.Add("破損");
            reasonPicker.Items.Add("棚卸差異");
            reasonPicker.Items.Add("その他");

            reasonPicker.SelectedIndexChanged += (s, e) =>
            {
                if (reasonPicker.SelectedIndex >= 0)
                    reasonPicker.Title = reasonPicker.SelectedItem?.ToString();
                else
                    reasonPicker.Title = "調整理由を選択";
            };

            var reasonBorder = CreateInputBorder(reasonPicker, Colors.White);

            formContainer.Children.Add(reasonLabel);
            formContainer.Children.Add(reasonBorder);

            // === 5. 登録ボタン ===
            registerButton = new Button
            {
                Text = "調整を登録",
                BackgroundColor = Color.FromArgb("#245a96"),   // ← 色を統一
                TextColor = Colors.White,
                HeightRequest = 35,                             // ← 45 から 35 に
                CornerRadius = 5,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            registerButton.Clicked += async (s, e) => await OnRegisterClicked(s, e);
            formContainer.Children.Add(registerButton);

            mainGrid.Add(formContainer, 0, 0);

            // 【修正】Border で包む（他の画面と統一）
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
                HeightRequest = 35        // ← 40 から 35 に統一
            };
        }

        #region Logic & Events

        private void InitializeMockData()
        {
        }

        private int CalculateAdjustedStock()
        {
            int diff = 0;
            if (int.TryParse(differenceEntry?.Text, out int parsedDiff))
            {
                diff = parsedDiff;
            }
            return _currentStockValue + diff;
        }

        private void OnDifferenceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (adjustedStockEntry != null)
            {
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();
            }
        }

        private async Task OnLocationChanged(object sender, EventArgs e)
        {
            if (locationPicker == null) return;

            if (locationPicker.SelectedIndex >= 0)
                locationPicker.Title = locationPicker.SelectedItem?.ToString();
            else
                locationPicker.Title = "ロケーションを選択";

            if (locationPicker.SelectedIndex < 0)
            {
                await Task.CompletedTask;
                return;
            }

            string selectedLocation = locationPicker.SelectedItem?.ToString() ?? "";

            if (selectedLocation.Contains("A-05")) _currentStockValue = 480;
            else if (selectedLocation.Contains("A-06")) _currentStockValue = 120;
            else _currentStockValue = 0;

            if (currentStockEntry != null)
                currentStockEntry.Text = _currentStockValue.ToString();

            if (adjustedStockEntry != null)
                adjustedStockEntry.Text = CalculateAdjustedStock().ToString();

            await Task.CompletedTask;
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            await DisplayAlert("スキャン", "バーコードスキャナーを起動します (実装待ち)", "OK");
        }

        private async Task OnRegisterClicked(object sender, EventArgs e)
        {
            await DisplayAlert("完了", "在庫調整を登録しました (ダミー)", "OK");
        }

        #endregion
    }
}