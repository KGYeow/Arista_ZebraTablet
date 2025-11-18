using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using BarcodeScanning;
using System.ComponentModel;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using BarcodeScanning;
using Arista_ZebraTablet.Shared.Application.Regex;

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
    private readonly BarcodeDetectorService _detectorService;
    private readonly BarcodeScannerService _scannerControlService;
    private readonly BarcodeMode _mode;
    private static readonly List<string> PreferredCategoryOrder = new()
    {
        "ASY", "ASY-OTL", "Serial Number", "MAC Address", "Deviation", "PCA"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveBarcodeScannerPage"/> class.
    /// Configures the camera reader, wires control events, and primes autofocus.
    /// </summary>
    /// <param name="scannerControlService">Mediator for UI control actions (switch camera, torch, pause/resume).</param>
    /// <param name="detectorService">Collector/service that stores and manages detected barcode results.</param>
    /// <param name="mode">Classification mode (e.g., <see cref="BarcodeMode.Standard"/> or <see cref="BarcodeMode.Unique"/>).</param>
    public LiveBarcodeScannerPage(BarcodeScannerService scannerControlService, BarcodeDetectorService detectorService, BarcodeMode mode)
    {
        InitializeComponent();

        _scannerControlService = scannerControlService;
        _detectorService = detectorService;
        _mode = mode;

        // Configure barcode detection behavior.
        //Camera = new BarcodeReaderOptions
        //{
        //    AutoRotate = true,
        //    Multiple = true,
        //    TryHarder = true,
        //    Formats = (BarcodeForma,
        //};

        // Subscribe to control events published by the mediator service.
        _scannerControlService.SwitchCameraRequested += OnSwitchCamera;
        _scannerControlService.ToggleTorchRequested += OnToggleTorch;
        _scannerControlService.ToggleScanPausedRequested += OnTogglePauseScan;

        // Prime the camera's autofocus on startup.
        Camera.Focus();
    }

    /// <summary>
    /// Starts listening for camera state changes and aligns the pause overlay with
    /// the current detection state when the page appears.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        //Camera.PropertyChanged += CameraViewPropertyChanged;

        // Sync overlay with current detection state.
        //UpdatePauseOverlay(paused: !CameraView.IsDetecting);

        // Ask camera permission (recommended by the library)
        await Methods.AskForRequiredPermissionAsync(); // from BarcodeScanning.Native.Maui
        Camera.CameraEnabled = true;   // s

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
        //CameraView.PropertyChanged -= CameraViewPropertyChanged;
        base.OnDisappearing();

    }

    /// <summary>
    /// Handles camera property changes and updates the pause overlay when
    /// <see cref="CameraBarcodeReaderView.IsDetecting"/> changes.
    /// </summary>
    /// <param name="sender">The camera view.</param>
    /// <param name="e">Property change arguments.</param>
    //private void CameraViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName == nameof(CameraBarcodeReaderView.IsDetecting))
    //    {
    //        MainThread.BeginInvokeOnMainThread(() =>
    //        {
    //            UpdatePauseOverlay(paused: Camera.IsDetecting).ConfigureAwait(false);
    //        });
    //    }
    //}

    /// <summary>
    /// Shows or hides the pause overlay and toggles the camera preview visibility
    /// to reflect whether scanning is paused.
    /// </summary>
    /// <param name="paused">If <see langword="true"/>, show overlay and hide preview; otherwise hide overlay and show preview.</param>
    /// <returns>A task that completes when the overlay animation finishes.</returns>
    private async Task UpdatePauseOverlay(bool paused)
    {
        await AnimatePauseOverlayAsync(paused);
        Camera.PauseScanning = true;
        PauseOverlay.IsVisible = true;

    }

    /// <summary>
    /// Switches between front and rear cameras in response to a request from
    /// <see cref="BarcodeScannerService"/>.
    /// </summary>
    private void OnSwitchCamera()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {

            Camera.CameraFacing = Camera.CameraFacing == CameraFacing.Back
                ? CameraFacing.Front
                : CameraFacing.Back; // toggle

        });
    }

    /// <summary>
    /// Toggles the torch (flash) in response to a request from <see cref="BarcodeScannerService"/>.
    /// </summary>
    private void OnToggleTorch()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Camera.TorchOn = !Camera.TorchOn;
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

            Camera.PauseScanning = !Camera.PauseScanning;
            PauseOverlay.IsVisible = Camera.PauseScanning;

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

    private void OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        var barcodeResults = e.BarcodeResults?.Where(r => !string.IsNullOrWhiteSpace(r.RawValue)).ToList();
        if (barcodeResults == null || !barcodeResults.Any()) return;

        var currentGroup = _detectorService.CurrentGroup ?? new BarcodeGroupItemViewModel
        {
            Id = Guid.NewGuid(),
            Name = "Scanned Barcode Group",
            Source = BarcodeSource.Camera,
            Timestamp = DateTime.Now
        };

        foreach (var result in barcodeResults)
        {
            var category = _mode == BarcodeMode.Standard
                ? BarcodeClassifier.Classify(result.RawValue)
                : UniqueBarcodeClassifier.Classify(result.RawValue);

            var barcodeItem = new ScanBarcodeItemViewModel
            {
                Id = Guid.NewGuid(),
                Value = result.RawValue,
                Category = category,
                BarcodeType = result.BarcodeFormat.ToString(),
                ScannedTime = DateTime.Now
            };

            // ✅ Replace if category exists, else add
            var existingItem = currentGroup.Barcodes.FirstOrDefault(b => b.Category == category);
            if (existingItem != null)
            {
                // Replace existing barcode
                var index = currentGroup.Barcodes.IndexOf(existingItem);
                currentGroup.Barcodes[index] = barcodeItem;
            }
            else
            {
                // Add new barcode
                currentGroup.Barcodes.Add(barcodeItem);
            }

            // Notify Razor component
            _detectorService.RaiseScanReceived(barcodeItem);
        }

        currentGroup.Timestamp = DateTime.Now;
        currentGroup.Barcodes = currentGroup.Barcodes.OrderBy(b => PreferredCategoryOrder.IndexOf(b.Category) >= 0 ? PreferredCategoryOrder.IndexOf(b.Category) : int.MaxValue).ToList();
        _detectorService.CurrentGroup = currentGroup;
    }
}