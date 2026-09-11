using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.EvangCustom;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.TitleBar;
using EvangSol.Mobibrary.Utilities.Behavior;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Converter;
using System.Reflection;
using EvangSol.Mobibrary.EvangWidget;

namespace EvangSol.Mobibrary.EvangViewModel
{
    public enum LayoutPattern
    {
        FlexGrid = 0,
        MultiColumn = 1,
        SingleColumn = 2,
    }

    public abstract class EvangTemplateVM<B> : EvangContentVM<B> where B : BindableBrokerView
    {
        public PageSettingAttribute? pageattr;
        public List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)>? vmproplist;  //property list

        List<(CompositeAttribute, EvangElement)> _extralist;
        protected Dictionary<string, EvangElement> _extraelems;

        public EvangTemplateVM(string caption) : base(caption, null)
        {
            pageattr = GetType().GetCustomAttribute(typeof(PageSettingAttribute)) as PageSettingAttribute;
            _extralist = new List<(CompositeAttribute, EvangElement)>();
            _extraelems = new Dictionary<string, EvangElement>();
        }

        public override void BeforeBaseRendering(string caption, object? viewmodel)
        {
            var tp = ClassMapping.GetMappingType(typeof(PageTitle));
            var titleview = Activator.CreateInstance(tp, [Navigation, GetCustomString(caption) ?? GetType().Name]) as IBaseTitleView;
            if (titleview != null )
                TitleView = titleview;
        }

        #region Create controls
        protected  View? CreateViewControl(PropertyInfo prop, ControlAttribute cattr, ref EvangElement ele, BindableBrokerView bbv)
        {
            var viewtype = typeof(Label);
            foreach (var tp in prop.PropertyType.GetGenericArguments())
                viewtype = tp;

            View? control = null;
            try
            {
	            var faces = viewtype.GetInterfaces();
	            if (faces.Contains(typeof(IGroupCompositeView)))
	            {
	                if (viewtype.Name == "RadioGroupComposite")
	                {
                        bbv.SetBinding(prop.Name);
	                    control = CreateRadioGroup(cattr, prop.Name);
	                    control.SetBinding(RadioGroupComposite.CheckedProperty, bbv.GetBaseProperty(prop.Name));
	                }
                    else if (viewtype.Name == "RadioGroupLabelComposite")
                    {
                        bbv.SetBinding(prop.Name);
                        control = CreateRadioGroupLabel(cattr, prop.Name);
                        control.SetBinding(RadioGroupComposite.CheckedProperty, bbv.GetBaseProperty(prop.Name));
                    }
                }
	            else if (faces.Contains(typeof(IInputControl)))
	            {
                    control = CreateCompositeView(viewtype, cattr, prop.Name, ref ele, bbv);
	            }
	            else
	            {
	                switch (viewtype.Name)
	                {
	                    case "BoxView":
	                        control = new BoxView
	                        {
	                            HeightRequest = cattr.Height > 0 ? cattr.Height : (pageattr != null && pageattr.CompositeHeight > 0) ? pageattr.CompositeHeight : CommonViewSetting.COMPOSITE_HEIGHT,
	                            BackgroundColor = Colors.Transparent
	                        };
	                        break;
	                    case "Label":
                            bbv.SetBinding(prop.Name);
                            var height = cattr.Height > 0 ? cattr.Height : (pageattr != null && pageattr.CompositeHeight > 0) ? pageattr.CompositeHeight : CommonViewSetting.COMPOSITE_HEIGHT;
	                        var label = new Label
	                        {
	                            HeightRequest = height * cattr.MaxLines,
	                            FontSize = cattr.TextSize > 0 ? cattr.TextSize : CommonViewSetting.LABEL_FONTSIZE,
	                            BackgroundColor = GetColor(cattr.BackgroundColor) ?? Colors.Transparent,
	                            TextColor = GetColor(cattr.TextColor) ?? GetColor("Gray900"),
	                            FontAttributes = GetFontAttr(cattr.FontAttributes) ?? FontAttributes.None,
	                            HorizontalTextAlignment = GetAlignment(cattr.TextAlignment) ?? TextAlignment.Start,
	                            Padding = new Thickness(5, 0),
	                            MaxLines = cattr.MaxLines,
	                            LineBreakMode = Enum.Parse<LineBreakMode>(cattr.LineBreakMode),
	                            VerticalOptions = LayoutOptions.Start,
	                        };
	                        if (string.IsNullOrEmpty(cattr.Label))
	                        {
	                            label.SetBinding(Label.TextProperty, bbv.GetBaseProperty(prop.Name));
	                        }
	                        else
	                        {
	                            label.Text = GetCustomString(cattr.Label) ?? cattr.Label;
	                        }
	                        if (cattr.HasBorder)
	                        {
	                            control = new Border
	                            {
	                                Stroke = GetColor("Gray300"),
	                                Content = label,
	                            };
	                        }
	                        else
	                        {
	                            control = label;
	                        }
	                        break;
	                    case "Button":
	                        control = new Button
	                        {
	                            HeightRequest = cattr.Height > 0 ? cattr.Height : (pageattr != null && pageattr.CompositeHeight > 0) ? pageattr.CompositeHeight : CommonViewSetting.COMPOSITE_HEIGHT,
	                            FontSize = cattr.TextSize > 0 ? cattr.TextSize : CommonViewSetting.LABEL_FONTSIZE,
	                            BackgroundColor = GetColor(cattr.BackgroundColor) ?? GetColor("Primary"),
	                            TextColor = GetColor(cattr.TextColor) ?? GetColor("Gray900"),
	                            FontAttributes = GetFontAttr(cattr.FontAttributes) ?? FontAttributes.None,
	                            Text = GetCustomString(cattr.Label) ?? cattr.Label,
	                            VerticalOptions = LayoutOptions.Start,
	                        };
	                        if (cattr.Width > 0)
	                            control.WidthRequest = cattr.Width;
	                        break;
                        case "SingleTapButton":
                            control = new SingleTapButton
                            {
                                HeightRequest = cattr.Height > 0 ? cattr.Height : (pageattr != null && pageattr.CompositeHeight > 0) ? pageattr.CompositeHeight : CommonViewSetting.COMPOSITE_HEIGHT,
                                FontSize = cattr.TextSize > 0 ? cattr.TextSize : CommonViewSetting.LABEL_FONTSIZE,
                                BackgroundColor = GetColor(cattr.BackgroundColor) ?? GetColor("Primary"),
                                TextColor = GetColor(cattr.TextColor) ?? GetColor("Gray900"),
                                FontAttributes = GetFontAttr(cattr.FontAttributes) ?? FontAttributes.None,
                                Text = GetCustomString(cattr.Label) ?? cattr.Label,
                                VerticalOptions = LayoutOptions.Start,
                            };
                            if (cattr.Width > 0)
                                control.WidthRequest = cattr.Width;
                            break;
                        case "StackLayout":
	                        control = new StackLayout();
	                        break;
	                    case "FlexLayout":
	                        control = new FlexLayout();
	                        break;
	                    case "ProgressBar":
	                        bbv.SetBinding(prop.Name);
	                        var barheight = cattr.Height > 0 ? cattr.Height : (pageattr != null && pageattr.CompositeHeight > 0) ? pageattr.CompositeHeight : CommonViewSetting.COMPOSITE_HEIGHT;
	                        control = new ProgressBar
	                        {
#if ANDROID
                                ScaleY = 5,
#elif WINDOWS
                                ScaleY = 0.7,
#endif
                                HeightRequest = barheight,
	                            MinimumHeightRequest = barheight,
	                            ProgressColor = GetColor(cattr.TextColor) ?? GetColor("Magenta"),
	                        };
	                        control.SetBinding(ProgressBar.ProgressProperty, bbv.GetBaseProperty(prop.Name), converter: new StringToDoubleConverter());
	                        break;
	                    default:
	                        break;
	                }
	            }
	
	            if (control != null)
	            {
                    control.ClassId = prop.Name;
                    ele.ElementObject = control;
                    //if there's an initial value, set it to the control
                    if (!string.IsNullOrEmpty(ele.Value))
                    {
                        ele.SetValue(ele.Value);
                    }
                    //set ShowOnly status
                    if (ele.ShowOnly)
                        ele.SetShowOnly(true);
                    return control;
	            }
	            throw new Exception($"Failed to create type {viewtype.Name}.");
            }
            catch (Exception ex)
            {
                ShowException($"Failed to create type {viewtype.Name}." + Environment.NewLine + $"{ ex.Message}" + Environment.NewLine + $"{ ex.StackTrace}");
            }
            return null;
        }

        EvangCompositeView? CreateCompositeView(Type viewtype, ControlAttribute cattr, string propname, ref EvangElement ele, BindableBrokerView bbv)
        {
            var cpattr = cattr as CompositeAttribute;
            if (cpattr == null)
                throw new Exception($"Composite controls must be with a EvangCompositeAttribute. Property {propname}.");
            //compose label setting
            var labelsetting = new CommonViewSetting
            {
                Width = cpattr == null ? null : cpattr.LabelWidth >= 0 ? cpattr.LabelWidth : null,
                TextSize = cpattr == null ? null : cpattr.LabelTextSize > 0 ? cpattr.LabelTextSize : null,
                TextColor = GetColor(cpattr?.LabelTextColor),
                BackgroundColor = GetColor(cpattr?.LabelBackgroundColor),
                FontAttributes = GetFontAttr(cpattr?.LabelFontAttributes),
                Alignment = GetAlignment(cpattr?.LabelTextAlignment),
                LabelHeight = cpattr?.LabelHeight < 0 ? pageattr?.LabelHeight ?? -1 : cpattr?.LabelHeight ?? -1,
                MessageLabel = cpattr?.MessageLabel,
            };
            //compose input setting
            (int, int, int, int)? Padding = null;
            if (!string.IsNullOrEmpty(cattr.Padding))
            {
                //convert padding string to integer tuple
                var pads = cattr.Padding.Split(",");
                if (pads.Length == 4)
                {
                    int[] p = new int[4];
                    for (int i = 0; i < 4; i++)
                        int.TryParse(pads[i], out p[i]);
                    Padding = (p[0], p[1], p[2], p[3]);
                }
            }
            var inputsetting = new CommonViewSetting
            {
                Width = cattr.Width > 0 ? cattr.Width : null,
                TextSize = cattr.TextSize > 0 ? cattr.TextSize : null,
                TextColor = GetColor(cattr.TextColor),
                BackgroundColor = GetColor(cattr.BackgroundColor),
                FontAttributes = GetFontAttr(cattr.FontAttributes),
                Alignment = GetAlignment(cattr.TextAlignment),
                Padding = Padding,
                InputType = cattr.InputType,
                Placeholder = GetCustomString(cattr.Placeholder) ?? cattr.Placeholder,
                DateFormat = cattr.DateFormat,
                TimeFormat = cattr.TimeFormat,
                EditorHeight = cpattr?.EditorHeight,
                MaxLength = cattr.MaxLength,
                ScanType = cpattr?.ScanType ?? "",
                ShowKeyBoardIcon = cpattr?.ShowKeyBoardIcon ?? false,
                DecimalFormat = cattr.DecimalFormat,
                InEntry = bbv is SwipeEntryView,
            };
            //get composite control height
            var height = cattr.Height;
            if (height <= 0)
            {
                if (pageattr?.CompositeHeight == null)
                    height = (int)CommonViewSetting.COMPOSITE_HEIGHT;
                else
                    height = pageattr.CompositeHeight;
            }

            var cv = Activator.CreateInstance(viewtype,
            [
                cattr.Label,
                height,
                cattr.Required,
                cpattr?.ShowKeyBoardIcon,
                labelsetting,
                inputsetting,
                cpattr?.ExtraWidth,
                cpattr?.LayoutPattern == null ? null : Enum.Parse<CompositeLayoutPattern>(cpattr?.LayoutPattern!)
            ]) as EvangCompositeView;

            if (cv != null)
            {
                cv.ControlName = propname;
                if (cattr.InputType == "Decimal")
                {
                    var unit = cpattr!.ExtraValue;
                    if (string.IsNullOrEmpty(unit) && cattr.DecimalFormat == null)
                        throw new Exception($"No ExtraValue set to property {propname}. If InputType is Decimal and DecimalFormat is not set, ExtraValue should be set to a property's name with prefix @.");

                    if (!string.IsNullOrEmpty(unit) && unit[0] == '@')
                        unit = unit.Substring(1);
                    cv.SetInputBinding(bbv.SetBinding(propname), unit, bbv);
                    //set DecimalValidationBehavior's decimal format
                    if (cv is IDecimalFormat ifu)
                    {
                        if (cattr.DecimalFormat == null)
                            ifu.SetUnit(unit!, bbv);
                        else
                            ifu.SetFormat(cattr.DecimalFormat);
                    }
                }
                else
                    cv.SetInputBinding(bbv.SetBinding(propname));
            }
            return cv;
        }

        protected void RenderExtraViewControl(List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)> proplist, BindableBrokerView bbv)
        {
            foreach (var (extracveattr, ele) in _extralist)
            {
                var extrol = CreateExtraViewControl(extracveattr, ele, proplist, bbv);
                if (extrol != null)
                {
                    var ve = _extraelems[extracveattr.ExtraValue!.Substring(1)];
                    ve.PropName = extracveattr.ExtraValue.Substring(1);
                    ve.ViewModel = bbv;
                    ve.ElementObject = extrol;
                    //if there's an initial value, set it to the control
                    if (!string.IsNullOrEmpty(ve.Value))
                        ve.SetValue(ve.Value);
                }
            }
        }

        protected View? CreateExtraViewControl(
            CompositeAttribute extracveattr,
            EvangElement ele,
            List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)> proplist,
            BindableBrokerView bbv)
        {
            var bcv = ele.ElementObject as EvangCompositeView;
            if (bcv == null)
                return null;

            if (extracveattr.ExtraValue![0] == '@')
            {
                var ind = proplist.FindIndex(x => x.Item1 == extracveattr.ExtraValue.Substring(1));
                if (ind == -1)
                    return null;
                var subprop = proplist[ind].Item2;

                var viewtype = typeof(Label);
                foreach (var tp in subprop.PropertyType.GetGenericArguments())
                    viewtype = tp;

                View? subcon = null;
                bbv.SetBinding(subprop.Name);
                var subveattr = subprop.GetCustomAttribute(typeof(ControlAttribute)) as ControlAttribute;
                var textsize = subveattr != null && subveattr.TextSize > 0 ? subveattr.TextSize : CommonViewSetting.INPUT_FONTSIZE;
                switch (viewtype.Name)
                {
                    case "Label":
                        subcon = new Label
                        {
                            FontSize = textsize,
                            TextColor = GetColor(subveattr?.TextColor ?? CommonViewSetting.INPUT_FONTCOLOR),
                            BackgroundColor = GetColor(subveattr?.BackgroundColor),
                            FontAttributes = GetFontAttr(subveattr?.FontAttributes) ?? FontAttributes.None,
                            HorizontalTextAlignment = GetAlignment(subveattr?.TextAlignment) ?? TextAlignment.Start,
                            VerticalTextAlignment = TextAlignment.Center,
                        };
                        subcon.SetBinding(Label.TextProperty, bbv.GetBaseProperty(subprop.Name));
                        break;
                    case "UnitLabel":
                        var unitlabel = new UnitLabel
                        {
                            FontSize = textsize,
                            TextColor = GetColor(subveattr?.TextColor ?? CommonViewSetting.INPUT_FONTCOLOR),
                            BackgroundColor = GetColor(subveattr?.BackgroundColor),
                            FontAttributes = GetFontAttr(subveattr?.FontAttributes) ?? FontAttributes.None,
                            HorizontalTextAlignment = GetAlignment(subveattr?.TextAlignment) ?? TextAlignment.Start,
                            VerticalTextAlignment = TextAlignment.Center,
                        };
                        var converter = new UnitIdToUnitNameConverter();
                        var binding = new Binding(bbv.GetBaseProperty(subprop.Name), BindingMode.OneWay, converter);
                        unitlabel.SetBinding(Label.TextProperty, binding, converter);
                        subcon = unitlabel;
                        break;
                    case "Button":
                        var veattr = subprop.GetCustomAttribute(typeof(ControlAttribute)) as ControlAttribute;
                        subcon = new Button
                        {
                            FontSize = textsize,
                            TextColor = GetColor(subveattr?.TextColor) ?? Colors.White,
                            FontAttributes = GetFontAttr(subveattr?.FontAttributes) ?? FontAttributes.None,
                            BackgroundColor = GetColor(subveattr?.BackgroundColor ?? "Primary"),
                            Text = GetCustomString(veattr!.Label) ?? veattr.Label,
                        };
                        break;
                    case "UnifiedDropDown":
                        subcon = new UnifiedDropDown(extracveattr?.Required ?? false,
                            new CommonViewSetting
                            {
                                Width = extracveattr!.Width > 0 ? extracveattr.Width : null,
                                TextSize = extracveattr.TextSize > 0 ? extracveattr.TextSize : null,
                                TextColor = GetColor(extracveattr.TextColor),
                                BackgroundColor = GetColor(extracveattr.BackgroundColor),
                                FontAttributes = GetFontAttr(extracveattr.FontAttributes),
                                Alignment = GetAlignment(extracveattr.TextAlignment),
                            })
                        {
                            HeightRequest = pageattr?.CompositeHeight ?? CommonViewSetting.COMPOSITE_HEIGHT,
                        };
                        break;
                }
                if (subcon != null)
                {
                    subcon.ClassId = subprop.Name;
                    bcv.InsertExtraControl(subcon, subprop.Name);
                }
                return subcon;
            }
            else
            {
                bcv.InsertExtraControl(new Label
                {
                    FontSize = CommonViewSetting.INPUT_FONTSIZE,
                    TextColor = GetColor(CommonViewSetting.INPUT_FONTCOLOR),
                    HorizontalTextAlignment = TextAlignment.Start,
                    VerticalTextAlignment = TextAlignment.Center,
                    Text = GetCustomString(extracveattr.ExtraValue) ?? extracveattr.ExtraValue,
                }, null);
            }
            return null;
        }

        protected RadioGroupComposite CreateRadioGroup(ControlAttribute cattr, string propname)
        {
            var cpattr = cattr as CompositeAttribute;
            if (cpattr == null || string.IsNullOrWhiteSpace(cpattr.Radios))
                throw new ArgumentException($"No Radios setting found in attribute of {propname}.");
            //radio button setting
            var radiosetting = new CommonViewSetting
            {
                TextSize = cpattr.TextSize > 0 ? cpattr.TextSize : null,
                TextColor = GetColor(cpattr?.TextColor),
                FontAttributes = GetFontAttr(cpattr?.FontAttributes),
                Width = cpattr?.Width
            };
            //get composite control height
            var height = CommonViewSetting.COMPOSITE_HEIGHT;
            if (pageattr != null && pageattr.CompositeHeight > 0)
                height = pageattr.CompositeHeight;
            else if (cpattr!.Height > 0)
                height = cpattr!.Height;
            //get layout pattern
            var flexsetting = new CommonFlexSetting
            {
                Height = (int)height,
                Direction = cpattr!.Direction,
                Wrap = cpattr.Wrap,
                JustifyContent = cpattr.JustifyContent,
                AlignItems = cpattr.AlignItems,
                AlignContent = cpattr.AlignContent,
            };
            return new RadioGroupComposite(propname, cpattr!.Radios, radiosetting, flexsetting);
        }

        protected RadioGroupLabelComposite CreateRadioGroupLabel(ControlAttribute cattr, string propname)
        {
            var cpattr = cattr as CompositeAttribute;
            if (cpattr == null || string.IsNullOrWhiteSpace(cpattr.Radios))
                throw new ArgumentException($"No Radios setting found in attribute of {propname}.");
            //label setting
            var labelsetting = new CommonViewSetting
            {
                TextSize = cpattr.LabelTextSize > 0 ? cpattr.LabelTextSize : null,
                TextColor = GetColor(cpattr?.LabelTextColor),
                BackgroundColor = GetColor(cpattr?.LabelBackgroundColor),
                FontAttributes = GetFontAttr(cpattr?.LabelFontAttributes),
                Width = cpattr?.LabelWidth,
                Alignment = GetAlignment(cpattr?.LabelTextAlignment)
            };
            //radio button setting
            var radiosetting = new CommonViewSetting
            {
                TextSize = cpattr!.TextSize > 0 ? cpattr.TextSize : null,
                TextColor = GetColor(cpattr?.TextColor),
                FontAttributes = GetFontAttr(cpattr?.FontAttributes),
                Width = cpattr?.Width
            };
            //get composite control height
            var height = CommonViewSetting.COMPOSITE_HEIGHT;
            if (pageattr != null && pageattr.CompositeHeight > 0)
                height = pageattr.CompositeHeight;
            else if (cpattr!.Height > 0)
                height = cpattr!.Height;
            //FlexLayout's setting
            var flexsetting = new CommonFlexSetting
            {
                Height = (int)height,
                FlexHeight = cpattr!.FlexHeight,
                Direction = cpattr.Direction,
                Wrap = cpattr.Wrap,
                JustifyContent = cpattr.JustifyContent,
                AlignItems = cpattr.AlignItems,
                AlignContent = cpattr.AlignContent,
            };
            //get layout pattern
            CompositeLayoutPattern layoupattern;
            var success = Enum.TryParse(cpattr?.LayoutPattern!, true, out layoupattern);
            //create and return
            return new RadioGroupLabelComposite(
                cpattr!.Label,
                propname,
                cpattr!.Radios,
                success ? layoupattern : null,
                labelsetting,
                radiosetting,
                flexsetting);
        }
        #endregion

        #region single column filling up
        public (ScrollView, List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)>) RenderSingleColumn<M>(BindableBrokerView bbv, ref double autoheight, Layout? toplayout = null)
        {
            _extralist.Clear();
            _extraelems.Clear();

            autoheight = 0;
            var spacing = CommonViewSetting.LAYOUT_SPACING;
            if (pageattr != null)
                spacing = pageattr.LayoutSpacing;

            StackLayout layout = new()
            {
                Spacing = pageattr?.LayoutSpacing ?? CommonViewSetting.LAYOUT_SPACING,
                Padding = new Thickness(pageattr?.LayoutPadding ?? CommonViewSetting.LAYOUT_PADDING, 0)
            };
            BeforeRendering(layout, bbv);

            var proplist = ClassMapping.GetPropertyList<ControlAttribute>(typeof(M));
            List<(PropertyInfo, PropertyInfo?, ParallelAttribute)> paralist = new();
            CompositeAttribute? cpattr = null;
            int rows = 0;
            foreach (var (_, prop, hideprop, cattr) in proplist)
            {
                //if it's hidden, continue
                //if (!veattr.Visible)
                //    continue;

                var pattr = prop.GetCustomAttribute<ParallelAttribute>()!;
                if (pattr != null && (!pattr.Start || paralist.Count < 1))
                {
                    paralist.Add((prop, hideprop, pattr));
                    continue;
                }
                else if (paralist.Count > 0)
                {
                    int loc = 0;
                    var grid = RenderParallel(bbv, paralist, ref loc, ref autoheight);
                    autoheight += spacing;
                    if (loc == 0)
                    {
                        layout.Add(grid);
                        rows++;
                    }
                    else if (loc == 1 && toplayout != null)
                        toplayout.Add(grid);
                    paralist.Clear();

                    if (pattr != null)
                    {
                        paralist.Add((prop, hideprop, pattr));
                        continue;
                    }
                }

                var ele = CreateViewElement(prop, hideprop, bbv);
                if (prop.PropertyType.Name == "SubViewElement`1")
                {
                    _extraelems.Add(prop.Name, ele);
                }
                else
                {
                    if (cattr.Location == 0)
                    {
                        var control = CreateViewControl(prop, cattr, ref ele, bbv);
                        if (control is EvangCompositeView bcv)
                            autoheight += bcv.CompositeHeight + spacing;
                        else
                            autoheight += (cattr.Height > 0 ? cattr.Height : CommonViewSetting.COMPOSITE_HEIGHT) + spacing;
                        layout.Add(control);
                        rows++;
                    }
                    else if (cattr.Location == 1 && toplayout != null)
                    {
                        toplayout.Add(CreateViewControl(prop, cattr, ref ele, bbv));
                    }
                }

                cpattr = cattr as CompositeAttribute;
                if (cpattr != null && cpattr.ExtraWidth > 0 && !string.IsNullOrEmpty(cpattr.ExtraValue))
                    _extralist.Add((cpattr, ele));
            }

            if (paralist.Count > 0)
            {
                int loc = 0;
                var grid = RenderParallel(bbv, paralist, ref loc, ref autoheight);
                if (loc == 0)
                {
                    layout.Add(grid);
                    rows++;
                }
                else if (loc == 1 && toplayout != null)
                    toplayout.Add(grid);
                paralist.Clear();
            }

            RenderExtraViewControl(proplist, bbv);

            AfterRendering(layout, bbv);

            ScrollView scroll = new()
            {
                Content = layout,
            };
            return (scroll, proplist);
        }

        Grid RenderParallel(BindableBrokerView bbv, List<(PropertyInfo, PropertyInfo?, ParallelAttribute)> paralist, ref int location, ref double autoheight)
        {
            ColumnDefinitionCollection columns = new();
            int columnspacing = -1;
            foreach (var (prop, hideprop, attr) in paralist)
            {
                if (columnspacing == -1)
                    columnspacing = attr.ColumnSpacing;
                columns.Add(new ColumnDefinition { Width = new GridLength(attr.Width, GridUnitType.Star) });
            }
            Grid grid = new Grid
            {
                ColumnSpacing = columnspacing,
                RowDefinitions = { new RowDefinition() },
                ColumnDefinitions = columns
            };

            int i = 0;
            View? view = null;
            double maxHeight = 0;
            foreach (var (prop, hideprop, attr) in paralist)
            {
                var ele = CreateViewElement(prop, hideprop, bbv);
                var cattr = prop.GetCustomAttribute<ControlAttribute>()!;
                view = CreateViewControl(prop, cattr, ref ele, bbv);
                grid.Add(view, i);
                location = cattr.Location;
                double height = 0;
                if (view is EvangCompositeView bcv)
                {
                    height = bcv.CompositeHeight;
                }
                else
                {
                    height = cattr.Height;
                }
                if (maxHeight < height)
                {
                    maxHeight = height;
                }

                i++;
            }
            if(maxHeight <= 0)
            {
                maxHeight = CommonViewSetting.COMPOSITE_HEIGHT;
            }
            autoheight += maxHeight;

            return grid;
        }
        #endregion

        #region multicolumn filling up
        public (Grid, List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)>) RenderMultiColumn<B>(int colcount, int rightwidth, BindableBrokerView bbv, ref double autoheight, List<StackLayout>? panes = null) where B : BindableBrokerView
        {
            _extralist.Clear();
            _extraelems.Clear();

            autoheight = 0;
            List<double> paneheight = new();
            var spacing = CommonViewSetting.LAYOUT_SPACING;
            if (pageattr != null)
                spacing = pageattr.LayoutSpacing;

            var columns = new ColumnDefinitionCollection { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } };
            if (colcount == 2 && rightwidth > 0)
                columns.Add(new ColumnDefinition { Width = new GridLength(rightwidth, GridUnitType.Absolute) });
            else
                for (int i = 1; i < colcount; i++)
                    columns.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var grid = new Grid
            {
                RowDefinitions = { new RowDefinition() },
                ColumnDefinitions = columns,
            };

            if (panes == null)
                panes = new List<StackLayout>();
            for (var i = 0; i < colcount; i++)
            {
                var pane = new StackLayout
                {
                    Spacing = pageattr?.LayoutSpacing ?? CommonViewSetting.LAYOUT_SPACING,
                    Padding = new Thickness(pageattr?.LayoutPadding ?? CommonViewSetting.LAYOUT_PADDING, 0)
                };
                panes.Add(pane);
                grid.Add(pane, i);
                paneheight.Add(0);
            }

            BeforeRendering(grid, bbv);

            var proplist = ClassMapping.GetPropertyList<ControlAttribute>(typeof(B));
            foreach (var (_, prop, hideprop, cattr) in proplist)
            {
                var ele = CreateViewElement(prop, hideprop, bbv);
                //if it's hidden, continue
                //if (!veattr.Visible)
                //    continue;

                if (prop.PropertyType.Name == "SubViewElement`1")
                {
                    _extraelems.Add(prop.Name, ele);
                }
                else if (cattr.Location < panes.Count)
                {
                    var topane = panes[cattr.Location];
                    var control = CreateViewControl(prop, cattr, ref ele, bbv);
                    if (control is EvangCompositeView bcv)
                        paneheight[cattr.Location] += bcv.CompositeHeight + spacing;
                    else
                        paneheight[cattr.Location] += (cattr.Height > 0 ? cattr.Height : CommonViewSetting.COMPOSITE_HEIGHT) + spacing;
                    topane.Add(control);
                }

                var cpattr = cattr as CompositeAttribute;
                if (cpattr != null && cpattr.ExtraWidth > 0 && !string.IsNullOrEmpty(cpattr.ExtraValue))
                    _extralist.Add((cpattr, ele));
            }

            RenderExtraViewControl(proplist, bbv);

            AfterRendering(grid, bbv);

            foreach (var h in paneheight)
            {
                if (autoheight < h)
                    autoheight = h;
            }

            return (grid, proplist);
        }

        protected EvangElement CreateViewElement(PropertyInfo prop, PropertyInfo? hideprop, BindableBrokerView bbv)
        {
            var viewele = prop.GetValue(bbv, null) as EvangElement;
            if (viewele == null)
                viewele = Activator.CreateInstance(prop.PropertyType) as EvangElement;
            if (viewele == null)
                throw new Exception($"Failed to create EvangElement type for property {prop.Name}.");
            viewele.PropName = prop.Name;
            viewele.ViewModel = bbv;
            prop.SetValue(bbv, viewele);
            hideprop?.SetValue(bbv, viewele);
            return viewele;
        }
        #endregion

        #region grid filling up
        public (Grid, List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)>) RenderGrid<M>(int eleminwidth, int colcount, int rightwidth, ref double autoheight, BindableBrokerView bbv)
        {
            var proplist = ClassMapping.GetPropertyList<ControlAttribute>(typeof(M));
            var rowcount = 0;
            if (colcount == 0)
                colcount = (int)(DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density / eleminwidth);
            rowcount = RenderGrid(null, proplist, colcount, bbv);
            autoheight = rowcount * ((pageattr?.CompositeHeight ?? (int)CommonViewSetting.COMPOSITE_HEIGHT) + (pageattr?.LayoutSpacing ?? CommonViewSetting.LAYOUT_SPACING));

            var columns = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            };
            if (colcount == 2)
            {
                if (rightwidth > 0)
                    columns.Add(new ColumnDefinition { Width = new GridLength(rightwidth, GridUnitType.Absolute) });
                else if (rightwidth < 0)
                    columns.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                else
                    columns.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            else
            {
                for (var i = 1; i < colcount; i++)
                {
                    columns.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
            }

            var rows = new RowDefinitionCollection();
            for (var i = 0; i < rowcount; i++)
            {
                rows.Add(new RowDefinition { Height = pageattr?.CompositeHeight ?? CommonViewSetting.COMPOSITE_HEIGHT });
            }
            var grid = new Grid
            {
                RowDefinitions = rows,
                ColumnDefinitions = columns,
                RowSpacing = pageattr?.LayoutSpacing ?? CommonViewSetting.LAYOUT_SPACING,
                Padding = new Thickness(pageattr?.LayoutPadding ?? CommonViewSetting.LAYOUT_PADDING, 0),
            };

            BeforeRendering(grid, bbv);
            RenderGrid(grid, proplist, colcount, bbv);
            AfterRendering(grid, bbv);

            return (grid, proplist);
        }

        int RenderGrid(Grid? grid, List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)> proplist, int clcnt, BindableBrokerView bbv)
        {
            _extralist.Clear();
            _extraelems.Clear();

            int row = 0, col = 0, rowspan = 1, addRowCnt = 1;
            foreach (var (_, prop, hideprop, cattr) in proplist)
            {
                //if it's hidden, continue
                //if (!veattr.Visible)
                //    continue;

                if (cattr.RowSpan < 1)
                    throw new Exception($"Invalid RowSpan value for property {prop.Name}.");
                rowspan = Math.Max(rowspan, cattr.RowSpan);
                if (cattr.RowSpan == rowspan)
                    addRowCnt = 1;
                else
                    addRowCnt = rowspan;

                var currow = row;
                EvangElement? viewele = null;
                if (prop.PropertyType.Name == "SubViewElement`1")
                {
                    viewele = CreateViewElement(prop, hideprop, bbv);
                    _extraelems.Add(prop.Name, viewele);
                }
                else if (cattr.ColSpan < 1)
                {
                    if (col > 0)
                    {
                        currow += addRowCnt;
                        row += rowspan;
                        rowspan = 1;
                        col = 0;
                    }
                    viewele = AddToGrid(grid, prop, hideprop, cattr, currow, col, clcnt, bbv);
                    col = clcnt;
                }
                else if (cattr.ForeFront)
                {
                    if (col > 0)
                    {
                        currow += addRowCnt;
                        row += rowspan;
                        rowspan = 1;
                        col = 0;
                    }
                    viewele = AddToGrid(grid, prop, hideprop, cattr, currow, col, clcnt, bbv);
                    col += cattr.ColSpan;
                }
                else
                {
                    if (col > 0 && col + cattr.ColSpan > clcnt)
                    {
                        currow += addRowCnt;
                        row += rowspan;
                        rowspan = 1;
                        col = 0;
                    }
                    viewele = AddToGrid(grid, prop, hideprop, cattr, currow, col, clcnt, bbv);
                    col += cattr.ColSpan;
                }

                if (viewele != null)
                {
                    var cpattr = cattr as CompositeAttribute;
                    if (cpattr != null && cpattr.ExtraWidth > 0 && !string.IsNullOrEmpty(cpattr.ExtraValue))
                        _extralist.Add((cpattr, viewele));
                }
            }

            RenderExtraViewControl(proplist, bbv);

            return row + 1;
        }

        EvangElement? AddToGrid(Grid? grid, PropertyInfo prop, PropertyInfo? hideprop, ControlAttribute cattr, int row, int col, int colcount, BindableBrokerView bbv)
        {
            if (grid == null)
                return null;

            var ele = CreateViewElement(prop, hideprop, bbv);
            grid.AddWithSpan(CreateViewControl(prop, cattr, ref ele, bbv), row, col, cattr.RowSpan, cattr.ColSpan < 1 ? colcount : cattr.ColSpan);
            return ele;
        }
        #endregion

        #region overridable
        public virtual void BeforeRendering(Layout layout, BindableBrokerView? bv) { }

        public virtual void AfterRendering(Layout layout, BindableBrokerView? bv) { }

        public virtual void BeforeTemplateRendering() { }

        public virtual void AfterTemplateRendering() { }

        public abstract Grid? LayoutGrid { get; set; }
        #endregion
    }
}
