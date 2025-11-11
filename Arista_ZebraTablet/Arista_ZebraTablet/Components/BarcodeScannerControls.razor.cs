using Arista_ZebraTablet.Services;
using Arista_ZebraTablet.Shared.Theme;
using Microsoft.AspNetCore.Components;

namespace Arista_ZebraTablet.Components;

/// <summary>
/// Code-behind for the <c>BarcodeScannerControls</c> overlay component.
/// Renders quick actions to switch camera, toggle torch/flash, and pause/resume scanning
/// while keeping the camera preview visible under a transparent theme.
/// </summary>
public partial class BarcodeScannerControls : ComponentBase
{
    #region Dependencies

    /// <summary>
    /// Mediator service that broadcasts scanner control requests
    /// (switch camera, toggle torch, pause/resume).
    /// </summary>
    [Inject] private BarcodeScannerService ScannerControlService { get; set; } = default!;

    #endregion

    #region State & theme

    /// <summary>
    /// Theme applied to the overlay. Uses a transparent background so
    /// the native camera preview beneath the BlazorWebView remains visible.
    /// </summary>
    /// <remarks>
    /// Ensure both <c>Background</c> (and, if needed, <c>Surface</c>) are transparent.
    /// </remarks>
    private MyCustomTheme myScannerControlsTheme = new();

    /// <summary>
    /// Local UI state mirroring the torch on/off visual toggle.
    /// </summary>
    /// <remarks>
    /// This is for immediate visual feedback. The actual torch state is controlled
    /// by the MAUI page that hosts the camera view.
    /// </remarks>
    private bool flashLightOn;

    /// <summary>
    /// Indicates whether barcode detection is currently paused (for button icon state).
    /// </summary>
    /// <remarks>
    /// The underlying detection state is managed by the MAUI page handling the camera.
    /// </remarks>
    private bool scanPaused;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes the component and enforces a transparent background
    /// so the overlay does not occlude the camera preview.
    /// </summary>
    protected override Task OnInitializedAsync()
    {
        // Transparent ARGB color: 00 (alpha) + 000000 (black) = fully transparent.
        myScannerControlsTheme.PaletteLight.Background = "#00000000";
        return Task.CompletedTask;
    }

    #endregion

    #region Event handlers (invoked by UI)

    /// <summary>
    /// Requests a camera location toggle (front ↔ rear).
    /// The MAUI page subscribes and performs the actual camera switch on the UI thread.
    /// </summary>
    private Task SwitchCamera()
    {
        ScannerControlService.RequestSwitchCamera();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Toggles the device torch and updates the local icon state.
    /// The MAUI page executes the actual torch change.
    /// </summary>
    private Task ToggleTorch()
    {
        ScannerControlService.RequestToggleTorch();
        flashLightOn = !flashLightOn;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pauses or resumes barcode scanning and updates the local icon state.
    /// The MAUI page flips the detection flag on the camera view.
    /// </summary>
    private Task ToggleScanPause()
    {
        ScannerControlService.RequestToggleScanPause();
        scanPaused = !scanPaused;
        return Task.CompletedTask;
    }

    #endregion
}