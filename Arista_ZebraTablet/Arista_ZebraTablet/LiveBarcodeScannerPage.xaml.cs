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
    private readonly GroupingService _groupingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveBarcodeScannerPage"/> class.
    /// Configures the camera reader, wires control events, and primes autofocus.
    /// </summary>
    /// <param name="scannerControlService">Mediator for UI control actions (switch camera, torch, pause/resume).</param>
    /// <param name="scannerService">Collector/service that stores and manages detected barcode results.</param>
    /// <param name="mode">Classification mode (e.g., <see cref="BarcodeMode.Standard"/> or <see cref="BarcodeMode.Unique"/>).</param>
    public LiveBarcodeScannerPage(BarcodeScannerService scannerControlService, BarcodeDetectorService scannerService, BarcodeMode mode, GroupingService groupingService)
    {
        InitializeComponent();

        _scannerControlService = scannerControlService;
        _scannerService = scannerService;
        _mode = mode;
        _groupingService = groupingService;

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
        CameraView.PropertyChanged += CameraViewPropertyChanged;

        // Sync overlay with current detection state.
        UpdatePauseOverlay(paused: !CameraView.IsDetecting);
    }

    /// <summary>
    /// Stops listening for camera state changes when the page disappears.
    /// </summary>
    /// <remarks>
    /// If this page’s lifetime is shorter than <see cref="BarcodeScannerService"/>,
    /// consider unsubscribing from its events to avoid retaining page instances.
    /// </remarks>
    protected override void OnDisappearing()
    {
        CameraView.PropertyChanged -= CameraViewPropertyChanged;
        base.OnDisappearing();
    }

    /// <summary>
    /// Handles camera property changes and updates the pause overlay when
    /// <see cref="CameraBarcodeReaderView.IsDetecting"/> changes.
    /// </summary>
    /// <param name="sender">The camera view.</param>
    /// <param name="e">Property change arguments.</param>
    private void CameraViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CameraBarcodeReaderView.IsDetecting))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdatePauseOverlay(paused: !CameraView.IsDetecting).ConfigureAwait(false);
            });
        }
    }

    /// <summary>
    /// Shows or hides the pause overlay and toggles the camera preview visibility
    /// to reflect whether scanning is paused.
    /// </summary>
    /// <param name="paused">If <see langword="true"/>, show overlay and hide preview; otherwise hide overlay and show preview.</param>
    /// <returns>A task that completes when the overlay animation finishes.</returns>
    private async Task UpdatePauseOverlay(bool paused)
    {
        await AnimatePauseOverlayAsync(paused);
        CameraView.IsVisible = !paused;
    }

    /// <summary>
    /// Switches between front and rear cameras in response to a request from
    /// <see cref="BarcodeScannerService"/>.
    /// </summary>
    private void OnSwitchCamera()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CameraView.CameraLocation = CameraView.CameraLocation == CameraLocation.Rear
                ? CameraLocation.Front
                : CameraLocation.Rear;
        });
    }

    /// <summary>
    /// Toggles the torch (flash) in response to a request from <see cref="BarcodeScannerService"/>.
    /// </summary>
    private void OnToggleTorch()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CameraView.IsTorchOn = !CameraView.IsTorchOn;
        });
    }

    /// <summary>
    /// Pauses or resumes barcode detection in response to a request from
    /// <see cref="BarcodeScannerService"/>.
    /// </summary>
    private void OnTogglePauseScan()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CameraView.IsDetecting = !CameraView.IsDetecting;
        });
    }

    /// <summary>
    /// Animates the pause overlay in or out.
    /// </summary>
    /// <param name="show">If <see langword="true"/>, fade in; otherwise fade out.</param>
    /// <returns>A task that completes when the animation finishes.</returns>
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

    /// 
    /// <summary>
    /// Processes detected barcodes and adds them to the scanner service,
    /// classifying each item according to the active <see cref="BarcodeMode"/>.
    /// </summary>
    /// <param name="sender">The camera view raising the event.</param>
    /// <param name="e">Detection results containing zero or more barcodes.</param>
    //private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    //{
    //    var barcodeResults = e.Results ?? Enumerable.Empty<BarcodeResult>(); // Get detected barcodes


    //    // Generate a new FrameId for this camera frame

    //    var frameItem = new FrameItemViewModel
    //    {
    //        CapturedTime = DateTime.Now,
    //        DetectResult = new DetectResultViewModel()
    //    };



    //    foreach (var result in barcodeResults)
    //    {
    //        if (string.IsNullOrWhiteSpace(result.Value))
    //            continue; // Skip empty results

    //        // Classify barcode using regex
    //        var category = _mode == BarcodeMode.Standard
    //            ? BarcodeClassifier.Classify(result.Value)
    //            : UniqueBarcodeClassifier.Classify(result.Value);


    //        // Create ScanBarcodeItemViewModel and assign FrameId

    //        frameItem.DetectResult.Barcodes.Add(new ScanBarcodeItemViewModel
    //        {
    //            Id = Guid.NewGuid(),
    //            Value = result.Value,
    //            Category = category,
    //            BarcodeType = result.Format.ToString(),
    //            ScannedTime = DateTime.Now
    //        });



    //        // Add result to scanner service
    //        //_scannerService.Add(result.Value, result.Format.ToString(), category);
    //        _scannerService.Frames.Add(frameItem); // New collection for frames
    //    }
    //}

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var barcodeResults = e.Results?.Where(r => !string.IsNullOrWhiteSpace(r.Value)).ToList();
        if (barcodeResults == null || !barcodeResults.Any()) return;

        // Extract values for comparison
        var newValues = barcodeResults.Select(r => r.Value).OrderBy(v => v).ToList();

        // Check last frame
        var lastFrame = _scannerService.Frames.LastOrDefault();
        if (lastFrame != null)
        {
            var lastValues = lastFrame.DetectResult.Barcodes.Select(b => b.Value).OrderBy(v => v).ToList();

            // If same barcodes, update instead of adding
            if (newValues.SequenceEqual(lastValues))
            {
                // Optional: update timestamp or preview image
                lastFrame.CapturedTime = DateTime.Now;
                return; // Do nothing else
            }
        }

        // Otherwise, create new frame
        var frameItem = new FrameItemViewModel
        {
            CapturedTime = DateTime.Now,
            DetectResult = new DetectResultViewModel()
        };

        foreach (var result in barcodeResults)
        {
            var category = _mode == BarcodeMode.Standard
                ? BarcodeClassifier.Classify(result.Value)
                : UniqueBarcodeClassifier.Classify(result.Value);

            var barcodeItem = new ScanBarcodeItemViewModel
            {
                Id = Guid.NewGuid(),
                Value = result.Value,
                Category = category,
                BarcodeType = result.Format.ToString(),
                ScannedTime = DateTime.Now
            };

            frameItem.DetectResult.Barcodes.Add(barcodeItem);

            // ✅ Notify Razor component
            _scannerService.RaiseScanReceived(barcodeItem);

            // ✅ Update grouping service
            _groupingService.AddBarcode(barcodeItem);
        }

        _scannerService.Frames.Add(frameItem);

    }
}