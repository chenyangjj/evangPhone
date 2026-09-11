using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangWidget;
using EvangSol.Mobibrary.Utilities.Common;
using System.Reflection;

namespace EvangSol.Mobibrary.EvangViewModel
{
    public class ScrollableTableTemplateVM<V, T> : ScrollableTemplateVM<V> where V : SingleTableView<T>, new() where T : TableEntryView, new()
    {
        public StackLayout? StackLayout;
        public EvangDataTable<T>? DataGrid { get; set; }

        public List<(string, PropertyInfo, PropertyInfo?, ColumnAttribute)>? entryproplist;  //entry property list
        bool enablegridrowlongpress;

        public ScrollableTableTemplateVM(
            string caption,         //page caption
            int topheight = 0,      //top pane height
            int bottomheight = 0,   //bottom button pane height
            bool enablegridrowlongpress = false //enable grid row long press
            ) : base(caption, topheight, bottomheight)
        {
            this.enablegridrowlongpress = enablegridrowlongpress;
        }

        public override void CreateViewModel()
        {
            vw = (V)ClassMapping.CreateViewModelInstance(typeof(V));
            entryproplist = ClassMapping.GetPropertyList<ColumnAttribute>(typeof(T));
            vw.TemplateTable = new DataEntries<T>();
        }

        public override void AfterRendering(Layout layout, BindableBrokerView? em)
        {
            StackLayout = layout as StackLayout;
        }

        protected override void AdditionalRendering()
        {
            CreateDataGrid();
            DataGrid!.GetEntryModel = () =>
            {
                if (vw.TableEntryList == null)
                    return (null, -1);
                //while sorting the third time, TableCounter gonna exceed TableEntryList's length.
                //the reason is unknown, do a workaround that if the counter exceeded, reset it to zero.
                if (vw.TableCounter >= vw.TableEntryList.Count)
                    vw.TableCounter = 0;
                var em = vw.TableEntryList[vw.TableCounter];
                var row = vw.TableCounter;
                vw.TableCounter++;
                return (em, row);
            };
        }

        void CreateDataGrid()
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
            StackLayout!.Add(DataGrid);
        }

        public virtual void OnTableRowRendering(EvangDataTable grid, int row, StackLayout layout, T em)
        {
        }
    }
}
