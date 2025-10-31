#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Arista_ZebraTablet
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Handle a decode that launched the app cold (Start Activity delivery)
            if (Intent?.Action == DataWedgeClient.ScanAction)
                HandleDecodeIntent(Intent!);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent?.Action == DataWedgeClient.ScanAction)
                HandleDecodeIntent(intent);
        }

        private void HandleDecodeIntent(Intent intent)
        {
            var data = intent.GetStringExtra("com.symbol.datawedge.data_string");
            var sym = intent.GetStringExtra("com.symbol.datawedge.label_type") ?? "Unknown";

            var svc = MauiApplication.Current?.Services?.GetService<Arista_ZebraTablet.Services.BarcodeDetectorService>();
            if (!string.IsNullOrWhiteSpace(data))
                svc?.Add(data, sym, BarcodeClassifier.Classify(data));
        }
    }
}
#endif