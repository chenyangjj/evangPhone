#nullable enable
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.Android;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class DropDownSelectHandler : ViewHandler<DropDownSelect, MauiDropDownBox>
    {
        protected override MauiDropDownBox CreatePlatformView() => new(Context, VirtualView);

        protected override void ConnectHandler(MauiDropDownBox platformView)
        {
            base.ConnectHandler(platformView);

            // Perform any control setup here
        }

        protected override void DisconnectHandler(MauiDropDownBox platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }


        public static void MapOptions(DropDownSelectHandler handler, DropDownSelect dropdownbox)
        {
            handler.PlatformView?.SetOptions();
        }

        public static void MapBlur(DropDownSelectHandler handler, DropDownSelect dropdownbox, object? args)
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
