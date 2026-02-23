using System.Text.Json.Serialization;
using MovieSite.Application.Common;
using MovieSite.Domain.ValueObjects;

namespace MovieSite.Application.Movies;

public sealed record CreditPersonDto(
    [property: JsonPropertyName("person_id")] long PersonId,
    [property: JsonPropertyName("name_cn")] string NameCn,
    [property: JsonPropertyName("name_en")] string? NameEn,
    [property: JsonPropertyName("avatar_cos_key")] string? AvatarCosKey,
    [property: JsonPropertyName("character_name")] string? CharacterName,
    [property: JsonPropertyName("display_order")] int DisplayOrder
);

public sealed record AwardItemDto(
    [property: JsonPropertyName("event_name")] string EventName,
    [property: JsonPropertyName("edition_number")] int EditionNumber,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("is_winner")] bool IsWinner
);

public sealed record VideoDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("published_at")] DateOnly? PublishedAt
);

public sealed record FranchiseRef(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name_cn")] string NameCn,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("total")] int Total
);

public sealed record MovieDetailDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title_cn")] string TitleCn,
    [property: JsonPropertyName("title_original")] string? TitleOriginal,
    [property: JsonPropertyName("title_aliases")] string[] TitleAliases,
    [property: JsonPropertyName("tagline")] string? Tagline,
    [property: JsonPropertyName("synopsis")] string? Synopsis,
    [property: JsonPropertyName("genres")] string[] Genres,
    [property: JsonPropertyName("region")] string[] Region,
    [property: JsonPropertyName("language")] string[] Language,
    [property: JsonPropertyName("release_dates")] List<ReleaseDate> ReleaseDates,
    [property: JsonPropertyName("durations")] List<Duration> Durations,
    [property: JsonPropertyName("douban_score")] decimal? DoubanScore,
    [property: JsonPropertyName("douban_rating_count")] int? DoubanRatingCount,
    [property: JsonPropertyName("douban_rating_dist")] DoubanRatingDist? DoubanRatingDist,
    [property: JsonPropertyName("imdb_score")] decimal? ImdbScore,
    [property: JsonPropertyName("imdb_id")] string? ImdbId,
    [property: JsonPropertyName("poster_cos_key")] string? PosterCosKey,
    [property: JsonPropertyName("backdrop_cos_key")] string? BackdropCosKey,
    [property: JsonPropertyName("extra_backdrops")] string[] ExtraBackdrops,
    [property: JsonPropertyName("extra_posters")] string[] ExtraPosters,
    [property: JsonPropertyName("production_companies")] string[] ProductionCompanies,
    [property: JsonPropertyName("distributors")] string[] Distributors,
    [property: JsonPropertyName("franchise")] FranchiseRef? Franchise,
    [property: JsonPropertyName("cast")] List<CreditPersonDto> Cast,
    [property: JsonPropertyName("directors")] List<CreditPersonDto> Directors,
    [property: JsonPropertyName("awards")] List<AwardItemDto> Awards,
    [property: JsonPropertyName("videos")] List<VideoDto> Videos,
    [property: JsonPropertyName("similar")] List<MediaCardDto> Similar
);

public sealed record CreditsResponseDto(
    [property: JsonPropertyName("directors")] List<CreditPersonDto> Directors,
    [property: JsonPropertyName("writers")] List<CreditPersonDto> Writers,
    [property: JsonPropertyName("cast")] List<CreditPersonDto> Cast,
    [property: JsonPropertyName("producers")] List<CreditPersonDto> Producers,
    [property: JsonPropertyName("others")] List<CreditPersonDto> Others
);
