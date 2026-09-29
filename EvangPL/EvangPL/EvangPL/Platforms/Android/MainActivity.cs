using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using EvangSol.Mobibrary.DataFeed;

namespace EvangPL
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleInstance, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(
        [Intent.ActionView],
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "com.evangsol.evangmes",
        DataHost = "oauth2callback")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var targetColor = Android.Graphics.Color.ParseColor("#000000");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window?.SetStatusBarColor(targetColor);

                double luminance = (0.299 * targetColor.R + 0.587 * targetColor.G + 0.114 * targetColor.B) / 255;

                var decorView = Window?.DecorView;
                if (decorView != null)
                {
                    if (luminance > 0.5)
                    {
                        decorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightStatusBar;
                    }
                    else
                    {
                        decorView.SystemUiVisibility = (StatusBarVisibility)0;
                    }
                }
            }
            // Handle cold start (app was closed)
            HandleOAuthRedirect(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            // Handle warm start (app was in background)
            HandleOAuthRedirect(intent);
        }

        private void HandleOAuthRedirect(Intent? intent)
        {
            if (intent?.Data?.Scheme == "com.evangsol.evangmes" && intent.Data.Host == "oauth2callback")
            {
                var callbackurl = intent.Data.ToString();
                if (!string.IsNullOrEmpty(callbackurl))
                    OAuthState.CallbackTcs?.TrySetResult(new Uri(callbackurl));
            }
        }
    }
}
