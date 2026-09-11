using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
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
