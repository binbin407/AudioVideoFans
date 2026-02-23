using Microsoft.AspNetCore.Mvc;
using MovieSite.Application.Movies;

namespace MovieSite.API.Controllers;

[ApiController]
[Route("api/v1/movies")]
public sealed class MoviesController(MovieApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMoviesAsync(
        [FromQuery(Name = "genre")] string[]? genres,
        [FromQuery(Name = "region")] string[]? regions,
        [FromQuery(Name = "decade")] string? decade,
        [FromQuery(Name = "year")] int? year,
        [FromQuery(Name = "lang")] string? language,
        [FromQuery(Name = "score")] decimal? minScore,
        [FromQuery(Name = "sort")] string sort = "popularity",
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 24,
        CancellationToken ct = default)
    {
        var filter = new MovieListFilterDto(
            genres,
            regions,
            decade,
            year,
            language,
            minScore,
            sort,
            page,
            pageSize
        );

        var data = await service.GetMovieListAsync(filter, ct);
        return Ok(data);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetMovieDetailAsync(long id, CancellationToken ct)
    {
        var data = await service.GetMovieDetailAsync(id, ct);
        if (data is null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "电影不存在"
                }
            });
        }

        return Ok(data);
    }

    [HttpGet("{id:long}/credits")]
    public async Task<IActionResult> GetMovieCreditsAsync(long id, CancellationToken ct)
    {
        var data = await service.GetMovieCreditsAsync(id, ct);
        if (data is null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "电影不存在"
                }
            });
        }

        return Ok(data);
    }
}
