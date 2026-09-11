using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using static Microsoft.Maui.Controls.VisualStateManager;

namespace EvangSol.Mobibrary.EvangComposite
{
    public abstract class EvangContentView : ContentView, IBaseUtils
    {
        public EvangContentView()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BaseCustom", (handler, view) =>
            {
                if (view is Entry)
                {
#if ANDROID
                    handler.PlatformView.SetPadding(10, 15, 10, 5);
                    //handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS || MACCATALYST

#endif
                }
            });

            Style style = new(typeof(Entry))
            {
                Setters =
                {
                    new Setter
                    {
                        Property = VisualStateGroupsProperty,
                        Value = new VisualStateGroupList
                        {
                            new VisualStateGroup
                            {
                                Name = nameof(CommonStates),
                                States =
                                {
                                    new VisualState
                                    {
                                        Name = CommonStates.Disabled,
                                        Setters =
                                        {
                                            new Setter
                                            {
                                                Property = BackgroundColorProperty,
                                                Value = Colors.LightGray
                                            }
                                        }
                                    },
                                    new VisualState
                                    {
                                        Name = CommonStates.Normal
                                    }
                                }
                            }
                        }
                    }
                }
            };
            Resources.Add(style);
        }

        //concise way to get custom string
        public string S(string str) => GetCustomString(str) ?? string.Empty;

        //get the page it resides
        EvangContentVM? parentpage = null;
        public EvangContentVM? ParentPage
        {
            get
            {
                if (parentpage == null)
                {
                    var element = (Element)this;
                    while (element != null)
                    {
                        if (element is EvangContentVM page)
                        {
                            parentpage = page;
                            break;
                        }
                        element = element.Parent;
                    }
                }
                return parentpage;
            }
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
