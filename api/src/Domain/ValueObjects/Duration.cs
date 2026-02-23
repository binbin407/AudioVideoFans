using System.Text.Json.Serialization;

namespace MovieSite.Domain.ValueObjects;

public sealed record Duration
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("minutes")]
    public int Minutes { get; init; }
}