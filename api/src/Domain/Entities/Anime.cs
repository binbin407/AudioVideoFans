using MovieSite.Domain.ValueObjects;
using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("anime")]
public sealed class Anime
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "title_cn")]
    public string TitleCn { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "title_original")]
    public string? TitleOriginal { get; set; }

    [SugarColumn(ColumnName = "title_aliases", ColumnDataType = "text[]")]
    public string[] TitleAliases { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "synopsis")]
    public string? Synopsis { get; set; }

    [SugarColumn(ColumnName = "genres", ColumnDataType = "text[]")]
    public string[] Genres { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "origin")]
    public string Origin { get; set; } = "other";

    [SugarColumn(ColumnName = "source_material")]
    public string? SourceMaterial { get; set; }

    [SugarColumn(ColumnName = "studio")]
    public string? Studio { get; set; }

    [SugarColumn(ColumnName = "first_air_date")]
    public DateOnly? FirstAirDate { get; set; }

    [SugarColumn(ColumnName = "last_air_date")]
    public DateOnly? LastAirDate { get; set; }

    [SugarColumn(ColumnName = "air_status")]
    public string? AirStatus { get; set; }

    [SugarColumn(ColumnName = "next_episode_info", ColumnDataType = "jsonb", IsJson = true)]
    public NextEpisodeInfo? NextEpisodeInfo { get; set; }

    [SugarColumn(ColumnName = "number_of_seasons")]
    public int NumberOfSeasons { get; set; }

    [SugarColumn(ColumnName = "number_of_episodes")]
    public int NumberOfEpisodes { get; set; }

    [SugarColumn(ColumnName = "douban_score")]
    public decimal? DoubanScore { get; set; }

    [SugarColumn(ColumnName = "douban_rating_count")]
    public int? DoubanRatingCount { get; set; }

    [SugarColumn(ColumnName = "douban_rating_dist", ColumnDataType = "jsonb", IsJson = true)]
    public DoubanRatingDist? DoubanRatingDist { get; set; }

    [SugarColumn(ColumnName = "imdb_score")]
    public decimal? ImdbScore { get; set; }

    [SugarColumn(ColumnName = "imdb_id")]
    public string? ImdbId { get; set; }

    [SugarColumn(ColumnName = "poster_cos_key")]
    public string? PosterCosKey { get; set; }

    [SugarColumn(ColumnName = "backdrop_cos_key")]
    public string? BackdropCosKey { get; set; }

    [SugarColumn(ColumnName = "extra_backdrops", ColumnDataType = "text[]")]
    public string[] ExtraBackdrops { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "extra_posters", ColumnDataType = "text[]")]
    public string[] ExtraPosters { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "production_companies", ColumnDataType = "text[]")]
    public string[] ProductionCompanies { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "popularity")]
    public int Popularity { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "published";

    [SugarColumn(ColumnName = "deleted_at")]
    public DateTimeOffset? DeletedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "search_vector", IsOnlyIgnoreInsert = true, IsOnlyIgnoreUpdate = true)]
    public string? SearchVector { get; set; }
}
