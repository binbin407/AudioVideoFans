using System.Text.Json.Serialization;

namespace MovieSite.Application.Common;

public sealed record MediaCardDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title_cn")] string TitleCn,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("poster_cos_key")] string? PosterCosKey,
    [property: JsonPropertyName("douban_score")] decimal? DoubanScore,
    [property: JsonPropertyName("genres")] string[] Genres
);
