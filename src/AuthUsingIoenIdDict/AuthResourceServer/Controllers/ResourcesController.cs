using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthResourceServer.Controllers
{
    [ApiController]
    [Authorize]
    public class ResourcesController : Controller
    {
        [Authorize]
        [HttpGet("")]
        public async Task<IActionResult> GetSecretResources()
        {
            var user = HttpContext.User?.Identity?.Name;
            return Ok($"user: {user}");
        }
    }
}
