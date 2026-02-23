namespace MovieSite.Application.TvSeries.DTOs;

public sealed record SeasonSummaryDto(
    long Id,
    int SeasonNumber,
    string? Name,
    int EpisodeCount,
    DateOnly? FirstAirDate,
    string? PosterCosKey,
    string? Overview,
    decimal? VoteAverage
);
