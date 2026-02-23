using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MovieSite.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
public abstract class AdminControllerBase : ControllerBase
{
}
