using EvangSol.Mobibrary.EvangView;
using System.Globalization;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    public class FormatDecimalConverter : IValueConverter
    {
        string? unit;
        BindableBrokerView? bpvm;
        string? format;

        public FormatDecimalConverter(string? unit, BindableBrokerView? bpvm, string? format)
        {
            this.unit = unit;
            this.bpvm = bpvm;
            this.format = format;
        }

        public void SetFormat(string format)
        {
            this.format = format;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? currformat = null;
            if (string.IsNullOrEmpty(format))
            {
                if (!string.IsNullOrEmpty(unit) && bpvm != null)
                    currformat = bpvm.GetDecimalFormat(unit);
            }
            else
            {
                currformat = format;
            }

            if (string.IsNullOrEmpty(currformat))
                return value;

            var str = value as string;
            decimal d;
            if (decimal.TryParse(str, out d))
                return d.ToString(currformat);

            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            return value;
        }
    }
}
