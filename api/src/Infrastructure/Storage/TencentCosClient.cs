using MovieSite.Domain;

namespace MovieSite.Infrastructure.Storage;

public sealed class TencentCosClient(string cdnBase) : ITencentCosClient
{
    private readonly string _cdnBase = cdnBase.TrimEnd('/');

    public string? GetCdnUrl(string? cosKey)
    {
        if (string.IsNullOrWhiteSpace(cosKey))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_cdnBase))
        {
            return cosKey;
        }

        return $"{_cdnBase}/{cosKey.TrimStart('/')}";
    }

    public Task<string> UploadAsync(Stream fileStream, string cosKey, string contentType)
    {
        throw new NotImplementedException("COS upload will be implemented in admin media endpoints.");
    }

    public Task DeleteAsync(string cosKey)
    {
        throw new NotImplementedException("COS delete will be implemented in admin media endpoints.");
    }
}
