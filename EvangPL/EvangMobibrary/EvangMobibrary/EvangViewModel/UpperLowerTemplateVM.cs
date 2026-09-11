using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangWidget;
using EvangSol.Mobibrary.Utilities.Common;
using System.Reflection;

namespace EvangSol.Mobibrary.EvangViewModel
{
    public class UpperLowerTemplateVM<V, T> : EvangTemplateVM<V> where V : SingleTableView<T>, new() where T : TableEntryView, new()
    {
        public override Grid? LayoutGrid { get; set; }
        public List<(string, PropertyInfo, PropertyInfo?, ColumnAttribute)>? entryproplist;  //entry property list

        public Grid? TopGrid { get; set; }
        public ScrollView? TopScroll { get; set; }
        public EvangDataTable<T>? DataGrid { get; set; }

        protected LayoutPattern toplayout;
        int eleminwidth;
        int colcount;
        int topheight;
        int rightwidth;
        int bottomheight;
        bool enablegridrowlongpress;

        public UpperLowerTemplateVM(
            string caption,             //page caption
            LayoutPattern toplayout,    //top part layout pattern
            int eleminwidth,            //minimal width of view elements in top area
            int colcount = 1,           //top area columns count, when colcount=0, it's calculated based on eleminwidth
            int topheight = 0,          //top area height, when colcount=0 and topheight=0, this height is calculated dynamically
            int rightwidth = 0,         //when colcount=2, the right pane's width, 0 means equal width
            int bottomheight = 0,       //bottom area height, for pages with bottom buttons
            bool enablegridrowlongpress = false //enable grid row long press
            ) : base(caption)
        {
            this.toplayout = toplayout;
            this.eleminwidth = eleminwidth;
            this.colcount = colcount;
            this.topheight = topheight;
            this.rightwidth = rightwidth;
            this.bottomheight = bottomheight;
            this.enablegridrowlongpress = enablegridrowlongpress;

            try
            {
                vw = (V)ClassMapping.CreateViewModelInstance(typeof(V));
                entryproplist = ClassMapping.GetPropertyList<ColumnAttribute>(typeof(T));
                vw.TemplateTable = new DataEntries<T>();

                RenderTemplate();
            }
            catch (Exception ex)
            {
                ShowException(ex.Message + Environment.NewLine + ex.InnerException?.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private Page? GetCurrentPage()
        {
            if (Application.Current?.Windows == null) return null;

            var window = Application.Current.Windows.FirstOrDefault();
            return window?.Page ?? window?.Navigation?.ModalStack.LastOrDefault()
                   ?? window?.Navigation?.NavigationStack.LastOrDefault();
        }

        public void RenderTemplate()
        {
            var hasdatagrid = typeof(T) != typeof(TableEntryView);
            var displayheight = DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density;
            var activePage = GetCurrentPage();
            if (activePage != null)
                displayheight = activePage.Height;

            BeforeTemplateRendering();

            double autoheight = 0;
            switch(toplayout)
            {
                case LayoutPattern.FlexGrid:
                    (TopGrid, vmproplist) = RenderGrid<V>(eleminwidth, colcount, rightwidth, ref autoheight, vw);
                    break;
                case LayoutPattern.MultiColumn:
                    (TopGrid, vmproplist) = RenderMultiColumn<V>(colcount, rightwidth, vw, ref autoheight);
                    break;
                case LayoutPattern.SingleColumn:
                    (TopScroll, vmproplist) = RenderSingleColumn<V>(vw, ref autoheight);
                    break;
            }
            if (topheight < 1)
                topheight = (int)autoheight;

            LayoutGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = hasdatagrid ? topheight : displayheight - bottomheight },
                    new RowDefinition { Height = hasdatagrid ? displayheight - topheight - bottomheight : 0 },
                    new RowDefinition { Height = bottomheight },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                },
            };
            if (TopGrid != null)
                LayoutGrid.Add(TopGrid);
            else if (TopScroll != null)
                LayoutGrid.Add(TopScroll);

            if (hasdatagrid)
            {
                CreateDataGrid(LayoutGrid, colcount);
                DataGrid!.GetEntryModel = () =>
                {
                    if (vw.TableEntryList == null)
                        return (null, -1);
                    //while sorting the third time, TableCounter gonna exceed TableEntryList's length.
                    //the reason is unknown, do a workaround that if the counter exceeded, reset it to zero.
                    if (vw.TableCounter >= vw.TableEntryList.Count)
                        vw.TableCounter = 0;
                    var te = vw.TableEntryList[vw.TableCounter];
                    var row = vw.TableCounter;
                    vw.TableCounter++;
                    return (te, row);
                };
            }

            if (bottomheight > 0)
            {
                LayoutGrid.Add(RenderBottomBar(), 0, 2);
            }

            AfterTemplateRendering();

            Content = LayoutGrid;
        }

        void CreateDataGrid(Grid grid, int colcount)
        {
            DataGrid = new EvangDataTable<T>(
                entryproplist,
                new EvangDataTable.HeaderFeature
                {
                    Height = (pageattr != null && pageattr.DataGridHeadHeight > 0) ? pageattr.DataGridHeadHeight : null,
                    TextSize = (pageattr != null && pageattr.DataGridHeadTextSize > 0) ? pageattr.DataGridHeadTextSize : CommonViewSetting.HEADER_FONTSIZE
                },
                new EvangDataTable.BodyFeature
                {
                    Height = (pageattr != null && pageattr.DataGridRowHeight > 0) ? pageattr.DataGridRowHeight : null,
                    FixHeight = pageattr != null ? pageattr.DataGridRowFixHeight : DeviceInfo.Idiom != DeviceIdiom.Phone,
                    TextSize = (pageattr != null && pageattr.DataGridRowTextSize > 0) ? pageattr.DataGridRowTextSize : CommonViewSetting.INPUT_FONTSIZE,
                    EnableLongPress = enablegridrowlongpress
                },
                OnTableRowRendering, this is ISwipeActions ? this as ISwipeActions : null);
            DataGrid.tbbody.SetBinding(ItemsView.ItemsSourceProperty, "TableEntryList");
            DataGrid.tbbody.SetBinding(SelectableItemsView.SelectedItemProperty, "SelectedEntry");
            DataGrid.VerticalOptions = LayoutOptions.Fill;
            DataGrid.GridSort = new DefaultTableSort<V, T>(vw, entryproplist!).SortTable;
            Grid.SetRow(DataGrid, 1);
            Grid.SetColumnSpan(DataGrid, colcount);
            grid.Add(DataGrid);

            //DataGrid.cvbody.Scrolled += OnDataGridScrolled;
        }

        public virtual void OnTableRowRendering(EvangDataTable grid, int row, StackLayout layout, T te)
        {
        }

        //private void OnDataGridScrolled(object? sender, ItemsViewScrolledEventArgs e)
        //{
        //    if (Math.Abs(e.VerticalDelta) < 10)
        //        return;
        //    grid!.RowDefinitions[0].Height = new GridLength(Math.Max(grid.RowDefinitions[0].Height.Value - e.VerticalDelta, 0));
        //    grid.RowDefinitions[1].Height = new GridLength(grid.RowDefinitions[1].Height.Value + e.VerticalDelta);
        //}

        public virtual Layout RenderBottomBar()
        {
            throw new NotImplementedException();
        }
    }
}
