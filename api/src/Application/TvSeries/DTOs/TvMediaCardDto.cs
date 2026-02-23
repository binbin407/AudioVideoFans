using MovieSite.Application.Common;

namespace MovieSite.Application.TvSeries.DTOs;

public sealed record TvMediaCardDto(
    long Id,
    string ContentType,
    string TitleCn,
    int? Year,
    string? PosterCosKey,
    decimal? DoubanScore,
    string[] Genres,
    string? AirStatus
) : MediaCardDto(Id, ContentType, TitleCn, Year, PosterCosKey, DoubanScore, Genres);
