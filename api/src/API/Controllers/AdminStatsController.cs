using Microsoft.AspNetCore.Mvc;

namespace MovieSite.API.Controllers;

public sealed class AdminStatsController : AdminControllerBase
{
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        return Ok(new
        {
            totalMovies = 0,
            totalTvSeries = 0,
            totalAnime = 0
        });
    }
}
