using System.Text.Json.Serialization;

namespace MovieSite.Domain.ValueObjects;

public sealed record NextEpisodeInfo
{
    [JsonPropertyName("air_date")]
    public DateOnly? AirDate { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("season_number")]
    public int? SeasonNumber { get; init; }

    [JsonPropertyName("episode_number")]
    public int? EpisodeNumber { get; init; }
}