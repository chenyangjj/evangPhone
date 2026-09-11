using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.Utilities.Common;
using System.Globalization;
using System.Reflection;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    //SIR0189273
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var sourcestring = (string)value;
            if (string.IsNullOrEmpty(sourcestring))
                return null;

            if (sourcestring.Contains(','))
            {
                var strs = sourcestring.Split(',');
                if (strs.Length == 3)
                {
                    return new FontImageSource
                    {
                        Glyph = strs[0],
                        FontFamily = strs[1],
                        Color = BaseUtils.GetColor(strs[2]),
                    };
                }
            }
            else if (ClassMapping.ref_assemblies != null)
            {
                //SIR0189380
                foreach (Assembly asm in ClassMapping.ref_assemblies)
                {
                    var resourceNames = asm.GetManifestResourceNames();
                    if (resourceNames.Contains(sourcestring))
                        return ImageSource.FromResource(sourcestring, asm);
                }
            }
            return sourcestring;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
