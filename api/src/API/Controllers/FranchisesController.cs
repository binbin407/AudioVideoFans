using Microsoft.AspNetCore.Mvc;
using MovieSite.Application.Franchises;

namespace MovieSite.API.Controllers;

[ApiController]
[Route("api/v1/franchises")]
public sealed class FranchisesController(FranchiseApplicationService service) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetFranchiseAsync(long id, CancellationToken ct)
    {
        var result = await service.GetFranchiseDetailAsync(id, ct);
        if (result is null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "系列不存在"
                }
            });
        }

        return Ok(result);
    }
}
