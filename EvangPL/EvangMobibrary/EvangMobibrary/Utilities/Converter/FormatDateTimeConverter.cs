using EvangSol.Mobibrary.DataFeed;
using System.Globalization;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    public class FormatDateTimeConverter : IValueConverter
    {
        string? format;

        public FormatDateTimeConverter(string format)
        {
            if (format.Contains("MasterParams"))
            {
                var list = LocalMemory.GetMaster("MasterParams") as List<MasterParams>;
                if (list != null)
                {
                    var fmt = list.Where(x => x.paramid == format).Select(x => x.paramval);
                    if (fmt.Any())
                        this.format = fmt.First();
                }
            }
            else
            {
                this.format = format;
            }
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var str = value as string;
            DateTime d;
            if (DateTime.TryParseExact(str, format, null, DateTimeStyles.None, out DateTime exact))
            {
                return exact.ToString(format);
            }
            else if (DateTime.TryParse(str, out d))
            {
                return d.ToString(format);
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
