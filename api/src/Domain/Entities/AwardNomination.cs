using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("award_nominations")]
public sealed class AwardNomination
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "ceremony_id")]
    public long CeremonyId { get; set; }

    [SugarColumn(ColumnName = "category")]
    public string Category { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content_type")]
    public string? ContentType { get; set; }

    [SugarColumn(ColumnName = "content_id")]
    public long? ContentId { get; set; }

    [SugarColumn(ColumnName = "person_id")]
    public long? PersonId { get; set; }

    [SugarColumn(ColumnName = "is_winner")]
    public bool IsWinner { get; set; }

    [SugarColumn(ColumnName = "note")]
    public string? Note { get; set; }
}
