using CommunityToolkit.Mvvm.ComponentModel;
using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.Utilities.Behavior;
using EvangSol.Mobibrary.Utilities.Common;
using System.Reflection;
using EvangSol.Mobibrary.EvangModel;

namespace EvangSol.Mobibrary.EvangView
{
    public interface IInputControl
    {
        public bool IsVisible { get; set; }

        public bool ShowOnly { get; set; }

        public string Text { get; set; }

        public string Value { get; set; }

        public IList<Behavior> ControlBehaviors { get; }

        public void Blur();
    }

    public class EvangView : ObservableObject, IMappingBase
    {
        //create a new view model with empty values
        public static E CreateEmptyValueViewModel<E, A>(List<(string, PropertyInfo, PropertyInfo?, A)> proplist) where E : EvangEntryView, new() where A : EvangAttribute
        {
            var basevm = ClassMapping.CreateViewModelInstance(typeof(E));
            if (basevm == null)
                throw new Exception($"Failed to create entry model {typeof(E).Name}.");
            var em = (E)basevm;
            em.InitPropertyViewModel(null, proplist);

            foreach (var (_, prop, hideprop, _) in proplist)
            {
                //must be BaseViewElement or @SubViewElement
                if (prop.PropertyType.BaseType != typeof(EvangElement) && prop.PropertyType.BaseType!.BaseType != typeof(EvangElement))
                    continue;
                var obj = Activator.CreateInstance(prop.PropertyType);
                if (obj == null)
                    continue;
                var ve = obj as EvangElement;
                if (ve == null)
                    continue;
                //set empty value
                ve.Value = string.Empty;
                prop.SetValue(em, ve);
                hideprop?.SetValue(em, ve);
                em.SetPropMemberValue(prop.Name, ve.Value);
            }
            return em;
        }

        public bool RetrieveInputData<J, A>(
            EvangContentVM page,
            List<(string, PropertyInfo, PropertyInfo?, A)> proplist,
            ref J jsonmodel,
            bool inputonly = true,
            bool novalidation = false)  //set to true to suppress validation, it's for retrieving input data before serverside validation
            where J : EvangJsonModel where A : EvangAttribute
        {
            var jsontype = novalidation ? jsonmodel.GetType() : ClassMapping.GetMappingType(typeof(J));
            foreach (var (_, prop, _, _) in proplist)
            {
                var ve = prop.GetValue(this, null) as EvangElement;
                if (ve == null)
                    continue;
                //check if it's an input control
                var iic = ve.ElementObject as IInputControl;
                if (iic == null)
                    continue;
                //check if it's visible
                if (!iic.IsVisible)
                    continue;
                //check if it's show only
                if (inputonly && iic.ShowOnly)
                    continue;
                var jsonprop = jsontype.GetProperty(prop.Name);
                if (jsonprop == null || !jsonprop.CanWrite)
                    continue;
                //if input value is empty do Required check, if not do validation to the input value
                if (string.IsNullOrEmpty(iic.Value) && string.IsNullOrEmpty(iic.Text))
                {
                    var vea = prop.GetCustomAttribute(typeof(ControlAttribute)) as ControlAttribute;
                    if (vea == null)
                        continue;
                    // if Required = true, the input value must not be empty.
                    if (vea.Required && !novalidation)
                    {
                        var label = prop.Name;
                        //if it's composite control get label text
                        if (ve.ElementObject is EvangCompositeView bcv)
                        {
                            label = bcv.Label.Text;
                            //if it has error icon, show it
                            if (bcv is EvangShowComposite bscv)
                                bscv.ShowErrorIcon();
                        }
                        //show validation error message
                        page.ShowError(string.Format(BaseUtils.GetCustomString("errRequired") ?? "errRequired", label));
                        return false;
                    }
                    jsonprop.SetValue(jsonmodel, string.Empty);
                }
                else
                {
                    if (!novalidation)
                    {
                        //loop through input control's behavior list
                        foreach (var behavior in iic.ControlBehaviors)
                        {
                            //if it's IInputValidate, execute Validation func
                            if (behavior is IInputValidate iiv && !iiv.Validation(ve.Value))
                            {
                                //to get label text and show error icon
                                var label = prop.Name;
                                if (ve.ElementObject is EvangCompositeView bcv)
                                {
                                    label = bcv.Label.Text;
                                    if (bcv is EvangShowComposite bscv)
                                        bscv.ShowErrorIcon();
                                }
                                //show validation error message set in the IInputValidate behavior
                                page.ShowError(iiv.ErrorMessage);
                                return false;
                            }
                        }
                    }
                    jsonprop.SetValue(jsonmodel, iic.Value);
                }
            }
            return true;
        }
    }

    public class BindableBrokerView : EvangView
    {
        public Dictionary<string, string> _propsmap = new Dictionary<string, string>();
        Dictionary<string, EvangElement?>? _viewelements;
        int propindex = 0;

        public EvangJsonModel? DataModel { get; set; }

        public string? GetBaseProperty(string vmprop) => _propsmap?[vmprop];

        public EvangElement? this[string name] => GetViewElement(name);
        public Dictionary<string, EvangElement?> ViewElements
        {
            get
            {
                if (_viewelements == null)
                    GetViewElement();
                return _viewelements!;
            }
        }
        public EvangElement? GetViewElement(string? name = null)
        {
            if (_viewelements == null)
            {
                _viewelements = new Dictionary<string, EvangElement?>();
                var props = GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(EvangAttribute)));
                foreach (var prop in props)
                {
                    var bve = prop.GetValue(this) as EvangElement;
                    if (bve != null)
                        _viewelements.Add(prop.Name, bve);
                }
            }
            if (name == null)
                return null;
            if (_viewelements.Keys.Contains(name))
                return _viewelements[name];
            return null;
        }

        public string SetBinding(string propname)
        {
            if (_propsmap.ContainsKey(propname))
                return _propsmap[propname];
            var prop = $"Prop{propindex}";
            _propsmap.Add(propname, prop);
            _propsmap.Add(prop, propname);
            propindex++;
            return prop;
        }

        public void SetViewElementValue(string propname, string strval)
        {
            //コントロールの値を編集した際に未操作時間監視のカウンターをリセット
            //Threads.NoActionThread.NoActionCounter = 0;

            var prop = GetType().GetProperty(propname);
            if (prop == null)
                return;
            if (prop.PropertyType.BaseType != typeof(EvangElement) && prop.PropertyType.BaseType!.BaseType != typeof(EvangElement))
                return;
            var ve = prop.GetValue(this, null) as EvangElement;
            if (ve == null)
                return;
            ve.Value = strval;
        }

        public void SetPropMemberValue(string propname, string strval)
        {
            var prop = _propsmap[propname].ToLower();
            var fieldinfo = GetType().GetField($"_{prop}");
            if (fieldinfo == null)
                return;
            fieldinfo.SetValue(this, strval);
            OnPropertyChanged(_propsmap[propname]);
        }

        public string? GetDecimalFormat(string unitprop)
        {
            var propinfo = GetType().GetProperty(unitprop);
            if (propinfo == null)
                return null;
            var ve = propinfo.GetValue(this, null) as EvangElement;
            if (ve == null)
                return null;
            var unit = ve.Value;
            if (string.IsNullOrEmpty(unit))
                return null;
            return BaseUtils.GetQtyformat(unit);
        }

        public Dictionary<string, string>? VMLabels { get; set; }
        public Dictionary<string, string> GetVMLabels()
        {
            if (VMLabels == null)
            {
                VMLabels = new Dictionary<string, string>();
                var props = GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(EvangAttribute)));
                foreach (var prop in props)
                {
                    var bve = prop.GetValue(this) as EvangElement;
                    if (bve == null) continue;
                    var bcv = bve.ElementObject as EvangCompositeView;
                    if (bcv == null) continue;

                    var cveattr = prop.GetCustomAttribute<CompositeAttribute>();
                    VMLabels.Add(prop.Name, BaseUtils.GetCustomString(cveattr?.MessageLabel) ?? cveattr?.MessageLabel ?? bcv.Label.Text);
                }
            }
            return VMLabels;
        }

        public EvangElement? GetCurrentFocused()
        {
            var props = GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(EvangAttribute)));
            foreach (var prop in props)
            {
                var bve = prop.GetValue(this) as EvangElement;
                if (bve == null) continue;
                var bcv = bve.ElementObject as EvangCompositeView;
                if (bcv == null) continue;
                if (bcv.InputControl != null && bcv.InputControl.IsFocused)
                    return bve;
            }
            return null;
        }

        public void SetShowOnlyToAllControls(bool show)
        {
            var props = GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(EvangAttribute)));
            foreach (var prop in props)
            {
                var bve = prop.GetValue(this) as EvangElement;
                bve?.SetShowOnly(show);
            }
        }

        #region basis properties
        public string? _prop0;
        public string? Prop0
        {
            get => _prop0;
            set
            {
                if (_prop0 != value)
                {

                    _prop0 = value;
                    SetViewElementValue(_propsmap["Prop0"], value ?? "");
                }
            }
        }

        public string? _prop1;
        public string? Prop1
        {
            get => _prop1;
            set
            {
                if (_prop1 != value)
                {
                    _prop1 = value;
                    SetViewElementValue(_propsmap["Prop1"], value ?? "");
                }
            }
        }

        public string? _prop2;
        public string? Prop2
        {
            get => _prop2;
            set
            {
                if (_prop2 != value)
                {
                    _prop2 = value;
                    SetViewElementValue(_propsmap["Prop2"], value ?? "");
                }
            }
        }

        public string? _prop3;
        public string? Prop3
        {
            get => _prop3;
            set
            {
                if (_prop3 != value)
                {
                    _prop3 = value;
                    SetViewElementValue(_propsmap["Prop3"], value ?? "");
                }
            }
        }

        public string? _prop4;
        public string? Prop4
        {
            get => _prop4;
            set
            {
                if (_prop4 != value)
                {
                    _prop4 = value;
                    SetViewElementValue(_propsmap["Prop4"], value ?? "");
                }
            }
        }

        public string? _prop5;
        public string? Prop5
        {
            get => _prop5;
            set
            {
                if (_prop5 != value)
                {
                    _prop5 = value;
                    SetViewElementValue(_propsmap["Prop5"], value ?? "");
                }
            }
        }

        public string? _prop6;
        public string? Prop6
        {
            get => _prop6;
            set
            {
                if (_prop6 != value)
                {
                    _prop6 = value;
                    SetViewElementValue(_propsmap["Prop6"], value ?? "");
                }
            }
        }

        public string? _prop7;
        public string? Prop7
        {
            get => _prop7;
            set
            {
                if (_prop7 != value)
                {
                    _prop7 = value;
                    SetViewElementValue(_propsmap["Prop7"], value ?? "");
                }
            }
        }

        public string? _prop8;
        public string? Prop8
        {
            get => _prop8;
            set
            {
                if (_prop8 != value)
                {
                    _prop8 = value;
                    SetViewElementValue(_propsmap["Prop8"], value ?? "");
                }
            }
        }

        public string? _prop9;
        public string? Prop9
        {
            get => _prop9;
            set
            {
                if (_prop9 != value)
                {
                    _prop9 = value;
                    SetViewElementValue(_propsmap["Prop9"], value ?? "");
                }
            }
        }

        public string? _prop10;
        public string? Prop10
        {
            get => _prop10;
            set
            {
                if (_prop10 != value)
                {
                    _prop10 = value;
                    SetViewElementValue(_propsmap["Prop10"], value ?? "");
                }
            }
        }

        public string? _prop11;
        public string? Prop11
        {
            get => _prop11;
            set
            {
                if (_prop11 != value)
                {
                    _prop11 = value;
                    SetViewElementValue(_propsmap["Prop11"], value ?? "");
                }
            }
        }

        public string? _prop12;
        public string? Prop12
        {
            get => _prop12;
            set
            {
                if (_prop12 != value)
                {
                    _prop12 = value;
                    SetViewElementValue(_propsmap["Prop12"], value ?? "");
                }
            }
        }

        public string? _prop13;
        public string? Prop13
        {
            get => _prop13;
            set
            {
                if (_prop13 != value)
                {
                    _prop13 = value;
                    SetViewElementValue(_propsmap["Prop13"], value ?? "");
                }
            }
        }

        public string? _prop14;
        public string? Prop14
        {
            get => _prop14;
            set
            {
                if (_prop14 != value)
                {
                    _prop14 = value;
                    SetViewElementValue(_propsmap["Prop14"], value ?? "");
                }
            }
        }

        public string? _prop15;
        public string? Prop15
        {
            get => _prop15;
            set
            {
                if (_prop15 != value)
                {
                    _prop15 = value;
                    SetViewElementValue(_propsmap["Prop15"], value ?? "");
                }
            }
        }

        public string? _prop16;
        public string? Prop16
        {
            get => _prop16;
            set
            {
                if (_prop16 != value)
                {
                    _prop16 = value;
                    SetViewElementValue(_propsmap["Prop16"], value ?? "");
                }
            }
        }

        public string? _prop17;
        public string? Prop17
        {
            get => _prop17;
            set
            {
                if (_prop17 != value)
                {
                    _prop17 = value;
                    SetViewElementValue(_propsmap["Prop17"], value ?? "");
                }
            }
        }

        public string? _prop18;
        public string? Prop18
        {
            get => _prop18;
            set
            {
                if (_prop18 != value)
                {
                    _prop18 = value;
                    SetViewElementValue(_propsmap["Prop18"], value ?? "");
                }
            }
        }

        public string? _prop19;
        public string? Prop19
        {
            get => _prop19;
            set
            {
                if (_prop19 != value)
                {
                    _prop19 = value;
                    SetViewElementValue(_propsmap["Prop19"], value ?? "");
                }
            }
        }

        public string? _prop20;
        public string? Prop20
        {
            get => _prop20;
            set
            {
                if (_prop20 != value)
                {
                    _prop20 = value;
                    SetViewElementValue(_propsmap["Prop20"], value ?? "");
                }
            }
        }

        public string? _prop21;
        public string? Prop21
        {
            get => _prop21;
            set
            {
                if (_prop21 != value)
                {
                    _prop21 = value;
                    SetViewElementValue(_propsmap["Prop21"], value ?? "");
                }
            }
        }

        public string? _prop22;
        public string? Prop22
        {
            get => _prop22;
            set
            {
                if (_prop22 != value)
                {
                    _prop22 = value;
                    SetViewElementValue(_propsmap["Prop22"], value ?? "");
                }
            }
        }

        public string? _prop23;
        public string? Prop23
        {
            get => _prop23;
            set
            {
                if (_prop23 != value)
                {
                    _prop23 = value;
                    SetViewElementValue(_propsmap["Prop23"], value ?? "");
                }
            }
        }

        public string? _prop24;
        public string? Prop24
        {
            get => _prop24;
            set
            {
                if (_prop24 != value)
                {
                    _prop24 = value;
                    SetViewElementValue(_propsmap["Prop24"], value ?? "");
                }
            }
        }

        public string? _prop25;
        public string? Prop25
        {
            get => _prop25;
            set
            {
                if (_prop25 != value)
                {
                    _prop25 = value;
                    SetViewElementValue(_propsmap["Prop25"], value ?? "");
                }
            }
        }

        public string? _prop26;
        public string? Prop26
        {
            get => _prop26;
            set
            {
                if (_prop26 != value)
                {
                    _prop26 = value;
                    SetViewElementValue(_propsmap["Prop26"], value ?? "");
                }
            }
        }

        public string? _prop27;
        public string? Prop27
        {
            get => _prop27;
            set
            {
                if (_prop27 != value)
                {
                    _prop27 = value;
                    SetViewElementValue(_propsmap["Prop27"], value ?? "");
                }
            }
        }

        public string? _prop28;
        public string? Prop28
        {
            get => _prop28;
            set
            {
                if (_prop28 != value)
                {
                    _prop28 = value;
                    SetViewElementValue(_propsmap["Prop28"], value ?? "");
                }
            }
        }

        public string? _prop29;
        public string? Prop29
        {
            get => _prop29;
            set
            {
                if (_prop29 != value)
                {
                    _prop29 = value;
                    SetViewElementValue(_propsmap["Prop29"], value ?? "");
                }
            }
        }
        #endregion
    }
}
