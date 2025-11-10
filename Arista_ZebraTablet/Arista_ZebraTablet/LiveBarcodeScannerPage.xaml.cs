using Arista_ZebraTablet.Services; // Import app-level services
using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
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

    // Animation control
    private CancellationTokenSource? _scanAnimCts;
    private bool _animStarted;

    public LiveBarcodeScannerPage(BarcodeDetectorService scannerService, BarcodeMode mode)
    {
        InitializeComponent();              // Load XAML UI
        _scannerService = scannerService;   // Assign service for storing results
        _mode = mode;                       // Assign mode for classification

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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Start scan-line animation once size is known
        ScanBox.SizeChanged += ScanBox_SizeChanged;
        TryStartScanAnimation();
    }

    protected override void OnDisappearing()
    {
        ScanBox.SizeChanged -= ScanBox_SizeChanged;
        StopScanAnimation();

        base.OnDisappearing();
    }

    private void ScanBox_SizeChanged(object? sender, EventArgs e) => TryStartScanAnimation();

    private void TryStartScanAnimation()
    {
        if (_animStarted) return;
        if (ScanBox.Width <= 0 || ScanBox.Height <= 0) return;

        _animStarted = true;
        _scanAnimCts = new CancellationTokenSource();
        _ = RunScanLineAnimationAsync(_scanAnimCts.Token);
    }

    private void StopScanAnimation()
    {
        _scanAnimCts?.Cancel();
        _scanAnimCts?.Dispose();
        _scanAnimCts = null;
        _animStarted = false;

        // Reset position (optional)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ScanLine.TranslationY = 0;
            ScanLineGlow.TranslationY = 0;
        });
    }

    private async Task RunScanLineAnimationAsync(CancellationToken token)
    {
        await Task.Yield();
        const uint travelMs = 1200;
        const uint pauseMs = 120;

        async Task MoveToY(double y)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.WhenAll(
                    ScanLine.TranslateTo(0, y, travelMs, Easing.SinInOut),
                    ScanLineGlow.TranslateTo(0, y, travelMs, Easing.SinInOut)
                );
            });
        }

        while (!token.IsCancellationRequested)
        {
            var maxY = Math.Max(0, ScanBox.Height - ScanLine.Height);
            await MoveToY(maxY);
            await Task.Delay((int)pauseMs, token);
            await MoveToY(0);
            await Task.Delay((int)pauseMs, token);
        }
    }

    /// <summary>
    /// Barcode Detection Handler: called when barcodes are detected by the camera
    /// </summary>
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var barcodeResults = e.Results ?? Enumerable.Empty<BarcodeResult>(); // Get detected barcodes


        // Generate a new FrameId for this camera frame
        var frameId = Guid.NewGuid();


        foreach (var result in barcodeResults)
        {
            if (string.IsNullOrWhiteSpace(result.Value))
                continue; // Skip empty results

            // Classify barcode using regex
            var category = _mode == BarcodeMode.Standard
                ? BarcodeClassifier.Classify(result.Value)
                : UniqueBarcodeClassifier.Classify(result.Value);


            // Create ScanBarcodeItemViewModel and assign FrameId
            var barcodeItem = new ScanBarcodeItemViewModel
            {
                Id = Guid.NewGuid(),
                Value = result.Value,
                Category = category,
                BarcodeType = result.Format.ToString(),
                ScannedTime = DateTime.Now,
                FrameId = frameId // ✅ Important for grouping
            };


            // Add result to scanner service
            _scannerService.Add(result.Value, result.Format.ToString(), category);
        }
    }
}