#nullable enable
using Microsoft.Maui.Handlers;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.Android;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class AutoCompleteHandler : ViewHandler<AutoComplete, MauiAutoComplete>
    {
        protected override MauiAutoComplete CreatePlatformView() => new(Context, VirtualView);

        protected override void ConnectHandler(MauiAutoComplete platformView)
        {
            base.ConnectHandler(platformView);

            // Perform any control setup here
        }

        protected override void DisconnectHandler(MauiAutoComplete platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }


        public static void MapOptions(AutoCompleteHandler handler, AutoComplete autoComplete)
        {
            handler.PlatformView?.SetOptions();
        }

        public static void MapClearText(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            handler.PlatformView?.Clear();
        }

        public static void MapSetFocus(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            handler.PlatformView?.Focus();
        }

        public static void MapBlur(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            handler.PlatformView?.Blur();
        }

        public static void MapSoftInput(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            if (args == null)
                return;
            var toggle = (SoftInputEventArgs)args;
            handler.PlatformView?.ToggleKeyboard(toggle.Show);
        }

        public string AutoText
        {
            get => PlatformView?.Text ?? string.Empty;
            set => PlatformView.Text = value;
        }

        public string AutoValue
        {
            get => PlatformView?.Value ?? string.Empty;
            set => PlatformView.Value = value;
        }
    }
}
