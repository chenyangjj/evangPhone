using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class EditorComposite : EvangIconComposite
    {
        public Editor? Editor { get; set; }

        public EditorComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public EditorComposite(
            string labeltext,
            double height,
            bool required = false,
            bool keyboardicon = true,
            CommonViewSetting? labelviewsetting = null,
            CommonViewSetting? inputviewsetting = null,
            int rightwidth = 0,
            CompositeLayoutPattern? pattern = null)
            : base(labeltext, height, required, keyboardicon, labelviewsetting, inputviewsetting, rightwidth, pattern)
        {
        }

        public override void AddControl()
        {
            base.AddControl();

            var editorHeight = COMPOSITE_HEIGHT * 2;
            if (InputViewSetting!.EditorHeight != 0)
                editorHeight = (double)InputViewSetting!.EditorHeight!;

            Editor = new Editor
            {
                FontSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                BackgroundColor = InputViewSetting?.BackgroundColor ?? DefaultBackgroudColor,
                TextColor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                VerticalTextAlignment = TextAlignment.Start,
                Placeholder = GetCustomString(InputViewSetting?.Placeholder) ?? InputViewSetting?.Placeholder ?? string.Empty,
                PlaceholderColor = GetColor("Gray300"),

                HeightRequest = editorHeight,
                VerticalOptions = LayoutOptions.Start,
                MaxLength = InputViewSetting?.MaxLength == 0 ? CommonViewSetting.INPUT_MAXLENGTH : InputViewSetting!.MaxLength
            };

            Label.VerticalOptions = LayoutOptions.Start;
            Label.Padding = new Thickness(0, 10, 2.5, 0);
            
            Editor.Focused += OnEditorFocused;
            Editor.Unfocused += OnEditorUnfocused;

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(Editor, 2);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(Editor, 1, 1);
        }

        private void OnEditorUnfocused(object? sender, FocusEventArgs e)
        {
            if (ClearIcon is null)
                return;
            ClearIcon.IsVisible = e.IsFocused;
            HideErrorIcon();
        }

        private void OnEditorFocused(object? sender, FocusEventArgs e)
        {
            if (ClearIcon is null)
                return;
            ClearIcon.IsVisible = e.IsFocused;
            HideErrorIcon();
        }

        public override void OnKeyBoardIconClicked(object? sender, EventArgs e)
        {
            //SIR0187610
            if (Editor!.IsSoftInputShowing())
            {
                Editor!.HideSoftInputAsync(CancellationToken.None);
            }
            else
            {
                Editor!.ShowSoftInputAsync(CancellationToken.None);
                Editor?.Focus();
            }

        }

        public override void OnClearEntryClicked(object? sender, EventArgs e)
        {
            Editor!.Text = string.Empty;
            base.OnClearEntryClicked(sender, e); //SIR0188536
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            Editor.SetBinding(Entry.TextProperty, propname);
            Editor!.PropertyChanged += OnEditorPropertyChanged;
        }

        private void OnEditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
            {
                if (ShowLabel != null)
                    ShowLabel.Text = Editor?.Text ?? string.Empty;
            }
        }

        public override View? InputControl => Editor;

        public override string Text
        {
            get
            {
                if (ShowOnly && ShowLabel != null)
                    return ShowLabel.Text;
                return Editor?.Text ?? string.Empty;

            }
            set
            {
                if (ShowLabel != null)
                    ShowLabel.Text = value;
                Editor!.Text = value;
            }
        }

        public override void Blur()
        {
            Editor?.Unfocus();
        }
    }
}
