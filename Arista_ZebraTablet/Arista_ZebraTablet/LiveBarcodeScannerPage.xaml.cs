//using Arista_ZebraTablet.Services; // Import app-level services (e.g., BarcodeDetectorService)
//using Arista_ZebraTablet.Shared.Application.ViewModels; // Import shared view models
//using ZXing.Net.Maui; // Barcode detection library for MAUI
//using ZXing.Net.Maui.Controls; // UI controls for barcode scanning

//namespace Arista_ZebraTablet;

///// ===============================
///// 🔹 BarcodeScannerPage Component
///// ===============================
///// This page handles live camera barcode scanning in MAUI.
///// It supports two modes:
///// 1. Single-shot mode: returns one barcode and closes.
///// 2. List mode: continuously adds detected barcodes to a list.
//public partial class LiveBarcodeScannerPage : ContentPage
//{
//    private readonly BarcodeDetectorService _scannerService; // Service to store and manage scan results
//    private readonly TaskCompletionSource<string?>? _singleShotTcs; // Used in single-shot mode to return one result

//    /// ===============================
//    /// 🔸 Constructor for List Mode
//    /// ===============================
//    public LiveBarcodeScannerPage(BarcodeDetectorService scannerService)
//    {
//        InitializeComponent();            // Load XAML UI
//        _scannerService = scannerService; // Assign service for storing results

//        CameraView.Options = new BarcodeReaderOptions
//        {
//            AutoRotate = true,
//            Multiple = true,
//            TryHarder = true,
//            Formats = BarcodeFormats.All
//        };

//        // Trigger autofocus when the page is initialized
//        CameraView.AutoFocus(); // This calls the AutoFocus method internally

//    }

//    /// ===============================
//    /// 🔸 Constructor for Single-Shot Mode
//    /// ===============================
//    public LiveBarcodeScannerPage(TaskCompletionSource<string?> singleShotTcs, BarcodeDetectorService scannerService)
//    {
//        InitializeComponent();              // Load XAML UI
//        _singleShotTcs = singleShotTcs;     // Assign task completion source for returning result
//        _scannerService = scannerService;   // Assign service for storing results

//        CameraView.Options = new BarcodeReaderOptions
//        {
//            AutoRotate = true,
//            Multiple = true,
//            TryHarder = true,
//            Formats = BarcodeFormats.All
//        };

//        // Trigger autofocus when the page is initialized
//        CameraView.AutoFocus(); // This calls the AutoFocus method internally

//    }

//    /// ===============================
//    /// 🔸 Lifecycle: OnAppearing
//    /// ===============================
//    // Starts barcode detection when the page appears
//    protected override void OnAppearing()
//    {
//        base.OnAppearing();             // Call base method
//        CameraView.IsDetecting = true;  // Enable barcode detection
//    }

//    /// ===============================
//    /// 🔸 Lifecycle: OnDisappearing
//    /// ===============================
//    // Stops barcode detection when the page disappears
//    protected override void OnDisappearing()
//    {
//        CameraView.IsDetecting = false; // Disable barcode detection
//        base.OnDisappearing();          // Call base method
//    }

//    /// ===============================
//    /// 🔸 Complete Single-Shot Flow
//    /// ===============================
//    // Called when a barcode is detected in single-shot mode
//    private async void CompleteSingleShotAsync(string value)
//    {
//        if (_singleShotTcs is null) return;             // If not in single-shot mode, exit
//        if (!_singleShotTcs.Task.IsCompleted)
//            _singleShotTcs.TrySetResult(value);         // Return the scanned value

//        await Navigation.PopModalAsync(animated: true); // Close the scanner page
//    }

//    /// ===============================
//    /// 🔸 Barcode Detection Handler
//    /// ===============================
//    // Called when barcodes are detected by the camera
//    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
//    {
//        var list = e.Results ?? Enumerable.Empty<BarcodeResult>(); // Get detected barcodes

//        foreach (var r in list)
//        {
//            if (string.IsNullOrWhiteSpace(r.Value))
//                continue; // Skip empty results

//            var category = BarcodeClassifier.Classify(r.Value); // Classify barcode using regex

//            if (_singleShotTcs is not null)
//            {
//                // In single-shot mode, return result and exit
//                MainThread.BeginInvokeOnMainThread(() => CompleteSingleShotAsync($"{r.Value} | {category}"));
//                return;
//            }

//            // In list mode, add result to scanner service
//            _scannerService.Add(r.Value, r.Format.ToString(), category);
//        }
//    }

//    // Turns barcode detection on/off
//    private void OnToggleDetecting(object sender, EventArgs e)
//        => CameraView.IsDetecting = !CameraView.IsDetecting;

//    // Clears all scanned results
//    private void OnClear(object sender, EventArgs e)
//        => _scannerService.Clear();
//}

using Arista_ZebraTablet.Services; // Import app-level services (e.g., BarcodeDetectorService)
using Arista_ZebraTablet.Shared.Application.ViewModels; // Import shared view models
using Arista_ZebraTablet.Shared.Application.Enums; // For BarcodeMode enum
using ZXing.Net.Maui; // Barcode detection library for MAUI
using ZXing.Net.Maui.Controls; // UI controls for barcode scanning

namespace Arista_ZebraTablet;

/// ===============================
/// 🔹 BarcodeScannerPage Component
/// ===============================
/// This page handles live camera barcode scanning in MAUI.
/// It supports two modes:
/// 1. Single-shot mode: returns one barcode and closes.
/// 2. List mode: continuously adds detected barcodes to a list.
public partial class LiveBarcodeScannerPage : ContentPage
{
    private readonly BarcodeDetectorService _scannerService; // Service to store and manage scan results
    private readonly TaskCompletionSource<string?>? _singleShotTcs; // Used in single-shot mode to return one result
    private readonly BarcodeMode _mode; // Mode passed from home page (Standard or Unique)

    /// ===============================
    /// 🔸 Constructor for List Mode
    /// ===============================
    public LiveBarcodeScannerPage(BarcodeDetectorService scannerService, BarcodeMode mode)
    {
        InitializeComponent();            // Load XAML UI
        _scannerService = scannerService; // Assign service for storing results
        _mode = mode;                     // Assign mode for classification

        CameraView.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = true,
            TryHarder = true,
            Formats = BarcodeFormats.All
        };

        CameraView.AutoFocus(); // Trigger autofocus

        // Simulate continuous autofocus every 3 seconds
        Device.StartTimer(TimeSpan.FromSeconds(3), () =>
        {
            CameraView.AutoFocus();
            return true; // Keep running
        });


    }

    /// ===============================
    /// 🔸 Constructor for Single-Shot Mode
    /// ===============================
    public LiveBarcodeScannerPage(TaskCompletionSource<string?> singleShotTcs, BarcodeDetectorService scannerService, BarcodeMode mode)
    {
        InitializeComponent();              // Load XAML UI
        _singleShotTcs = singleShotTcs;     // Assign task completion source for returning result
        _scannerService = scannerService;   // Assign service for storing results
        _mode = mode;                       // Assign mode for classification

        CameraView.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = true,
            TryHarder = true,
            Formats = BarcodeFormats.All
        };

        CameraView.AutoFocus(); // Trigger autofocus

        // Simulate continuous autofocus every 3 seconds
        Device.StartTimer(TimeSpan.FromSeconds(3), () =>
        {
            CameraView.AutoFocus();
            return true; // Keep running
        });

    }

    /// ===============================
    /// 🔸 Lifecycle: OnAppearing
    /// ===============================
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CameraView.IsDetecting = true; // Enable barcode detection
    }

    /// ===============================
    /// 🔸 Lifecycle: OnDisappearing
    /// ===============================
    protected override void OnDisappearing()
    {
        CameraView.IsDetecting = false; // Disable barcode detection
        base.OnDisappearing();
    }

    /// ===============================
    /// 🔸 Complete Single-Shot Flow
    /// ===============================
    private async void CompleteSingleShotAsync(string value)
    {
        if (_singleShotTcs is null) return;
        if (!_singleShotTcs.Task.IsCompleted)
            _singleShotTcs.TrySetResult(value);

        await Navigation.PopModalAsync(animated: true); // Close scanner page
    }

    /// ===============================
    /// 🔸 Barcode Detection Handler
    /// ===============================
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var list = e.Results ?? Enumerable.Empty<BarcodeResult>();

        foreach (var r in list)
        {
            if (string.IsNullOrWhiteSpace(r.Value))
                continue;

            // Mode-based classification
            var category = _mode switch
            {
                BarcodeMode.Standard => BarcodeClassifier.Classify(r.Value),
                BarcodeMode.Unique => UniqueBarcodeClassifier.Classify(r.Value),
                _ => "Unknown"
            };

            if (_singleShotTcs is not null)
            {
                MainThread.BeginInvokeOnMainThread(() => CompleteSingleShotAsync($"{r.Value} | {category}"));
                return;
            }

            _scannerService.Add(r.Value, r.Format.ToString(), category);
        }
    }

    private void OnToggleDetecting(object sender, EventArgs e)
        => CameraView.IsDetecting = !CameraView.IsDetecting;

    private void OnClear(object sender, EventArgs e)
        => _scannerService.Clear();
}