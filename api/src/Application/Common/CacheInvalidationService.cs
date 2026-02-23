using MovieSite.Domain;

namespace MovieSite.Application.Common;

public sealed class CacheInvalidationService(IRedisCache redis)
{
    public async Task InvalidateMovieAsync(long movieId)
    {
        await redis.DeleteAsync(CacheKeys.MovieDetail(movieId));
        await redis.DeleteByPatternAsync("movies:list:*");
        await redis.DeleteAsync(CacheKeys.HomeBanners);
    }

    public async Task InvalidateTvAsync(long tvId)
    {
        await redis.DeleteAsync(CacheKeys.TvDetail(tvId));
        await redis.DeleteByPatternAsync("tv:list:*");
    }

    public async Task InvalidateAnimeAsync(long animeId)
    {
        await redis.DeleteAsync(CacheKeys.AnimeDetail(animeId));
        await redis.DeleteByPatternAsync("anime:list:*");
    }

    public Task InvalidateHomeAsync()
    {
        return redis.DeleteAsync(CacheKeys.HomeBanners);
    }

    public async Task FlushPublicListCachesAsync()
    {
        await redis.DeleteByPatternAsync("movies:list:*");
        await redis.DeleteByPatternAsync("tv:list:*");
        await redis.DeleteByPatternAsync("anime:list:*");
        await redis.DeleteAsync(CacheKeys.HomeBanners);
    }
}
