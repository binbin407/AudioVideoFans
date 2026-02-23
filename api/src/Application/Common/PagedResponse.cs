using System.Text.Json.Serialization;

namespace MovieSite.Application.Common;

public sealed record PaginationDto(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("page_size")] int PageSize,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("total_pages")] int TotalPages
);

public sealed record PagedResponse<T>(
    [property: JsonPropertyName("data")] List<T> Data,
    [property: JsonPropertyName("pagination")] PaginationDto Pagination
);

public sealed record ApiError(string Code, string Message);
