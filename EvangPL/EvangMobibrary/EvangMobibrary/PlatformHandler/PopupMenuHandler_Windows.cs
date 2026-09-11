using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.Windows;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PopupMenuHandler : ViewHandler<PopupMenu, MauiPopupMenu>
    {
        protected override MauiPopupMenu CreatePlatformView() => new(VirtualView);

        protected override void ConnectHandler(MauiPopupMenu platformView)
        {
            base.ConnectHandler(platformView);

            // Perform any control setup here
        }

        protected override void DisconnectHandler(MauiPopupMenu platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

        public static void MapMenuItems(PopupMenuHandler handler, PopupMenu popupmenu)
        {
            handler.PlatformView?.SetMenuItems();
        }

        public static void MapShowMenu(PopupMenuHandler handler, PopupMenu popupmenu, object? args)
        {
            if (args == null)
                return;
            var pos = (ShowMenuEventArgs)args;
            handler.PlatformView?.ShowPopupMenu(pos.x, pos.y, pos.enablist);
        }

        public static void MapHideMenu(PopupMenuHandler handler, PopupMenu popupmenu, object? args)
        {
            handler.PlatformView?.HidePopupMenu();
        }
    }
}
