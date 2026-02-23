namespace MovieSite.Application.Common;

public static class CosUrlHelper
{
    private static string _cdnBase = string.Empty;

    public static void Configure(string cdnBase)
    {
        _cdnBase = cdnBase.TrimEnd('/');
    }

    public static string? ToUrl(string? cosKey)
    {
        if (string.IsNullOrWhiteSpace(cosKey))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(_cdnBase)
            ? cosKey
            : $"{_cdnBase}/{cosKey.TrimStart('/')}";
    }
}
