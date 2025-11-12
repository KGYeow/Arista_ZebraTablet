using Arista_ZebraTablet.Shared.Application.Enums;

namespace Arista_ZebraTablet.Shared.Application.ViewModels;


public partial class BarcodeGroupItemViewModel
{
    /// <summary>
    /// Unique identifier for the barcode group.
    /// </summary>
    public Guid Id { get; set; }


    public string GroupName { get; set; } = null;

    public FileState State { get; set; }


    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Barcode detection results associated with this image.
    /// </summary>
    public DetectResultViewModel DetectResult { get; set; } = null!;
}