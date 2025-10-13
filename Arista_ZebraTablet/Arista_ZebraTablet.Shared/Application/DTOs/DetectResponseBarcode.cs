using System.Text.Json.Serialization;

namespace Arista_ZebraTablet.Shared.Application.DTOs;

public sealed class DetectResponseBarcode
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("symbology")]
    public string? Symbology { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}