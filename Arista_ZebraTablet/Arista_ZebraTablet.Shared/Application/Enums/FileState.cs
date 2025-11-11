namespace Arista_ZebraTablet.Shared.Application.Enums;

/// <summary>
/// Represents the processing state of an uploaded file during barcode detection.
/// </summary>
/// <remarks>
/// Used to track and display the current status of each image file in the UI.
/// </remarks>
public enum FileState
{
    /// <summary>
    /// The file is ready to be processed but has not yet started detection.
    /// </summary>
    Ready,

    /// <summary>
    /// The file is currently undergoing barcode detection.
    /// </summary>
    Detecting,

    /// <summary>
    /// Barcode detection has completed successfully for the file.
    /// </summary>
    Done,

    /// <summary>
    /// An error occurred during barcode detection for the file.
    /// </summary>
    Error
}