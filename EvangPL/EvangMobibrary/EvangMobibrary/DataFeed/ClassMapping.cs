using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.TitleBar;
using System.Diagnostics;
using System.Reflection;

namespace EvangSol.Mobibrary.DataFeed
{
    //for ClassMapping to recoginize mapping classes
    public interface IMappingBase { }

    public class ClassMapping
    {
        public static List<Assembly>? ref_assemblies;

        //dictionary to hold all inherited classes
        private static Dictionary<Type, Type> inheritmap = new Dictionary<Type, Type>();

        public static void Init()
        {
            var templist = new List<Type>
            {
                typeof(EvangContentVM),
                typeof(EvangTitleBar),
                typeof(EvangView.EvangView),
                typeof(BindableBrokerView),
                typeof(EvangEntryView),
                typeof(TableEntryView),
            };
            var templates = from x in Assembly.GetExecutingAssembly().GetTypes() where x.IsClass && x.BaseType!.Name == "BaseTemplatePage`1" select x;
            foreach (var tmpt in templates)
                templist.Add(tmpt);

            //get all the stacked assemblies
            StackFrame[] frames = new StackTrace().GetFrames();
            ref_assemblies = (from f in frames select f.GetMethod()!.ReflectedType!.Assembly).Distinct().ToList();
            //only take the first three, assume the standard app is only inherited once
            ref_assemblies.RemoveRange(3, ref_assemblies.Count - 3);
            foreach (Assembly asm in ref_assemblies)
            {
                //single out all inherited classes and hold them in dict
                var inherits = from x in asm.GetTypes() where x.IsClass && !x.IsGenericType && x.BaseType != null && !x.BaseType!.IsGenericType
                               && x.GetInterface("IMappingBase") != null && !templist.Contains(x) && !templist.Contains(x.BaseType!) select x;
                foreach (var tp in inherits)
                    if (!inheritmap.ContainsKey(tp.BaseType!))
                        inheritmap.Add(tp.BaseType!, tp);
            }
        }

        public static Type GetMappingType(Type tp)
        {
            if (inheritmap.ContainsKey(tp))
            {
                tp = inheritmap[tp];
                //only three layer inheritance is possible (MobileBaseClient -> Standard App -> Custom App)
                //so just do one more search
                if (inheritmap.ContainsKey(tp))
                    tp = inheritmap[tp];
            }
            return tp;
        }

        public static ContentPage CreatePageInstance(Type tp)
        {
            string originName = tp.Name;
            tp = GetMappingType(tp);
            var inst = Activator.CreateInstance(tp, new object[] { }) as ContentPage;
            if (inst == null)
                throw new Exception($"Failed to create instance of {tp.Name}.");

            (inst as EvangContentVM)!.WinId = originName;

            return inst;
        }

        public static ContentPage? CreatePageInstance(string tpnm)
        {
            var pagenm = tpnm;
            if (pagenm.Substring(0, 3) == "Tab")
                pagenm = pagenm.Substring(3);
            //search through assemblies to find the given page
            foreach (Assembly asm in ref_assemblies!)
            {
                var asmnm = asm.GetName().Name;
                // Type? pagetype = Type.GetType($"InventorySys.Views.{pagenm}.{tpnm}, {asmnm}");
                Type? pagetype = Type.GetType($"EvangPL.Views.{pagenm}.{tpnm}, {asmnm}");
                if (pagetype != null)
                {
                    var ret = CreatePageInstance(pagetype);
                    (ret as EvangContentVM)!.WinId = tpnm;

                    return ret;
                }
            }
            return null;
        }

        public static EvangView.EvangView CreateViewModelInstance(Type tp)
        {
            tp = GetMappingType(tp);
            var inst = Activator.CreateInstance(tp, new object[] { }) as EvangView.EvangView;
            if (inst == null)
                throw new Exception($"Failed to create instance of {tp.Name}.");
            return inst;
        }

        public static EvangTitleBar CreateTitleViewInstance(Type tp, INavigation navi, string caption)
        {
            tp = GetMappingType(tp);
            var inst = Activator.CreateInstance(tp, new object[] { navi, caption }) as EvangTitleBar;
            if (inst == null)
                throw new Exception($"Failed to create instance of {tp.Name}.");
            return inst;
        }

        public static EvangJsonModel CreateJsonModelInstance(Type tp)
        {
            tp = GetMappingType(tp);
            var inst = Activator.CreateInstance(tp, new object[] { }) as EvangJsonModel;
            if (inst == null)
                throw new Exception($"Failed to create instance of {tp.Name}.");
            return inst;
        }

        public static List<(string, PropertyInfo, PropertyInfo?, T)> GetPropertyList<T>(Type tp) where T : EvangAttribute
        {
            //a list to hold
            //1. property name
            //2. given type's property info or derived type's property if the given type is inherited and this property is redefined
            //3. if the given type if inherited and this property is redifined, set to the given type's property info
            //4. a combined ViewElementAttribute if the given type has a descent, or the given type's ViewElementAttribute if not
            List<(string, PropertyInfo, PropertyInfo?, T)> propertylist = new();

            ////if the given type has a descent
            //if (inheritmap.ContainsKey(tp))
            //{
            //    //and the descent type has ViewModelAttribute attribute
            //    var detp = inheritmap[tp];
            //    var vmattr = detp.GetCustomAttribute(typeof(ViewModelAttribute)) as ViewModelAttribute;
            //    //and IgnoreParent is true
            //    if (vmattr != null && vmattr.IgnoreParent)
            //    {
            //        //then, ignore all the properties in the given type, return all the properties of the descent type
            //        foreach (var deprop in detp.GetProperties().Where(p => Attribute.IsDefined(p, typeof(T))))
            //        {
            //            //get ViewElementAttribute
            //            var childattr = deprop.GetCustomAttribute<T>();
            //            //add to list
            //            propertylist.Add((deprop.Name, deprop, tp.GetProperty(deprop.Name), childattr!));
            //        }
            //        return propertylist;
            //    }
            //}

            //list to hold given type's properties names, for judging if it's a redifinition if the given type has a child class
            List<string> tppropertynames = new();

            //get given type's properties
            var props = tp.GetProperties().Where(p => Attribute.IsDefined(p, typeof(T)));
            foreach (var tpprop in props)
            {
                tppropertynames.Add(tpprop.Name);
                //get ViewElementAttribute
                var parentattr = tpprop.GetCustomAttribute<T>();
                //put into propertylist by order
                propertylist.Add((tpprop.Name, tpprop, null, parentattr!));
            }

            //if the given type has a descent
            if (inheritmap.ContainsKey(tp))
            {
                //get properties while excluding properties inherited from its parent class.
                props = inheritmap[tp].GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(p => Attribute.IsDefined(p, typeof(T)));
                foreach (var childprop in props)
                {
                    //get child type's ViewElementAttribute
                    var childattr = childprop.GetCustomAttribute<T>();

                    //check if its name is in propertylist
                    var index = propertylist.FindIndex(x => x.Item1 == childprop.Name);
                    //if in propertylist
                    if (index != -1)
                    {
                        //get the tuple values
                        PropertyInfo parentprop;
                        T parentattr;
                        (_, parentprop, _, parentattr) = propertylist[index];

                        ////if it's not visible, remove it
                        //if (!(childattr!.FlipVisible ^ parentattr!.Visible))
                        //{
                        //    propertylist.Remove(propertylist[index]);
                        //    continue;
                        //}

                        //combine attribute values
                        //childattr = CombineAttributes(childattr, parentattr!) ?? childattr;

                        ////if it's a change of placement
                        //if (!string.IsNullOrEmpty(childattr.PlaceAfterOf))
                        //{
                        //    var item = childattr.PlaceAfterOf;
                        //    if (item[0] == '@')
                        //        item = item.Substring(1);
                        //    //keep the removing item, for after inserting the index could change
                        //    var old = propertylist[index];
                        //    //if it's Head, put it to the first place
                        //    if (item == "Head")
                        //    {
                        //        propertylist.Insert(0, (parentprop.Name, childprop, parentprop, childattr));
                        //        //delete current item
                        //        propertylist.Remove(old);
                        //    }
                        //    else
                        //    {
                        //        //search the after-of item
                        //        var toindex = propertylist.FindIndex(x => x.Item1 == item);
                        //        //change placement
                        //        if (toindex != -1)
                        //        {
                        //            //insert it to the designated place
                        //            propertylist.Insert(toindex + 1, (parentprop.Name, childprop, parentprop, childattr));
                        //            //delete current item
                        //            propertylist.Remove(old);
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    propertylist[index] = (childprop.Name, childprop, parentprop, childattr);
                        //}
                    }
                    //if not, add to propertylist
                    else
                    {
                        ////append
                        //if (string.IsNullOrEmpty(childattr!.PlaceAfterOf))
                        //{
                        //    propertylist.Add((childprop.Name, childprop, null, childattr));
                        //}
                        ////insert
                        //else
                        //{
                        //    var item = childattr.PlaceAfterOf;
                        //    if (item[0] == '@')
                        //        item = item.Substring(1);
                        //    //if Head, insert it to the very beginning
                        //    if (item == "Head")
                        //    {
                        //        propertylist.Insert(0, (childprop.Name, childprop, null, childattr));
                        //    }
                        //    else
                        //    {
                        //        //search the after-of item
                        //        var toindex = propertylist.FindIndex(x => x.Item1 == item);
                        //        //insert it to the designated place
                        //        if (toindex != -1)
                        //            propertylist.Insert(toindex + 1, (childprop.Name, childprop, null, childattr));
                        //    }
                        //}
                    }
                }
            }
            return propertylist;
        }

        //static T? CombineAttributes<T>(T child, T parent) where T : BaseAttribute
        //{
        //    if (child is ViewElementAttribute)
        //    {
        //        var childattr = child as ViewElementAttribute;
        //        var parentattr = parent as ViewElementAttribute;
        //        if (childattr == null || parentattr == null)
        //            return null;
        //        return CombineViewElementAttribute(childattr, parentattr) as T;
        //    }
        //    else if (child is DataGridColumnAttribute)
        //    {
        //        var childattr = child as DataGridColumnAttribute;
        //        var parentattr = parent as DataGridColumnAttribute;
        //        if (childattr == null || parentattr == null)
        //            return null;
        //        return CombineDataGridColumnAttribute(childattr, parentattr) as T;
        //    }
        //    return null;
        //}

        //static ViewElementAttribute CombineViewElementAttribute(ViewElementAttribute child, ViewElementAttribute parent)
        //{
        //    //BaseAttribute properties
        //    child.Width = child.Width > 0 ? child.Width : parent.Width;
        //    child.Height = child.Height > 0 ? child.Height : parent.Height;

        //    //ViewElementAttribute properties
        //    child.Required = child.FlipRequired ? !parent.Required : parent.Required;
        //    child.Location = child.RedoLocation ? child.Location : parent.Location;
        //    child.Label = string.IsNullOrEmpty(child.Label) ? parent.Label : child.Label;
        //    child.ColSpan = child.RedoColSpan ? child.ColSpan : parent.ColSpan;
        //    child.RowSpan = child.RedoRowSpan ? child.RowSpan : parent.RowSpan;
        //    child.ForeFront = child.FlipForeFront ? !parent.ForeFront : parent.ForeFront;
        //    child.HasBorder = child.FlipHasBorder ? !parent.HasBorder : parent.HasBorder;
        //    child.Visible = child.FlipVisible ? !parent.Visible : parent.Visible;
        //    child.TextSize = child.TextSize > 0 ? child.TextSize : parent.TextSize;
        //    child.BackgroundColor ??= parent.BackgroundColor;
        //    child.TextColor ??= parent.TextColor;
        //    child.FontAttributes ??= parent.FontAttributes;
        //    child.TextAlignment ??= parent.TextAlignment;
        //    child.Padding ??= parent.Padding;
        //    child.InputType ??= parent.InputType;
        //    child.Placeholder ??= parent.Placeholder;
        //    child.MaxLength = child.MaxLength > 0 ? child.MaxLength : parent.MaxLength;
        //    child.MaxLines = child.MaxLines > 0 ? child.MaxLines : parent.MaxLines;
        //    child.DateFormat ??= parent.DateFormat;
        //    child.TimeFormat ??= parent.TimeFormat;
        //    child.DecimalFormat ??= parent.DecimalFormat;

        //    //CompositeViewElementAttribute properties
        //    if (child is CompositeViewElementAttribute cvchild && parent is CompositeViewElementAttribute cvparent)
        //    {
        //        cvchild.LabelWidth = cvchild.LabelWidth > 0 ? cvchild.LabelWidth : cvparent.LabelWidth;
        //        cvchild.LabelTextSize = cvchild.LabelTextSize > 0 ? cvchild.LabelTextSize : cvparent.LabelTextSize;
        //        cvchild.LabelBackgroundColor ??= cvparent.LabelBackgroundColor;
        //        cvchild.LabelTextColor ??= cvparent.LabelTextColor;
        //        cvchild.LabelFontAttributes ??= cvparent.LabelFontAttributes;
        //        cvchild.LabelTextAlignment ??= cvparent.LabelTextAlignment;
        //        cvchild.LabelHeight = cvparent.LabelHeight;
        //        cvchild.LayoutPattern ??= cvparent.LayoutPattern;
        //        cvchild.EntryType ??= cvparent.EntryType;
        //        cvchild.ShowKeyBoardIcon = cvchild.FlipShowKeyBoardIcon ? !cvparent.ShowKeyBoardIcon : cvparent.ShowKeyBoardIcon;
        //        cvchild.ScanType ??= cvparent.ScanType;
        //        cvchild.EditorHeight = cvchild.EditorHeight > 0 ? cvchild.EditorHeight : cvparent.EditorHeight;
        //        cvchild.ExtraWidth = cvchild.ExtraWidth > 0 ? cvchild.ExtraWidth : cvparent.ExtraWidth;
        //        cvchild.ExtraValue ??= cvparent.ExtraValue;
        //        cvchild.Radios ??= cvparent.Radios;
        //        cvchild.FlexHeight = cvparent.FlexHeight;
        //        cvchild.Direction ??= cvparent.Direction;
        //        cvchild.Wrap ??= cvparent.Wrap;
        //        cvchild.JustifyContent ??= cvparent.JustifyContent;
        //        cvchild.AlignItems ??= cvparent.AlignItems;
        //        cvchild.AlignContent ??= cvparent.AlignContent;
        //        cvchild.MessageLabel ??= cvparent.MessageLabel; //SIR0188675
        //    }
        //    return child;
        //}

        //static DataGridColumnAttribute CombineDataGridColumnAttribute(DataGridColumnAttribute child, DataGridColumnAttribute parent)
        //{
        //    //BaseAttribute properties
        //    child.Width = child.Width > 0 ? child.Width : parent.Width;
        //    child.Height = child.Height > 0 ? child.Height : parent.Height;

        //    //DataGridColumnAttribute properties
        //    child.Sorting ??= parent.Sorting;
        //    child.HeadText ??= parent.HeadText;
        //    child.HeadTextSize = child.HeadTextSize > 0 ? child.HeadTextSize : parent.HeadTextSize;
        //    child.ColAlignment ??= parent.ColAlignment;
        //    child.ColWidth = child.ColWidth > 0 ? child.ColWidth : parent.ColWidth;
        //    child.ForeFront = child.FlipForeFront ? !parent.ForeFront : parent.ForeFront;
        //    child.propTextSize = child.propTextSize > 0 ? child.propTextSize : parent.propTextSize;
        //    child.propTextColor ??= parent.propTextColor;
        //    child.propBackgroundColor ??= parent.propBackgroundColor;
        //    child.propFontAttributes ??= parent.propFontAttributes;
        //    child.FormatUnit ??= parent.FormatUnit;
        //    child.FormatString ??= parent.FormatString;
        //    child.FormatDateTime ??= parent.FormatDateTime;
        //    child.RequiredInput = child.FlipRequiredInput ? !parent.RequiredInput : parent.RequiredInput;

        //    return child;
        //}
    }
}
