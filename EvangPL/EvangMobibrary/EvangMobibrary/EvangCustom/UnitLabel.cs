using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Converter;
using System.Globalization;

namespace EvangSol.Mobibrary.EvangCustom
{
    public class UnitLabel : Label, IInputControl
    {
        public bool ShowOnly { get => true; set => _ = value; }

        public string Value
        {
            get => (converter?.ConvertBack(Text, typeof(string), null, CultureInfo.CurrentCulture) as string) ?? string.Empty;
            set => _ = value;
        }

        public IList<Behavior> ControlBehaviors => Behaviors;

        public void Blur() { }

        UnitIdToUnitNameConverter? converter;
        public void SetBinding(BindableProperty targetProperty, BindingBase binding, UnitIdToUnitNameConverter converter)
        {
            this.converter = converter;
            SetBinding(targetProperty, binding);
        }
    }
}
