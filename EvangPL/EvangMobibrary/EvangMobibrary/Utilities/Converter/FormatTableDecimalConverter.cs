using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;
using System.Globalization;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    public class FormatTableDecimalConverter : IValueConverter
    {
        TableEntryView em;
        string? unitname;
        string? format;

        public FormatTableDecimalConverter(TableEntryView em, string? unitname, string? format)
        {
            this.em = em;
            this.unitname = unitname;
            this.format = format;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? currformat = null;
            if (string.IsNullOrEmpty(format))
            {
                if (!string.IsNullOrEmpty(unitname))
                {
                    if (unitname.StartsWith('@'))
                        unitname = unitname.Substring(1);
                    currformat = em.GetDecimalFormat(unitname);
                }
            }
            else
            {
                currformat = format;
            }

            if (string.IsNullOrEmpty(currformat))
                return value;

            return BaseUtils.FormatDecimal((string)value!, currformat);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            return value;
        }
    }
}
