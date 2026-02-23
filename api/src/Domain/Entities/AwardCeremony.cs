using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("award_ceremonies")]
public sealed class AwardCeremony
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "event_id")]
    public long EventId { get; set; }

    [SugarColumn(ColumnName = "edition_number")]
    public int EditionNumber { get; set; }

    [SugarColumn(ColumnName = "year")]
    public int Year { get; set; }

    [SugarColumn(ColumnName = "ceremony_date")]
    public DateOnly? CeremonyDate { get; set; }
}
