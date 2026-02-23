namespace MovieSite.Application.Common;

public record ContentListFilter(
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
