using Arista_ZebraTablet.Shared.Application.Enums;
using Arista_ZebraTablet.Shared.Application.ViewModels;

namespace Arista_ZebraTablet.Shared.Services
{
    public interface IBarcodeDetectorService
    {
        List<ImgItemViewModel> UploadedImages { get; set; }
        Guid? SelectedImageId { get; set; }

        /// <summary>
        /// Navigates to the barcode scanner page.
        /// </summary>
        Task NavigateToScannerAsync(BarcodeMode mode);

        /// <summary>
        /// Decodes barcodes from an image byte array.
        /// </summary>
        List<ScanBarcodeItemViewModel> DecodeFromImage(byte[] imageBytes, BarcodeMode mode);
    }
}