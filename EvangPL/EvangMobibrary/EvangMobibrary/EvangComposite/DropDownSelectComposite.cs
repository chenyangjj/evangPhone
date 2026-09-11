using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangWidget;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class DropDownSelectComposite : EvangIconComposite
    {
        public UnifiedDropDown? DropDown;

        public DropDownSelectComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public DropDownSelectComposite(
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

            DropDown = new UnifiedDropDown(Required, InputViewSetting)
            {
                BackgroundColor = DefaultBackgroudColor,
            };
#if ANDROID
            DropDown.Padding = new Thickness(0, 5, 0, 0);
#endif

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(DropDown, 2);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(DropDown, 1, 1);

            DropDown.Options = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("empty", " "),
            };
        }

        public override View? InputControl => DropDown;

        public override string Text
        {
            get
            {
                if (ShowLabel != null && ShowLabel.IsVisible)
                    return ShowLabel.Text;
                return DropDown!.Text;
            }
            set
            {
                if (ShowLabel != null)
                    ShowLabel.Text = value;
                DropDown!.Text = value;
            }
        }

        public override string Value
        {
            get
            {
                if (ShowLabel != null && ShowLabel.IsVisible)
                    return DropDown!.GetValueByText(ShowLabel.Text);
                return DropDown!.Value;
            }
            set
            {
                DropDown!.Value = value;
                if (ShowLabel != null)
                    ShowLabel.Text = DropDown!.GetTextByValue(value);
            }
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            DropDown.SetBinding(UnifiedDropDown.SelectionProperty, propname);
            DropDown!.PropertyChanged += OnDropDownBoxPropertyChanged;
        }

        private void OnDropDownBoxPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Selection")
            {
                if (ShowLabel != null)
                    ShowLabel.Text = DropDown?.Text ?? string.Empty;
            }
        }

        //SIR0188675
        public bool IncludeBlankOption { get; set; } = false;   //SIR0189760

        public List<KeyValuePair<string, string>> Options
        {
            get => DropDown!.Options;
            set
            {
                //SIR0188675
                //if values are the same, append keys to their values
                Dictionary<string, int> dict = new();
                for (int i = 0; i < value.Count; i++)
                {
                    var pair = value[i];
                    if (pair.Key == null)
                        continue;

                    if (string.IsNullOrEmpty(pair.Value))
                    {
                        var p = new KeyValuePair<string, string>(pair.Key, $"({pair.Key})");
                        value.Remove(pair);
                        value.Insert(i, p);
                        continue;
                    }

                    if (dict.ContainsKey(pair.Value))
                    {
                        var p = new KeyValuePair<string, string>(pair.Key, $"{pair.Value ?? string.Empty}({pair.Key})");
                        value.Remove(pair);
                        value.Insert(i, p);

                        var k = dict[pair.Value!];
                        p = new KeyValuePair<string, string>(value[k].Key, $"{value[k].Value ?? string.Empty}({value[k].Key})");
                        value.Remove(value[k]);
                        value.Insert(k, p);
                    }
                    else
                    {
                        dict.Add(pair.Value, i);
                    }
                }

                if (value.Count > 0)
                {
                    if (IncludeBlankOption)
                    {
                        if (!string.IsNullOrEmpty(value[0].Value) || !string.IsNullOrEmpty(value[0].Key))
                            value.Insert(0, new KeyValuePair<string, string>("", " "));
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(value[0].Value) && string.IsNullOrEmpty(value[0].Key))
                            value.Remove(value[0]);
                    }
                }

                DropDown!.Options = value;
            }
        }

        public override void Blur()
        {
            DropDown?.Blur();
        }

        public override void OnKeyBoardIconClicked(object? sender, EventArgs e) { }
    }
}
