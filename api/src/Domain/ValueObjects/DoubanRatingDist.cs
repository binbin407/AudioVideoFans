namespace MovieSite.Domain.ValueObjects;

public sealed record DoubanRatingDist
{
    public decimal? Five { get; init; }

    public decimal? Four { get; init; }

    public decimal? Three { get; init; }

    public decimal? Two { get; init; }

    public decimal? One { get; init; }
}