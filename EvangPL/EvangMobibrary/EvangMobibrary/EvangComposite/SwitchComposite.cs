using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangComposite
{
    public class SwitchComposite: EvangCompositeView
    {
        public Switch? Switch { get; set; }

        public SwitchComposite() : base(string.Empty, COMPOSITE_HEIGHT, false, false, null, null)
        {
        }

        public SwitchComposite(
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
            Switch = new Switch
            {
            };

            if (Pattern == CompositeLayoutPattern.Parallel)
                Grid.Add(Switch, 2);
            else if (Pattern == CompositeLayoutPattern.Tandem)
                Grid.Add(Switch, 1, 1);
        }

        public override void SetInputBinding(string propname, string? unit, BindableBrokerView? bpvm)
        {
            Switch.SetBinding(Switch.IsToggledProperty, propname);
        }

        public override string Text
        {
            get => Switch!.IsToggled.ToString();
            set
            {
                bool _x = false;
                if (Switch != null && bool.TryParse(value, out _x))
                {
                    Switch.IsToggled = _x;
                }
            }
        }

        public override bool ShowOnly
        {
            get => !Switch!.IsEnabled;
            set => Switch!.IsEnabled = !value;
        }

        public override View? InputControl => null;

        public override void Blur()
        {
            Switch?.Unfocus();
        }
    }
}
