using System.Text.Json.Serialization;

namespace Arista_ZebraTablet.Shared.Application.DTOs;

public sealed class DetectResponseBarcodeDto
{
    [JsonPropertyName("decodedValue")]
    public string? Value { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("barcodeType")]
    public string? Symbology { get; set; }

    [JsonPropertyName("confidenceScore")]
    public double? ConfidenceScore { get; set; }
}