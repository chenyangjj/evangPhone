using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.MaciOS;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PlatformDatePickerHandler : ViewHandler<PlatformDatePicker, MauiDatePicker>
    {
        protected override MauiDatePicker CreatePlatformView()
        {
            throw new NotImplementedException();
        }

        public static void MapShowDialog(PlatformDatePickerHandler handler, PlatformDatePicker datepicker, object? args)
        {
            throw new NotImplementedException();
        }

        public static void MapBlur(PlatformDatePickerHandler handler, PlatformDatePicker datepicker, object? args)
        {
            throw new NotImplementedException();
        }

        public string Text
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string Value
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public DateTime? Current => throw new NotImplementedException();
    }
}
