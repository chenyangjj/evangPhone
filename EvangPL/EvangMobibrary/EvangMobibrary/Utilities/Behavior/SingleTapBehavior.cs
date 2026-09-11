namespace EvangSol.Mobibrary.Utilities.Behavior
{
    public class SingleTapBehavior : Behavior<Button>
    {
        private bool _isTapped;
        private Button? _associatedButton;

        public int DelayDuration { get; set; } = 1000;

        protected override void OnAttachedTo(Button bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedButton = bindable;
            bindable.Clicked += OnButtonClicked;
        }

        protected override void OnDetachingFrom(Button bindable)
        {
            base.OnDetachingFrom(bindable);
            _associatedButton = null;
            bindable.Clicked -= OnButtonClicked;
        }

        private async void OnButtonClicked(object? sender, EventArgs e)
        {
            if (_isTapped) return;

            _isTapped = true;

            try
            {
                if (_associatedButton != null)
                {
                    _associatedButton.IsEnabled = false;
                }

                // Wait for the specified delay duration
                await Task.Delay(DelayDuration);
            }
            finally
            {
                if (_associatedButton != null)
                {
                    _associatedButton.IsEnabled = true;
                }
                _isTapped = false;
            }
        }
    }
}
