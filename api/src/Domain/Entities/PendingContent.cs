using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("pending_content")]
public sealed class PendingContent
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "source")]
    public string Source { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "source_url")]
    public string SourceUrl { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content_type")]
    public string ContentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "raw_data", ColumnDataType = "jsonb", IsJson = true)]
    public object RawData { get; set; } = new();

    [SugarColumn(ColumnName = "review_status")]
    public string ReviewStatus { get; set; } = "pending";

    [SugarColumn(ColumnName = "reviewed_at")]
    public DateTimeOffset? ReviewedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
