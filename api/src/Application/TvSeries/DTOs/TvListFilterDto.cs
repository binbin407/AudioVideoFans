using MovieSite.Application.Common;

namespace MovieSite.Application.TvSeries.DTOs;

public sealed record TvListFilterDto(
    string[]? Genres,
    string[]? Regions,
    string? Decade,
    int? Year,
    string? Language,
    decimal? MinScore,
    string Sort = "popularity",
    int Page = 1,
    int PageSize = 24,
    string[]? AirStatus = null
) : ContentListFilter(Genres, Regions, Decade, Year, Language, MinScore, Sort, Page, PageSize);
