using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.DataFeed;
using System.Reflection;

namespace EvangSol.Mobibrary.EvangView
{
    public class EvangEntryView : BindableBrokerView
    {
        public void InitPropertyViewModel<A>(Dictionary<string, string>? propsmap, List<(string, PropertyInfo, PropertyInfo?, A)>? proplist) where A : EvangAttribute
        {
            if (propsmap != null)
            {
                _propsmap = propsmap;
                return;
            }
            int index = 0;
            proplist ??= ClassMapping.GetPropertyList<A>(GetType());
            foreach (var (_, prop, _, _) in proplist)
            {
                if (!prop.PropertyType.Name.Contains("EvangElement"))
                    throw new CustomAttributeFormatException($"{prop.Name} property's type must be EvangElement.");

                var baseprop = $"Prop{index}";
                _propsmap.Add(prop.Name, baseprop);
                _propsmap.Add(baseprop, prop.Name);
                index++;
            }
        }

        public void InitShowOnlyStatus(bool showonly)
        {
            var props = GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(EvangAttribute)));
            foreach (var prop in props)
            {
                var bve = prop.GetValue(this) as EvangElement;
                if (bve != null)
                    bve.SetShowOnly(showonly);
            }
        }
    }
}
