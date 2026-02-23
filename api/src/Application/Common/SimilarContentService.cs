using MovieSite.Domain.Entities;
using SqlSugar;
using AnimeEntity = MovieSite.Domain.Entities.Anime;
using TvSeriesEntity = MovieSite.Domain.Entities.TvSeries;

namespace MovieSite.Application.Common;

public sealed class SimilarContentService(ISqlSugarClient db)
{
    public async Task<List<MediaCardDto>> GetSimilarAsync(
        string contentType,
        long contentId,
        int limit = 6,
        CancellationToken ct = default)
    {
        if (limit <= 0)
        {
            return new List<MediaCardDto>();
        }

        var normalizedType = NormalizeContentType(contentType);
        var keywordIds = await db.Queryable<ContentKeyword>()
            .Where(x => x.ContentType == normalizedType && x.ContentId == contentId)
            .Select(x => x.KeywordId)
            .ToListAsync();

        var result = keywordIds.Count > 0
            ? await QueryByKeywordOverlapAsync(normalizedType, contentId, keywordIds.Distinct().ToArray(), limit)
            : new List<MediaCardDto>();

        if (result.Count >= limit)
        {
            return result.Take(limit).ToList();
        }

        var targetGenres = await GetTargetGenresAsync(normalizedType, contentId);
        if (targetGenres.Length == 0)
        {
            return result;
        }

        var existingIds = result.Select(x => x.Id).ToHashSet();
        var fallback = await QueryByGenreFallbackAsync(
            normalizedType,
            contentId,
            targetGenres,
            existingIds,
            limit - result.Count);

        result.AddRange(fallback);
        return result.Take(limit).ToList();
    }

    private static string NormalizeContentType(string contentType)
    {
        return contentType switch
        {
            "movie" => "movie",
            "tv_series" => "tv_series",
            "anime" => "anime",
            _ => throw new ArgumentException($"Unsupported content type: {contentType}", nameof(contentType))
        };
    }

    private async Task<List<MediaCardDto>> QueryByKeywordOverlapAsync(
        string contentType,
        long contentId,
        long[] keywordIds,
        int limit)
    {
        if (keywordIds.Length == 0)
        {
            return new List<MediaCardDto>();
        }

        var keywordParams = keywordIds
            .Select((_, index) => $"@kw{index}")
            .ToArray();

        var table = contentType switch
        {
            "movie" => "movies",
            "tv_series" => "tv_series",
            _ => "anime"
        };

        var dateYearExpr = contentType == "movie"
            ? "(SELECT MIN((rd->>'date')::date) FROM jsonb_array_elements(c.release_dates) rd WHERE rd->>'date' IS NOT NULL)"
            : "c.first_air_date";

        var sql = $@"
            SELECT
                c.id AS Id,
                c.title_cn AS TitleCn,
                EXTRACT(YEAR FROM {dateYearExpr})::int AS Year,
                c.poster_cos_key AS PosterCosKey,
                c.douban_score AS DoubanScore,
                c.genres AS Genres,
                COUNT(ck.keyword_id) AS KeywordOverlap
            FROM {table} c
            LEFT JOIN content_keywords ck
                ON ck.content_type = @contentType
               AND ck.content_id = c.id
               AND ck.keyword_id IN ({string.Join(",", keywordParams)})
               AND ck.deleted_at IS NULL
            WHERE c.id <> @contentId
              AND c.deleted_at IS NULL
              AND c.status = 'published'
            GROUP BY c.id, c.title_cn, c.poster_cos_key, c.douban_score, c.genres, {dateYearExpr}
            ORDER BY COUNT(ck.keyword_id) DESC, c.douban_score DESC NULLS LAST
            LIMIT @limit";

        var parameters = new Dictionary<string, object>
        {
            ["contentType"] = contentType,
            ["contentId"] = contentId,
            ["limit"] = limit
        };

        for (var i = 0; i < keywordIds.Length; i++)
        {
            parameters[$"kw{i}"] = keywordIds[i];
        }

        var rows = await db.Ado.SqlQueryAsync<SimilarSqlRow>(sql, parameters);
        return rows.Select(ToMediaCard).ToList();
    }

    private async Task<List<MediaCardDto>> QueryByGenreFallbackAsync(
        string contentType,
        long contentId,
        string[] genres,
        HashSet<long> existingIds,
        int remaining)
    {
        if (remaining <= 0)
        {
            return new List<MediaCardDto>();
        }

        var table = contentType switch
        {
            "movie" => "movies",
            "tv_series" => "tv_series",
            _ => "anime"
        };

        var dateYearExpr = contentType == "movie"
            ? "(SELECT MIN((rd->>'date')::date) FROM jsonb_array_elements(c.release_dates) rd WHERE rd->>'date' IS NOT NULL)"
            : "c.first_air_date";

        var idFilterSql = existingIds.Count == 0
            ? string.Empty
            : $" AND c.id NOT IN ({string.Join(",", existingIds.Select((_, idx) => $"@exclude{idx}"))})";

        var sql = $@"
            SELECT
                c.id AS Id,
                c.title_cn AS TitleCn,
                EXTRACT(YEAR FROM {dateYearExpr})::int AS Year,
                c.poster_cos_key AS PosterCosKey,
                c.douban_score AS DoubanScore,
                c.genres AS Genres,
                0 AS KeywordOverlap
            FROM {table} c
            WHERE c.id <> @contentId
              AND c.deleted_at IS NULL
              AND c.status = 'published'
              AND c.genres && @genres::text[]
              {idFilterSql}
            ORDER BY c.douban_score DESC NULLS LAST
            LIMIT @remaining";

        var parameters = new Dictionary<string, object>
        {
            ["contentId"] = contentId,
            ["genres"] = genres,
            ["remaining"] = remaining
        };

        if (existingIds.Count > 0)
        {
            var i = 0;
            foreach (var id in existingIds)
            {
                parameters[$"exclude{i}"] = id;
                i++;
            }
        }

        var rows = await db.Ado.SqlQueryAsync<SimilarSqlRow>(sql, parameters);
        return rows.Select(ToMediaCard).ToList();
    }

    private async Task<string[]> GetTargetGenresAsync(string contentType, long contentId)
    {
        return contentType switch
        {
            "movie" => await db.Queryable<Movie>()
                .Where(x => x.Id == contentId && x.DeletedAt == null && x.Status == "published")
                .Select(x => x.Genres)
                .FirstAsync() ?? Array.Empty<string>(),
            "tv_series" => await db.Queryable<TvSeriesEntity>()
                .Where(x => x.Id == contentId && x.DeletedAt == null && x.Status == "published")
                .Select(x => x.Genres)
                .FirstAsync() ?? Array.Empty<string>(),
            _ => await db.Queryable<AnimeEntity>()
                .Where(x => x.Id == contentId && x.DeletedAt == null && x.Status == "published")
                .Select(x => x.Genres)
                .FirstAsync() ?? Array.Empty<string>()
        };
    }

    private static MediaCardDto ToMediaCard(SimilarSqlRow row)
    {
        return new MediaCardDto(
            row.Id,
            row.TitleCn,
            row.Year == 0 ? null : row.Year,
            row.PosterCosKey,
            row.DoubanScore,
            row.Genres ?? Array.Empty<string>()
        );
    }

    private sealed class SimilarSqlRow
    {
        public long Id { get; init; }
        public string TitleCn { get; init; } = string.Empty;
        public int Year { get; init; }
        public string? PosterCosKey { get; init; }
        public decimal? DoubanScore { get; init; }
        public string[]? Genres { get; init; }
        public int KeywordOverlap { get; init; }
    }
}
