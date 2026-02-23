using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MovieSite.Application.Common;
using MovieSite.Application.TvSeries.DTOs;
using MovieSite.Domain;
using MovieSite.Domain.Entities;
using MovieSite.Domain.Repositories;
using DomainTvSeries = MovieSite.Domain.Entities.TvSeries;

namespace MovieSite.Application.TvSeries;

public sealed class TvSeriesApplicationService(
    ITvSeriesRepository tvSeriesRepository,
    IRedisCache redisCache)
{
    private static readonly TimeSpan TvListTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TvDetailTtl = TimeSpan.FromHours(1);

    public async Task<PagedResponse<TvMediaCardDto>> GetTvListAsync(TvListFilterDto filter, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.TvList(GetFilterHash(filter));
        var cached = await redisCache.GetAsync<PagedResponse<TvMediaCardDto>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        string[]? languages = !string.IsNullOrWhiteSpace(filter.Language)
            ? [filter.Language!]
            : null;

        var (start, end) = ParseDecade(filter.Decade);

        var query = new TvSeriesListQuery(
            filter.Genres,
            filter.Regions,
            languages,
            filter.Year,
            start,
            end,
            filter.MinScore,
            filter.AirStatus,
            filter.Sort,
            filter.Page,
            filter.PageSize
        );

        var (items, total) = await tvSeriesRepository.GetListAsync(query, ct);

        var data = items.Select(MapToCard).ToList();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)filter.PageSize);
        var response = new PagedResponse<TvMediaCardDto>(
            Data: data,
            Pagination: new PaginationDto(filter.Page, filter.PageSize, total, totalPages)
        );

        await redisCache.SetAsync(cacheKey, response, TvListTtl);
        return response;
    }

    public async Task<TvSeriesDetailDto> GetTvDetailAsync(long id, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.TvDetail(id);
        var cached = await redisCache.GetAsync<TvSeriesDetailDto>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var series = await tvSeriesRepository.GetByIdAsync(id, ct);
        if (series is null || series.DeletedAt != null || !string.Equals(series.Status, "published", StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException($"TV series {id} not found");
        }

        var creditsTask = tvSeriesRepository.GetCreditsWithPeopleAsync(id, ct);
        var seasonsTask = tvSeriesRepository.GetSeasonsBySeriesIdAsync(id, ct);
        var videosTask = tvSeriesRepository.GetVideosAsync(id, ct);
        var similarTask = tvSeriesRepository.GetSimilarAsync(id, 6, ct);

        await Task.WhenAll(creditsTask, seasonsTask, videosTask, similarTask);

        var credits = creditsTask.Result;
        var seasons = seasonsTask.Result;
        var videos = videosTask.Result;
        var similar = similarTask.Result;

        var directors = credits
            .Where(x => string.Equals(x.Role, "director", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(x.Department, "directing", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayOrder)
            .Take(20)
            .Select(x => new CreditPersonDto(
                x.PersonId,
                x.NameCn,
                x.AvatarCosKey,
                x.Role,
                x.Department,
                x.CharacterName
            ))
            .ToList();

        var cast = credits
            .Where(x => string.Equals(x.Role, "cast", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(x.Department, "acting", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayOrder)
            .Take(20)
            .Select(x => new CreditPersonDto(
                x.PersonId,
                x.NameCn,
                x.AvatarCosKey,
                x.Role,
                x.Department,
                x.CharacterName
            ))
            .ToList();

        var seasonsSummary = seasons
            .OrderBy(x => x.SeasonNumber)
            .Select(x => new SeasonSummaryDto(
                x.Id,
                x.SeasonNumber,
                x.Name,
                x.EpisodeCount,
                x.FirstAirDate,
                x.PosterCosKey,
                x.Overview,
                x.VoteAverage
            ))
            .ToList();

        var nextEpisodeInfo = GetNextEpisodeInfo(series);

        var detail = new TvSeriesDetailDto(
            Id: series.Id,
            TitleCn: series.TitleCn,
            TitleOriginal: series.TitleOriginal,
            TitleAliases: series.TitleAliases,
            Synopsis: series.Synopsis,
            Genres: series.Genres,
            Region: series.Region,
            Language: series.Language,
            FirstAirDate: series.FirstAirDate,
            LastAirDate: series.LastAirDate,
            AirStatus: series.AirStatus,
            NumberOfSeasons: series.NumberOfSeasons,
            NumberOfEpisodes: series.NumberOfEpisodes,
            DoubanScore: series.DoubanScore,
            ImdbScore: series.ImdbScore,
            PosterCosKey: series.PosterCosKey,
            BackdropCosKey: series.BackdropCosKey,
            ExtraPosters: series.ExtraPosters,
            ExtraBackdrops: series.ExtraBackdrops,
            SeasonsSummary: seasonsSummary,
            NextEpisodeInfo: nextEpisodeInfo,
            Directors: directors,
            Cast: cast,
            Videos: videos.Select(v => new VideoDto(v.Id, v.Title, v.Url, v.Type, v.PublishedAt)).ToList(),
            Similar: similar.Select(MapToCard).ToList()
        );

        await redisCache.SetAsync(cacheKey, detail, TvDetailTtl);
        return detail;
    }

    public async Task<SeasonDetailDto> GetSeasonDetailAsync(long seriesId, int seasonNumber, CancellationToken ct = default)
    {
        var series = await tvSeriesRepository.GetByIdAsync(seriesId, ct);
        if (series is null || series.DeletedAt != null || !string.Equals(series.Status, "published", StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException($"TV series {seriesId} not found");
        }

        var season = await tvSeriesRepository.GetSeasonByNumberAsync(seriesId, seasonNumber, ct);
        if (season is null)
        {
            throw new KeyNotFoundException($"Season {seasonNumber} for TV series {seriesId} not found");
        }

        var episodesTask = tvSeriesRepository.GetEpisodesBySeasonIdAsync(season.Id, ct);
        var adjacentTask = tvSeriesRepository.GetAdjacentSeasonNumbersAsync(seriesId, seasonNumber, ct);

        await Task.WhenAll(episodesTask, adjacentTask);

        var episodes = episodesTask.Result
            .OrderBy(x => x.EpisodeNumber)
            .Select(x => new EpisodeDto(
                x.Id,
                x.EpisodeNumber,
                x.Name,
                x.AirDate,
                x.DurationMin,
                x.StillCosKey,
                x.Overview,
                x.VoteAverage
            ))
            .ToList();

        var (prev, next) = adjacentTask.Result;

        return new SeasonDetailDto(
            Id: season.Id,
            SeriesId: series.Id,
            SeriesTitleCn: series.TitleCn,
            SeasonNumber: season.SeasonNumber,
            Name: season.Name,
            EpisodeCount: season.EpisodeCount,
            FirstAirDate: season.FirstAirDate,
            PosterCosKey: season.PosterCosKey,
            Overview: season.Overview,
            VoteAverage: season.VoteAverage,
            Episodes: episodes,
            PrevSeasonNumber: prev,
            NextSeasonNumber: next
        );
    }

    public async Task<List<TvMediaCardDto>> GetSimilarAsync(long id, CancellationToken ct = default)
    {
        var source = await tvSeriesRepository.GetByIdAsync(id, ct);
        if (source is null || source.DeletedAt != null || !string.Equals(source.Status, "published", StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException($"TV series {id} not found");
        }

        var list = await tvSeriesRepository.GetSimilarAsync(id, 6, ct);
        return list.Select(MapToCard).ToList();
    }

    private static TvMediaCardDto MapToCard(DomainTvSeries series)
    {
        var year = series.FirstAirDate?.Year;
        return new TvMediaCardDto(
            Id: series.Id,
            ContentType: "tv_series",
            TitleCn: series.TitleCn,
            Year: year,
            PosterCosKey: series.PosterCosKey,
            DoubanScore: series.DoubanScore,
            Genres: series.Genres,
            AirStatus: series.AirStatus
        );
    }

    private static (int? Start, int? End) ParseDecade(string? decade)
    {
        if (string.IsNullOrWhiteSpace(decade))
        {
            return (null, null);
        }

        var text = decade.Trim();
        if (text.Length == 5 && text.EndsWith('s') && int.TryParse(text[..4], out var start))
        {
            return (start, start + 9);
        }

        return (null, null);
    }

    private static NextEpisodeInfoDto? GetNextEpisodeInfo(DomainTvSeries series)
    {
        if (!string.Equals(series.AirStatus, "airing", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (series.NextEpisodeInfo is null)
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Serialize(series.NextEpisodeInfo);
            var safe = JsonSerializer.Deserialize<NextEpisodeInfoDto>(payload);
            return safe;
        }
        catch
        {
            return null;
        }
    }

    private static string GetFilterHash(TvListFilterDto filter)
    {
        var payload = JsonSerializer.Serialize(new
        {
            filter.Genres,
            filter.Regions,
            filter.Decade,
            filter.Year,
            filter.Language,
            filter.MinScore,
            filter.Sort,
            filter.Page,
            filter.PageSize,
            filter.AirStatus
        });

        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
