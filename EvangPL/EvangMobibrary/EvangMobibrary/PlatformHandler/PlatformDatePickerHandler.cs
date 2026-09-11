#if IOS || MACCATALYST
using PlatformView = EvangSol.Mobibrary.Platforms.MaciOS.MauiDatePicker;
#elif ANDROID
using PlatformView = EvangSol.Mobibrary.Platforms.Android.MauiDatePicker;
#elif WINDOWS
using PlatformView = EvangSol.Mobibrary.Platforms.Windows.MauiDatePicker;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif
using EvangSol.Mobibrary.PlatformControl;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class PlatformDatePickerHandler
    {
        public static IPropertyMapper<PlatformDatePicker, PlatformDatePickerHandler> PropertyMapper = new PropertyMapper<PlatformDatePicker, PlatformDatePickerHandler>(ViewMapper)
        {
        };

        public static CommandMapper<PlatformDatePicker, PlatformDatePickerHandler> CommandMapper = new(ViewCommandMapper)
        {
            [nameof(PlatformDatePicker.ShowDialog)] = MapShowDialog,
            [nameof(PlatformDatePicker.ClearFocus)] = MapBlur,
        };

        public PlatformDatePickerHandler() : base(PropertyMapper, CommandMapper)
        {
        }
    }
}
