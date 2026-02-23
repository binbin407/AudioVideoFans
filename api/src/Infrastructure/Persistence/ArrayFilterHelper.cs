namespace MovieSite.Infrastructure.Persistence;

public static class ArrayFilterHelper
{
    public static string BuildArrayOverlap(string columnName, string paramName)
    {
        return $"{columnName} && @{paramName}::text[]";
    }

    public static (int Start, int End) DecadeToYearRange(string decade)
    {
        return decade switch
        {
            "2020s" => (2020, 2029),
            "2010s" => (2010, 2019),
            "2000s" => (2000, 2009),
            "90s" => (1990, 1999),
            "earlier" => (1888, 1989),
            _ => throw new ArgumentException($"Unknown decade: {decade}")
        };
    }
}
