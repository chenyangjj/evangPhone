using System.Collections.ObjectModel;

namespace EvangSol.Mobibrary.EvangView
{
    public class SwipeEntryView : EvangEntryView
    {
        public string? EntryId { get; set; }

        //for create new sheet when click on the nextarrow. please set it to be true for a new sheet.
        //after saving the page, set it to be false
        public bool NotSaved { get; set; }
    }

    public class SwipeEntries<S> where S : SwipeEntryView
    {
        public ObservableCollection<S> Entries { get; set; } = new ObservableCollection<S>();
    }
}
