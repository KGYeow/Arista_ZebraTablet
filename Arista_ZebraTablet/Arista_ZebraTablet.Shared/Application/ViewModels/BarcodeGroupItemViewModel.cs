using Arista_ZebraTablet.Shared.Application.Enums;

namespace Arista_ZebraTablet.Shared.Application.ViewModels;

/// <summary>
/// Represents an barcode group item and its associated barcode detection results.
/// </summary>
public partial class BarcodeGroupItemViewModel
{
    /// <summary>
    /// Unique identifier for the barcode group item.
    /// </summary>
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public BarcodeSource Source { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public ImgItemViewModel? ImgItem { get; set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Barcode detection results associated with this barcode group item.
    /// </summary>
    public DetectResultViewModel DetectResult { get; set; } = null!;
}