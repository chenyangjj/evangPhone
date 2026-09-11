using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.MaciOS;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PopupMenuHandler : ViewHandler<PopupMenu, MauiPopupMenu>
    {
        protected override MauiPopupMenu CreatePlatformView()
        {
            throw new NotImplementedException();
        }

        public static void MapMenuItems(PopupMenuHandler handler, PopupMenu popupmenu)
        {
            throw new NotImplementedException();
        }

        public static void MapShowMenu(PopupMenuHandler handler, PopupMenu popupmenu, object? args)
        {
            throw new NotImplementedException();
        }

        public static void MapHideMenu(PopupMenuHandler handler, PopupMenu popupmenu, object? args)
        {
            throw new NotImplementedException();
        }
    }
}
