using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MovieSite.Application.Common;
using MovieSite.Domain;
using MovieSite.Domain.Entities;
using SqlSugar;

namespace MovieSite.Application.Movies;

public sealed class MovieApplicationService(ISqlSugarClient db, IRedisCache redis, SimilarContentService similarContentService)
{
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DetailCacheTtl = TimeSpan.FromHours(1);

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

    public async Task<MovieDetailDto?> GetMovieDetailAsync(long id, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.MovieDetail(id);
        var cached = await redis.GetAsync<MovieDetailDto>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var movie = await db.Queryable<Movie>()
            .FirstAsync(x => x.Id == id && x.DeletedAt == null && x.Status == "published");

        if (movie is null)
        {
            return null;
        }

        var castTask = GetMovieCreditGroupAsync(id, "actor", 20);
        var directorsTask = GetMovieCreditGroupAsync(id, "director", null);
        var awardsTask = GetAwardsAsync(id);
        var videosTask = GetVideosAsync(id);
        var franchiseTask = GetFranchiseRefAsync(movie);
        var similarTask = similarContentService.GetSimilarAsync("movie", id, 6, ct);

        await Task.WhenAll(castTask, directorsTask, awardsTask, videosTask, franchiseTask, similarTask);

        var detail = new MovieDetailDto(
            movie.Id,
            movie.TitleCn,
            movie.TitleOriginal,
            movie.TitleAliases ?? Array.Empty<string>(),
            movie.Tagline,
            movie.Synopsis,
            movie.Genres ?? Array.Empty<string>(),
            movie.Region ?? Array.Empty<string>(),
            movie.Language ?? Array.Empty<string>(),
            movie.ReleaseDates ?? new List<MovieSite.Domain.ValueObjects.ReleaseDate>(),
            movie.Durations ?? new List<MovieSite.Domain.ValueObjects.Duration>(),
            movie.DoubanScore,
            movie.DoubanRatingCount,
            movie.DoubanRatingDist,
            movie.ImdbScore,
            movie.ImdbId,
            movie.PosterCosKey,
            movie.BackdropCosKey,
            movie.ExtraBackdrops ?? Array.Empty<string>(),
            movie.ExtraPosters ?? Array.Empty<string>(),
            movie.ProductionCompanies ?? Array.Empty<string>(),
            movie.Distributors ?? Array.Empty<string>(),
            franchiseTask.Result,
            castTask.Result,
            directorsTask.Result,
            awardsTask.Result,
            videosTask.Result,
            similarTask.Result
        );

        await redis.SetAsync(cacheKey, detail, DetailCacheTtl);
        return detail;
    }

    public async Task<CreditsResponseDto?> GetMovieCreditsAsync(long id, CancellationToken ct = default)
    {
        var exists = await db.Queryable<Movie>()
            .AnyAsync(x => x.Id == id && x.DeletedAt == null && x.Status == "published");

        if (!exists)
        {
            return null;
        }

        var sql = @"
            SELECT
                c.role AS Role,
                c.character_name AS CharacterName,
                c.display_order AS DisplayOrder,
                p.id AS PersonId,
                p.name_cn AS NameCn,
                p.name_en AS NameEn,
                p.avatar_cos_key AS AvatarCosKey
            FROM credits c
            JOIN people p ON p.id = c.person_id
            WHERE c.content_type = 'movie'
              AND c.content_id = @movieId
              AND c.deleted_at IS NULL
              AND p.deleted_at IS NULL
            ORDER BY c.display_order ASC";

        var rows = await db.Ado.SqlQueryAsync<CreditRow>(sql, new Dictionary<string, object>
        {
            ["movieId"] = id
        });

        var directors = new List<CreditPersonDto>();
        var writers = new List<CreditPersonDto>();
        var cast = new List<CreditPersonDto>();
        var producers = new List<CreditPersonDto>();
        var others = new List<CreditPersonDto>();

        foreach (var row in rows)
        {
            var dto = new CreditPersonDto(
                row.PersonId,
                row.NameCn,
                row.NameEn,
                row.AvatarCosKey,
                row.CharacterName,
                row.DisplayOrder
            );

            switch (row.Role)
            {
                case "director":
                    directors.Add(dto);
                    break;
                case "writer":
                    writers.Add(dto);
                    break;
                case "actor":
                    cast.Add(dto);
                    break;
                case "producer":
                    producers.Add(dto);
                    break;
                default:
                    others.Add(dto);
                    break;
            }
        }

        return new CreditsResponseDto(directors, writers, cast, producers, others);
    }

    private async Task<List<CreditPersonDto>> GetMovieCreditGroupAsync(long movieId, string role, int? limit)
    {
        var sql = @"
            SELECT
                p.id AS PersonId,
                p.name_cn AS NameCn,
                p.name_en AS NameEn,
                p.avatar_cos_key AS AvatarCosKey,
                c.character_name AS CharacterName,
                c.display_order AS DisplayOrder
            FROM credits c
            JOIN people p ON p.id = c.person_id
            WHERE c.content_type = 'movie'
              AND c.content_id = @movieId
              AND c.role = @role
              AND c.deleted_at IS NULL
              AND p.deleted_at IS NULL
            ORDER BY c.display_order ASC";

        var rows = await db.Ado.SqlQueryAsync<CreditRow>(sql, new Dictionary<string, object>
        {
            ["movieId"] = movieId,
            ["role"] = role
        });

        var selected = limit.HasValue ? rows.Take(limit.Value) : rows;
        return selected.Select(x => new CreditPersonDto(
            x.PersonId,
            x.NameCn,
            x.NameEn,
            x.AvatarCosKey,
            x.CharacterName,
            x.DisplayOrder
        )).ToList();
    }

    private async Task<List<AwardItemDto>> GetAwardsAsync(long movieId)
    {
        var sql = @"
            SELECT
                e.name_cn AS EventName,
                c.edition_number AS EditionNumber,
                n.category AS Category,
                n.is_winner AS IsWinner
            FROM award_nominations n
            JOIN award_ceremonies c ON c.id = n.ceremony_id
            JOIN award_events e ON e.id = c.event_id
            WHERE n.content_type = 'movie'
              AND n.content_id = @movieId
              AND n.deleted_at IS NULL
              AND c.deleted_at IS NULL
              AND e.deleted_at IS NULL
            ORDER BY c.edition_number DESC, e.name_cn ASC
            LIMIT 50";

        var rows = await db.Ado.SqlQueryAsync<AwardRow>(sql, new Dictionary<string, object>
        {
            ["movieId"] = movieId
        });

        return rows.Select(x => new AwardItemDto(
            x.EventName,
            x.EditionNumber,
            x.Category,
            x.IsWinner
        )).ToList();
    }

    private async Task<List<VideoDto>> GetVideosAsync(long movieId)
    {
        var sql = @"
            SELECT
                id AS Id,
                title AS Title,
                url AS Url,
                type AS Type,
                published_at AS PublishedAt
            FROM media_videos
            WHERE content_type = 'movie'
              AND content_id = @movieId
              AND deleted_at IS NULL
            ORDER BY published_at DESC NULLS LAST, id DESC";

        var rows = await db.Ado.SqlQueryAsync<VideoRow>(sql, new Dictionary<string, object>
        {
            ["movieId"] = movieId
        });

        return rows.Select(x => new VideoDto(x.Id, x.Title, x.Url, x.Type, x.PublishedAt)).ToList();
    }

    private async Task<FranchiseRef?> GetFranchiseRefAsync(Movie movie)
    {
        if (!movie.FranchiseId.HasValue)
        {
            return null;
        }

        var franchise = await db.Queryable<Franchise>()
            .FirstAsync(x => x.Id == movie.FranchiseId.Value && x.DeletedAt == null);

        if (franchise is null)
        {
            return null;
        }

        var total = await db.Queryable<Movie>()
            .Where(x => x.FranchiseId == movie.FranchiseId.Value && x.DeletedAt == null && x.Status == "published")
            .CountAsync();

        return new FranchiseRef(
            franchise.Id,
            franchise.NameCn,
            movie.FranchiseOrder ?? 0,
            total
        );
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

    private sealed class CreditRow
    {
        public string Role { get; init; } = string.Empty;
        public long PersonId { get; init; }
        public string NameCn { get; init; } = string.Empty;
        public string? NameEn { get; init; }
        public string? AvatarCosKey { get; init; }
        public string? CharacterName { get; init; }
        public int DisplayOrder { get; init; }
    }

    private sealed class AwardRow
    {
        public string EventName { get; init; } = string.Empty;
        public int EditionNumber { get; init; }
        public string Category { get; init; } = string.Empty;
        public bool IsWinner { get; init; }
    }

    private sealed class VideoRow
    {
        public long Id { get; init; }
        public string? Title { get; init; }
        public string Url { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public DateOnly? PublishedAt { get; init; }
    }
}
