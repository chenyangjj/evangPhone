using EvangSol.Mobibrary.Utilities.Common;
using WinUI = Windows.UI;

namespace EvangSol.Mobibrary.Platforms.Windows
{
    public static class WindowsUtils
    {
        public static Microsoft.UI.Xaml.Media.SolidColorBrush ConvertToWinColor(Color? mauicolor)
        {
            if (mauicolor == null)
                mauicolor = BaseUtils.GetColor(CommonViewSetting.INPUT_FONTCOLOR);
            byte r, g, b, a;
            mauicolor!.ToRgba(out r, out g, out b, out a);
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(WinUI.Color.FromArgb(a, r, g, b));
        }
    }
}
