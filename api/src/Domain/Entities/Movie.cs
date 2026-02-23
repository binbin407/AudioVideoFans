using MovieSite.Domain.ValueObjects;
using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("movies")]
public sealed class Movie
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "title_cn")]
    public string TitleCn { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "title_original")]
    public string? TitleOriginal { get; set; }

    [SugarColumn(ColumnName = "title_aliases", ColumnDataType = "text[]")]
    public string[] TitleAliases { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "tagline")]
    public string? Tagline { get; set; }

    [SugarColumn(ColumnName = "synopsis")]
    public string? Synopsis { get; set; }

    [SugarColumn(ColumnName = "genres", ColumnDataType = "text[]")]
    public string[] Genres { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "region", ColumnDataType = "text[]")]
    public string[] Region { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "language", ColumnDataType = "text[]")]
    public string[] Language { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "release_dates", ColumnDataType = "jsonb", IsJson = true)]
    public List<ReleaseDate> ReleaseDates { get; set; } = new();

    [SugarColumn(ColumnName = "durations", ColumnDataType = "jsonb", IsJson = true)]
    public List<Duration> Durations { get; set; } = new();

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

    [SugarColumn(ColumnName = "mtime_score_music")]
    public decimal? MtimeScoreMusic { get; set; }

    [SugarColumn(ColumnName = "mtime_score_visual")]
    public decimal? MtimeScoreVisual { get; set; }

    [SugarColumn(ColumnName = "mtime_score_director")]
    public decimal? MtimeScoreDirector { get; set; }

    [SugarColumn(ColumnName = "mtime_score_story")]
    public decimal? MtimeScoreStory { get; set; }

    [SugarColumn(ColumnName = "mtime_score_performance")]
    public decimal? MtimeScorePerformance { get; set; }

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

    [SugarColumn(ColumnName = "distributors", ColumnDataType = "text[]")]
    public string[] Distributors { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "franchise_id")]
    public long? FranchiseId { get; set; }

    [SugarColumn(ColumnName = "franchise_order")]
    public int? FranchiseOrder { get; set; }

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
