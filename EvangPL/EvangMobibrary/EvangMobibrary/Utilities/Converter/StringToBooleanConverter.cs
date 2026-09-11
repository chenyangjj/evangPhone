using System.Globalization;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var str = value as string;
            bool d;
            if (bool.TryParse(str, out d))
            {
                return d;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            return ((bool)value).ToString();
        }
    }
}
