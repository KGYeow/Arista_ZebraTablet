namespace Arista_ZebraTablet.Shared.Application.DTOs;

public sealed class BarcodeDto
{
    public string? Value { get; set; }
    public string? Symbology { get; set; }
    public double? Confidence { get; set; }
}