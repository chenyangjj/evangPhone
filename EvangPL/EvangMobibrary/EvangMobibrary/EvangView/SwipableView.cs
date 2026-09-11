using System.Collections.ObjectModel;

namespace EvangSol.Mobibrary.EvangView
{
    public class SwipableView<S> : BindableBrokerView where S : SwipeEntryView
    {
        SwipeEntries<S>? _swipentries;
        public SwipeEntries<S>? SwipeEntries
        {
            get => _swipentries;
            set
            {
                _swipentries = value;
                EntryList = value?.Entries;
            }
        }

        public ObservableCollection<S>? EntryList
        {
            get => SwipeEntries?.Entries;
            set
            {
                if (SwipeEntries is null) return;
                SwipeEntries.Entries = value ?? new ObservableCollection<S>();
                OnEntryList?.Invoke(EntryCount);
                OnPropertyChanged("EntryList");
            }
        }

        public Action<int>? OnEntryList { get; set; }

        public int EntryCount => SwipeEntries == null ? 0 : SwipeEntries.Entries.Count;

        public int Counter { get; set; }

        S? _currentry;
        public S? CurrEntry
        {
            get => _currentry;
            set
            {
                _currentry = value;
                OnPropertyChanged("CurrEntry");
            }
        }
    }
}
