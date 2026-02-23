namespace MovieSite.Domain.ValueObjects;

public sealed record FamilyMember
{
    public string Name { get; init; } = string.Empty;

    public string? Relation { get; init; }
}