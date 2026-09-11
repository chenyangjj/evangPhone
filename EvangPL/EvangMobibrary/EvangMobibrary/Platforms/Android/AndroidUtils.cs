using Android.Util;
using Android.Views;
using Andwidg = Android.Widget;
using Andraph = Android.Graphics;
using Android.Graphics;
using Microsoft.Maui.Platform;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Platforms.Android
{
    public static class AndroidUtils
    {
        public static void ApplyStyle(this Andwidg.TextView tv, CommonViewSetting setting)
        {
            if (setting.TextSize != null)
                tv.SetTextSize(ComplexUnitType.Sp, (float)setting.TextSize);
            else
                tv.SetTextSize(ComplexUnitType.Sp, (float)CommonViewSetting.INPUT_FONTSIZE);

            if (setting.TextColor != null)
                tv.SetTextColor(setting.TextColor.ToPlatform());
            else
                tv.SetTextColor(BaseUtils.GetColor("Gray900")!.ToPlatform());

            if (setting.Alignment != null)
                switch (setting.Alignment)
                {
                    case Microsoft.Maui.TextAlignment.Start:
                        tv.SetForegroundGravity(GravityFlags.Start);
                        break;
                    case Microsoft.Maui.TextAlignment.End:
                        tv.SetForegroundGravity(GravityFlags.End);
                        break;
                    case Microsoft.Maui.TextAlignment.Center:
                        tv.SetForegroundGravity(GravityFlags.Center);
                        break;
                }

            if (setting.FontAttributes != null)
                switch (setting.FontAttributes)
                {
                    case FontAttributes.Bold:
                        tv.Typeface = Typeface.DefaultBold;
                        break;
                    case FontAttributes.Italic:
                        tv.Typeface = Typeface.Create(Typeface.Default, TypefaceStyle.Italic);
                        break;
                }

            if (setting.Placeholder != null)
            {
                tv.Hint = setting.Placeholder;
            }
            tv.SetHintTextColor(Andraph.Color.Argb(255, 172, 172, 172));

            if (setting.Padding != null)
                tv.SetPadding(setting.Padding.Value.Item1, setting.Padding.Value.Item2, setting.Padding.Value.Item3, setting.Padding.Value.Item4);
        }
    }
}
