using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("franchises")]
public sealed class Franchise
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "name_cn")]
    public string NameCn { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name_en")]
    public string? NameEn { get; set; }

    [SugarColumn(ColumnName = "overview")]
    public string? Overview { get; set; }

    [SugarColumn(ColumnName = "poster_cos_key")]
    public string? PosterCosKey { get; set; }

    [SugarColumn(ColumnName = "deleted_at")]
    public DateTimeOffset? DeletedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
