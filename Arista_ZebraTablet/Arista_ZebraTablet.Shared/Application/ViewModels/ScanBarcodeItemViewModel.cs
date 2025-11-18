namespace Arista_ZebraTablet.Shared.Application.ViewModels;

/// <summary>
/// Represents a single scanned barcode item, including its value, category, and metadata.
/// </summary>
public sealed class ScanBarcodeItemViewModel
{
    /// <summary>
    /// Unique identifier for the scanned barcode item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The raw value of the scanned barcode (e.g., alphanumeric string).
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// The category assigned to the barcode (e.g., ASY, PCA, Serial Number).
    /// </summary>
    public string Category { get; set; } = null!;

    /// <summary>
    /// The timestamp when the barcode was scanned.
    /// </summary>
    public DateTime ScannedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// The type of barcode detected (e.g., QR_CODE, CODE_128).
    /// </summary>
    public string BarcodeType { get; set; } = null!;
}