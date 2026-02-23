using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("credits")]
public sealed class Credit
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "person_id")]
    public long PersonId { get; set; }

    [SugarColumn(ColumnName = "content_type")]
    public string ContentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content_id")]
    public long ContentId { get; set; }

    [SugarColumn(ColumnName = "role")]
    public string Role { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "department")]
    public string? Department { get; set; }

    [SugarColumn(ColumnName = "character_name")]
    public string? CharacterName { get; set; }

    [SugarColumn(ColumnName = "display_order")]
    public int DisplayOrder { get; set; }
}
