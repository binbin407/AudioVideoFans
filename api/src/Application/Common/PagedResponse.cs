namespace MovieSite.Application.Common;

public sealed record PaginationDto(int Page, int PageSize, int Total, int TotalPages);

public sealed record PagedResponse<T>(List<T> Data, PaginationDto Pagination);

public sealed record ApiError(string Code, string Message);
