using Arista_ZebraTablet.Services; // Import app-level services
using Arista_ZebraTablet.Shared.Application.Enums;
using ZXing.Net.Maui; // Barcode detection library for MAUI

namespace Arista_ZebraTablet;

/// <summary>
/// LiveBarcodeScannerPage Component
/// </summary>
/// <remarks>
/// This page handles live camera barcode scanning in MAUI.
/// It continuously adds detected barcodes to a list.
/// </remarks>
public partial class LiveBarcodeScannerPage : ContentPage
{
    private readonly BarcodeDetectorService _scannerService; // Service to store and manage scan results
    private readonly BarcodeMode _mode; // Categorizing mode passed from home page (Standard or Unique)

    public LiveBarcodeScannerPage(BarcodeDetectorService scannerService, BarcodeMode mode)
    {
        InitializeComponent();              // Load XAML UI
        _scannerService = scannerService;   // Assign service for storing results
        _mode = mode;                       // Assign mode for classification
        BindingContext = this;              // expose IsDetectingFromCamera to XAML binding

        CameraView.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = true,
            TryHarder = true,
            Formats = BarcodeFormats.All
        };

        // Trigger autofocus when the page is initialized
        CameraView.AutoFocus(); // This calls the AutoFocus method internally

        // Simulate continuous autofocus every 3 seconds
        //Device.StartTimer(TimeSpan.FromSeconds(3), () =>
        //{
        //    CameraView.AutoFocus();
        //    return true; // Keep running
        //});
    }

    /// <summary>
    /// Barcode Detection Handler: called when barcodes are detected by the camera
    /// </summary>
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var barcodeResults = e.Results ?? Enumerable.Empty<BarcodeResult>(); // Get detected barcodes

        foreach (var result in barcodeResults)
        {
            if (string.IsNullOrWhiteSpace(result.Value))
                continue; // Skip empty results

            // Classify barcode using regex
            var category = _mode == BarcodeMode.Standard
                ? BarcodeClassifier.Classify(result.Value)
                : UniqueBarcodeClassifier.Classify(result.Value);

            // Add result to scanner service
            _scannerService.Add(result.Value, result.Format.ToString(), category);
        }
    }
}