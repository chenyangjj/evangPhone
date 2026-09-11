#if IOS || MACCATALYST
using PlatformView = EvangSol.Mobibrary.Platforms.MaciOS.MauiAutoComplete;
#elif ANDROID
using PlatformView = EvangSol.Mobibrary.Platforms.Android.MauiAutoComplete;
//#elif WINDOWS
//using PlatformView = EvangSol.Mobibrary.Platforms.Windows.MauiAutoComplete;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif
using EvangSol.Mobibrary.PlatformControl;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class AutoCompleteHandler
    {
        public static IPropertyMapper<AutoComplete, AutoCompleteHandler> PropertyMapper = new PropertyMapper<AutoComplete, AutoCompleteHandler>(ViewHandler.ViewMapper)
        {
            [nameof(AutoComplete.Options)] = MapOptions,
        };

        public static CommandMapper<AutoComplete, AutoCompleteHandler> CommandMapper = new(ViewCommandMapper)
        {
            [nameof(AutoComplete.ClearText)] = MapClearText,
            [nameof(AutoComplete.SetFocus)] = MapSetFocus,
            [nameof(AutoComplete.ClearFocus)] = MapBlur,
            [nameof(AutoComplete.ToggleSoftInput)] = MapSoftInput,
        };

        public AutoCompleteHandler() : base(PropertyMapper, CommandMapper)
        {
        }
    }
}
