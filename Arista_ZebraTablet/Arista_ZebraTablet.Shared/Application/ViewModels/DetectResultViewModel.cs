//using Arista_ZebraTablet.Shared.Application.DTOs;

namespace Arista_ZebraTablet.Shared.Application.ViewModels;

public sealed class DetectResultViewModel
{
    public List<ScanBarcodeItemViewModel> Barcodes { get; set; } = [];
}