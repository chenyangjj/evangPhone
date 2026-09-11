using EvangSol.Mobibrary.EvangCustom;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.TitleBar
{
    public interface IBaseTitleView
    {
        public View GetTitleView(double width);
    }

    public abstract class EvangTitleBar : IBaseTitleView, IBaseUtils, IMappingBase
    {
        public double title_fontsize => CommonViewSetting.TITLE_FONTSIZE;
        public string title_textcolor => CommonViewSetting.TITLE_FONTCOLOR;
        public string title_fontattr = "Bold";
        public string title_alignment = "Center";

        public INavigation? Navi { get; set; }
        public string Caption { get; set; }

        public EvangTitleBar(INavigation navi, string caption)
        {
            Navi = navi;
            Caption = caption;
        }

        //main title view layout
        public abstract Grid? GridLayout { get; set; }

        //override this function to create custom title views
        public abstract View GetTitleView(double width);

        //for override
        protected virtual void CustomMenuItem(ref List<KeyValuePair<string, PopupMenu.MenuItemSetting?>> menulist) { }

        // a convenient way to get master data
        public object? this[string key] => LocalMemory.GetMaster(key);

        //concise way to get custom string
        public string S(string str) => GetCustomString(str) ?? string.Empty;

        //show popup menu
        public virtual void ShowPopupMenu(TopIconImage icon, PopupMenu popup, List<(int, bool)>? enablist = null)
        {
            var bounds = icon!.Bounds;
            var x = (bounds.X + bounds.Width) * DeviceDisplay.Current.MainDisplayInfo.Density;
            var y = (bounds.Y + bounds.Height + 20) * DeviceDisplay.Current.MainDisplayInfo.Density;
            //it's hard to match the actual height of the title view when screen density is larger than 1
            //here we give a compensation to the height when screen density is 2
            if (DeviceDisplay.Current.MainDisplayInfo.Density == 2)
                y += 30;
            popup.DoShowMenu((int)x, (int)y, enablist);
        }

        #region BaseUtils
        public TextAlignment? GetAlignment(string? align) => BaseUtils.GetAlignment(align);

        public Color? GetColor(string? clr) => BaseUtils.GetColor(clr);

        public FontAttributes? GetFontAttr(string? font) => BaseUtils.GetFontAttr(font);

        public T? GetPropertyValue<T>(object obj, string propname) => BaseUtils.GetPropertyValue<T>(obj, propname);

        public bool SetPropertyValue<T>(object obj, string propname, T? val) => BaseUtils.SetPropertyValue(obj, propname, val);

        public string? GetCustomString(string? key) => BaseUtils.GetCustomString(key);

        public void SetTimer(int interval, Action timeout) => BaseUtils.SetTimer(interval, timeout);

        public void SetInterval(int interval, Func<bool> func) => BaseUtils.SetInterval(interval, func);
        #endregion
    }

}
