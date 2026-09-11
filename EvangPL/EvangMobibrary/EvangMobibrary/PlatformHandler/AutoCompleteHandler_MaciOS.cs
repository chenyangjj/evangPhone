#nullable enable
using Microsoft.Maui.Handlers;
using EvangSol.Mobibrary.Platforms.MaciOS;
using EvangSol.Mobibrary.PlatformControl;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class AutoCompleteHandler : ViewHandler<AutoComplete, MauiAutoComplete>
    {
        protected override MauiAutoComplete CreatePlatformView()
        {
            throw new NotImplementedException();
        }


        public static void MapOptions(AutoCompleteHandler handler, AutoComplete autoComplete)
        {
            throw new NotImplementedException();
        }

        public static void MapClearText(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            throw new NotImplementedException();
        }

        public static void MapSetFocus(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            throw new NotImplementedException();
        }

        public static void MapBlur(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            throw new NotImplementedException();
        }

        public static void MapSoftInput(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            throw new NotImplementedException();
        }

        public string AutoText
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string AutoValue
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
