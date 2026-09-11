namespace EvangSol.Mobibrary.EvangWidget
{
    public class ZoomableView : ContentView
    {
        protected double _currentScale = 1;
        protected double _startScale = 1;
        protected double _xOffset = 0;
        protected double _yOffset = 0;
        protected double _xOffsetCorrection = 0;
        protected double _yOffsetCorrection = 0;

        public ZoomableView()
        {
            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            GestureRecognizers.Add(pinchGesture);

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            GestureRecognizers.Add(panGesture);

            var tapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tapGesture.Tapped += OnDoubleTapped;
            GestureRecognizers.Add(tapGesture);
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if WINDOWS
            var platformView = (Microsoft.Maui.Platform.ContentPanel)Handler.PlatformView;
            platformView.PointerWheelChanged += OnPointerWheelChanged;
#endif
        }

#if WINDOWS
        // マウスホイールイベントのハンドラ
        public virtual void OnPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint((Microsoft.Maui.Platform.ContentPanel)sender).Properties.MouseWheelDelta;
            var zoomFactor = delta > 0 ? 1.1 : 0.9; // ズームイン・ズームアウトの倍率

            // 現在のスケールに倍率を掛けて新しいスケールを計算
            _currentScale *= zoomFactor;
            _currentScale = Math.Max(1, _currentScale); // 最小スケールを1に制限

            // スケールの更新
            Content.Scale = _currentScale;

            // ズームの中心をマウスカーソル位置に設定
            var position = e.GetCurrentPoint((Microsoft.Maui.Platform.ContentPanel)sender).Position;
            var originX = position.X / Width;
            var originY = position.Y / Height;

            e.Handled = true; // イベントの処理済みフラグを設定
        }
#endif

        public virtual void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    _startScale = Content.Scale;
                    Content.AnchorX = 0;
                    Content.AnchorY = 0;
                    break;
                case GestureStatus.Running:
                    {
                        _currentScale += (e.Scale - 1) * _startScale;
                        _currentScale = Math.Max(1, _currentScale);

                        var renderedX = Content.X + _xOffset;
                        var deltaX = renderedX / Width;
                        var deltaWidth = Width / (Content.Width * _startScale);
                        var originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

                        var renderedY = Content.Y + _yOffset;
                        var deltaY = renderedY / Height;
                        var deltaHeight = Height / (Content.Height * _startScale);
                        var originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

                        var targetX = _xOffset - originX * Content.Width * (_currentScale - _startScale);
                        var targetY = _yOffset - originY * Content.Height * (_currentScale - _startScale);

                        Content.TranslationX = Math.Min(0, Math.Max(targetX, -Content.Width * (_currentScale - 1)));
                        Content.TranslationY = Math.Min(0, Math.Max(targetY, -Content.Height * (_currentScale - 1)));

                        Content.Scale = _currentScale;
                        break;
                    }
                case GestureStatus.Completed:
                    _xOffset = Content.TranslationX;
                    _yOffset = Content.TranslationY;
                    break;
            }
        }

        View? ParentView => Parent as View;

        public virtual void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (Content.Scale == 1)
            {
                return;
            }

            switch (e.StatusType)
            {
                case GestureStatus.Running:

                    var newX = e.TotalX * Scale + _xOffset - _xOffsetCorrection;
                    var newY = e.TotalY * Scale + _yOffset - _yOffsetCorrection;

                    var width = Content.Width * Content.Scale;
                    var height = Content.Height * Content.Scale;

                    var parentWidth = ParentView!.Width;
                    var parentHeight = ParentView!.Height;

                    var canMoveX = width > parentWidth;
                    var canMoveY = height > parentHeight;

                    if (canMoveX)
                    {
                        var minX = (width - parentWidth / 2) * -1;
                        var maxX = Math.Min(parentWidth / 2, width / 2);

                        if (newX < minX)
                        {
                            newX = minX;
                        }

                        if (newX > maxX)
                        {
                            newX = maxX;
                        }
                    }
                    else
                    {
                        newX = 0;
                    }

                    if (canMoveY)
                    {
                        var minY = (height - parentHeight / 2) * -1;
                        var maxY = Math.Min(parentHeight / 2, height / 2);

                        if (newY < minY)
                        {
                            newY = minY;
                        }

                        if (newY > maxY)
                        {
                            newY = maxY;
                        }
                    }
                    else
                    {
                        newY = 0;
                    }
                    if (_xOffsetCorrection == 0 & _yOffsetCorrection == 0)
                    {
                        _xOffsetCorrection = newX - _xOffset;
                        _yOffsetCorrection = newY - _yOffset;
                        Content.TranslationX = newX - _xOffsetCorrection;
                        Content.TranslationY = newY - _yOffsetCorrection;
                    }
                    else
                    {
                        Content.TranslationX = newX;
                        Content.TranslationY = newY;
                    }

                    Content.TranslationX = newX;
                    Content.TranslationY = newY;
                    _currentScale = Content.Scale;
                    _xOffsetCorrection = 0;
                    _yOffsetCorrection = 0;
                    break;
                case GestureStatus.Completed:
                    _xOffset = Content.TranslationX;
                    _yOffset = Content.TranslationY;
                    break;
                case GestureStatus.Started:
                    break;
                case GestureStatus.Canceled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public virtual async void OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            const int animationSteps = 10;
            const int animationDelay = 10; // milliseconds

            double startScale = Content.Scale;
            double startX = Content.TranslationX;
            double startY = Content.TranslationY;

            double scaleStep = (1 - startScale) / animationSteps;
            double xStep = (0 - startX) / animationSteps;
            double yStep = (0 - startY) / animationSteps;

            for (int i = 0; i < animationSteps; i++)
            {
                Content.Scale += scaleStep;
                Content.TranslationX += xStep;
                Content.TranslationY += yStep;
                await Task.Delay(animationDelay);
            }

            // 完全にリセット
            Content.Scale = 1;
            Content.TranslationX = 0;
            Content.TranslationY = 0;

            // 内部状態もリセット
            _currentScale = 1;
            _startScale = 1;
            _xOffset = 0;
            _yOffset = 0;
        }
    }
}
