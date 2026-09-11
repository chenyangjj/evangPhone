#if IOS || MACCATALYST
using PlatformView = EvangSol.Mobibrary.Platforms.MaciOS.MauiDropDownSelect;
#elif ANDROID
using PlatformView = EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox;
#elif WINDOWS
using PlatformView = EvangSol.Mobibrary.Platforms.Windows.MauiDropDownBox;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif
using EvangSol.Mobibrary.PlatformControl;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class DropDownSelectHandler
    {
        public static IPropertyMapper<DropDownSelect, DropDownSelectHandler> PropertyMapper = new PropertyMapper<DropDownSelect, DropDownSelectHandler>(ViewMapper)
        {
            [nameof(DropDownSelect.Options)] = MapOptions,
        };

        public static CommandMapper<DropDownSelect, DropDownSelectHandler> CommandMapper = new(ViewCommandMapper)
        {
            [nameof(DropDownSelect.ClearFocus)] = MapBlur,
        };

        public DropDownSelectHandler() : base(PropertyMapper, CommandMapper)
        {
        }
    }
}
