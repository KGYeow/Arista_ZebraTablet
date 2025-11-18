using Arista_ZebraTablet.Shared.Application.Enums;

namespace Arista_ZebraTablet.Shared.Application.ViewModels;

/// <summary>
/// Represents an image used for barcode detection, including metadata and UI preview details.
/// </summary>
public partial class ImgItemViewModel
{
    /// <summary>
    /// The original file name of the image.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// The MIME type of the image (e.g., "image/jpeg", "image/png").
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The raw image data as a byte array.
    /// </summary>
    public byte[]? Bytes { get; set; }

    /// <summary>
    /// A URL pointing to the thumbnail version of the image for quick display.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// A Base64-encoded data URL for rendering the full-size image preview in the UI.
    /// </summary>
    public string? PreviewDataUrl { get; set; }

    /// <summary>
    /// The current processing state of the image (e.g., Ready, Detecting, Done, Error).
    /// </summary>
    public FileState State { get; set; }
}