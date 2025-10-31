using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;

namespace Arista_ZebraTablet.Shared.Services
{
    public interface IBarcodeDetectorService
    {

        /// <summary>
        /// Navigates to the barcode scanner page.
        /// </summary>
        // Navigation methods
        Task NavigateToScannerAsync(BarcodeMode mode);
        //Task NavigateToScannerAsync();

        /// <summary>
        /// Decodes barcodes from an image byte array.
        /// </summary>
        List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes);

        // New overload
        List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes, BarcodeMode mode);

    }
}