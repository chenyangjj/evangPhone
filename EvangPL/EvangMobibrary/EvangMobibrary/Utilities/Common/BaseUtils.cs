using EvangSol.Mobibrary.DataFeed;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Win32;
using EvangSol.Mobibrary.Resources.Strings;

namespace EvangSol.Mobibrary.Utilities.Common
{
    public interface IBaseUtils
    {
        public Color? GetColor(string? clr);
        public FontAttributes? GetFontAttr(string? font);
        public TextAlignment? GetAlignment(string? align);
        public T? GetPropertyValue<T>(object obj, string propname);
        public bool SetPropertyValue<T>(object obj, string propname, T? val);
        public string? GetCustomString(string key);
        public void SetTimer(int interval, Action timeout);
        public void SetInterval(int interval, Func<bool> func);
    }

    public class BaseUtils
    {
        #region base utilities
        public static Color? GetColor(string? clr)
        {
            if (clr == null)
                return null;
            if (clr.Substring(0, 1) == "#")
            {
                if (Color.TryParse(clr, out var color))
                    return color;
                else
                    throw new ArgumentException("Invalid value for Color", clr);
            }
            else
            {
                ResourceDictionary? ColorResource = Application.Current?.Resources.MergedDictionaries.FirstOrDefault();
                Color? color = null;
                if (ColorResource != null)
                {
                    if (ColorResource.Keys.Contains(clr))
                        color = ColorResource[clr] as Color;
                }
                //if not found in resources, search Microsoft.Maui.Graphics.Colors
                if (color == null)
                {
                    var colorfld = typeof(Colors).GetField(clr, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                    if (colorfld != null)
                        color = colorfld.GetValue(null) as Color;
                }
                if (color == null)
                    throw new ArgumentException("Invalid value for Color", clr);
                return color;
            }
        }

        public static FontAttributes? GetFontAttr(string? font) =>
            font switch
            {
                null => null,
                "None" => FontAttributes.None,
                "Bold" => FontAttributes.Bold,
                "Italic" => FontAttributes.Italic,
                _ => throw new ArgumentException("Invalid value for FontAttributes", font)
            };

        public static TextAlignment? GetAlignment(string? align) =>
            align switch
            {
                null => null,
                "Start" => TextAlignment.Start,
                "Center" => TextAlignment.Center,
                "End" => TextAlignment.End,
                _ => throw new ArgumentException("Invalid value for Alignment", align)
            };

        public static T? GetPropertyValue<T>(object obj, string propname)
        {
            if (obj == null)
                return default;
            var prop = obj.GetType().GetProperty(propname);
            if (prop == null)
                return default;
            object? val = prop.GetValue(obj);
            return val == null ? default : (T?)val;
        }

        public static bool SetPropertyValue<T>(object obj, string propname, T? val)
        {
            if (obj == null)
                return false;
            var prop = obj.GetType().GetProperty(propname);
            if (prop == null)
                return false;
            var type = typeof(T);
            if (val == null && (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>)))
                return false;
            prop.SetValue(obj, val);
            return true;
        }

        static List<Type>? string_resources;
        public static string? GetCustomString(string? key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            PropertyInfo? prop = null;
            if (ClassMapping.ref_assemblies == null)
            {
                prop = typeof(StringResources).GetProperty(key, BindingFlags.NonPublic | BindingFlags.Static);
                if (prop != null)
                    return (string?)prop.GetValue(null, null);
                return string.Empty;
            }
            else
            {
                if (string_resources == null)
                {
                    string_resources = new List<Type>();
                    foreach (var asm in ClassMapping.ref_assemblies)
                    {
                        var qset = (from x in asm.GetTypes() where x.Name == "StringResources" select x);
                        if (qset.Count() < 1)
                            continue;
                        string_resources.Insert(0, qset.First());
                    }
                }
                foreach (var type in string_resources)
                {
                    prop = type.GetProperty(key, BindingFlags.NonPublic | BindingFlags.Static);
                    if (prop != null)
                        return (string?)prop.GetValue(null, null);
                }
            }
            return null;
        }

        public static void SetTimer(int interval, Action timeout)
        {
            IDispatcherTimer _timer;
            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(interval);
            _timer.Tick += (sender, e) =>
            {
                _timer.Stop();
                timeout();
            };
            _timer.Start();
        }

        public static void SetInterval(int interval, Func<bool> func)
        {
            _ = SetInterval(TimeSpan.FromMilliseconds(interval), func);
        }
        static async Task SetInterval(TimeSpan timeout, Func<bool> func)
        {
            await Task.Delay(timeout).ConfigureAwait(false);
            if (func())
                _ = SetInterval(timeout, func);
        }
        #endregion

        #region others
        public static string GetQtyformat(string unit, string? decimals = null)
        {
            var dataformat = LocalMemory.GetMaster("MasterParams") as List<MasterParams>;
            if (dataformat == null)
                return string.Empty;

            string? qtyFormat = dataformat.Where(x => x.paramid == "Quantity").Select(x => x.paramval).First();
            if (qtyFormat == null)
                return string.Empty;

            var format = (LocalMemory.GetMaster("unitt") as List<MasterUnit>)!.Where(x => x.unit == unit).FirstOrDefault()?.qtyFormat;
            if (!string.IsNullOrEmpty(format))
                qtyFormat = format;

            if (!string.IsNullOrEmpty(decimals))
            {
                int decim = 0;
                if (int.TryParse(decimals, out decim))
                {
                    if (qtyFormat.Contains("."))
                    {
                        int index = qtyFormat.IndexOf(".");
                        qtyFormat = qtyFormat.Substring(0, index) + "." + string.Empty.PadRight(decim, '0');
                    }
                    else
                    {
                        if (decim > 0)
                            qtyFormat = qtyFormat + "." + string.Empty.PadRight(decim, '0');
                    }
                }
            }
            return qtyFormat;
        }

        public static T? JsonToClass<T>(string json) where T : class, new()
        {
            return JsonSerializer.Deserialize<T>(json);
        }

        public static string ToJson(object obj)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            return JsonSerializer.Serialize(obj, options);
        }

        public static T ValueCopyFrom<S, T>(T target, S source)
        {
            var srctype = source!.GetType();
            foreach (var tgtprop in target!.GetType().GetProperties())
            {
                if (tgtprop.GetSetMethod() == null || tgtprop.GetIndexParameters().Length > 0)
                    continue;
                var srcprop = srctype.GetProperty(tgtprop.Name);
                if (srcprop == null || srcprop.GetGetMethod() == null || srcprop.GetIndexParameters().Length > 0)
                    continue;
                tgtprop.SetValue(target, srcprop.GetValue(source));
            }
            return target;
        }

        public static T ValueCopyTo<S, T>(S source, T target)
        {
            var trgtype = target!.GetType();
            foreach (var srcprop in source!.GetType().GetProperties())
            {
                if (srcprop.GetGetMethod() == null || srcprop.GetIndexParameters().Length > 0)
                    continue;
                var tgtprop = trgtype.GetProperty(srcprop.Name);
                if (tgtprop == null || tgtprop.GetSetMethod() == null || tgtprop.GetIndexParameters().Length > 0)
                    continue;
                tgtprop.SetValue(target, srcprop.GetValue(source));
            }
            return target;
        }

        public static List<T> LocalModelListToJsonModelList<S, T>(List<S> source) where T : new()
        {
            var targetList = new List<T>();
            foreach (S row in source)
            {
                var convertedRow = ValueCopyTo(row, new T());
                targetList.Add(convertedRow);
            }

            return targetList;
        }

        //format decimal values with or without tailing unit
        public static string FormatDecimal(string str, string format)
        {
            int i = 0;
            for (; i < str.Length; i++)
            {
                if (!"-1234567890.,".Contains(str[i]))
                    break;
            }
            var val = str.Substring(0, i);
            var unt = str.Substring(i);
            double db;
            if (double.TryParse(val, out db))
                str = db.ToString(format) + unt;
            return str;
        }

        public static decimal ConvertItemUnit(string itemno, string frunit, string tounit, decimal qty)
        {
            if (string.IsNullOrEmpty(frunit) || string.IsNullOrEmpty(tounit))
                return -99999;
            if (frunit == tounit)
                return qty;
            try
            {
                var itemuexchgt = LocalMemory.GetMaster("itemuexchgt") as List<MasterUnitExchg>;
                var qset = from x in itemuexchgt where x.itemNo == itemno && x.frUnit == frunit && x.toUnit == tounit select x;
                if (qset.Count() < 1)
                {
                    qset = from x in itemuexchgt where x.itemNo == "*" && x.frUnit == frunit && x.toUnit == tounit select x;
                    if (qset.Count() < 1)
                        return -99999;
                }
                var uexchgt = qset.First();
                return qty * decimal.Parse(uexchgt.toQty!) / decimal.Parse(uexchgt.frQty!);
            }
            catch { }
            return -99999;
        }

        public static string ConvertQty(string? qty, string? unit, string? decimals = null)
        {
            if (string.IsNullOrEmpty(qty))
                return string.Empty;
            if (string.IsNullOrEmpty(unit))
                return qty;
            string convertedQty = qty;
            var qtyFormat = GetQtyformat(unit, decimals);
            if (!string.IsNullOrEmpty(qty))
            {
                double qtyDouble = 0;
                if (double.TryParse(qty, out qtyDouble))
                {
                    convertedQty = qtyDouble.ToString(qtyFormat);
                }
            }
            return convertedQty;
        }

        public static string GetDevice_id()
        {
            var device_id = "";
#if ANDROID
            device_id = Android.Provider.Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
#elif WINDOWS
            try
            {
                const string keyPath = @"SOFTWARE\Microsoft\Cryptography";
                const string valueName = "MachineGuid";

                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                var machineGuid = key?.GetValue(valueName) as string;
                
                device_id = machineGuid ?? "unknown";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get MachineGuid: {ex.Message}");
            }
#endif
            return device_id!;
        }

        #endregion
    }

    public class CommonViewSetting
    {
        #region common style settings
        public static int LAYOUT_SPACING;       //default layout spacing size
        public static int LAYOUT_PADDING;       //default layout padding size

        public static double LABEL_FONTSIZE;                //default label font size
        public static string LABEL_FONTCOLOR = "";          //default label font color
        public static double INPUT_FONTSIZE;                //default input font size
        public static string INPUT_FONTCOLOR = "";          //default input font color
        public static double COMPOSITE_HEIGHT;              //default composite control height
        public static double COMPOSITE_TANDEMLABEL_HEIGHT;  //default composite label height in tandem pattern
        public static double HEADER_FONTSIZE;               //default table header font size
        public static double TABLE_HEADERHEIGHT;            //default table header height
        public static double TABLE_ROWHEIGHT;               //default table row height
        public static double SCREEN_SIZE_THRESHOLD;         //default dialog size threshold
        public static int DIALOG_WIDTH = 500;               //default dialog width

        public static double TITLE_FONTSIZE = 24;           //default title view font size
        public static string TITLE_FONTCOLOR = "White";     //default title view font color
        //SIR0188664
        public static int LONGPRESS_DURATION = 600;         //default duration in milliseconds for long press

        public const int INPUT_MAXLENGTH = -1;              //default input max lenght, -1: Unlimited
        //SIR0189273
        public const int GRIDCELL_IMAGE_SIZE = 30;          //default image height in data grid cell
        #endregion

        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? TextSize { get; set; }
        public Color? TextColor { get; set; }
        public Color? BackgroundColor { get; set; }
        public FontAttributes? FontAttributes { get; set; }
        public TextAlignment? Alignment { get; set; }
        public (int, int, int, int)? Padding { get; set; }
        public bool Visibility { get; set; } = true;
        public string? InputType { get; set; }
        public string? Placeholder { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public double? EditorHeight { get; set; }
        public int MaxLength { get; set; }
        public string? ScanType { get; set; }
        public bool ShowKeyBoardIcon { get; set; }  //for autocomplete
        public string? DecimalFormat { get; set; }
        public bool InEntry { get; set; }   //if this control is inside a entry view of swipable view
        public double LabelHeight { get; set; } //the label's height if in tandem layout
        public string? MessageLabel { get; set; }
    }
}
