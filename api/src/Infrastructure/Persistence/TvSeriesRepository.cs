using MovieSite.Domain.Entities;
using MovieSite.Domain.Repositories;
using SqlSugar;

namespace MovieSite.Infrastructure.Persistence;

public sealed class TvSeriesRepository(ISqlSugarClient db)
    : SqlSugarRepository<TvSeries>(db), ITvSeriesRepository
{
    public async Task<List<TvSeries>> GetByAirStatusAsync(string airStatus, CancellationToken ct = default)
    {
        return await Db.Queryable<TvSeries>()
            .Where(x => x.AirStatus == airStatus && x.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<(List<TvSeries> Items, int Total)> GetListAsync(TvSeriesListQuery query, CancellationToken ct = default)
    {
        var qb = Db.Queryable<TvSeries>()
            .Where(x => x.DeletedAt == null && x.Status == "published");

        if (query.Genres is { Length: > 0 })
        {
            qb = qb.Where("genres && @genres::text[]", new { genres = query.Genres });
        }

        if (query.Regions is { Length: > 0 })
        {
            qb = qb.Where("region && @regions::text[]", new { regions = query.Regions });
        }

        if (query.Languages is { Length: > 0 })
        {
            qb = qb.Where("language && @languages::text[]", new { languages = query.Languages });
        }

        if (query.AirStatuses is { Length: > 0 })
        {
            qb = qb.Where("air_status = ANY(@statuses::varchar[])", new { statuses = query.AirStatuses });
        }

        if (query.MinScore.HasValue)
        {
            qb = qb.Where(x => x.DoubanScore != null && x.DoubanScore >= query.MinScore.Value);
        }

        if (query.Year.HasValue)
        {
            qb = qb.Where("EXTRACT(YEAR FROM first_air_date) = @year", new { year = query.Year.Value });
        }
        else if (query.DecadeStart.HasValue && query.DecadeEnd.HasValue)
        {
            qb = qb.Where("EXTRACT(YEAR FROM first_air_date) BETWEEN @start AND @end", new
            {
                start = query.DecadeStart.Value,
                end = query.DecadeEnd.Value
            });
        }

        qb = query.Sort switch
        {
            "douban_score" => qb.OrderBy("douban_score DESC NULLS LAST"),
            "first_air_date" => qb.OrderBy("first_air_date DESC NULLS LAST"),
            _ => qb.OrderBy("popularity DESC")
        };

        var total = await qb.CountAsync();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 24 : query.PageSize;
        var offset = (page - 1) * pageSize;

        var items = await qb
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<TvSeason>> GetSeasonsBySeriesIdAsync(long seriesId, CancellationToken ct = default)
    {
        return await Db.Queryable<TvSeason>()
            .Where(x => x.SeriesId == seriesId && x.DeletedAt == null)
            .OrderBy(x => x.SeasonNumber)
            .ToListAsync();
    }

    public async Task<TvSeason?> GetSeasonByNumberAsync(long seriesId, int seasonNumber, CancellationToken ct = default)
    {
        return await Db.Queryable<TvSeason>()
            .Where(x => x.SeriesId == seriesId && x.SeasonNumber == seasonNumber && x.DeletedAt == null)
            .FirstAsync();
    }

    public async Task<List<TvEpisode>> GetEpisodesBySeasonIdAsync(long seasonId, CancellationToken ct = default)
    {
        return await Db.Queryable<TvEpisode>()
            .Where(x => x.SeasonId == seasonId && x.DeletedAt == null)
            .OrderBy(x => x.EpisodeNumber)
            .ToListAsync();
    }

    public async Task<(int? PrevSeasonNumber, int? NextSeasonNumber)> GetAdjacentSeasonNumbersAsync(
        long seriesId,
        int seasonNumber,
        CancellationToken ct = default
    )
    {
        var prev = await Db.Queryable<TvSeason>()
            .Where(x => x.SeriesId == seriesId && x.DeletedAt == null && x.SeasonNumber < seasonNumber)
            .OrderBy(x => x.SeasonNumber, OrderByType.Desc)
            .Select(x => x.SeasonNumber)
            .FirstAsync();

        var next = await Db.Queryable<TvSeason>()
            .Where(x => x.SeriesId == seriesId && x.DeletedAt == null && x.SeasonNumber > seasonNumber)
            .OrderBy(x => x.SeasonNumber)
            .Select(x => x.SeasonNumber)
            .FirstAsync();

        return (prev == 0 ? null : prev, next == 0 ? null : next);
    }

    public async Task<List<TvCreditPerson>> GetCreditsWithPeopleAsync(long seriesId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
              c.person_id AS PersonId,
              p.name_cn AS NameCn,
              p.avatar_cos_key AS AvatarCosKey,
              c.role AS Role,
              c.department AS Department,
              c.character_name AS CharacterName,
              c.display_order AS DisplayOrder
            FROM credits c
            INNER JOIN people p ON p.id = c.person_id
            WHERE c.content_type = 'tv_series'
              AND c.content_id = @seriesId
              AND c.deleted_at IS NULL
              AND p.deleted_at IS NULL
            ORDER BY c.display_order ASC
            """;

        return await Db.Ado.SqlQueryAsync<TvCreditPerson>(sql, new { seriesId });
    }

    public async Task<List<MediaVideo>> GetVideosAsync(long seriesId, CancellationToken ct = default)
    {
        return await Db.Queryable<MediaVideo>()
            .Where(x => x.ContentType == "tv_series" && x.ContentId == seriesId && x.DeletedAt == null)
            .OrderBy(x => x.PublishedAt, OrderByType.Desc)
            .ToListAsync();
    }

    public async Task<List<TvSeries>> GetSimilarAsync(long seriesId, int limit, CancellationToken ct = default)
    {
        var source = await Db.Queryable<TvSeries>()
            .Where(x => x.Id == seriesId && x.DeletedAt == null && x.Status == "published")
            .FirstAsync();

        if (source is null)
        {
            return [];
        }

        var query = Db.Queryable<TvSeries>()
            .Where(x => x.Id != seriesId && x.DeletedAt == null && x.Status == "published");

        if (source.Genres is { Length: > 0 })
        {
            query = query.Where("genres && @genres::text[]", new { genres = source.Genres });
        }

        if (!string.IsNullOrWhiteSpace(source.AirStatus))
        {
            query = query.Where(x => x.AirStatus == source.AirStatus);
        }

        return await query
            .OrderBy("popularity DESC")
            .Take(limit)
            .ToListAsync();
    }
}
