using Arista_ZebraTablet.Shared.Application.Enums;

namespace Arista_ZebraTablet.Shared.Application.ViewModels
{
    /// <summary>
    /// Represents a grouped barcode item that combines barcode detection results
    /// from either image uploads or live scanner input.
    /// </summary>
    public partial class BarcodeGroupItemViewModel
    {
        /// <summary>
        /// Unique identifier for the barcode group item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display name for the group (e.g., image file name or scanner session name).
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Indicates the source of the barcode group (Image Upload or Scanner).
        /// </summary>
        public BarcodeSource Source { get; set; }

        /// <summary>
        /// Timestamp when the group was created or last updated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Optional error message if barcode processing failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Associated image item if the source is Image Upload.
        /// </summary>
        public ImgItemViewModel ImgItem { get; set; } = null!;

        /// <summary>
        /// Collection of barcodes detected or scanned for this group.
        /// </summary>
        public List<ScanBarcodeItemViewModel> Barcodes { get; set; } = new();
    }
}