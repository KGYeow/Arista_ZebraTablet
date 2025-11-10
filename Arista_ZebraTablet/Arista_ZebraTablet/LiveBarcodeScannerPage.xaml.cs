using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Application.Enums;
using System.ComponentModel;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Arista_ZebraTablet.Shared.Application.ViewModels;

namespace Arista_ZebraTablet;

/// <summary>
/// A page that hosts live camera barcode scanning using <c>ZXing.Net.MAUI</c>.
/// </summary>
/// <remarks>
/// <para>
/// The page configures the camera for continuous barcode detection, pushes results to
/// <see cref="BarcodeDetectorService"/>, and reacts to user actions (switch camera, toggle torch,
/// pause/resume detection) published through <see cref="BarcodeScannerService"/>.
/// </para>
/// <para>
/// UI updates are marshaled to the UI thread using <see cref="MainThread.BeginInvokeOnMainThread(System.Action)"/>.
/// The <see cref="CameraView"/> control is declared in the associated XAML file.
/// </para>
/// </remarks>
public partial class LiveBarcodeScannerPage : ContentPage
{
    private readonly BarcodeDetectorService _scannerService;
    private readonly BarcodeScannerService _scannerControlService;
    private readonly BarcodeMode _mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveBarcodeScannerPage"/> class.
    /// Configures the camera reader, wires control events, and primes autofocus.
    /// </summary>
    public LiveBarcodeScannerPage(BarcodeScannerService scannerControlService, BarcodeDetectorService scannerService, BarcodeMode mode)
    {
        InitializeComponent();

        _scannerControlService = scannerControlService;
        _scannerService = scannerService;
        _mode = mode;

        // Configure barcode detection behavior.
        CameraView.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = true,
            TryHarder = true,
            Formats = BarcodeFormats.All
        };

        // Subscribe to control events published by the mediator service.
        _scannerControlService.SwitchCameraRequested += OnSwitchCamera;
        _scannerControlService.ToggleTorchRequested += OnToggleTorch;
        _scannerControlService.ToggleScanPausedRequested += OnTogglePauseScan;

        // Prime the camera's autofocus on startup.
        CameraView.AutoFocus();
    }

    /// <summary>
    /// Starts listening for camera state changes and aligns the pause overlay with
    /// the current detection state when the page appears.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CameraView.PropertyChanged += CameraView_PropertyChanged;

        // Initialize overlay to current state
        UpdatePauseOverlay(paused: !CameraView.IsDetecting);
    }

    protected override void OnDisappearing()
    {
        CameraView.PropertyChanged -= CameraView_PropertyChanged;
        base.OnDisappearing();
    }

    private void CameraView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CameraBarcodeReaderView.IsDetecting))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdatePauseOverlay(paused: !CameraView.IsDetecting);
            });
        }
    }

    /// <summary>
    /// Shows a dark cover and optionally hides the camera preview when paused.
    /// </summary>
    private async Task UpdatePauseOverlay(bool paused)
    {
        await AnimatePauseOverlayAsync(paused);
        CameraView.IsVisible = !paused;
    }

    private void OnSwitchCamera()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CameraView.CameraLocation = CameraView.CameraLocation == CameraLocation.Rear
                ? CameraLocation.Front
                : CameraLocation.Rear;
        });
    }

    private void OnToggleTorch()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CameraView.IsTorchOn = !CameraView.IsTorchOn;
        });
    }

    private void OnTogglePauseScan()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CameraView.IsDetecting = !CameraView.IsDetecting;
        });
    }

    private async Task AnimatePauseOverlayAsync(bool show)
    {
        if (show)
        {
            PauseOverlay.Opacity = 0;
            PauseOverlay.IsVisible = true;
            await PauseOverlay.FadeTo(1, 180, Easing.CubicOut);
        }
        else
        {
            await PauseOverlay.FadeTo(0, 160, Easing.CubicIn);
            PauseOverlay.IsVisible = false;
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