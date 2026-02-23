using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("keywords")]
public sealed class Keyword
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;
}
