using MovieSite.Domain.Entities;

namespace MovieSite.Domain.Repositories;

public sealed record TvSeriesListQuery(
    string[]? Genres,
    string[]? Regions,
    string[]? Languages,
    int? Year,
    int? DecadeStart,
    int? DecadeEnd,
    decimal? MinScore,
    string[]? AirStatuses,
    string Sort,
    int Page,
    int PageSize
);

public sealed record TvCreditPerson(
    long PersonId,
    string NameCn,
    string? AvatarCosKey,
    string Role,
    string? Department,
    string? CharacterName,
    int DisplayOrder
);

public interface ITvSeriesRepository : IRepository<TvSeries>
{
    Task<List<TvSeries>> GetByAirStatusAsync(string airStatus, CancellationToken ct = default);

    Task<(List<TvSeries> Items, int Total)> GetListAsync(TvSeriesListQuery query, CancellationToken ct = default);

    Task<List<TvSeason>> GetSeasonsBySeriesIdAsync(long seriesId, CancellationToken ct = default);

    Task<TvSeason?> GetSeasonByNumberAsync(long seriesId, int seasonNumber, CancellationToken ct = default);

    Task<List<TvEpisode>> GetEpisodesBySeasonIdAsync(long seasonId, CancellationToken ct = default);

    Task<(int? PrevSeasonNumber, int? NextSeasonNumber)> GetAdjacentSeasonNumbersAsync(
        long seriesId,
        int seasonNumber,
        CancellationToken ct = default
    );

    Task<List<TvCreditPerson>> GetCreditsWithPeopleAsync(long seriesId, CancellationToken ct = default);

    Task<List<MediaVideo>> GetVideosAsync(long seriesId, CancellationToken ct = default);

    Task<List<TvSeries>> GetSimilarAsync(long seriesId, int limit, CancellationToken ct = default);
}
