using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangCustom;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Converter;
using System.Collections.ObjectModel;
using System.Reflection;
using EvangSol.Mobibrary.EvangComposite;

namespace EvangSol.Mobibrary.EvangWidget;

#region EvangDataTable
public class EvangDataTable : EvangContentView
{
    protected class ColHeaders : EvangView.EvangView
    {
        #region columns
        public string col0 { get; set; } = string.Empty;
        public string col1 { get; set; } = string.Empty;
        public string col2 { get; set; } = string.Empty;
        public string col3 { get; set; } = string.Empty;
        public string col4 { get; set; } = string.Empty;
        public string col5 { get; set; } = string.Empty;
        public string col6 { get; set; } = string.Empty;
        public string col7 { get; set; } = string.Empty;
        public string col8 { get; set; } = string.Empty;
        public string col9 { get; set; } = string.Empty;
        public string col10 { get; set; } = string.Empty;
        public string col11 { get; set; } = string.Empty;
        public string col12 { get; set; } = string.Empty;
        public string col13 { get; set; } = string.Empty;
        public string col14 { get; set; } = string.Empty;
        public string col15 { get; set; } = string.Empty;
        public string col16 { get; set; } = string.Empty;
        public string col17 { get; set; } = string.Empty;
        public string col18 { get; set; } = string.Empty;
        public string col19 { get; set; } = string.Empty;
        #endregion

        #region Sort properties
        bool? _sort0;
        public bool? Sort0
        {
            get => _sort0;
            set
            {
                if (_sort0 != value)
                {
                    _sort0 = value;
                    OnPropertyChanged("Sort0");
                }
            }
        }

        bool? _sort1;
        public bool? Sort1
        {
            get => _sort1;
            set
            {
                if (_sort1 != value)
                {
                    _sort1 = value;
                    OnPropertyChanged("Sort1");
                }
            }
        }

        bool? _sort2;
        public bool? Sort2
        {
            get => _sort2;
            set
            {
                if (_sort2 != value)
                {
                    _sort2 = value;
                    OnPropertyChanged("Sort2");
                }
            }
        }

        bool? _sort3;
        public bool? Sort3
        {
            get => _sort3;
            set
            {
                if (_sort3 != value)
                {
                    _sort3 = value;
                    OnPropertyChanged("Sort3");
                }
            }
        }

        bool? _sort4;
        public bool? Sort4
        {
            get => _sort4;
            set
            {
                if (_sort4 != value)
                {
                    _sort4 = value;
                    OnPropertyChanged("Sort4");
                }
            }
        }

        bool? _sort5;
        public bool? Sort5
        {
            get => _sort5;
            set
            {
                if (_sort5 != value)
                {
                    _sort5 = value;
                    OnPropertyChanged("Sort5");
                }
            }
        }

        bool? _sort6;
        public bool? Sort6
        {
            get => _sort6;
            set
            {
                if (_sort6 != value)
                {
                    _sort6 = value;
                    OnPropertyChanged("Sort6");
                }
            }
        }

        bool? _sort7;
        public bool? Sort7
        {
            get => _sort7;
            set
            {
                if (_sort7 != value)
                {
                    _sort7 = value;
                    OnPropertyChanged("Sort7");
                }
            }
        }

        bool? _sort8;
        public bool? Sort8
        {
            get => _sort8;
            set
            {
                if (_sort8 != value)
                {
                    _sort8 = value;
                    OnPropertyChanged("Sort8");
                }
            }
        }

        bool? _sort9;
        public bool? Sort9
        {
            get => _sort9;
            set
            {
                if (_sort9 != value)
                {
                    _sort9 = value;
                    OnPropertyChanged("Sort9");
                }
            }
        }

        bool? _sort10;
        public bool? Sort10
        {
            get => _sort10;
            set
            {
                if (_sort10 != value)
                {
                    _sort10 = value;
                    OnPropertyChanged("Sort10");
                }
            }
        }

        bool? _sort11;
        public bool? Sort11
        {
            get => _sort11;
            set
            {
                if (_sort11 != value)
                {
                    _sort11 = value;
                    OnPropertyChanged("Sort11");
                }
            }
        }

        bool? _sort12;
        public bool? Sort12
        {
            get => _sort12;
            set
            {
                if (_sort12 != value)
                {
                    _sort12 = value;
                    OnPropertyChanged("Sort12");
                }
            }
        }

        bool? _sort13;
        public bool? Sort13
        {
            get => _sort13;
            set
            {
                if (_sort13 != value)
                {
                    _sort13 = value;
                    OnPropertyChanged("Sort13");
                }
            }
        }

        bool? _sort14;
        public bool? Sort14
        {
            get => _sort14;
            set
            {
                if (_sort14 != value)
                {
                    _sort14 = value;
                    OnPropertyChanged("Sort14");
                }
            }
        }

        bool? _sort15;
        public bool? Sort15
        {
            get => _sort15;
            set
            {
                if (_sort15 != value)
                {
                    _sort15 = value;
                    OnPropertyChanged("Sort15");
                }
            }
        }

        bool? _sort16;
        public bool? Sort16
        {
            get => _sort16;
            set
            {
                if (_sort16 != value)
                {
                    _sort16 = value;
                    OnPropertyChanged("Sort16");
                }
            }
        }

        bool? _sort17;
        public bool? Sort17
        {
            get => _sort17;
            set
            {
                if (_sort17 != value)
                {
                    _sort17 = value;
                    OnPropertyChanged("Sort17");
                }
            }
        }

        bool? _sort18;
        public bool? Sort18
        {
            get => _sort18;
            set
            {
                if (_sort18 != value)
                {
                    _sort18 = value;
                    OnPropertyChanged("Sort18");
                }
            }
        }

        bool? _sort19;
        public bool? Sort19
        {
            get => _sort19;
            set
            {
                if (_sort19 != value)
                {
                    _sort19 = value;
                    OnPropertyChanged("Sort19");
                }
            }
        }
        #endregion
    }

    public class TableFeature
    {
        public double? TextSize { get; set; }
        public Color? BackgroundColor { get; set; }
        public Color? TextColor { get; set; }
        public FontAttributes? FontAttributes { get; set; }
        public TextAlignment? Alignment { get; set; }
    }

    public class HeaderFeature : TableFeature
    {
        public double? Height { get; set; }
    }

    public class BodyFeature : TableFeature
    {
        public double? Height { get; set; }
        public bool FixHeight { get; set; }
        public bool EnableLongPress { get; set; }
    }
}
#endregion

public class EvangDataTable<T> : EvangDataTable where T : TableEntryView, new()
{
    #region class members
    public delegate void OnTableRowRendering(EvangDataTable grid, int row, StackLayout layout, T em);

    #region events
    public class TableRowTapEventArgs : EventArgs
    {
        public TableEntryView? entry { get; set; }
        public int position { get; set; }
    }
    public event EventHandler<TableRowTapEventArgs>? OnTableRowTap;

    public class TableSelectEventArgs : EventArgs
    {
        public bool IsAllSelected { get; set; } //if CheckAll is tapped only this property is set
        public bool IsSelected { get; set; }    //it's set when a row is selected
        public T? em { get; set; }             //it's set when a row is selected
    }
    public event EventHandler<TableSelectEventArgs>? OnTableSelectChanged;

    //grid row long press event
    public class TableLongPressEventArgs : EventArgs
    {
        public T? em { get; set; }
    }
    public event EventHandler<TableLongPressEventArgs>? OnTableRowLongPress;

    public event EventHandler? OnHeadLoaded;    //header rendering finish event
    public event EventHandler? OnRowsLoaded;    //rows rendering finish event
    #endregion

    #region properties
    public Func<(T?, int)>? GetEntryModel { get; set; }

    public TableSwipeRow? CurrentSwipeView { get; set; }

    public TableEntryView? CurrentEntry { get; set; }

    public int CurrentRowIndex { get; set; }

    public CheckBox? CheckAll { get; set; }

    ColHeaders colHeaders { get; set; } = new ColHeaders(); //actual grid header binding class

    public Dictionary<string, string> VisMap { get; set; } = new();

    #endregion

    #region variables
    public CollectionView tbhead;   //table header part
    public CollectionView tbbody;   //table body part
    string prevsort = string.Empty; //keep previous sorting column's binding Sort property
    T? tableentry = new T();       //create an empty entry model to do initiation
    int linecount = 1;              //for count up and keep line count for each row
    ISwipeActions? iswipacts;       //for swipe menu of each row
    List<(PropertyInfo, int, ColumnAttribute)> colprops;    //column properties

    //variables for grid row long press
    bool enablelongpress;
    bool rowlongpressed;

    //variables for column show/hide
    List<ColumnDefinition> headcolumns = new();         //header column definitions
    List<List<ColumnDefinition>> rowcolumnlist = new(); //row column definition list
    int totalrows = 0;              //the count of rendering rows
    int rowcounter = 0;             //count how many rows have been rendered
    #endregion
    #endregion

    #region constructor
    public EvangDataTable(
        List<(string, PropertyInfo, PropertyInfo?, ColumnAttribute)>? entryproplist,
        HeaderFeature? hdprop = null,
        BodyFeature? rwprop = null,
        OnTableRowRendering? onrowrender = null,
        ISwipeActions? iswipacts = null)
    {
        this.iswipacts = iswipacts;
        enablelongpress = rwprop?.EnableLongPress ?? false;

        colprops = new();
        int i = 0;
        if (entryproplist == null)
            entryproplist = ClassMapping.GetPropertyList<ColumnAttribute>(typeof(T));
        foreach (var (_, prop, _, dcattr) in entryproplist)
        {
            if (i > 19) throw new Exception("Can not be over 20 columns.");
            //escape @SubViewElement
            if (prop.PropertyType.Name == "SubViewElement`1")
                continue;
            //count up row count
            if (dcattr.ForeFront)
                linecount++;
            //add to column info list
            colprops.Add((prop, i, dcattr));
            //copy header caption to binding col property
            var pn = $"col{i}";
            typeof(ColHeaders).GetProperty(pn)?.SetValue(colHeaders, string.IsNullOrEmpty(dcattr.HeadText) ? string.Empty : GetCustomString(dcattr.HeadText) ?? prop.Name); //SIR0189618
            i++;
        }
        tableentry.InitPropertyViewModel(null, entryproplist);

        foreach (var (prop, k, _) in colprops)
            VisMap.Add(prop.Name, $"Vis{k}");


        #region grid header
        //create grid header
        var hdheight = CommonViewSetting.TABLE_HEADERHEIGHT;
        if (hdprop != null)
        {
            if (hdprop.Height > 0)
                hdheight = hdprop.Height.Value;
        }
        var bgcolor = hdprop?.BackgroundColor ?? GetColor("Primary");
        tbhead = new CollectionView()
        {
            HeightRequest = hdheight * linecount,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            BackgroundColor = bgcolor,

            ItemTemplate = new DataTemplate(() =>
            {
                List<(Grid, int)> cellist;  //to keep each field's grid and column position, especially for multi-line grid
                Grid grid;
                (grid, headcolumns) = CreateLayoutGrid(colprops, hdheight, out cellist);
                rowcolumnlist.Clear();

                var textcolor = hdprop?.TextColor ?? GetColor("White");
                double? fontsize = CommonViewSetting.HEADER_FONTSIZE;
                if (hdprop != null && hdprop.TextSize > 0)
                    fontsize = hdprop.TextSize;

                double? defFontsize = fontsize;
                //loop through column infos to create each header cell
                foreach (var (prop, i, attr) in colprops)
                {
                    //if HeadText is "CheckAll", make this column selection column and add checkall checkbox to header
                    var (g, c) = cellist[i];
                    if (attr.HeadText == "CheckAll")
                    {
                        CheckAll = new CheckBox
                        {
#if WINDOWS
                            Color = GetColor("Secondary"),
                            Margin = new Thickness(10, 3, 0, 0),
#else
                            Color = textcolor,
#endif
                            BackgroundColor = bgcolor,
                            Scale = 1.5,
                            IsEnabled = !attr.ShowOnly,
                        };
                        CheckAll.CheckedChanged += OnCheckAllCheckedChanged;
#if WINDOWS
                        g.Add(new StackLayout
                        {
                            BackgroundColor = bgcolor,
                            Children =
                            {
                                CheckAll
                            }
                        }, c);
#else
                        g.Add(CheckAll, c);
#endif
                        continue;
                    }

                    fontsize = defFontsize;
                    if (attr.HeadTextSize > 0)
                        fontsize = attr.HeadTextSize;
                    var label = new Label
                    {
                        FontSize = fontsize.Value,
                        BackgroundColor = bgcolor,
                        TextColor = textcolor,
                        FontAttributes = hdprop?.FontAttributes ?? FontAttributes.Bold,
                        HorizontalTextAlignment = hdprop?.Alignment ?? GetAlignment(attr.ColAlignment) ?? TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                    //bind to col property
                    var pn = $"col{i}";
                    label.SetBinding(Label.TextProperty, pn);
                    //add tap gesture recognizer
                    var tapgr = new TapGestureRecognizer();
                    tapgr.Tapped += OnGridHeadTapped;
                    label.GestureRecognizers.Add(tapgr);

                    //if no sorting defined
                    if (string.IsNullOrEmpty(attr.Sorting))
                    {
                        g.Add(label, c);
                    }
                    //has sorting
                    else
                    {
                        //two column grid, one for sorting icon, one for header cell
                        var gd = new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                            },
                        };
                        //add ascend arrow icon
                        var st = $"Sort{i}";
                        var up = new Image
                        {
                            Source = ImageSource.FromResource("EvangSol.Mobibrary.Resources.Images.arrow_up.png"),
                            IsVisible = false,
                            Aspect = Aspect.AspectFit,
                            BackgroundColor = bgcolor,
                            HeightRequest = hdheight
                        };
                        //bind to Sort property
                        up.SetBinding(IsVisibleProperty,
                                    new Binding(st,
                                    converter: new IsEqualConverter(),
                                    converterParameter: true));
                        //add descend arrow icon
                        var down = new Image
                        {
                            Source = ImageSource.FromResource("EvangSol.Mobibrary.Resources.Images.arrow_down.png"),
                            IsVisible = false,
                            Aspect = Aspect.AspectFit,
                            BackgroundColor = bgcolor,
                            HeightRequest = hdheight
                        };
                        //bind to Sort property
                        down.SetBinding(IsVisibleProperty,
                                    new Binding(st,
                                    converter: new IsEqualConverter(),
                                    converterParameter: false));
                        //StackLayout to hold both icons
                        var stack = new StackLayout
                        {
                            up,
                            down,
                        };
                        gd.Add(stack);
                        //give an id to the header cell and insert to the second column
                        label.ClassId = $"{i}";
                        gd.Add(label, 1);
                        //insert the whole header cell grid
                        g.Add(gd, c);
                    }
                }
                SetTimer(100, () => OnHeadLoaded?.Invoke(this, EventArgs.Empty));
                return grid;
            }),

            ItemsSource = new List<ColHeaders> { colHeaders },  //set the header binding class to collection view's item source
        };
        #endregion

        #region grid rows
        //create grid body
        tbbody = new CollectionView
        {
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Start,
            SelectionMode = SelectionMode.Single,

            ItemTemplate = new DataTemplate(() =>
            {
                List<(Grid, int)> cellist;    //to keep each field's grid and column position, especially for multi-line grid
                var (grid, coldeflist) = CreateLayoutGrid(colprops, rwprop != null && rwprop.FixHeight ? rwprop.Height ?? CommonViewSetting.TABLE_ROWHEIGHT : null, out cellist);
                rowcolumnlist.Add(coldeflist);

                //for swipe menus
                var hasswipeitems = false;
                List<SwipeItem>? leftswipeitems = null;     //left swipe menus
                List<SwipeItem>? rightswipeitems = null;    //right swipe menus
#if ANDROID
                TableSwipeRow? swipeview = null;
#elif WINDOWS
                //on windows desktop use context menu for SwipeView not works with mouse
                PopupMenu? popupmenu = null;
#endif
                if (iswipacts is not null)
                {
                    leftswipeitems = iswipacts.GetLeftSwipeItemList();
                    rightswipeitems = iswipacts.GetRightSwipeItemList();
                    hasswipeitems = leftswipeitems != null && leftswipeitems.Count > 0 || rightswipeitems != null && rightswipeitems.Count > 0;
                    if (hasswipeitems)
                    {
#if ANDROID
                        swipeview = new TableSwipeRow();
                        swipeview.SwipeViewList = iswipacts.GetSwipeViewList();
                        iswipacts.GetSwipeViewList().Add(swipeview);
#elif WINDOWS
                        popupmenu = new PopupMenu(new PopupMenu.MenuItemSetting {
                            Height = 50,
                            Width = 200,
                            TextSize = (int)CommonViewSetting.INPUT_FONTSIZE,
                        });
                        popupmenu.MenuItems = new List<KeyValuePair<string, PopupMenu.MenuItemSetting?>>();
                        popupmenu.OnMenuTapped += iswipacts.OnContextMenuTapped;
#endif
                    }
                }
                
                //for swipe menu, hold entry model and row index
                int rowindex = -1;
                T? em = null;
                if (GetEntryModel != null)
                {
                    (em, rowindex) = GetEntryModel();
#if ANDROID
                    if (swipeview != null)
                    {
                        swipeview.RowIndex = rowindex;
                        swipeview.EntryModel = em;
                    }
#elif WINDOWS
                    if (popupmenu != null)
                    {
                        if (leftswipeitems != null)
                        {
                            foreach (var item in leftswipeitems)
                                popupmenu.MenuItems.Add(new KeyValuePair<string, PopupMenu.MenuItemSetting?>(item.Text, new PopupMenu.MenuItemSetting { Data = em }));
                        }
                        if (rightswipeitems != null)
                        {
                            foreach (var item in rightswipeitems)
                                popupmenu.MenuItems.Add(new KeyValuePair<string, PopupMenu.MenuItemSetting?>(item.Text, new PopupMenu.MenuItemSetting { Data = em }));
                        }
                    }
#endif
                }

                //Create and configure TouchBehavior for long press recognization
                TouchBehavior? touchBehavior = null;
                if (enablelongpress)
                {
                    touchBehavior = new TouchBehavior
                    {
                        LongPressDuration = CommonViewSetting.LONGPRESS_DURATION,
                        ShouldMakeChildrenInputTransparent = true
                    };
                    touchBehavior.LongPressCompleted += (s, e) =>
                    {
                        rowlongpressed = true;
                        tbbody!.SelectedItem = null;    //clear current selection make it selectable again
                        if (s is Label label && label.BindingContext is T em)
                            OnTableRowLongPress?.Invoke(s, new TableLongPressEventArgs { em = em });
                    };
                    touchBehavior.CurrentTouchStatusChanged += (s, e) =>
                    {
                        if (e.Status == TouchStatus.Canceled)
                            rowlongpressed = false;
                    };
                    touchBehavior.TouchGestureCompleted += (s, e) =>
                    {
                        if (rowlongpressed)
                        {
                            rowlongpressed = false;
                            return;
                        }
                        if (s is Label label && label.BindingContext is T em)
                            DoGridSelectionChanged(em);
                    };
                }

                var controlist = new List<(PropertyInfo, View)>();
                //go through each column's info to create cell controls
                foreach (var (prop, i, attr) in colprops)
                {
                    var viewtype = typeof(Label);
                    //get control class type
                    foreach (var tp in prop.PropertyType.GetGenericArguments())
                        viewtype = tp;

                    double? fontsize = CommonViewSetting.INPUT_FONTSIZE;
                    if (rwprop != null && rwprop.TextSize > 0)
                        fontsize = rwprop.TextSize;

                    if (attr.propTextSize > 0)
                        fontsize = attr.propTextSize;

                    var (g, c) = cellist[i];
                    var propname = tableentry!.GetBaseProperty(prop.Name);   //get binding Prop property
                    View? control = null;
                    //according to control type name to create actual control respectively
                    switch (viewtype.Name)
                    {
                        case "Label":
                            var label = new Label
                            {
                                FontSize = fontsize.Value,
                                BackgroundColor = rwprop?.BackgroundColor ?? GetColor(attr.propBackgroundColor) ?? Colors.Transparent,
                                TextColor = rwprop?.TextColor ?? GetColor(attr.propTextColor) ?? GetColor("Gray900"),
                                FontAttributes = rwprop?.FontAttributes ?? GetFontAttr(attr.propFontAttributes) ?? FontAttributes.None,
                                HorizontalTextAlignment = rwprop?.Alignment ?? GetAlignment(attr.ColAlignment) ?? TextAlignment.Center,
                                VerticalTextAlignment = TextAlignment.Center,
                            };
                            //do data binding
                            if (!string.IsNullOrWhiteSpace(attr.FormatUnit) && em is not null)
                            {
                                //if FormatUnit is defined, add FormatDecimalDataGridConverter with FormatUnit
                                var binding = new Binding(propname, BindingMode.TwoWay, new FormatTableDecimalConverter(em, attr.FormatUnit, null));
                                label.SetBinding(Label.TextProperty, binding);
                            }
                            else if (!string.IsNullOrEmpty(attr.FormatString) && em is not null)
                            {
                                //if FormatString is defined, add FormatDecimalDataGridConverter with FormatString
                                var binding = new Binding(propname, BindingMode.TwoWay, new FormatTableDecimalConverter(em, null, attr.FormatString));
                                label.SetBinding(Label.TextProperty, binding);
                            }
                            else if (!string.IsNullOrEmpty(attr.FormatDateTime))
                            {
                                //if FormatDateTime is defined, add FormatDateTimeConverter with FormatString
                                var binding = new Binding(propname, BindingMode.TwoWay, new FormatDateTimeConverter(attr.FormatDateTime));
                                label.SetBinding(Label.TextProperty, binding);
                            }
                            else
                            {
                                label.SetBinding(Label.TextProperty, propname);
                            }

                            if (enablelongpress)
                                label.Behaviors.Add(touchBehavior);
                            g.Add(label, c);    //add to row grid
                            controlist.Add((prop, label));
                            control = label;
                            break;
                        //render checkbox
                        case "CheckBox":
                            var checkbox = new CheckBox
                            {
                                BackgroundColor = rwprop?.BackgroundColor ?? GetColor(attr.propBackgroundColor) ?? Colors.Transparent,
                                Scale = 1.3,
                                IsEnabled = !attr.ShowOnly,
#if WINDOWS
                                Margin = new Thickness(7, 0, 0, 0),
#endif
                            };
                            if (attr.HeadText == "CheckAll")
                            {
                                //if it's checkall column, add CheckedChanged event handler
                                checkbox.CheckedChanged += OnRowCheckCheckedChanged;
                            }
                            checkbox.SetBinding(CheckBox.IsCheckedProperty, propname, converter: new StringToBooleanConverter());
                            g.Add(checkbox, c);
                            controlist.Add((prop, checkbox));
                            control = checkbox;
                            break;
                        case "Image":
                            var image = new Image
                            {
                                Aspect = Aspect.AspectFit,
                                HeightRequest = attr.Height > 0 ? attr.Height : rwprop?.Height ?? CommonViewSetting.GRIDCELL_IMAGE_SIZE,
                                WidthRequest = rwprop?.Height ?? CommonViewSetting.GRIDCELL_IMAGE_SIZE,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                            };
                            image.SetBinding(Image.SourceProperty, propname, converter: new StringToImageSourceConverter());
                            g.Add(image, c);
                            controlist.Add((prop, image));
                            control = image;
                            break;
                        case "Button":
                            var button = new Button
                            {
                                FontSize = fontsize.Value,
                                BackgroundColor = rwprop?.BackgroundColor ?? GetColor(attr.propBackgroundColor) ?? GetColor("Primary"),
                                TextColor = rwprop?.TextColor ?? GetColor(attr.propTextColor) ?? GetColor("Gray900"),
                                FontAttributes = rwprop?.FontAttributes ?? GetFontAttr(attr.propFontAttributes) ?? FontAttributes.None,
                            };
                            button.SetBinding(Button.TextProperty, propname);
                            g.Add(button, c);
                            controlist.Add((prop, button));
                            control = button;
                            break;
                        case "ImageButton":
                            var imagebutton = new ImageButton
                            {
                                Aspect = Aspect.AspectFit,
                                HeightRequest = attr.Height > 0 ? attr.Height : rwprop?.Height ?? CommonViewSetting.GRIDCELL_IMAGE_SIZE,
                                WidthRequest = rwprop?.Height ?? CommonViewSetting.GRIDCELL_IMAGE_SIZE,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                            };
                            imagebutton.SetBinding(Image.SourceProperty, propname, converter: new StringToImageSourceConverter());
                            g.Add(imagebutton, c);
                            controlist.Add((prop, imagebutton));
                            control = imagebutton;
                            break;
                        #region Not supported as basis
                        //case "ClearEntry":
                        //    var entry = new ClearEntry(
                        //        required: attr.RequiredInput,
                        //        fontsize: fontsize.Value,
                        //        textcolor: GetColor(attr.propTextColor),
                        //        fontattr: GetFontAttr(attr.propFontAttributes),
                        //        alignment: GetAlignment(attr.ColAlignment));
                        //    entry.SetBinding(ClearEntry.TextProperty, propname);
                        //    g.Add(entry, c);
                        //    break;
                        //Not supported as basis
                        //case "Picker":
                        //    var picker = new Picker
                        //    {
                        //        FontSize = fontsize.Value,
                        //        BackgroundColor = rwprop?.BackgroundColor ?? GetColor(attr.propBackgroundColor) ?? Colors.Transparent,
                        //        TextColor = rwprop?.TextColor ?? GetColor(attr.propTextColor) ?? GetColor("Gray900"),
                        //        FontAttributes = rwprop?.FontAttributes ?? GetFontAttr(attr.propFontAttributes) ?? FontAttributes.None,
                        //        HorizontalTextAlignment = rwprop?.Alignment ?? GetAlignment(attr.ColAlignment) ?? TextAlignment.Center,
                        //        VerticalTextAlignment = TextAlignment.Center
                        //    };
                        //    picker.SetBinding(Picker.SelectedItemProperty, propname);
                        //    g.Add(picker, c);
                        //    break;
                        //case "Switch":
                        //    var _switch = new Switch
                        //    {
                        //        BackgroundColor = rwprop?.BackgroundColor ?? GetColor(attr.propBackgroundColor) ?? Colors.Transparent,
                        //    };
                        //    g.Add(_switch, c);
                        //    break;
                        //Not supported as basis
                        //case "DatePicker":
                        //    break;
                        //Not supported as basis
                        //case "TimePicker":
                        //    break;
                        #endregion
                        default:
                            break;
                    }

                    control.SetBinding(IsVisibleProperty, VisMap[prop.Name]);
                }

                //StackLayout to hold the row grid and a bottom line
                var stack = new StackLayout
                {
                    grid,
                    new BoxView
                    {
                        Color = GetColor("Gray300"),
                        HeightRequest = 2,
                        HorizontalOptions = LayoutOptions.Fill
                    },
#if WINDOWS
                    popupmenu,
#endif
                };

                stack.BindingContextChanged += (sender, e) =>
                {
                    if (sender is StackLayout layout && layout.BindingContext is T em)
                    {
                        //binding controls to EntryModel
                        controlist.ForEach(x =>
                        {
                            var bve = x.Item1.GetValue(em) as EvangElement;
                            if (bve != null)
                            {
                                bve.ElementObject = x.Item2;
                                bve.PropName = x.Item1.Name;
                                bve.ViewModel = em;
                            }
                        });

                        //call onrowrender
                        onrowrender?.Invoke(this, rowindex, stack, em);
                    }
                };

#if ANDROID
                //add swipe menues
                if (hasswipeitems && swipeview != null)
                {
                    swipeview.Content = stack;
                    if (leftswipeitems != null && leftswipeitems.Count > 0)
                    {
                        var items = new SwipeItems(leftswipeitems);
                        items.SwipeBehaviorOnInvoked = SwipeBehaviorOnInvoked.RemainOpen;
                        swipeview.LeftItems = items;
                    }
                    if (rightswipeitems != null && rightswipeitems.Count > 0)
                    {
                        var items = new SwipeItems(rightswipeitems);
                        items.SwipeBehaviorOnInvoked = SwipeBehaviorOnInvoked.RemainOpen;
                        swipeview.RightItems = items;
                    }
                    swipeview.SwipeEnded += OnSwipeEnded;
                    return swipeview;
                }
#elif WINDOWS
                if (hasswipeitems && popupmenu != null)
                {
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) =>
                    {
                        Point? pos = e.GetPosition(null);
                        if (pos != null)
                            popupmenu!.DoShowMenu((int)pos.Value.X, (int)pos.Value.Y);
                    };
                    stack.GestureRecognizers.Add(tapGesture);
                }
#endif

                if (totalrows > 0)
                {
                    rowcounter++;
                    if (rowcounter == totalrows)
                        SetTimer(100, () => OnRowsLoaded?.Invoke(this, EventArgs.Empty));
                }
                return stack;
            }),
        };
        tbbody.SelectionChanged += OnGridSelectionChanged;
        tbbody.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "ItemsSource")
            {
                totalrows = (tbbody.ItemsSource as ObservableCollection<T>)?.Count ?? 0;
                rowcounter = 0;
                if (CheckAll != null && CheckAll.IsChecked)
                {
                    CheckAll.CheckedChanged -= OnCheckAllCheckedChanged;
                    CheckAll.IsChecked = false;
                    CheckAll.CheckedChanged += OnCheckAllCheckedChanged;
                }
            }
        };
        #endregion

        var layout = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition() },
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(hdheight * linecount, GridUnitType.Absolute) },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
            }
        };
        layout.Add(tbhead);
        layout.Add(tbbody, row: 1);
        Content = layout;
    }
    #endregion

    #region check-all functions
    private void OnCheckAllCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        foreach (var item in tbbody.ItemsSource)
        {
            var te = item as T;
            if (te is not null)
            {
                var bcv = te.GetViewElement("CheckSelect");
                if (bcv != null && bcv.ElementObject is CheckBox checkbox)
                {
                    //to prevent event death loop, remove event handler before changing checked state, then add it back
                    checkbox.CheckedChanged -= OnRowCheckCheckedChanged;
                    bcv.SetValue(e.Value.ToString());
                    checkbox.CheckedChanged += OnRowCheckCheckedChanged;
                }
            }
        }
        OnTableSelectChanged?.Invoke(CheckAll, new TableSelectEventArgs { IsAllSelected = e.Value });
    }

    private void OnRowCheckCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (CheckAll == null)
            return;

        bool all = true;
        if (e.Value)
        {
            foreach (var item in tbbody.ItemsSource)
            {
                var te = item as T;
                if (te is not null)
                {
                    var bcv = te.GetViewElement("CheckSelect");
                    if (bcv != null && bcv.Value != "True")
                    {
                        all = false;
                        break;
                    }
                }
            }
        }
        else
        {
            all = false;
        }
        //to prevent event death loop, remove event handler before changing checked state, then add it back
        CheckAll.CheckedChanged -= OnCheckAllCheckedChanged;
        CheckAll.IsChecked = all;
        CheckAll.CheckedChanged += OnCheckAllCheckedChanged;

        if (sender is CheckBox cb && cb.BindingContext is T entrymodel)
            OnTableSelectChanged?.Invoke(sender, new TableSelectEventArgs { IsSelected = e.Value, em = entrymodel });
    }

    public List<T>? GetSelectedItems()
    {
        if (CheckAll == null)
            return null;

        var list = new List<T>();
        foreach (var item in tbbody.ItemsSource)
        {
            var te = item as T;
            if (te is not null)
            {
                var bcv = te.GetViewElement("CheckSelect");
                if (bcv != null && bcv.Value == "True")
                    list.Add(te);
            }
        }
        return list;
    }
    #endregion

    #region swiping
    //there is an issue that you can not open two swipe menus at the same time, or you'll get an error "the specified child already has a parent. you must call removeview() on the child's parent first."
    //this even happens on the demo project provided by MicroSoft. after some digging, no solution could be found. so I have to do a workaround on this issue.
    //the workaround is that when you open one swipe menu, you disable all the others, and when you close the swipe menu, you enable all the swipe menus.
    private void OnSwipeEnded(object? sender, SwipeEndedEventArgs e)
    {
        if (iswipacts is null)
            return;
        CurrentSwipeView = sender as TableSwipeRow;
        //unfortunately, there's no event on closing swipe menus. i have to use a timer periodically check swipeview's open status.
        //if it's closed, stop the timer and enable all the swipe menus
        SetInterval(200, () =>
        {
            if ((CurrentSwipeView as ISwipeView)!.IsOpen)
            {
                //disable all the other menus
                foreach (var sv in iswipacts.GetSwipeViewList())
                {
                    if (sv == sender)
                        continue;
                    sv.IsEnabled = false;
                }
                return true;    //keep going
            }
            else
            {
                //enable all
                foreach (var sv in iswipacts.GetSwipeViewList())
                    sv.IsEnabled = true;
            }
            return false;   //stop timer
        });
    }
    #endregion

    #region CreateLayoutGrid
    (Grid, List<ColumnDefinition>) CreateLayoutGrid(List<(PropertyInfo, int, ColumnAttribute)> colprops, double? height, out List<(Grid, int)> cellist)	//SIR0189094
    {
        List<ColumnDefinition> coldeflist = new();
        //create layout grid with linecount lines
        RowDefinitionCollection rows = new();
        for (int i = 0; i < linecount; i++)
            if (height == null)
                rows.Add(new RowDefinition ());
            else
                rows.Add(new RowDefinition { Height = height.Value });
        Grid grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition() },
            RowDefinitions = rows
        };

        Grid? rowgrid = null;
        int row = 0, col = 0;
        cellist = new();
        //each line is a grid with multiple columns
        foreach (var (prop, i, attr) in colprops)
        {
            //when ForeFront is true, create a new line grid
            if (rowgrid == null || attr.ForeFront)
            {
                rowgrid = new Grid
                {
                    RowDefinitions =
                    {
                        height == null ? new RowDefinition() : new RowDefinition { Height = height.Value }
                    }
                };
                //bind background color
                rowgrid.SetBinding(BackgroundColorProperty, "BackgroundColor");
                //add to outer grid
                grid.Add(rowgrid, row: row);
                row++;      //line index count up
                col = 0;    //reset column position
            }
            //add a new column definition
            var coldef = new ColumnDefinition
            {
                Width = new GridLength(attr.ColWidth > 0 ? attr.ColWidth : 1, GridUnitType.Star)
            };
            coldeflist.Add(coldef);
            rowgrid.ColumnDefinitions.Add(coldef);
            cellist.Add((rowgrid, col));
            col++;  //column position count up
        }
        return (grid, coldeflist);
    }
    #endregion

    #region sorting
    public delegate IEnumerable<T> CollectionSort(string propname, bool ascend);
    public CollectionSort? GridSort { private get; set; }   //grid sorting function which give back a sorted data collection

    void OnGridHeadTapped(object? sender, TappedEventArgs e)
    {
        Label label = (Label)sender!;
        if (label == null || string.IsNullOrEmpty(label.ClassId))
            return;

        var currsort = $"Sort{label.ClassId}";
        if (prevsort != currsort)
        {
            //set pervious Sort property to null
            SetPropertyValue<bool?>(colHeaders, prevsort, null);
            prevsort = currsort;
        }

        //revert current sorting
        bool val = GetPropertyValue<bool>(colHeaders, currsort);
        SetPropertyValue(colHeaders, currsort, !val);

        if (GridSort != null)
        {
            //sorting current column
            var propname = colprops[int.Parse(label.ClassId)].Item1.Name;
            tbbody.ItemsSource = GridSort.Invoke(propname, val);
            OnPropertyChanged("TableEntryList"); //refresh the datagrid
        }
    }
    #endregion

    #region OnGridSelectionChanged
    void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (enablelongpress)
            return;
        //if no row selected, return.
        if (e.CurrentSelection.Count < 1)
            return;
        DoGridSelectionChanged((e.CurrentSelection[0] as T)!);
    }

    void DoGridSelectionChanged(T em)
    {
        //find current selection's index
        var ind = tbbody.ItemsSource.Cast<T>().ToList().FindIndex(e => e.rowid == em.rowid);
        CurrentEntry = em;
        CurrentRowIndex = ind;
        CurrentSwipeView = null;
        //trigger row tap event
        OnTableRowTap?.Invoke(this, new TableRowTapEventArgs { entry = em, position = ind });
        //if SelectedItem has value, when you tap the same row again, CollectionView's SelectionChanged event is not gonna be triggered.
        //so set it to null directly after a tapping
        tbbody!.SelectedItem = null;
    }
    #endregion

    #region ToggleColumn
    public void ToggleColumn(string name, bool show)
    {
        foreach (var (prop, i, _) in colprops)
        {
            if (prop.Name == name)
                ToggleColumn(i, show);
        }
    }

    public void ToggleColumn(int colindex, bool show)
    {
        if (headcolumns.Count < 1 || colindex < 0 || colindex >= headcolumns.Count)
            return;

        GridLength length;
        if (show)
        {
            var (_, _, attr) = colprops[colindex];
            length = new GridLength(attr.ColWidth > 0 ? attr.ColWidth : 1, GridUnitType.Star);
        }
        else
        {
            length = new GridLength(0, GridUnitType.Absolute);
        }
        headcolumns[colindex].Width = length;

        foreach (var rowcoldef in rowcolumnlist)
            rowcoldef[colindex].Width = length;
    }
    #endregion
}

#region ISwipeActions
//interface for grid row swipe menu, if you want swipe menu, just add this interface to your page class and implement all the functions
public interface ISwipeActions
{
    //to get a GridRowSwipeView list for disable/enable all the rows
    public List<TableSwipeRow> GetSwipeViewList();

    //to get left swipe items, return null if you don't want them.
    public List<SwipeItem>? GetLeftSwipeItemList();

    //to get right swipe items, return null if you don't want them.
    public List<SwipeItem>? GetRightSwipeItemList();

    //on windows desktop SwipeView is not working with mouse, so use context menu instead.
    //this function is invoked when tap on menu item.
    public void OnContextMenuTapped(object? sender, MenuTappedEventArgs e);
}
#endregion

#region IGridSort
//interface for data table sorting
public interface ITableSort<T> where T : TableEntryView
{
    public IEnumerable<T> SortTable(string propname, bool ascend);
}
#endregion

#region default sorting class
//if you want your own, just define a new class which implements IGridSort<T> interface
public class DefaultTableSort<V, T> : ITableSort<T> where V : SingleTableView<T> where T : TableEntryView, new()
{
    V vw;
    List<(string, PropertyInfo, PropertyInfo?, ColumnAttribute)> entryproplist;

    public DefaultTableSort(V vw, List<(string, PropertyInfo, PropertyInfo?, ColumnAttribute)> entryproplist)
    {
        this.vw = vw;
        this.entryproplist = entryproplist;
    }

    public IEnumerable<T> SortTable(string propname, bool ascend)
    {
        if (vw.TableEntryList == null)
            throw new Exception("GridSort: No data list assigned.");

        var (_, propinfo, _, attr) = entryproplist.Find(x => x.Item1 == propname);
        if (propinfo == null)
            throw new Exception("GridSort: No such property.");

        var sorting = "Default";
        if (!string.IsNullOrEmpty(attr.Sorting))
            sorting = attr.Sorting;

        return DoSort(propinfo, sorting, ascend);
    }

    IEnumerable<T> DoSort(PropertyInfo propinfo, string sorting, bool ascend)
    {
        IEnumerable<T> list;
        //decimal value sorting
        if (sorting == "Number")
        {
            list = vw.TableEntryList!.OrderBy(x =>
            {
                var val = propinfo!.GetValue(x, null) as EvangElement;
                return double.Parse(val!.Value ?? "0");
            });
        }
        //decimal value with tailing unit sorting
        else if (sorting == "NumberWithUnit")
        {
            list = vw.TableEntryList!.OrderBy(x =>
            {
                var val = propinfo!.GetValue(x, null) as EvangElement;
                var str = val!.Value ?? "0";
                int i = 0;
                for (; i < str.Length; i++)
                {
                    if (!"-1234567890.,".Contains(str[i]))
                        break;
                }
                if (i == 0)
                    return 0;
                return double.Parse(str.Substring(0, i));
            });
        }
        //default sorting, sort by string
        else
        {
            list = vw.TableEntryList!.OrderBy(x =>
            {
                var val = propinfo!.GetValue(x, null) as EvangElement;
                return val!.Value ?? string.Empty;
            });
        };
        return ascend ? list.Reverse() : list;
    }
}
#endregion
