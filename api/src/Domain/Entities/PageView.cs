using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("page_views")]
public sealed class PageView
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "content_type")]
    public string ContentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content_id")]
    public long ContentId { get; set; }

    [SugarColumn(ColumnName = "viewed_at")]
    public DateTimeOffset ViewedAt { get; set; }
}
