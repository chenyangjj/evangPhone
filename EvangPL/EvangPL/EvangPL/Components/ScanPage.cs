using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace EvangPL.Components
{
    /// <summary>
    /// 汎用バーコード/QRコードスキャンページ
    /// どの画面からでも Navigation.PushAsync(new ScanPage()) で呼び出せる
    /// </summary>
    public class ScanPage : ContentPage
    {
        /// <summary>
        /// スキャン成功時に結果を回伝するイベント
        /// </summary>
        public event EventHandler<string>? BarcodeScanned;

        private readonly CameraBarcodeReaderView _cameraView;
        private bool _isHandled;

        public ScanPage()
        {
            Title = "スキャン";
            BackgroundColor = Colors.Black;

            _cameraView = new CameraBarcodeReaderView
            {
                IsDetecting = true,
                Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.TwoDimensional,
                    AutoRotate = true,
                    TryHarder = true,
                    Multiple = false
                }
            };
            _cameraView.BarcodesDetected += OnBarcodesDetected;

            var hintLabel = new Label
            {
                Text = "QRコードを枠内に合わせてください",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#80000000"),
                Padding = new Thickness(10, 6),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 60),
                FontSize = 13
            };

            var grid = new Grid();
            grid.Add(_cameraView);
            grid.Add(hintLabel);

            Content = grid;
        }

        private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            if (_isHandled) return;

            var first = e.Results?.FirstOrDefault();
            if (first == null || string.IsNullOrWhiteSpace(first.Value)) return;

            _isHandled = true;
            _cameraView.IsDetecting = false;

            Dispatcher.Dispatch(async () =>
            {
                try
                {
                    BarcodeScanned?.Invoke(this, first.Value);
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScanPage] クローズ例外: {ex}");
                }
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                _cameraView.IsDetecting = false;
                _cameraView.BarcodesDetected -= OnBarcodesDetected;
            }
            catch { }
            _isHandled = true;
        }
    }
}