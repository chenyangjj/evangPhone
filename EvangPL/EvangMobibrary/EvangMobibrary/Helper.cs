using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.PlatformHandler;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.LifecycleEvents;
using System.Reflection;
using System.Runtime.InteropServices;
using EvangSol.Mobibrary.LocalModel;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary
{
    public static class Helper
    {
        public static Application? currapp;

        public static MauiAppBuilder UseEvangMobibrary(this MauiAppBuilder builder)
        {
            builder.UseMauiCommunityToolkit();

            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler(typeof(DropDownSelect), typeof(DropDownSelectHandler));
                handlers.AddHandler(typeof(AutoComplete), typeof(AutoCompleteHandler));
                handlers.AddHandler(typeof(PopupMenu), typeof(PopupMenuHandler));
                handlers.AddHandler(typeof(PlatformDatePicker), typeof(PlatformDatePickerHandler));
                handlers.AddHandler(typeof(PlatformTimePicker), typeof(PlatformTimePickerHandler));
            });

            AppDomain.CurrentDomain.UnhandledException += async (s, e) =>
            {
#if DEBUG
                Console.WriteLine($"---------- AppDomain UnhandledException - {e.ExceptionObject.ToString()}");
#endif
            };
            TaskScheduler.UnobservedTaskException += async (s, e) =>
            {
#if DEBUG
                Console.WriteLine($"---------- TaskScheduler UnobservedTaskException - {e.Exception.ToString()}");
#endif
            };

            builder.ConfigureLifecycleEvents(events =>
            {
                //maximize app window on startup on Windows
#if WINDOWS
                events.AddWindows(windows => windows
                    .OnWindowCreated(window =>
                    {
                        window.ExtendsContentIntoTitleBar = false;  // Optional: adjust title bar
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                    
                        // Maximize the window
                        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                        {
                            p.Maximize();
                        }
                    
                        // Or alternatively:
                        //appWindow.Resize(new Windows.Graphics.SizeInt32(1920, 1080)); // Set specific size
                    }));
#endif
            });

            return builder;
        }

        public static void AddConfiguration(this MauiAppBuilder builder, Assembly assembly, string configfile, string? settingfile = null)
        {
            if(ClassMapping.ref_assemblies == null)
                ClassMapping.Init();
            EvangLM.CreateLocalTables(ClassMapping.ref_assemblies!);

            var count = LocalStorage.ExecuteScalar<int>("select count(*) from Config");
            if (count < 1)
            {
                using Stream? stream = assembly.GetManifestResourceStream(configfile);

                if (stream != null)
                {
                    IConfigurationRoot config = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();
                    builder.Configuration.AddConfiguration(config);
                }

                LocalStorage.config = new ConfigLM();
                foreach (var propinfo in LocalStorage.config.GetType().GetProperties())
                {
                    var type = propinfo.PropertyType;
                    if (type.IsGenericType)
                    {
                        var list = Activator.CreateInstance(type);
                        builder.Configuration.GetSection(propinfo.Name).Bind(list);

                        if (propinfo.Name == "Accounts")
                        {
                            int counter = 0;
                            foreach (AccountLM item in (List<AccountLM>)list!)
                            {
                                item.seq = counter;
                                counter++;
                            }
                        }
                        propinfo.SetValue(LocalStorage.config, list);
                    }
                    else
                    {
                        var val = builder.Configuration.GetValue(type, propinfo.Name);
                        propinfo.SetValue(LocalStorage.config, val);
                    }
                }
                LocalStorage.Insert(LocalStorage.config);
                foreach (var accnt in LocalStorage.config!.Accounts!)
                    LocalStorage.Insert(accnt);
            }
            else
            {
                LocalStorage.config = LocalStorage.Read<ConfigLM>().First();
                LocalStorage.config.Accounts = LocalStorage.Query<AccountLM>("select * from Account");
            }

            if (settingfile != null)
                LoadCustomStyleSetting(builder, assembly, settingfile);
        }

        public static void LoadCustomStyleSetting(this MauiAppBuilder builder, Assembly assembly, string settingfile)
        {
            var isTab = DeviceInfo.Idiom != DeviceIdiom.Phone;

            using Stream? stream = assembly.GetManifestResourceStream(settingfile);
            if (stream != null)
            {
                IConfigurationRoot setting = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
                builder.Configuration.AddConfiguration(setting);

                //if you want to differentiate tablet and PDA's style, add "TAB_" or "PHO_" prefix to the common style variables respectively
                var prefix = isTab ? "TAB_" : "PHO_";
                foreach (FieldInfo field in typeof(CommonViewSetting).GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    //first search with prefix
                    var val = builder.Configuration.GetValue(field.FieldType, prefix + field.Name);
                    //if not found, search without prefix
                    if (val == null)
                        val = builder.Configuration.GetValue(field.FieldType, field.Name);
                    //if get the value, set it to the global variable
                    if (val != null)
                        field.SetValue(null, val);
                }
            }
            else
            {
                //default common style settings
                CommonViewSetting.LAYOUT_SPACING = 5;
                CommonViewSetting.LAYOUT_PADDING = 5;
                CommonViewSetting.LABEL_FONTCOLOR = "PrimaryDarkText";
                CommonViewSetting.INPUT_FONTCOLOR = "Gray900";
                CommonViewSetting.COMPOSITE_TANDEMLABEL_HEIGHT = 36;
                CommonViewSetting.TITLE_FONTCOLOR = "White";
                if (isTab)
                {
                    CommonViewSetting.LABEL_FONTSIZE = 23;
                    CommonViewSetting.INPUT_FONTSIZE = 21;
                    CommonViewSetting.COMPOSITE_HEIGHT = 52;
                    CommonViewSetting.HEADER_FONTSIZE = 22;
                    CommonViewSetting.TABLE_HEADERHEIGHT = 48;
                    CommonViewSetting.TABLE_ROWHEIGHT = 40;
                    CommonViewSetting.SCREEN_SIZE_THRESHOLD = 600;
                    CommonViewSetting.DIALOG_WIDTH = 500;
                    CommonViewSetting.TITLE_FONTSIZE = 24;
                }
                else
                {
                    CommonViewSetting.LABEL_FONTSIZE = 20;
                    CommonViewSetting.INPUT_FONTSIZE = 18;
                    CommonViewSetting.COMPOSITE_HEIGHT = 47;
                    CommonViewSetting.HEADER_FONTSIZE = 19;
                    CommonViewSetting.TABLE_HEADERHEIGHT = 41;
                    CommonViewSetting.TABLE_ROWHEIGHT = 34;
                    CommonViewSetting.SCREEN_SIZE_THRESHOLD = 400;
                    CommonViewSetting.DIALOG_WIDTH = 350;
                    CommonViewSetting.TITLE_FONTSIZE = 20;
                }
            }
        }
    }

    public class ErrorInfo
    {
        public string? text { get; set; }
        public string? note { get; set; }
    }
}
