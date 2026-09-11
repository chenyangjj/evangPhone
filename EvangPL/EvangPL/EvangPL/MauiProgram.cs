using CommunityToolkit.Maui;
using EvangSol.Mobibrary;
using EvangSol.Mobibrary.DataFeed;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using System.Reflection;

namespace EvangPL
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseEvangMobibrary()
                .UseMauiCommunityToolkit();

            builder.ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddAndroid(android => android
                    .OnCreate((activity, bundle) =>
                    {
                        //For most MAUI apps, SingleTop is often the best choice
                        activity.Intent?.AddFlags(Android.Content.ActivityFlags.SingleTop);
                    }));
            });
#endif

            LocalStorage.configFileName = "EvangPL.config.json";
            builder.AddConfiguration(Assembly.GetExecutingAssembly(), LocalStorage.configFileName,
                "EvangPL.DefaultStyles.json");

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
