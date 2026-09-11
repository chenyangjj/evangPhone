#if IOS || MACCATALYST
using PlatformView = EvangSol.Mobibrary.Platforms.MaciOS.MauiPopupMenu;
#elif ANDROID
using PlatformView = EvangSol.Mobibrary.Platforms.Android.MauiPopupMenu;
#elif WINDOWS
using PlatformView = EvangSol.Mobibrary.Platforms.Windows.MauiPopupMenu;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif
using EvangSol.Mobibrary.PlatformControl;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PopupMenuHandler
    {
        public static IPropertyMapper<PopupMenu, PopupMenuHandler> PropertyMapper = new PropertyMapper<PopupMenu, PopupMenuHandler>(ViewMapper)
        {
            [nameof(PopupMenu.MenuItems)] = MapMenuItems,
        };

        public static CommandMapper<PopupMenu, PopupMenuHandler> CommandMapper = new(ViewCommandMapper)
        {
            [nameof(PopupMenu.ShowMenu)] = MapShowMenu,
            [nameof(PopupMenu.HideMenu)] = MapHideMenu,
        };

        public PopupMenuHandler() : base(PropertyMapper, CommandMapper)
        {
        }
    }
}
