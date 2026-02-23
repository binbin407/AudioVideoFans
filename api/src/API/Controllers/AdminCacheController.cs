using Microsoft.AspNetCore.Mvc;
using MovieSite.Application.Common;

namespace MovieSite.API.Controllers;

public sealed class AdminCacheController(CacheInvalidationService cacheInvalidationService) : AdminControllerBase
{
    [HttpGet("cache/flush")]
    public async Task<IActionResult> FlushAsync()
    {
        await cacheInvalidationService.FlushPublicListCachesAsync();
        return Ok(new { success = true });
    }
}
