using MovieSite.Domain.ValueObjects;
using SqlSugar;

namespace MovieSite.Domain.Entities;

[SugarTable("people")]
public sealed class Person
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "name_cn")]
    public string NameCn { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name_en")]
    public string? NameEn { get; set; }

    [SugarColumn(ColumnName = "name_aliases", ColumnDataType = "text[]")]
    public string[] NameAliases { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "gender")]
    public string? Gender { get; set; }

    [SugarColumn(ColumnName = "birth_date")]
    public DateOnly? BirthDate { get; set; }

    [SugarColumn(ColumnName = "death_date")]
    public DateOnly? DeathDate { get; set; }

    [SugarColumn(ColumnName = "birth_place")]
    public string? BirthPlace { get; set; }

    [SugarColumn(ColumnName = "nationality")]
    public string? Nationality { get; set; }

    [SugarColumn(ColumnName = "height_cm")]
    public int? HeightCm { get; set; }

    [SugarColumn(ColumnName = "professions", ColumnDataType = "text[]")]
    public string[] Professions { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "biography")]
    public string? Biography { get; set; }

    [SugarColumn(ColumnName = "imdb_id")]
    public string? ImdbId { get; set; }

    [SugarColumn(ColumnName = "family_members", ColumnDataType = "jsonb", IsJson = true)]
    public List<FamilyMember> FamilyMembers { get; set; } = new();

    [SugarColumn(ColumnName = "avatar_cos_key")]
    public string? AvatarCosKey { get; set; }

    [SugarColumn(ColumnName = "photos_cos_keys", ColumnDataType = "text[]")]
    public string[] PhotosCosKeys { get; set; } = Array.Empty<string>();

    [SugarColumn(ColumnName = "popularity")]
    public int Popularity { get; set; }

    [SugarColumn(ColumnName = "deleted_at")]
    public DateTimeOffset? DeletedAt { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "search_vector", IsOnlyIgnoreInsert = true, IsOnlyIgnoreUpdate = true)]
    public string? SearchVector { get; set; }
}
