using Prometheus;

namespace MovieSite.API.Observability;

public static class AppMetrics
{
    public static readonly Counter SearchRequests = Metrics
        .CreateCounter("moviesite_search_requests_total", "Total search requests", ["type"]);

    public static readonly Histogram CacheHitRatio = Metrics
        .CreateHistogram("moviesite_cache_hit_ratio", "Cache hit ratio per endpoint");
}
