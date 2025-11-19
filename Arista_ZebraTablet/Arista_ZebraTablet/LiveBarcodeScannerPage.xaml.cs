using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;
using BarcodeScanning;
using System.ComponentModel;
using BarcodeFormats = BarcodeScanning.BarcodeFormats;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Arista_ZebraTablet.Shared.Application.Regex;

namespace Arista_ZebraTablet
{
    /// <summary>
    /// Live barcode-scanning page backed by <c>ZXing.Net.MAUI</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Configures the camera for continuous barcode detection, funnels results
    /// into <see cref="BarcodeDetectorService"/>, and reacts to UI control actions
    /// (switch camera, toggle torch, pause/resume) published via <see cref="BarcodeScannerService"/>.
    /// </para>
    /// <para>
    /// UI updates are marshalled to the UI thread using
    /// <see cref="MainThread.BeginInvokeOnMainThread(System.Action)"/>.
    /// The <see cref="CameraView"/> control is declared in the associated XAML file.
    /// </para>
    /// </remarks>
    public partial class LiveBarcodeScannerPage : ContentPage
    {
        #region Dependencies 
        private readonly BarcodeDetectorService _detectorService;
        private readonly BarcodeScannerService _scannerControlService;
        private readonly BarcodeMode _mode;

        /// <summary>
        /// Preferred ordering of categories when presenting grouped results.
        /// </summary>
        private static readonly List<string> PreferredCategoryOrder = new()
        {
            "ASY", "ASY-OTL", "Serial Number", "MAC Address", "Deviation", "PCA"
        };

        // Loading state for detection processing (for a spinner/progress indicator)
        private bool _isDetectionProcessing; // true while a batch of detections is being processed
        private int? _detectionProgress;     // 0–100; null when not processing
        #endregion

        #region Constructor & camera configuration 
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

            // ===== Native scanner configuration =====
            // Narrow formats to what you use most; add/remove as needed.
            Camera.BarcodeSymbologies = BarcodeFormats.All;      // flags enum of supported symbologies
            Camera.ViewfinderMode = true;                        // detect only what’s visible in preview
            Camera.AimMode = false;                              // set true to detect center-only
            Camera.PoolingInterval = 250;                        // pool window (ms) for multi-barcode grouping
            Camera.CaptureQuality = CaptureQuality.Highest;      // speed/quality tradeoff
            Camera.TapToFocusEnabled = true;                     // allow user tap to focus

            // Subscribe to control events published by the mediator service.
            _scannerControlService.SwitchCameraRequested += OnSwitchCamera;
            _scannerControlService.ToggleTorchRequested += OnToggleTorch;
            _scannerControlService.ToggleScanPausedRequested += OnTogglePauseScan;

            // Prime the camera's autofocus on startup.
            Camera.Focus();
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Starts listening for camera state changes and aligns the pause overlay with
        /// the current detection state when the page appears.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Camera.PropertyChanged += CameraPropertyChanged;

            // Ask camera permission (recommended by the library)
            await Methods.AskForRequiredPermissionAsync(); // from BarcodeScanning.Native.Maui

            Camera.CameraEnabled = true; // enable camera feed

            // Sync overlay with current detection state.
            await UpdatePauseOverlay(paused: Camera.PauseScanning);
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
            Camera.PropertyChanged -= CameraPropertyChanged;
            base.OnDisappearing();
        }
        #endregion

        #region Camera property changes & pause overlay
        /// <summary>
        /// Handles camera property changes and updates the pause overlay when
        /// <see cref="CameraView.PauseScanning"/> changes.
        /// </summary>
        private async void CameraPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BarcodeScanning.CameraView.PauseScanning))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    UpdatePauseOverlay(paused: Camera.PauseScanning));
            }
        }

        /// <summary>
        /// Shows or hides the pause overlay and toggles the camera preview visibility
        /// to reflect whether scanning is paused.
        /// </summary>
        /// <param name="paused">If <see langword="true"/>, show overlay and hide preview; otherwise hide overlay and show preview.</param>
        private async Task UpdatePauseOverlay(bool paused)
        {
            await AnimatePauseOverlayAsync(paused);
            Camera.IsVisible = !paused;
        }

        /// <summary>
        /// Animates the pause overlay in or out.
        /// </summary>
        /// <param name="show">If <see langword="true"/>, fade in; otherwise fade out.</param>
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
        #endregion

        #region Properties (bindable state)
        /// <summary>
        /// Logical flag indicating whether detection processing is underway.
        /// </summary>
        public bool IsDetectionProcessing
        {
            get => _isDetectionProcessing;
            set { _isDetectionProcessing = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 0–100 progress percentage while processing a detection batch; <c>null</c> when idle.
        /// </summary>
        public int? DetectionProgress
        {
            get => _detectionProgress;
            set { _detectionProgress = value; OnPropertyChanged(); }
        }
        #endregion

        #region Detection pipeline
        /// <summary>
        /// Processes detected barcodes and adds them to the scanner service,
        /// classifying each item according to the active <see cref="BarcodeMode"/>.
        /// </summary>
        /// <param name="sender">The camera view raising the event.</param>
        /// <param name="e">Detection results containing zero or more barcodes.</param>
        private async void OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
        {
            // Filter out empty results
            var barcodeResults = e.BarcodeResults?.Where(r => !string.IsNullOrWhiteSpace(r.RawValue)).ToList();
            if (barcodeResults == null || !barcodeResults.Any()) return;

            // === Show loader & pause camera during UI update ===
            IsDetectionProcessing = true;
            DetectionProgress = 0;

            // Let the UI render the spinner before heavy work
            await Task.Yield();

            // Create or reuse the current group
            var currentGroup = _detectorService.CurrentGroup ?? new BarcodeGroupItemViewModel
            {
                Id = Guid.NewGuid(),
                Name = "Scanned Barcode Group",
                Source = BarcodeSource.Camera,
                Timestamp = DateTime.Now
            };

            for (int i = 0; i < barcodeResults.Count; i++)
            {
                var result = barcodeResults[i];

                // Choose classifier by mode
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
                    // Replace existing barcode to keep the latest value per category
                    var index = currentGroup.Barcodes.IndexOf(existingItem);
                    currentGroup.Barcodes[index] = barcodeItem;
                }
                else
                {
                    // Add new barcode
                    currentGroup.Barcodes.Add(barcodeItem);
                }

                // Notify Razor component/UI subscribers
                _detectorService.RaiseScanReceived(barcodeItem);

                // Update the batch progress (0–100)
                DetectionProgress = (int)Math.Round(((i + 1) * 100.0) / barcodeResults.Count);
                await Task.Yield();
            }

            // Finalize group
            currentGroup.Timestamp = DateTime.Now;
            currentGroup.Barcodes = currentGroup.Barcodes
                .OrderBy(b => PreferredCategoryOrder.IndexOf(b.Category) >= 0 ? PreferredCategoryOrder.IndexOf(b.Category) : int.MaxValue)
                .ToList();

            _detectorService.CurrentGroup = currentGroup;

            // === Hide loader & resume scanning (or leave paused if you need a "confirm" step) ===
            IsDetectionProcessing = false;
            DetectionProgress = null;
        }
        #endregion

        #region User controls & gestures (switch/torch/pause + pinch-to-zoom
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
            });
        }

        // Gestures: pinch-to-zoom state + handler
        // Fields to track pinch state
        double _startZoomFactor = -1;
        bool _pinchActive;

        /// <summary>
        /// Reset internal pinch tracking state.
        /// </summary>
        void ResetPinchState()
        {
            _pinchActive = false;
            _startZoomFactor = -1;
        }

        /// <summary>
        /// Pinch gesture handler bound from XAML.
        /// </summary>
        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            // Safety: camera may not be ready yet
            if (Camera == null || !Camera.CameraEnabled)
                return;

            switch (e.Status)
            {
                case GestureStatus.Started:
                    _pinchActive = true;
                    // Take the current zoom as baseline at gesture start
                    // If CurrentZoomFactor is not yet reported (could be -1), fall back to MinZoomFactor
                    var current = Camera.CurrentZoomFactor > 0 ? Camera.CurrentZoomFactor : Camera.MinZoomFactor;
                    _startZoomFactor = current;
                    break;

                case GestureStatus.Running:
                    if (!_pinchActive || _startZoomFactor < 0)
                        return;

                    // e.Scale is relative since start; >1 = zoom in, <1 = zoom out
                    var desired = (float)(_startZoomFactor * e.Scale);

                    // Clamp to camera limits
                    var min = Camera.MinZoomFactor > 0 ? Camera.MinZoomFactor : 1f;
                    var max = Camera.MaxZoomFactor > 0 ? Camera.MaxZoomFactor : Math.Max(1f, desired);
                    desired = Math.Clamp(desired, min, max);

                    // Apply to the camera (native control will update CurrentZoomFactor)
                    Camera.RequestZoomFactor = desired;
                    break;

                case GestureStatus.Canceled:
                case GestureStatus.Completed:
                    ResetPinchState();
                    break;
            }
        }
        #endregion

    }
}
