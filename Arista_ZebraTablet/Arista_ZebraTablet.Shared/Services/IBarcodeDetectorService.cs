using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;

namespace Arista_ZebraTablet.Shared.Services;

/// <summary>
/// Defines operations for barcode detection, state management, and navigation within the application.
/// </summary>
/// <remarks>
/// This service acts as a mediator between UI components and barcode processing logic.
/// It stores barcode groups from both sources (Image Upload and Scanner), manages reorder scope,
/// and provides methods for decoding barcodes and navigating to the scanner page.
/// </remarks>
public interface IBarcodeDetectorService
{
    #region State Properties

    /// <summary>
    /// Gets or sets the list of barcode groups representing detection results from both sources:
    /// Image Upload and Scanner.
    /// </summary>
    List<BarcodeGroupItemViewModel> BarcodeGroups { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the barcode group selected for reordering.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Guid.Empty"/> to indicate that all barcode groups should be included in the reorder operation.
    /// </remarks>
    Guid? SelectedBarcodeGroupId { get; set; }

    /// <summary>
    /// Gets or sets the currently active barcode source (Upload or Camera).
    /// Used to determine which results are displayed and processed.
    /// </summary>
    BarcodeSource SelectedBarcodeSource { get; set; }

    #endregion

    #region Navigation Methods

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

    #endregion

    #region Barcode Decoding

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

    #endregion
}