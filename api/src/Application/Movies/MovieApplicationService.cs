using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MovieSite.Application.Common;
using MovieSite.Domain;
using MovieSite.Domain.Entities;
using SqlSugar;

namespace MovieSite.Application.Movies;

public sealed class MovieApplicationService(ISqlSugarClient db, IRedisCache redis)
{
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<PagedResponse<MediaCardDto>> GetMovieListAsync(MovieListFilterDto filter, CancellationToken ct = default)
    {
        var normalized = Normalize(filter);
        var cacheKey = CacheKeys.MovieList(ComputeFilterHash(normalized));

        var cached = await redis.GetAsync<PagedResponse<MediaCardDto>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var whereClauses = new List<string>
        {
            "deleted_at IS NULL",
            "status = 'published'"
        };
        var parameters = new Dictionary<string, object>();

        if (normalized.Genres is { Length: > 0 })
        {
            whereClauses.Add(BuildArrayOverlap("genres", "genres"));
            parameters["genres"] = normalized.Genres;
        }

        if (normalized.Regions is { Length: > 0 })
        {
            whereClauses.Add(BuildArrayOverlap("region", "regions"));
            parameters["regions"] = normalized.Regions;
        }

        if (!string.IsNullOrWhiteSpace(normalized.Language))
        {
            whereClauses.Add("language @> ARRAY[@language]::text[]");
            parameters["language"] = normalized.Language;
        }

        if (normalized.MinScore.HasValue)
        {
            whereClauses.Add("douban_score >= @minScore");
            parameters["minScore"] = normalized.MinScore.Value;
        }

        if (normalized.Year.HasValue)
        {
            whereClauses.Add(@"EXISTS (
                SELECT 1 FROM jsonb_array_elements(release_dates) rd
                WHERE rd->>'date' IS NOT NULL AND (rd->>'date')::date BETWEEN @yearStart AND @yearEnd
            )");
            parameters["yearStart"] = new DateTime(normalized.Year.Value, 1, 1);
            parameters["yearEnd"] = new DateTime(normalized.Year.Value, 12, 31);
        }
        else if (!string.IsNullOrWhiteSpace(normalized.Decade))
        {
            var (start, end) = DecadeToYearRange(normalized.Decade);
            whereClauses.Add(@"EXISTS (
                SELECT 1 FROM jsonb_array_elements(release_dates) rd
                WHERE rd->>'date' IS NOT NULL AND (rd->>'date')::date BETWEEN @decadeStart AND @decadeEnd
            )");
            parameters["decadeStart"] = new DateOnly(start, 1, 1);
            parameters["decadeEnd"] = new DateOnly(end, 12, 31);
        }

        var whereSql = string.Join(" AND ", whereClauses);

        var orderBySql = normalized.Sort switch
        {
            "douban_score" => "douban_score DESC NULLS LAST",
            "release_date" => "(SELECT MIN((rd->>'date')::date) FROM jsonb_array_elements(release_dates) rd WHERE rd->>'date' IS NOT NULL) DESC NULLS LAST",
            _ => "popularity DESC"
        };

        var countSql = $"SELECT COUNT(1) FROM movies WHERE {whereSql}";

        var totalObj = await db.Ado.GetScalarAsync(countSql, parameters);
        var total = Convert.ToInt32(totalObj);

        var offset = (normalized.Page - 1) * normalized.PageSize;
        var dataSql = $@"
            SELECT *
            FROM movies
            WHERE {whereSql}
            ORDER BY {orderBySql}
            LIMIT @pageSize OFFSET @offset";

        parameters["pageSize"] = normalized.PageSize;
        parameters["offset"] = offset;

        var rows = await db.Ado.SqlQueryAsync<Movie>(dataSql, parameters);
        var cards = rows.Select(ToMediaCard).ToList();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalized.PageSize);
        var result = new PagedResponse<MediaCardDto>(
            cards,
            new PaginationDto(normalized.Page, normalized.PageSize, total, totalPages)
        );

        await redis.SetAsync(cacheKey, result, ListCacheTtl);
        return result;
    }

    private static MovieListFilterDto Normalize(MovieListFilterDto filter)
    {
        var genres = filter.Genres?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var regions = filter.Regions?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            < 1 => 24,
            > 100 => 100,
            _ => filter.PageSize
        };

        var sort = filter.Sort switch
        {
            "douban_score" => "douban_score",
            "release_date" => "release_date",
            _ => "popularity"
        };

        return filter with
        {
            Genres = genres,
            Regions = regions,
            Language = string.IsNullOrWhiteSpace(filter.Language) ? null : filter.Language.Trim(),
            Decade = string.IsNullOrWhiteSpace(filter.Decade) ? null : filter.Decade.Trim(),
            Sort = sort,
            Page = page,
            PageSize = pageSize
        };
    }

    private static string ComputeFilterHash(MovieListFilterDto filter)
    {
        var json = JsonSerializer.Serialize(filter);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildArrayOverlap(string columnName, string paramName)
    {
        return $"{columnName} && @{paramName}::text[]";
    }

    private static (int Start, int End) DecadeToYearRange(string decade)
    {
        return decade switch
        {
            "2020s" => (2020, 2029),
            "2010s" => (2010, 2019),
            "2000s" => (2000, 2009),
            "90s" => (1990, 1999),
            "earlier" => (1888, 1989),
            _ => throw new ArgumentException($"Unknown decade: {decade}")
        };
    }

    private static MediaCardDto ToMediaCard(Movie movie)
    {
        var year = movie.ReleaseDates
            .Where(x => x.Date.HasValue)
            .OrderBy(x => x.Date)
            .Select(x => x.Date!.Value.Year)
            .FirstOrDefault();

        return new MediaCardDto(
            movie.Id,
            movie.TitleCn,
            year == 0 ? null : year,
            movie.PosterCosKey,
            movie.DoubanScore,
            movie.Genres
        );
    }
}
