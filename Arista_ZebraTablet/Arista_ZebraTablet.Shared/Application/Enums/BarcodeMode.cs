namespace Arista_ZebraTablet.Shared.Application.Enums;

/// <summary>
/// Specifies the classification mode used for barcode detection and categorization.
/// </summary>
/// <remarks>
/// This enum is used to determine how scanned barcode values are interpreted and grouped.
/// </remarks>
public enum BarcodeMode
{
    /// <summary>
    /// Standard mode applies general classification rules to barcode values.
    /// </summary>
    Standard,

    /// <summary>
    /// Unique mode applies stricter or specialized classification rules to ensure uniqueness.
    /// </summary>
    Unique
}