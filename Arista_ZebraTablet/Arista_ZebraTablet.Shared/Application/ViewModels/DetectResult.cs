using Arista_ZebraTablet.Shared.Application.DTOs;

namespace Arista_ZebraTablet.Shared.Application.ViewModels;

public sealed class DetectResult
{
    public List<BarcodeDto> Barcodes { get; set; } = [];
}