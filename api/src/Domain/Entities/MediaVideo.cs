using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("media_videos")]
public sealed class MediaVideo
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "content_type")]
    public string ContentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content_id")]
    public long ContentId { get; set; }

    [SugarColumn(ColumnName = "title")]
    public string? Title { get; set; }

    [SugarColumn(ColumnName = "url")]
    public string Url { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "type")]
    public string Type { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "published_at")]
    public DateOnly? PublishedAt { get; set; }
}
