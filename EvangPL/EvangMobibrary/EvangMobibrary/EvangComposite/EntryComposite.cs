using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangCustom;
using EvangSol.Mobibrary.Utilities.Behavior;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class EntryComposite : EvangIconComposite, IDecimalFormat
    {
        public DeferFocusEntry? Entry { get; set; }

        public EntryComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public EntryComposite(
            string labeltext,
            double height,
            bool required = false,
            bool keyboardicon = false,
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

            var textcolor = InputViewSetting?.TextColor ?? GetColor(CommonViewSetting.INPUT_FONTCOLOR) ?? Colors.Black;
            var needbehavior = InputViewSetting?.InputType == "Integer" || InputViewSetting?.InputType == "Decimal";

            Entry = new DeferFocusEntry
            {
                FontSize = InputViewSetting?.TextSize ?? CommonViewSetting.INPUT_FONTSIZE,
                BackgroundColor = InputViewSetting?.BackgroundColor ?? DefaultBackgroudColor,
                TextColor = textcolor,
                FontAttributes = InputViewSetting?.FontAttributes ?? FontAttributes.None,
                VerticalTextAlignment = TextAlignment.End,
                HorizontalTextAlignment = InputViewSetting?.Alignment ?? (needbehavior ? TextAlignment.End : TextAlignment.Start),
                IsPassword = InputViewSetting?.InputType == "Password",
                Placeholder = GetCustomString(InputViewSetting?.Placeholder) ?? InputViewSetting?.Placeholder ?? string.Empty,
                PlaceholderColor = GetColor("Gray300"),
                Keyboard = needbehavior ? Keyboard.Numeric : Keyboard.Default,
            };

            //using behaviors to do validation
            if (InputViewSetting?.InputType == "Integer")
                Entry.Behaviors.Add(new IntegerValidateBehavior(BaseUtils.GetCustomString(LabelViewSetting?.MessageLabel) ?? LabelViewSetting?.MessageLabel ?? Label.Text, textcolor));//SIR0188675
            else if (InputViewSetting?.InputType == "Decimal")
                Entry.Behaviors.Add(new DecimalValidateBehavior(BaseUtils.GetCustomString(LabelViewSetting?.MessageLabel) ?? LabelViewSetting?.MessageLabel ?? Label.Text, textcolor));//SIR0188675
            Entry.Focused += OnEntryFocused;
            Entry.Unfocused += OnEntryUnfocused;

            if (Pattern == CompositeLayoutPattern.Parallel)
            {
                Grid.Add(Entry, 2);
            }
            else if (Pattern == CompositeLayoutPattern.Tandem)
            {
                Grid.Add(Entry, 1, 1);
            }
        }

        private void OnEntryUnfocused(object? sender, FocusEventArgs e)
        {
            if (ClearIcon is null)
                return;
            ClearIcon.IsVisible = e.IsFocused;
        }

        private void OnEntryFocused(object? sender, FocusEventArgs e)
        {
            if (ClearIcon is null)
                return;
            ClearIcon.IsVisible = e.IsFocused;
            HideErrorIcon();
        }

        public override void OnKeyBoardIconClicked(object? sender, EventArgs e)
        {
            Entry?.Focus();
            Entry?.ShowSoftInputAsync(CancellationToken.None);
        }

        public override void OnClearEntryClicked(object? sender, EventArgs e)
        {
            Entry!.Text = string.Empty;
            base.OnClearEntryClicked(sender, e); //SIR0188536
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            Entry!.SetBinding(Microsoft.Maui.Controls.Entry.TextProperty, propname);
            Entry!.PropertyChanged += OnEntryPropertyChanged;
        }

        private void OnEntryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
            {
                if (ShowLabel != null)
                    ShowLabel.Text = Entry?.Text ?? string.Empty;
            }
        }

        public override View? InputControl => Entry;

        public override string Text
        {
            get
            {
                if (ShowOnly && ShowLabel != null)
                    return ShowLabel.Text;
                return Entry?.Text ?? string.Empty;

            }
            set
            {
                if (ShowLabel != null)
                    ShowLabel.Text = value;
                Entry!.Text = value;
            }
        }

        public override void Blur()
        {
            Entry!.Unfocus();
        }

        #region IDecimalFormat
        public void SetUnit(string unit, BindableBrokerView bpvm)
        {
            if (Entry == null)
                return;
            foreach (var behavior in Entry.Behaviors)
                if (behavior is IDecimalFormat ifu)
                    ifu.SetUnit(unit, bpvm);
        }

        public void SetFormat(string format)
        {
            if (Entry == null)
                return;
            foreach (var behavior in Entry.Behaviors)
                if (behavior is IDecimalFormat ifu)
                    ifu.SetFormat(format);
        }
        #endregion
    }
}
