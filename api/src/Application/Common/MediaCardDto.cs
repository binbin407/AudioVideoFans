namespace MovieSite.Application.Common;

public sealed record MediaCardDto(
    long Id,
    string ContentType,
    string TitleCn,
    int? Year,
    string? PosterCosKey,
    decimal? DoubanScore,
    string[] Genres
);
