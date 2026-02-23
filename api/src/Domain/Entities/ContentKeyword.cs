using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("content_keywords")]
public sealed class ContentKeyword
{
    [SugarColumn(ColumnName = "keyword_id", IsPrimaryKey = true)]
    public long KeywordId { get; set; }

    [SugarColumn(ColumnName = "content_type", IsPrimaryKey = true)]
    public string ContentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content_id", IsPrimaryKey = true)]
    public long ContentId { get; set; }
}
