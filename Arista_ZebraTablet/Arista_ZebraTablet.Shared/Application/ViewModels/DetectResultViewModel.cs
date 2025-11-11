namespace Arista_ZebraTablet.Shared.Application.ViewModels;

/// <summary>
/// Represents the result of barcode detection from any source,
/// including uploaded images or live camera scanning.
/// </summary>
/// <remarks>
/// This view model aggregates all detected barcodes for a single detection operation.
/// It is used by both image-processing workflows and real-time scanning components.
/// </remarks>
public sealed class DetectResultViewModel
{
    /// <summary>
    /// A collection of detected barcodes, each represented by <see cref="ScanBarcodeItemViewModel"/>.
    /// </summary>
    /// <remarks>
    /// The list may be empty if no barcodes were detected.
    /// </remarks>
    public List<ScanBarcodeItemViewModel> Barcodes { get; set; } = [];
}