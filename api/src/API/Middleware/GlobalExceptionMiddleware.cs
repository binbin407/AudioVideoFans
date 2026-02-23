using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MovieSite.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception");

            var (statusCode, payload) = MapException(exception);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }

    private static (int StatusCode, object Payload) MapException(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException e =>
                (StatusCodes.Status404NotFound, Error("NOT_FOUND", e.Message)),
            UnauthorizedAccessException e =>
                (StatusCodes.Status401Unauthorized, Error("UNAUTHORIZED", e.Message)),
            ValidationException e =>
                (StatusCodes.Status422UnprocessableEntity, ValidationError(e)),
            _ =>
                (StatusCodes.Status500InternalServerError, Error("INTERNAL_ERROR", "服务器内部错误"))
        };
    }

    private static object ValidationError(ValidationException exception)
    {
        return new
        {
            error = new
            {
                code = "VALIDATION_ERROR",
                message = exception.Message,
                fields = exception.ValidationResult?.MemberNames?.ToArray() ?? Array.Empty<string>()
            }
        };
    }

    private static object Error(string code, string message)
    {
        return new
        {
            error = new
            {
                code,
                message
            }
        };
    }
}
