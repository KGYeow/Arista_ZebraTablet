using System.Text.Json.Serialization;

namespace Arista_ZebraTablet.Shared.Application.DTOs;

public sealed class DetectResponseEnvelopeDto
{
    [JsonPropertyName("items")]
    public List<DetectResponseImgItemDto>? Items { get; set; }
}