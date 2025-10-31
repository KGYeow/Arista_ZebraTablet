#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Arista_ZebraTablet.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel; // MainThread

namespace Arista_ZebraTablet;

// Exported=true is required on Android 12+
[BroadcastReceiver(Enabled = true, Exported = true)]
// Keep a filter with DEFAULT
[IntentFilter(new[] { DataWedgeClient.ScanAction }, Categories = new[] { Intent.CategoryDefault })]
public class DataWedgeReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        //base.OnReceive(context, intent);
        throw new Exception($"TESTING: {intent?.Action}");

        if (intent?.Action != DataWedgeClient.ScanAction) return;

        // Resolve the SAME singleton service used by your app and Blazor component
        var svc = MauiApplication.Current?.Services?.GetService<BarcodeDetectorService>();
        if (svc is null) return;

        // Local helper to add one result safely on UI thread
        void AddOne(string value, string sym)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var category = BarcodeClassifier.Classify(value);
            svc.Add(value, string.IsNullOrWhiteSpace(sym) ? "Unknown" : sym, category);
        }

        // Multi-barcode payload (e.g., SimulScan or DW multi-decode)
        if (intent.HasExtra("com.symbol.datawedge.barcodes"))
        {
            var list = intent.GetParcelableArrayListExtra("com.symbol.datawedge.barcodes");
            if (list is not null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var p in list)
                    {
                        if (p is Bundle b)
                        {
                            var value = b.GetString("com.symbol.datawedge.data_string");
                            var sym = b.GetString("com.symbol.datawedge.label_type") ?? "Unknown";
                            AddOne(value!, sym);
                        }
                    }
                });
            }
            return;
        }

        // Single decode extras (new and legacy keys)
        var data =
            intent.GetStringExtra("com.symbol.datawedge.data_string")
            ?? intent.GetStringExtra("com.motorolasolutions.emdk.datawedge.data_string");

        var symbology =
            intent.GetStringExtra("com.symbol.datawedge.label_type")
            ?? intent.GetStringExtra("com.motorolasolutions.emdk.datawedge.label_type")
            ?? "Unknown";

        if (string.IsNullOrWhiteSpace(data)) return;

        MainThread.BeginInvokeOnMainThread(() => AddOne(data!, symbology));
    }
}
#endif