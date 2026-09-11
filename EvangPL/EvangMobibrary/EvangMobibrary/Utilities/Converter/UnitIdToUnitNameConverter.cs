using EvangSol.Mobibrary.DataFeed;
using System.Globalization;

namespace EvangSol.Mobibrary.Utilities.Converter
{
    public class UnitIdToUnitNameConverter : IValueConverter
    {
        string? unitid; //SIR0189273

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var unit = value as string;
            var unitsn = (LocalMemory.GetMaster("unitt") as List<MasterUnit>)!.Where(x => x.unit == unit).FirstOrDefault()?.unitsn;
            if (!string.IsNullOrEmpty(unitsn))
            {
                unitid = unit;  //SIR0189273
                return unitsn;
            }
            return unit;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            //SIR0189273
            if (!string.IsNullOrEmpty(unitid))
                return unitid;

            var unitsn = value as string;
            var unit = (LocalMemory.GetMaster("unitt") as List<MasterUnit>)!.Where(x => x.unitsn == unitsn).FirstOrDefault()?.unit;
            if (!string.IsNullOrEmpty(unit))
                return unit;
            return unitsn;
        }
    }
}
