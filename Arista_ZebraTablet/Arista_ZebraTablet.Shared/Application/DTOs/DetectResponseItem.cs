using System.Text.Json.Serialization;

namespace Arista_ZebraTablet.Shared.Application.DTOs;

public sealed class DetectResponseItem
{
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("barcodes")]
    public List<DetectResponseBarcode>? Barcodes { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}