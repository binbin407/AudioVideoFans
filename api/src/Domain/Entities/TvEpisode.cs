using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("tv_episodes")]
public sealed class TvEpisode
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "season_id")]
    public long SeasonId { get; set; }

    [SugarColumn(ColumnName = "episode_number")]
    public int EpisodeNumber { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string? Name { get; set; }

    [SugarColumn(ColumnName = "air_date")]
    public DateOnly? AirDate { get; set; }

    [SugarColumn(ColumnName = "overview")]
    public string? Overview { get; set; }

    [SugarColumn(ColumnName = "duration_min")]
    public int? DurationMin { get; set; }

    [SugarColumn(ColumnName = "still_cos_key")]
    public string? StillCosKey { get; set; }

    [SugarColumn(ColumnName = "vote_average")]
    public decimal? VoteAverage { get; set; }

    [SugarColumn(ColumnName = "deleted_at")]
    public DateTimeOffset? DeletedAt { get; set; }
}
