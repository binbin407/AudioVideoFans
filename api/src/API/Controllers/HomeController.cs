using Microsoft.AspNetCore.Mvc;
using MovieSite.Application.Home;

namespace MovieSite.API.Controllers;

[ApiController]
[Route("api/v1/home")]
public sealed class HomeController(HomeApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHomeAsync(CancellationToken ct)
    {
        var data = await service.GetHomeDataAsync(ct);
        return Ok(data);
    }
}
