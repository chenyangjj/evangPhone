using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.Windows;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PlatformTimePickerHandler : ViewHandler<PlatformTimePicker, MauiTimePicker>
    {
        protected override MauiTimePicker CreatePlatformView() => new(VirtualView);

        protected override void ConnectHandler(MauiTimePicker platformView)
        {
            base.ConnectHandler(platformView);

            // Perform any control setup here
        }

        protected override void DisconnectHandler(MauiTimePicker platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

        public static void MapShowDialog(PlatformTimePickerHandler handler, PlatformTimePicker datepicker, object? args)
        {
            if (args == null)
                return;
            var t = args as PlatformTimeChangedEventArgs;
            if (t != null)
                handler.PlatformView?.ShowDialog(t.Time ?? DateTime.Now.TimeOfDay);
        }

        public static void MapBlur(PlatformTimePickerHandler handler, PlatformTimePicker datepicker, object? args)
        {
            handler.PlatformView?.Blur();
        }

        public string Text
        {
            get => PlatformView?.Text ?? string.Empty;
            set => PlatformView.Text = value;
        }

        public string Value
        {
            get => PlatformView?.Value ?? string.Empty;
            set => PlatformView.Value = value;
        }
    }
}
