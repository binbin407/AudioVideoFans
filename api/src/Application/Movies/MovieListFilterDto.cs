namespace MovieSite.Application.Movies;

public sealed record MovieListFilterDto(
    string[]? Genres,
    string[]? Regions,
    string? Decade,
    int? Year,
    string? Language,
    decimal? MinScore,
    string Sort = "popularity",
    int Page = 1,
    int PageSize = 24
);
