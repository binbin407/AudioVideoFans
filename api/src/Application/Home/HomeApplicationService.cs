using MovieSite.Application.Common;
using MovieSite.Domain;
using MovieSite.Domain.Entities;
using SqlSugar;
using AnimeEntity = MovieSite.Domain.Entities.Anime;
using TvSeriesEntity = MovieSite.Domain.Entities.TvSeries;

namespace MovieSite.Application.Home;

public sealed class HomeApplicationService(ISqlSugarClient db, IRedisCache redis)
{
    private static readonly TimeSpan HomeCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<HomeDto> GetHomeDataAsync(CancellationToken ct = default)
    {
        var cached = await redis.GetAsync<HomeDto>(CacheKeys.HomeBanners);
        if (cached is not null)
        {
            return cached;
        }

        var now = DateTimeOffset.UtcNow;

        var banners = await db.Queryable<FeaturedBanner>()
            .Where(x => (x.StartAt == null || x.StartAt <= now) && (x.EndAt == null || x.EndAt > now))
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var movieIds = banners.Where(x => x.ContentType == "movie").Select(x => x.ContentId).Distinct().ToArray();
        var tvIds = banners.Where(x => x.ContentType == "tv").Select(x => x.ContentId).Distinct().ToArray();
        var animeIds = banners.Where(x => x.ContentType == "anime").Select(x => x.ContentId).Distinct().ToArray();

        var movieMap = movieIds.Length == 0
            ? new Dictionary<long, Movie>()
            : (await db.Queryable<Movie>()
                .Where(x => movieIds.Contains(x.Id) && x.DeletedAt == null && x.Status == "published")
                .ToListAsync())
                .ToDictionary(x => x.Id);

        var tvMap = tvIds.Length == 0
            ? new Dictionary<long, TvSeriesEntity>()
            : (await db.Queryable<TvSeriesEntity>()
                .Where(x => tvIds.Contains(x.Id) && x.DeletedAt == null && x.Status == "published")
                .ToListAsync())
                .ToDictionary(x => x.Id);

        var animeMap = animeIds.Length == 0
            ? new Dictionary<long, AnimeEntity>()
            : (await db.Queryable<AnimeEntity>()
                .Where(x => animeIds.Contains(x.Id) && x.DeletedAt == null && x.Status == "published")
                .ToListAsync())
                .ToDictionary(x => x.Id);

        var bannerDtos = banners.Select(x =>
        {
            var (title, poster, backdrop) = ResolveBannerContent(x, movieMap, tvMap, animeMap);
            return new BannerDto(
                x.Id,
                x.ContentType,
                x.ContentId,
                title,
                poster,
                backdrop,
                x.DisplayOrder
            );
        }).ToList();

        var hotMovies = (await db.Queryable<Movie>()
            .Where(x => x.DeletedAt == null && x.Status == "published")
            .OrderByDescending(x => x.Popularity)
            .Take(8)
            .ToListAsync())
            .Select(ToMovieCard)
            .ToList();

        var hotTv = (await db.Queryable<TvSeriesEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "published")
            .OrderByDescending(x => x.Popularity)
            .Take(8)
            .ToListAsync())
            .Select(ToTvCard)
            .ToList();

        var hotAnimeCn = (await db.Queryable<AnimeEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "published" && x.Origin == "cn")
            .OrderByDescending(x => x.Popularity)
            .Take(8)
            .ToListAsync())
            .Select(ToAnimeCard)
            .ToList();

        var hotAnimeJp = (await db.Queryable<AnimeEntity>()
            .Where(x => x.DeletedAt == null && x.Status == "published" && x.Origin == "jp")
            .OrderByDescending(x => x.Popularity)
            .Take(8)
            .ToListAsync())
            .Select(ToAnimeCard)
            .ToList();

        var result = new HomeDto(
            bannerDtos,
            hotMovies,
            hotTv,
            hotAnimeCn,
            hotAnimeJp
        );

        await redis.SetAsync(CacheKeys.HomeBanners, result, HomeCacheTtl);

        return result;
    }

    private static (string TitleCn, string? PosterCosKey, string? BackdropCosKey) ResolveBannerContent(
        FeaturedBanner banner,
        IReadOnlyDictionary<long, Movie> movieMap,
        IReadOnlyDictionary<long, TvSeriesEntity> tvMap,
        IReadOnlyDictionary<long, AnimeEntity> animeMap)
    {
        if (banner.ContentType == "movie" && movieMap.TryGetValue(banner.ContentId, out var movie))
        {
            return (movie.TitleCn, movie.PosterCosKey, movie.BackdropCosKey);
        }

        if (banner.ContentType == "tv" && tvMap.TryGetValue(banner.ContentId, out var tv))
        {
            return (tv.TitleCn, tv.PosterCosKey, tv.BackdropCosKey);
        }

        if (banner.ContentType == "anime" && animeMap.TryGetValue(banner.ContentId, out var anime))
        {
            return (anime.TitleCn, anime.PosterCosKey, anime.BackdropCosKey);
        }

        return (string.Empty, null, null);
    }

    private static MediaCardDto ToMovieCard(Movie movie)
    {
        var year = movie.ReleaseDates
            .Where(x => x.Date.HasValue)
            .OrderBy(x => x.Date)
            .Select(x => x.Date!.Value.Year)
            .FirstOrDefault();

        return new MediaCardDto(
            movie.Id,
            movie.TitleCn,
            year == 0 ? null : year,
            movie.PosterCosKey,
            movie.DoubanScore,
            movie.Genres
        );
    }

    private static MediaCardDto ToTvCard(TvSeriesEntity tv)
    {
        return new MediaCardDto(
            tv.Id,
            tv.TitleCn,
            tv.FirstAirDate?.Year,
            tv.PosterCosKey,
            tv.DoubanScore,
            tv.Genres
        );
    }

    private static MediaCardDto ToAnimeCard(AnimeEntity anime)
    {
        return new MediaCardDto(
            anime.Id,
            anime.TitleCn,
            anime.FirstAirDate?.Year,
            anime.PosterCosKey,
            anime.DoubanScore,
            anime.Genres
        );
    }
}
