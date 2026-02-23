namespace MovieSite.Application.TvSeries.DTOs;

public sealed record NextEpisodeInfoDto(
    DateOnly? AirDate,
    string? Title,
    int? SeasonNumber,
    int? EpisodeNumber
);

public sealed record CreditPersonDto(
    long PersonId,
    string NameCn,
    string? AvatarCosKey,
    string Role,
    string? Department,
    string? CharacterName
);

public sealed record VideoDto(
    long Id,
    string? Title,
    string Url,
    string Type,
    DateOnly? PublishedAt
);

public sealed record TvSeriesDetailDto(
    long Id,
    string TitleCn,
    string? TitleOriginal,
    string[] TitleAliases,
    string? Synopsis,
    string[] Genres,
    string[] Region,
    string[] Language,
    DateOnly? FirstAirDate,
    DateOnly? LastAirDate,
    string? AirStatus,
    int NumberOfSeasons,
    int NumberOfEpisodes,
    decimal? DoubanScore,
    decimal? ImdbScore,
    string? PosterCosKey,
    string? BackdropCosKey,
    string[] ExtraPosters,
    string[] ExtraBackdrops,
    List<SeasonSummaryDto> SeasonsSummary,
    NextEpisodeInfoDto? NextEpisodeInfo,
    List<CreditPersonDto> Directors,
    List<CreditPersonDto> Cast,
    List<VideoDto> Videos,
    List<TvMediaCardDto> Similar
);
