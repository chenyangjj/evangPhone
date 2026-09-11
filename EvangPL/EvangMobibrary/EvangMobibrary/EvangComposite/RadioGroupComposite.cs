using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class RadioGroupComposite : EvangGroupComposite, IInputControl
    {
        public static readonly BindableProperty CheckedProperty =
            BindableProperty.Create(nameof(Checked), typeof(string), typeof(RadioGroupComposite), defaultBindingMode: BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvalue) =>
            {
                if (oldvalue != newvalue)
                {
                    var radiogroup = bindable as RadioGroupComposite;
                    if (radiogroup != null)
                        radiogroup.Value = (string)newvalue;
                }
            });
        public string? Checked
        {
            get { return (string)GetValue(CheckedProperty); }
            set { SetValue(CheckedProperty, value); }
        }

        public class CheckedChangeEventArgs : EventArgs
        {
            public string? CheckedValue { get; set; }
        }
        public event EventHandler<CheckedChangeEventArgs>? CheckChanged;

        public CommonViewSetting? RadioSetting { get; set; }
        Dictionary<string, RadioButton> Radios { get; set; }
        Dictionary<string, string> Labels { get; set; }
        string groupname;
        string radios;
        string? initialvalue;

        public RadioGroupComposite(
            string groupname,
            string radios,
            CommonViewSetting? radiosetting = null,
            CommonFlexSetting? flexsetting = null)
            : base(flexsetting)
        {
            this.groupname = groupname;
            this.radios = radios;
            RadioSetting = radiosetting;

            Value = string.Empty;
            Radios = new Dictionary<string, RadioButton>();
            Labels = new Dictionary<string, string>();
        }

        public override void RenderComposite()
        {
            base.RenderComposite();
            CreateRadios(groupname, radios);
        }

        public virtual void CreateRadios(string groupname, string radios)
        {
            var id = new Random().Next();
            foreach (var (label, val) in ParseRadioString(radios, groupname))
            {
                var rb = new RadioButton
                {
                    GroupName = $"{groupname}_{id}",
                    Value = val,
                    Content = GetCustomString(label) ?? label,
                    FontSize = RadioSetting?.TextSize ?? CommonViewSetting.LABEL_FONTSIZE,
                    TextColor = RadioSetting?.TextColor ?? GetColor(CommonViewSetting.LABEL_FONTCOLOR),
                    FontAttributes = RadioSetting?.FontAttributes ?? FontAttributes.Bold,
                    WidthRequest = RadioSetting?.Width ?? 200,
                    //IsChecked = Radios.Count == 0,
                };
                if (FlexSetting != null && FlexSetting.Height > 0)
                    rb.HeightRequest = FlexSetting.Height;
                rb.IsEnabled = !ShowOnly;
                rb.CheckedChanged += OnCheckedChanged;
                GroupLayout!.Add(rb);
                Radios.Add(val, rb);
                Labels.Add(val, label);
            }
            //ラジオボタンを作成した後のみ、初期値を設定できる
            if (!string.IsNullOrEmpty(initialvalue))
            {
                Value = initialvalue;
                initialvalue = null;
            }
        }

        List<(string, string)> ParseRadioString(string radios, string groupname)
        {
            var radist = new List<(string, string)>();
            foreach (var str in radios.Split(","))
            {
                var pair = str.Split('-');
                if (pair.Length != 2)
                    throw new Exception($"Invalid Radio Group string format for {groupname}.");
                if (string.IsNullOrWhiteSpace(pair[0]) || string.IsNullOrWhiteSpace(pair[1]))
                    throw new Exception($"Invalid Radio Group string format for {groupname}.");
                radist.Add((pair[0].Trim(), pair[1].Trim()));
            }
            return radist;
        }

        public RadioButton this[string val] => Radios[val.Trim()];

        public virtual void OnCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                var rb = sender as RadioButton;
                if (rb != null)
                {
                    Checked = Value = (string)rb.Value;
                    CheckChanged?.Invoke(this, new CheckedChangeEventArgs { CheckedValue = Value });
                }
            }
        }

        public string Value
        {
            get => Radios.Where(x => x.Value.IsChecked).Select(x => x.Key).FirstOrDefault() ?? string.Empty;
            set
            {
                //ラジオボタンがない時、初期値を保持する
                if (Radios == null || Radios.Count == 0)
                {
                    initialvalue = value;
                    return;
                }
                if (string.IsNullOrEmpty(value))
                {
                    foreach (var item in Radios)
                        item.Value.IsChecked = false;
                }
                else
                {
                    var radio = Radios.Where(x => x.Key == value).Select(x => x.Value).FirstOrDefault();
                    if (radio != null)
                        radio.IsChecked = true;
                }
            }
        }

        bool _showonly;
        public bool ShowOnly
        {
            get => _showonly;
            set
            {
                foreach (var radio in Radios.Values)
                    radio.IsEnabled = !value;
                _showonly = value;
            }
        }

        public string Text
        {
            get => Labels[Value];
            set => _ = value;
        }

        public IList<Behavior> ControlBehaviors => new List<Behavior>();

        public void Blur() { }
    }
}
