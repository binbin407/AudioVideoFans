namespace MovieSite.Application.TvSeries.DTOs;

public sealed record EpisodeDto(
    long Id,
    int EpisodeNumber,
    string? Name,
    DateOnly? AirDate,
    int? DurationMin,
    string? StillCosKey,
    string? Overview,
    decimal? VoteAverage
);

public sealed record SeasonDetailDto(
    long Id,
    long SeriesId,
    string SeriesTitleCn,
    int SeasonNumber,
    string? Name,
    int EpisodeCount,
    DateOnly? FirstAirDate,
    string? PosterCosKey,
    string? Overview,
    decimal? VoteAverage,
    List<EpisodeDto> Episodes,
    int? PrevSeasonNumber,
    int? NextSeasonNumber
);
