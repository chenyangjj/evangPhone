using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.Android;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PlatformDatePickerHandler : ViewHandler<PlatformDatePicker, MauiDatePicker>
    {
        protected override MauiDatePicker CreatePlatformView() => new(Context, VirtualView);

        protected override void ConnectHandler(MauiDatePicker platformView)
        {
            base.ConnectHandler(platformView);

            // Perform any control setup here
        }

        protected override void DisconnectHandler(MauiDatePicker platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

        public static void MapShowDialog(PlatformDatePickerHandler handler, PlatformDatePicker datepicker, object? args)
        {
            if (args == null)
                return;
            var d = args as PlatformDateChangedEventArgs;
            if (d != null)
                handler.PlatformView?.ShowDialog(d.Date ?? DateTime.Now);
        }

        public static void MapBlur(PlatformDatePickerHandler handler, PlatformDatePicker datepicker, object? args)
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

        public DateTime? Current => PlatformView?.Current;
    }
}
