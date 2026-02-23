using System.Text.Json.Serialization;

namespace MovieSite.Domain.ValueObjects;

public sealed record ReleaseDate
{
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    [JsonPropertyName("date")]
    public DateOnly? Date { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}