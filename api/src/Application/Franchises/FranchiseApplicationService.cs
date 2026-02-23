using System.Text.Json.Serialization;
using MovieSite.Domain.Entities;
using SqlSugar;

namespace MovieSite.Application.Franchises;

public sealed class FranchiseApplicationService(ISqlSugarClient db)
{
    public async Task<FranchiseDetailDto?> GetFranchiseDetailAsync(long id, CancellationToken ct = default)
    {
        var franchise = await db.Queryable<Franchise>()
            .FirstAsync(x => x.Id == id && x.DeletedAt == null);

        if (franchise is null)
        {
            return null;
        }

        var movies = await db.Queryable<Movie>()
            .Where(x => x.FranchiseId == id && x.DeletedAt == null && x.Status == "published")
            .OrderBy("franchise_order ASC NULLS LAST")
            .ToListAsync();

        var items = movies.Select(x => new FranchiseMovieDto(
            x.Id,
            x.TitleCn,
            ExtractYear(x.ReleaseDates),
            x.PosterCosKey,
            x.DoubanScore,
            x.FranchiseOrder)).ToList();

        return new FranchiseDetailDto(
            franchise.Id,
            franchise.NameCn,
            franchise.NameEn,
            franchise.Overview,
            franchise.PosterCosKey,
            items.Count,
            items);
    }

    private static int? ExtractYear(List<MovieSite.Domain.ValueObjects.ReleaseDate> releaseDates)
    {
        if (releaseDates.Count == 0)
        {
            return null;
        }

        var cnYear = releaseDates
            .Where(x => x.Date.HasValue && string.Equals(x.Region, "CN", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Date)
            .Select(x => x.Date!.Value.Year)
            .FirstOrDefault();

        if (cnYear != 0)
        {
            return cnYear;
        }

        var firstYear = releaseDates
            .Where(x => x.Date.HasValue)
            .OrderBy(x => x.Date)
            .Select(x => x.Date!.Value.Year)
            .FirstOrDefault();

        return firstYear == 0 ? null : firstYear;
    }
}

public sealed record FranchiseDetailDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name_cn")] string NameCn,
    [property: JsonPropertyName("name_en")] string? NameEn,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("poster_cos_key")] string? PosterCosKey,
    [property: JsonPropertyName("total_movies")] int TotalMovies,
    [property: JsonPropertyName("movies")] List<FranchiseMovieDto> Movies
);

public sealed record FranchiseMovieDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title_cn")] string TitleCn,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("poster_cos_key")] string? PosterCosKey,
    [property: JsonPropertyName("douban_score")] decimal? DoubanScore,
    [property: JsonPropertyName("sequence_number")] int? SequenceNumber
);
