using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.MaciOS;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PlatformTimePickerHandler : ViewHandler<PlatformTimePicker, MauiTimePicker>
    {
        protected override MauiTimePicker CreatePlatformView()
        {
            throw new NotImplementedException();
        }

        public static void MapShowDialog(PlatformTimePickerHandler handler, PlatformTimePicker datepicker, object? args)
        {
            throw new NotImplementedException();
        }

        public static void MapBlur(PlatformTimePickerHandler handler, PlatformTimePicker datepicker, object? args)
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
    }
}
