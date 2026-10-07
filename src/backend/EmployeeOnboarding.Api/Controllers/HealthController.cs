using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>Liveness endpoint used by CI and Azure App Service probes.</summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status = "Healthy",
        service = "EmployeeOnboarding.Api",
        utc = DateTime.UtcNow
    });
}
