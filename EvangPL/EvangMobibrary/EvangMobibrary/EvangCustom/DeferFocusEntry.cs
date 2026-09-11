using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangCustom
{
    //In Windows, when you click the clear icon in the EntryCompositeView, it first triggers the Unfocused event, then the Tapped event.
    //This is determined by Windows, but we want to reverse them. Therefore, we add a new event, DeferedUnfocused.
    //When Unfocused is triggered, we wait a bit and then call this DeferedUnfocused event.
    public class DeferFocusEntry : Entry
    {
        public event EventHandler<FocusEventArgs>? DeferedUnfocused;

        public DeferFocusEntry()
        {
            Unfocused += (sender, e) =>
            {
                BaseUtils.SetTimer(200, () =>
                {
                    DeferedUnfocused?.Invoke(sender, e);
                });
            };

            //this prevents the focus from moving to the next control
            Completed += (sender, e) =>
            {
                if (sender is Entry entry)
                {
                    //keep focus
                    entry.Focus();
                }
            };
        }
    }
}
