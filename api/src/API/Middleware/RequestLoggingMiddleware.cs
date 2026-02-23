namespace MovieSite.API.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = DateTimeOffset.UtcNow;

        await next(context);

        var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
        logger.LogInformation(
            "HTTP {Method} {Path} => {StatusCode} in {DurationMs} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            Math.Round(durationMs, 2));
    }
}
