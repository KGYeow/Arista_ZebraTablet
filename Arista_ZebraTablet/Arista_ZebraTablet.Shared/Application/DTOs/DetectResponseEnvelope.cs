using System.Text.Json.Serialization;

namespace Arista_ZebraTablet.Shared.Application.DTOs;

public sealed class DetectResponseEnvelope
{
    [JsonPropertyName("items")]
    public List<DetectResponseItem>? Items { get; set; }
}