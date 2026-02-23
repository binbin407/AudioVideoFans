namespace MovieSite.Domain;

public interface ITencentCosClient
{
    string? GetCdnUrl(string? cosKey);

    Task<string> UploadAsync(Stream fileStream, string cosKey, string contentType);

    Task DeleteAsync(string cosKey);
}
