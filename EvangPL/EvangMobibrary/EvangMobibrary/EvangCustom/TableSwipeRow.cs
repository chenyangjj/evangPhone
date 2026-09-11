using EvangSol.Mobibrary.EvangView;

namespace EvangSol.Mobibrary.EvangCustom
{
    public class TableSwipeRow : SwipeView
    {
        public int RowIndex { get; set; }
        public TableEntryView? EntryModel { get; set; }
        public List<TableSwipeRow>? SwipeViewList { get; set; }

        //hide base class's Close function, add swipe menu enable codes, please refer to EvangDataTable's comment
        new public void Close(bool animated = true)
        {
            base.Close(animated);

            if (SwipeViewList != null)
                foreach (var sv in SwipeViewList)
                    sv.IsEnabled = true;
        }
    }
}
