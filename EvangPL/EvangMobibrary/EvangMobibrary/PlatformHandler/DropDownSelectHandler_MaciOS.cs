#nullable enable
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.MaciOS;
using Microsoft.Maui.Handlers;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class DropDownSelectHandler : ViewHandler<DropDownSelect, MauiDropDownSelect>
    {
        protected override MauiDropDownSelect CreatePlatformView()
        {
            throw new NotImplementedException();
        }

        public static void MapOptions(DropDownSelectHandler handler, DropDownSelect dropdownbox)
        {
            throw new NotImplementedException();
        }

        public string Text
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string Value
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public static void MapBlur(DropDownSelectHandler handler, DropDownSelect dropdownbox, object? args)
        {
            throw new NotImplementedException();
        }
    }
}
