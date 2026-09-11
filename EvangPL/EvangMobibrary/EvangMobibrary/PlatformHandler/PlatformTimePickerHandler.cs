#if IOS || MACCATALYST
using PlatformView = EvangSol.Mobibrary.Platforms.MaciOS.MauiTimePicker;
#elif ANDROID
using PlatformView = EvangSol.Mobibrary.Platforms.Android.MauiTimePicker;
#elif WINDOWS
using PlatformView = EvangSol.Mobibrary.Platforms.Windows.MauiTimePicker;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif
using EvangSol.Mobibrary.PlatformControl;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PlatformTimePickerHandler
    {
        public static IPropertyMapper<PlatformTimePicker, PlatformTimePickerHandler> PropertyMapper = new PropertyMapper<PlatformTimePicker, PlatformTimePickerHandler>(ViewMapper)
        {
        };

        public static CommandMapper<PlatformTimePicker, PlatformTimePickerHandler> CommandMapper = new(ViewCommandMapper)
        {
            [nameof(PlatformTimePicker.ShowDialog)] = MapShowDialog,
            [nameof(PlatformTimePicker.ClearFocus)] = MapBlur,
        };

        public PlatformTimePickerHandler() : base(PropertyMapper, CommandMapper)
        {
        }
    }
}
