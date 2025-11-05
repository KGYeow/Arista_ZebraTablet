using Arista_ZebraTablet.Services; // Import app-level services
using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using System.Collections.ObjectModel;

namespace Arista_ZebraTablet;

public partial class ZebraBarcodeScannerPage : ContentPage
{
    private readonly BarcodeDetectorService _scannerService;    // Service to store and manage scan results
    public ObservableCollection<ScanBarcodeItemViewModel> Results => _scannerService.Results;

#if ANDROID
    private Android.Content.BroadcastReceiver? _decodeRx;
#endif

    public ZebraBarcodeScannerPage(BarcodeDetectorService scannerService)
    {
        InitializeComponent();
        _scannerService = scannerService;
        BindingContext = _scannerService; // Bind to observable collection

        Console.WriteLine($"BindingContext set: {BindingContext?.GetType().Name}");

        _scannerService.ScanReceived += OnScanReceived;


    }
    private void OnScanReceived(object? sender, ScanBarcodeItemViewModel e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Barcode Scanned",
                $"Value: {e.Value}\nType: {e.BarcodeType}\nCategory: {e.Category}",
                "OK");

            // Optional: force UI refresh
            BindingContext = null;
            BindingContext = _scannerService;
            Console.WriteLine("[DEBUG] UI rebinding triggered after scan.");
        });
    }

#if ANDROID
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // 1) Register a dynamic receiver for the decode channel (e.g., arista.zebra.SCAN)
        _decodeRx = new AnonymousReceiver((ctx, intent) =>
        {
            if (intent?.Action != DataWedgeClient.ScanAction) return;
            

            // Multi-barcode payload
            if (intent.HasExtra("com.symbol.datawedge.barcodes"))
            {
                var list = intent.GetParcelableArrayListExtra("com.symbol.datawedge.barcodes");
                if (list is null) return;
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var p in list)
                    {
                        if (p is Android.OS.Bundle b)
                        {
                            var value = b.GetString("com.symbol.datawedge.data_string");
                            Console.WriteLine($"Received simulated barcode: {value}"); //tester
                            var sym   = b.GetString("com.symbol.datawedge.label_type") ?? "Unknown";
                            if (!string.IsNullOrWhiteSpace(value))
                                _scannerService.Add(value, sym, BarcodeClassifier.Classify(value));
                        }
                    }
                });
                return;
            }

            // Single decode extras
            var data =
                intent.GetStringExtra("com.symbol.datawedge.data_string") ??
                intent.GetStringExtra("com.motorolasolutions.emdk.datawedge.data_string");
            var symbology =
                intent.GetStringExtra("com.symbol.datawedge.label_type") ??
                intent.GetStringExtra("com.motorolasolutions.emdk.datawedge.label_type") ?? "Unknown";

            if (string.IsNullOrWhiteSpace(data)) return;

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                _scannerService.Add(data!, symbology, BarcodeClassifier.Classify(data!)));
        });

        var f = new Android.Content.IntentFilter(DataWedgeClient.ScanAction);
        f.AddCategory(Android.Content.Intent.CategoryDefault);
        //Android.App.Application.Context.RegisterReceiver(_decodeRx, f); 
        Android.App.Application.Context.RegisterReceiver(_decodeRx, f, Android.Content.ReceiverFlags.Exported);

        // 2) Switch to Zebra DataWedge profile (so the right settings are active)
        Android.App.Application.Context.SendBroadcast(DataWedgeClient.BuildSwitchProfileIntent("ARISTAZebraTablet"));
    }

    private void OnStartScanClicked(object sender, EventArgs e)
    {
        var intent = DataWedgeClient.BuildApiIntent("START_SCANNING");
        Android.App.Application.Context.SendBroadcast(intent);
    }

    private void OnStopScanClicked(object sender, EventArgs e)
    {
        var intent = DataWedgeClient.BuildApiIntent("STOP_SCANNING");
        Android.App.Application.Context.SendBroadcast(intent);
    }

    // Optional: emit a fake decode to prove UI updates even without the scanner
    private void OnSimulateClicked(object sender, EventArgs e)
    {
        var intent = new Android.Content.Intent(DataWedgeClient.ScanAction);
        intent.PutExtra("com.symbol.datawedge.data_string", "SIM-123456");
        intent.PutExtra("com.symbol.datawedge.label_type",  "SIM_FORMAT");
        intent.AddCategory(Android.Content.Intent.CategoryDefault);
        Android.App.Application.Context.SendBroadcast(intent);
    }

    protected override void OnDisappearing()
    {
        if (_decodeRx is not null)
        {
            Android.App.Application.Context.UnregisterReceiver(_decodeRx);
            _decodeRx = null;
        }
        base.OnDisappearing();
    }


    // Tiny helper to avoid writing a standalone class file
    private sealed class AnonymousReceiver : Android.Content.BroadcastReceiver
    {
        private readonly Action<Android.Content.Context?, Android.Content.Intent?> _onReceive;
        public AnonymousReceiver(Action<Android.Content.Context?, Android.Content.Intent?> onReceive) => _onReceive = onReceive;
        public override void OnReceive(Android.Content.Context? context, Android.Content.Intent? intent) => _onReceive(context, intent);
    }
#endif
}