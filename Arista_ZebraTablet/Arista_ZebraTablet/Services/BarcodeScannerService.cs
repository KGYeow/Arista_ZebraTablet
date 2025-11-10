namespace Arista_ZebraTablet.Services;

/// <summary>
/// Provides a lightweight mediator for barcode-scanner control actions (switch camera, toggle torch/flash,
/// and pause/resume scanning). UI components call the request methods; platform-layer code subscribes
/// to the corresponding events to perform the actual hardware operations.
/// </summary>
public sealed class BarcodeScannerService
{
    /// <summary>
    /// Occurs when a request is made to switch the active camera (e.g., rear ↔ front).
    /// </summary>
    /// <remarks>
    /// Raised by <see cref="RequestSwitchCamera"/>. Subscribers should perform the camera swap and update
    /// any related UI state as needed.
    /// </remarks>
    public event Action? SwitchCameraRequested;

    /// <summary>
    /// Occurs when a request is made to toggle the device torch/flash.
    /// </summary>
    /// <remarks>
    /// Raised by <see cref="RequestToggleTorch"/>. Subscribers should enable or disable the torch depending
    /// on the current state.
    /// </remarks>
    public event Action? ToggleTorchRequested;

    /// <summary>
    /// Occurs when a request is made to pause or resume barcode scanning.
    /// </summary>
    /// <remarks>
    /// Raised by <see cref="RequestToggleScanPause"/>. Subscribers should flip the scanner's paused state
    /// and reflect the status in the UI as appropriate.
    /// </remarks>
    public event Action? ToggleScanPausedRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarcodeScannerService"/> class.
    /// </summary>
    public BarcodeScannerService()
    {
    }

    /// <summary>
    /// Requests that the active camera be switched.
    /// Triggers <see cref="SwitchCameraRequested"/>.
    /// </summary>
    public void RequestSwitchCamera() => SwitchCameraRequested?.Invoke();

    /// <summary>
    /// Requests toggling the device torch/flash.
    /// Triggers <see cref="ToggleTorchRequested"/>.
    /// </summary>
    public void RequestToggleTorch() => ToggleTorchRequested?.Invoke();

    /// <summary>
    /// Requests toggling the scanner's paused state (pause ↔ resume).
    /// Triggers <see cref="ToggleScanPausedRequested"/>.
    /// </summary>
    public void RequestToggleScanPause() => ToggleScanPausedRequested?.Invoke();
}