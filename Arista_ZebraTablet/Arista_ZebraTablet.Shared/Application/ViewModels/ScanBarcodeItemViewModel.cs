namespace Arista_ZebraTablet.Shared.Application.ViewModels;

public sealed class ScanBarcodeItemViewModel
{
    public string Value { get; set; } = null!;
    public string Category { get; set; } = null!;
    public DateTime ScannedTime { get; set; }
    public string BarcodeType { get; set; } = null!;
};