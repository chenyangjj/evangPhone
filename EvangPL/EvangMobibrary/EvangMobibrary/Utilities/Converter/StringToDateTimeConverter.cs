using System.Globalization;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    public class StringToDateTimeConverter : IValueConverter
    {
        string format;
        public StringToDateTimeConverter(string format)
        {
            this.format = format;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var str = value as string;
            DateTime d;
            if (DateTime.TryParse(str, out d))
            {
                return d;
            }
            return DateTime.Now;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            return ((DateTime)value).ToString(format);
        }
    }
}
