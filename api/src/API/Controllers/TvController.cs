using Microsoft.AspNetCore.Mvc;
using MovieSite.Application.Common;
using MovieSite.Application.TvSeries;
using MovieSite.Application.TvSeries.DTOs;

namespace MovieSite.API.Controllers;

[ApiController]
[Route("api/v1/tv")]
public sealed class TvController(TvSeriesApplicationService tvService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TvMediaCardDto>>> GetList(
        [FromQuery] string[]? genres,
        [FromQuery] string[]? regions,
        [FromQuery] string? decade,
        [FromQuery] int? year,
        [FromQuery] string? language,
        [FromQuery] decimal? minScore,
        [FromQuery] string sort = "popularity",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery(Name = "status")] string[]? airStatus = null,
        CancellationToken ct = default)
    {
        var filter = new TvListFilterDto(
            Genres: genres,
            Regions: regions,
            Decade: decade,
            Year: year,
            Language: language,
            MinScore: minScore,
            Sort: sort,
            Page: page,
            PageSize: pageSize,
            AirStatus: airStatus
        );

        var result = await tvService.GetTvListAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TvSeriesDetailDto>> GetDetail(long id, CancellationToken ct = default)
    {
        var result = await tvService.GetTvDetailAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}/seasons/{seasonNumber:int}")]
    public async Task<ActionResult<SeasonDetailDto>> GetSeasonDetail(long id, int seasonNumber, CancellationToken ct = default)
    {
        var result = await tvService.GetSeasonDetailAsync(id, seasonNumber, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}/similar")]
    public async Task<ActionResult<object>> GetSimilar(long id, CancellationToken ct = default)
    {
        var result = await tvService.GetSimilarAsync(id, ct);
        return Ok(new { data = result });
    }
}
