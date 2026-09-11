using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Behavior;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class AutoCompleteComposite : EvangIconComposite, ISoftInput
    {
        public AutoComplete? AutoComplete { get; set; }
        public StackLayout? AutoBackground { get; set; }

        AutoCompleteValidateBehavior? validatebehavior;

        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(AutoCompleteComposite), string.Empty, BindingMode.TwoWay,
            propertyChanged: (bindable, oldValue, newValue) => (bindable as AutoCompleteComposite)!.Text = (string)newValue);

        public AutoCompleteComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public AutoCompleteComposite(
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

#if WINDOWS
            //on Windows, clicking on clear icon causes dead loop.
            //since AutoSuggestBox has its own clear button, remove clear icon on Windows.
            ClearIcon = null;
#endif

            AutoComplete = new AutoComplete(InputViewSetting!);
            AutoComplete.OnAutoCompleteFocusChanged += OnAutoCompFocusChanged;
            //フォーカスを失ったときに適用するためにこのバリデータを保持します
            validatebehavior = new AutoCompleteValidateBehavior(BaseUtils.GetCustomString(LabelViewSetting?.MessageLabel) ?? LabelViewSetting?.MessageLabel ?? Label.Text, false);//SIR0188675
            AutoComplete.Behaviors.Add(validatebehavior);

            AutoBackground = new StackLayout
            {
                BackgroundColor = DefaultBackgroudColor,
                Children =
                {
                    AutoComplete,
                }
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(AutoBackground, 2);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(AutoBackground, 1, 1);
        }

        private void OnAutoCompFocusChanged(object? sender, AutoCompleteFocusChangeEventArgs e)
        {
            HideErrorIcon();
            if (e.HasFocus)
            {
                if (ClearIcon != null)
                    ClearIcon.IsVisible = true;
                if (ShowKeyBoardIcon)
                    AutoComplete?.SoftInput(false);
            }
            else
            {
                if (ClearIcon != null)
                    ClearIcon.IsVisible = false;
                //バリデーションをやる
                if (!string.IsNullOrEmpty(Text) && !validatebehavior!.Validation(Text))
                {
                    ShowErrorIcon();
                    ParentPage!.ShowError(validatebehavior.ErrorMessage);
                }
            }
        }

        public override void OnClearEntryClicked(object? sender, EventArgs e)
        {
            AutoComplete?.Clear();
        }

        public override void OnKeyBoardIconClicked(object? sender, EventArgs e)
        {
            AutoComplete?.Focus();
            AutoComplete?.SoftInput(true);
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            AutoComplete.SetBinding(AutoComplete.SelectionProperty, propname);
            AutoComplete!.PropertyChanged += OnAutoCompletePropertyChanged;
        }

        private void OnAutoCompletePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Selection")
            {
                if (ShowLabel != null)
                    ShowLabel.Text = AutoComplete?.Text ?? string.Empty;
            }
        }

        public void SoftInput(bool show) => AutoComplete?.SoftInput(show);

        public override View? InputControl => AutoComplete;

        public override bool ShowOnly
        {
            get => base.ShowOnly;
            set
            {
                base.ShowOnly = value;
                AutoBackground!.BackgroundColor = value ? Colors.Transparent : DefaultBackgroudColor;
            }
        }

        public override string Text
        {
            get
            {
                if (ShowLabel != null && ShowLabel.IsVisible)
                    return ShowLabel.Text;
                return AutoComplete!.Text;
            }
            set
            {
                AutoComplete!.Text = value;
                if (ShowOnly)
                {
                    if (ShowLabel == null)
                        CreateShowLabel();
                    ShowLabel!.Text = value;
                }
            }
        }

        public override string Value
        {
            get => AutoComplete!.Value;
            set
            {
                AutoComplete!.Value = value;
                if (ShowOnly)
                {
                    if (ShowLabel == null)
                        CreateShowLabel();
                    if (AutoComplete.Handler == null)
                        SetTimer(100, () => ShowLabel!.Text = AutoComplete.Text);
                    else
                        ShowLabel!.Text = AutoComplete.Text;
                }
            }
        }

        //SIR0188675
        public bool IncludeBlankOption { get; set; } = false;

        public List<IDataSelection> Options
        {
            get => AutoComplete!.Options;
            set
            {
                //SIR0188675
                //if values are the same, append keys to their values
                Dictionary<string, IDataSelection> dict = new();
                foreach (var item in value)
                {
                    if (item.Key == null)
                        continue;
                    if (string.IsNullOrEmpty(item.Val))
                    {
                        item.Val = $"({item.Key})";
                        continue;
                    }
                    if (dict.ContainsKey(item.Val))
                    {
                        var first = dict[item.Val];
                        first.Val = $"{first.Val ?? string.Empty}({first.Key})";
                        item.Val = $"{item.Val ?? string.Empty}({item.Key})";
                    }
                    else
                    {
                        dict.Add(item.Val, item);
                    }
                }
                if (value.Count > 0)
                {
                    if (IncludeBlankOption)
                    {
                        if (!string.IsNullOrEmpty(value[0].Val) || !string.IsNullOrEmpty(value[0].Key))
                            value.Insert(0, new BlankSelect());
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(value[0].Val) && string.IsNullOrEmpty(value[0].Key))
                            value.Remove(value[0]);
                    }
                }
                AutoComplete!.Options = value;
            }
        }

        public override void Blur()
        {
            AutoComplete?.Blur();
        }
    }
}
