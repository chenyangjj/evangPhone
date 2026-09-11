using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangCustom;
using EvangSol.Mobibrary.DataFeed;
using System.Collections.ObjectModel;
using System.Reflection;
using EvangSol.Mobibrary.EvangView;

namespace EvangSol.Mobibrary.EvangModel
{
    public abstract class EvangJsonModel : IMappingBase
    {
        #region properties
        public string? CheckSelect { get; set; }
        public bool ShowOnly { get; set; }
        #endregion

        #region static functions
        public delegate Color FuncSetBGColor(EvangJsonModel jm);

        //from json model list to create entry model list
        public static ObservableCollection<T> CreateEntryModelList<T>(List<EvangJsonModel> jsonlist, FuncSetBGColor? funcsetbgcolor = null) where T : TableEntryView, new()
        {
            //get property list
            var proplist = ClassMapping.GetPropertyList<ColumnAttribute>(typeof(T));
            //because we need to know which unit to use to format some decimal data
            //split property list into unit property list and the other property list
            //first set unit property's value, then others
            List<(PropertyInfo, PropertyInfo?)> units;
            List<(PropertyInfo, PropertyInfo?)> others;
            (units, others) = FilterUnitProperty(proplist);

            var entrylist = new ObservableCollection<T>();
            Dictionary<string, string>? propsmap = null;
            foreach (var json in jsonlist)
            {
                //create entry model
                var obj = ClassMapping.CreateViewModelInstance(typeof(T));
                if (obj == null)
                    throw new Exception($"Failed to create entry model {typeof(T).Name}.");
                var te = (T)obj;
                //entry model initiation, to create property mapping
                te.InitPropertyViewModel(propsmap, proplist);
                //if property mapping is already created, just pass it through to all the latters
                propsmap = te._propsmap;
                //value copy
                json.CopyValuesToEntryModel(ref te, units, others, false);
                //keep json data to entry model
                te.DataModel = json;
                //call back function for row rendering, mainly for change background color
                if (funcsetbgcolor != null)
                    te.BackgroundColor = funcsetbgcolor(json);
                entrylist.Add(te);
            }
            return entrylist;
        }

        //from json model list to create sheet model list, it's a copy from CreateEntryModelList with some minor changes
        public static ObservableCollection<S> CreateSheetModelList<S>(List<EvangJsonModel> jsonlist, bool showonly) where S : SwipeEntryView, new()
        {
            var proplist = ClassMapping.GetPropertyList<ControlAttribute>(typeof(S));
            List<(PropertyInfo, PropertyInfo?)> units;
            List<(PropertyInfo, PropertyInfo?)> others;
            (units, others) = FilterUnitProperty(proplist);

            var pagelist = new ObservableCollection<S>();
            Dictionary<string, string>? propsmap = null;
            foreach (var json in jsonlist)
            {
                var obj = ClassMapping.CreateViewModelInstance(typeof(S));
                if (obj == null)
                    throw new Exception($"Failed to create sheet model {typeof(S).Name}.");
                var se = (S)obj;
                se.InitPropertyViewModel(propsmap, proplist);
                propsmap = se._propsmap;
                json.CopyValuesToEntryModel(ref se, units, others, showonly || json.ShowOnly);
                se.DataModel = json;
                pagelist.Add(se);
            }
            return pagelist;
        }

        //split property list into unit property list and other property list
        static (List<(PropertyInfo, PropertyInfo?)>, List<(PropertyInfo, PropertyInfo?)>) FilterUnitProperty<A>(List<(string, PropertyInfo, PropertyInfo?, A)> proplist) where A : EvangAttribute
        {
            List<(PropertyInfo, PropertyInfo?)> units = new();
            List<(PropertyInfo, PropertyInfo?)> others = new();
            foreach (var (_, prop, hideprop, _) in proplist)
            {
                Type? viewtype = null;
                foreach (var tp in prop.PropertyType.GetGenericArguments())
                    viewtype = tp;
                if (viewtype != null && viewtype.Name == nameof(UnitLabel))
                    units.Add((prop, hideprop));
                else
                    others.Add((prop, hideprop));
            }
            return (units, others);
        }
        #endregion

        #region common functions
        public void CopyValuesToEntryModel<B>(ref B bv, List<(PropertyInfo, PropertyInfo?)> units, List<(PropertyInfo, PropertyInfo?)> others, bool showonly) where B : BindableBrokerView
        {
            var type = GetType();
            foreach (var (prop, hideprop) in units)
            {
                CopyValueToEntryModelProperty(ref bv, prop, hideprop, type, showonly);
            }
            foreach (var (prop, hideprop) in others)
            {
                CopyValueToEntryModelProperty(ref bv, prop, hideprop, type, showonly);
            }
        }

        void CopyValueToEntryModelProperty<B>(ref B bv, PropertyInfo prop, PropertyInfo? hideprop, Type type, bool showonly) where B : BindableBrokerView
        {
            //if it's not of type BaseViewElement or @SubViewElement
            if (prop.PropertyType.BaseType != typeof(EvangElement) && prop.PropertyType.BaseType!.BaseType != typeof(EvangElement))
                return;
            //if the property name is not existing in json model
            var selfprop = type.GetProperty(prop.Name);
            if (selfprop == null)
                return;
            //create ViewElement<T> or @SubViewElement<T>
            var obj = Activator.CreateInstance(prop.PropertyType);
            if (obj == null)
                return;
            var ele = obj as EvangElement;
            if (ele == null)
                return;
            //get json value
            var val = selfprop.GetValue(this, null);
            //set to ViewElement's Value property
            ele.Value = val == null ? string.Empty : (string)val;
            ele.ShowOnly = showonly;
            //set to view model property
            prop.SetValue(bv, ele);
            //set to parent view model property if there's one
            hideprop?.SetValue(bv, ele);
            //set value to binding property
            bv.SetPropMemberValue(prop.Name, ele.Value!);
        }

        //copy json model's values to view model, thus trigger onpropertychanged event to show values on the page
        public void RenderJsonValue<B>(B bv, List<(string, PropertyInfo, PropertyInfo?, EvangAttribute)>? proplist = null) where B : BindableBrokerView
        {
            if (proplist == null)
                proplist = ClassMapping.GetPropertyList<EvangAttribute>(typeof(B));
            List<(PropertyInfo, PropertyInfo?)> units = new();
            List<(PropertyInfo, PropertyInfo?)> others = new();
            (units, others) = FilterUnitProperty(proplist);

            var type = GetType();
            foreach (var (prop, hideprop) in units)
            {
                CopyValueToViewModel(bv, prop, hideprop, type);
            }
            foreach (var (prop, hideprop) in others)
            {
                CopyValueToViewModel(bv, prop, hideprop, type);
            }
            bv.DataModel = this;
        }

        void CopyValueToViewModel<B>(B bv, PropertyInfo prop, PropertyInfo? hideprop, Type type) where B : BindableBrokerView
        {
            if (prop.PropertyType.BaseType != typeof(EvangElement) && prop.PropertyType.BaseType!.BaseType != typeof(EvangElement))
                return;
            var selprop = type.GetProperty(prop.Name);
            if (selprop == null)
                return;
            //get view model property value
            var val = prop.GetValue(bv, null);
            //if null, create one
            if (val == null)
            {
                var obj = Activator.CreateInstance(prop.PropertyType);
                if (obj == null)
                    return;
                if (obj as EvangElement == null)
                    return;
                val = obj;
                //set value to view model property and parent view model property if applicable
                prop.SetValue(bv, val);
                hideprop?.SetValue(bv, val);
            }
            var ele = val as EvangElement;
            //get json value
            val = selprop.GetValue(this, null);
            //set json value to view model property, use SetValue funtion to trigger onpropertychanged event
            ele!.SetValue(val == null ? string.Empty : (string)val);
            //for carousel sheet, show value directly to the control
            if (bv is SwipeEntryView && ele.ElementObject is IInputControl iic)
                iic.Value = ele.Value ?? string.Empty;
        }

        public void CopyJsonValuesToSheetModel<S>(S se, List<(string, PropertyInfo, PropertyInfo?, EvangAttribute)> proplist) where S : SwipeEntryView
        {
            List<(PropertyInfo, PropertyInfo?)> units;
            List<(PropertyInfo, PropertyInfo?)> others;
            (units, others) = FilterUnitProperty(proplist);
            CopyValuesToEntryModel(ref se, units, others, false);
            se.DataModel = this;
        }

        public T? GetPropertyValue<T>(string name)
        {
            var prop = GetType().GetProperty(name);
            if (prop != null)
            {
                return (T?)prop.GetValue(this, null);
            }
            return default;
        }
        public string? GetPropertyValue(string name) => GetPropertyValue<string>(name);

        public void SetPropertyValue(string name, object? value)
        {
            var prop = GetType().GetProperty(name);
            if (prop != null)
                prop.SetValue(this, value);
        }
        #endregion
    }
}
