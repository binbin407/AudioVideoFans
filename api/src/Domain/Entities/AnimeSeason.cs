using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("anime_seasons")]
public sealed class AnimeSeason
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "anime_id")]
    public long AnimeId { get; set; }

    [SugarColumn(ColumnName = "season_number")]
    public int SeasonNumber { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string? Name { get; set; }

    [SugarColumn(ColumnName = "episode_count")]
    public int EpisodeCount { get; set; }

    [SugarColumn(ColumnName = "first_air_date")]
    public DateOnly? FirstAirDate { get; set; }

    [SugarColumn(ColumnName = "poster_cos_key")]
    public string? PosterCosKey { get; set; }

    [SugarColumn(ColumnName = "overview")]
    public string? Overview { get; set; }

    [SugarColumn(ColumnName = "vote_average")]
    public decimal? VoteAverage { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
