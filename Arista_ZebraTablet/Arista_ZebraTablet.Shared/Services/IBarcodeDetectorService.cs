using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;

namespace Arista_ZebraTablet.Shared.Services
{
    public interface IBarcodeDetectorService
    {
        /// <summary>
        /// Navigates to the barcode scanner page.
        /// </summary>
        Task NavigateToScannerAsync(BarcodeMode mode);

        /// <summary>
        /// Navigates to the zebra tablet barcode scanner page.
        /// </summary>
        Task NavigateToZebraScannerAsync();

        /// <summary>
        /// Decodes barcodes from an image byte array.
        /// </summary>
        List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes, BarcodeMode mode);
    }
}