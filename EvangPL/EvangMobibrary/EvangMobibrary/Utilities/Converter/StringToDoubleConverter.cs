using System.Globalization;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    public class StringToDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var str = value as string;
            double d;
            if (double.TryParse(str, out d))
            {
                return d;
            }
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            return ((double)value).ToString();
        }
    }
}
