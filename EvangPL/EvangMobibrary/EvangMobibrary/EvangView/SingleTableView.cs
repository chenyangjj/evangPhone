using EvangSol.Mobibrary.EvangCustom;
using System.Collections.ObjectModel;

namespace EvangSol.Mobibrary.EvangView
{
    public class SingleTableView<T> : BindableBrokerView where T : TableEntryView, new()
    {
        DataEntries<T>? _templatetable;
        public DataEntries<T>? TemplateTable
        {
            get => _templatetable;
            set
            {
                _templatetable = value;
                TableEntryList = value?.Entries;
            }
        }

        public ObservableCollection<T>? TableEntryList
        {
            get => TemplateTable?.Entries;
            set
            {
                if (TemplateTable is null) return;
                TemplateTable.Entries = value ?? new ObservableCollection<T>();
                if (TableElement != null && TableElement.DataGrid != null)
                {
                    foreach (var entry in TemplateTable.Entries)
                        entry.VisMap = TableElement.DataGrid.VisMap;
                }
                TableCounter = 0;
                SwipeViewList.Clear();
                OnPropertyChanged("TableEntryList");
            }
        }

        T _selectedentry = null!;
        public T SelectedEntry
        {
            get => _selectedentry;
            set
            {
                if (_selectedentry != value)
                    _selectedentry = value;
            }
        }

        //ObservableCollection<object> _multiselectedentries = null!;
        //public ObservableCollection<object> MultiSelectedEntries
        //{
        //    get => _multiselectedentries;
        //    set
        //    {
        //        if (_multiselectedentries != value)
        //            _multiselectedentries = value;
        //    }
        //}

        public DataGridViewElement<T>? TableElement { get; set; }

        public int TableCounter { get; set; }
        public List<TableSwipeRow> SwipeViewList { get; set; } = new();

        //gone - if true, the whole column is not holding any place.
        public void SetTableColumnVisibility(string name, bool visible, bool gone = false)
        {
            if (TableEntryList == null)
                return;
            foreach (var item in TableEntryList)
                item.SetCellVisibility(name, visible);

            if (TableElement != null && TableElement.DataGrid != null)
            {
                if (name == "CheckSelect" && TableElement.DataGrid.CheckAll != null)
                    TableElement.DataGrid.CheckAll.IsVisible = visible;
                if (gone)
                    TableElement.DataGrid.ToggleColumn(name, visible);
            }
        }
    }
}
