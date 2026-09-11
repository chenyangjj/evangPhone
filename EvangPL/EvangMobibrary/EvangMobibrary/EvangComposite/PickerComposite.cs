using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class PickerComposite : EvangShowComposite
    {
        public Picker? Picker;

        public PickerComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public PickerComposite(
            string labeltext,
            double height,
            bool required = false,
            bool dummy = false,
            CommonViewSetting? labelviewsetting = null,
            CommonViewSetting? inputviewsetting = null,
            int rightwidth = 0,
            CompositeLayoutPattern? pattern = null)
            : base(labeltext, height, required, false, labelviewsetting, inputviewsetting, rightwidth, pattern)
        {
        }

        public override void AddControl()
        {
            base.AddControl();

            Picker = new Picker
            {
                FontSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                BackgroundColor = InputViewSetting?.BackgroundColor ?? DefaultBackgroudColor,
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                HorizontalTextAlignment = InputViewSetting?.Alignment ?? TextAlignment.Start,
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(Picker, 2);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(Picker, 1, 1);

            Picker.Loaded += OnPickerLoaded;
        }

        private void OnPickerLoaded(object? sender, EventArgs e)
        {
            if (_options != null && _options.Count > 0)
                Picker!.SelectedItem = _options[0];
        }

        public override View? InputControl => Picker;

        public override string Text
        {
            get
            {
                if (ShowLabel != null && ShowLabel.IsVisible)
                    return ShowLabel.Text;
                if (Picker!.SelectedItem == null)
                    return string.Empty;
                return Picker!.SelectedItem.ToString()!;
            }
            set
            {
                if (ShowLabel != null)
                    ShowLabel.Text = value;
                Picker!.SelectedItem = value;
            }
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            Picker.SetBinding(Picker.SelectedItemProperty, propname);
            Picker!.PropertyChanged += OnPickerPropertyChanged;
        }

        private void OnPickerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedItem")
            {
                if (ShowLabel != null)
                    ShowLabel.Text = Picker?.SelectedItem.ToString() ?? string.Empty;
            }
        }

        List<string?>? _options = null;
        public List<string?> Options
        {
            get => Picker!.ItemsSource.Cast<string?>().ToList();
            set
            {
                Picker!.ItemsSource = value;
                _options = value;
                if (_options.Count > 0)
                    Picker!.SelectedItem = _options[0];
            }
        }

        public override void Blur()
        {
            Picker?.Unfocus();
        }
    }
}
