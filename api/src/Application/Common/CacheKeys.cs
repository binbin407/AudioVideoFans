namespace MovieSite.Application.Common;

public static class CacheKeys
{
    public static string MovieDetail(long id) => $"movie:detail:{id}";

    public static string TvDetail(long id) => $"tv:detail:{id}";

    public static string AnimeDetail(long id) => $"anime:detail:{id}";

    public static string PersonDetail(long id) => $"person:detail:{id}";

    public static string MovieList(string hash) => $"movies:list:{hash}";

    public static string TvList(string hash) => $"tv:list:{hash}";

    public static string AnimeList(string hash) => $"anime:list:{hash}";

    public static string RankingsScore(string type) => $"rankings:{type}:score";

    public static string RankingsHot(string type) => $"rankings:{type}:hot";

    public static string SearchAutocomplete(string query) =>
        $"search:autocomplete:{Uri.EscapeDataString(query)}";

    public static string HomeBanners => "home:banners";
}
