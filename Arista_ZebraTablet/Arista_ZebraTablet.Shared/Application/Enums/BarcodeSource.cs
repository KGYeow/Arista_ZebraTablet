namespace Arista_ZebraTablet.Shared.Application.Enums;

/// <summary>
/// Specifies the source from which barcodes are obtained.
/// </summary>
public enum BarcodeSource
{
    /// <summary>
    /// Barcodes detected using the device camera (live scanning).
    /// </summary>
    Camera,

    /// <summary>
    /// Barcodes detected from uploaded images.
    /// </summary>
    Upload
}