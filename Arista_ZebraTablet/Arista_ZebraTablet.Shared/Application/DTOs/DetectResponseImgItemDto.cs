using System.Text.Json.Serialization;

namespace Arista_ZebraTablet.Shared.Application.DTOs;

public sealed class DetectResponseImgItemDto
{
    [JsonPropertyName("imageFile")]
    public string? FileName { get; set; }

    [JsonPropertyName("barcodes")]
    public List<DetectResponseBarcodeDto>? Barcodes { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? Error { get; set; }
}