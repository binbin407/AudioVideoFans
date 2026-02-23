using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("featured_banners")]
public sealed class FeaturedBanner
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "content_type")]
    public string ContentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content_id")]
    public long ContentId { get; set; }

    [SugarColumn(ColumnName = "display_order")]
    public int DisplayOrder { get; set; }

    [SugarColumn(ColumnName = "start_at")]
    public DateTimeOffset? StartAt { get; set; }

    [SugarColumn(ColumnName = "end_at")]
    public DateTimeOffset? EndAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
