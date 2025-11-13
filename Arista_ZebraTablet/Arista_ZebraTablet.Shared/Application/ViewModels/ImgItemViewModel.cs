using Arista_ZebraTablet.Shared.Application.Enums;

namespace Arista_ZebraTablet.Shared.Application.ViewModels;

/// <summary>
/// Represents an uploaded image and its associated barcode detection results.
/// </summary>
public partial class ImgItemViewModel
{
    /// <summary>
    /// Original file name of the uploaded image.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// MIME type of the image (e.g., image/jpeg, image/png).
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Raw image bytes.
    /// </summary>
    public byte[]? Bytes { get; set; }

    /// <summary>
    /// ThumbnailUrl
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Base64-encoded data URL for previewing the image in the UI.
    /// </summary>
    public string? PreviewDataUrl { get; set; }

    /// <summary>
    /// Current processing state of the image (e.g., Ready, Detecting, Done, Error).
    /// </summary>
    public FileState State { get; set; }
}