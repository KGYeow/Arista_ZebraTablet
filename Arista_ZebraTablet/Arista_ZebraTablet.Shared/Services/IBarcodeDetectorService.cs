using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;

namespace Arista_ZebraTablet.Shared.Services;

/// <summary>
/// Defines operations for barcode detection and navigation within the application.
/// </summary>
/// <remarks>
/// This service acts as a mediator between UI components and barcode processing logic.
/// It stores uploaded image data, manages the selected image for reordering, and provides
/// methods for decoding barcodes and navigating to the scanner page.
/// </remarks>
public interface IBarcodeDetectorService
{
    /// <summary>
    /// Gets or sets the list of uploaded images along with their detection results.
    /// </summary>
    /// <remarks>
    /// This collection is shared across pages to maintain state between navigation.
    /// </remarks>
    List<ImgItemViewModel> UploadedImages { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the image selected for barcode reordering.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Guid.Empty"/> to indicate that all images should be included in the reorder operation.
    /// </remarks>
    Guid? SelectedImageId { get; set; }

    /// <summary>
    /// Navigates to the live barcode scanner page for the specified mode.
    /// </summary>
    /// <param name="mode">
    /// The barcode categorization mode to use during scanning (e.g., <see cref="BarcodeMode.Standard"/> or <see cref="BarcodeMode.Unique"/>).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous navigation operation.
    /// </returns>
    Task NavigateToScannerAsync(BarcodeMode mode);

    /// <summary>
    /// Decodes barcodes from the provided image byte array using the specified mode.
    /// </summary>
    /// <param name="imageBytes">The raw image data as a byte array.</param>
    /// <param name="mode">
    /// The barcode categorization mode to apply during decoding.
    /// </param>
    /// <returns>
    /// A list of <see cref="ScanBarcodeItemViewModel"/> representing the detected barcodes and their metadata.
    /// </returns>
    List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes, BarcodeMode mode);
}