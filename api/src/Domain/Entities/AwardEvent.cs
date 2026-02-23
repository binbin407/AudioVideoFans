using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("award_events")]
public sealed class AwardEvent
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "name_cn")]
    public string NameCn { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name_en")]
    public string? NameEn { get; set; }

    [SugarColumn(ColumnName = "slug")]
    public string Slug { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description")]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "official_url")]
    public string? OfficialUrl { get; set; }
}
