using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>
/// Personalized employee onboarding portal (User Story 2800).
/// </summary>
[ApiController]
[Route("api/portal")]
[Produces("application/json")]
public class PortalController : ControllerBase
{
    private readonly IPortalService _portalService;
    private readonly IOnboardingService _onboardingService;

    public PortalController(IPortalService portalService, IOnboardingService onboardingService)
    {
        _portalService = portalService;
        _onboardingService = onboardingService;
    }

    /// <summary>
    /// Returns the signed-in employee's assigned tasks, documents and onboarding
    /// content. The employee identity is taken from the authenticated principal
    /// when present, so one employee can never read another employee's record
    /// (User Story 2800).
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PortalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortalDto>> GetMyPortal(
        [FromQuery] string? email,
        CancellationToken cancellationToken)
    {
        var identityEmail = User?.Identity?.IsAuthenticated == true
            ? User.FindFirst("preferred_username")?.Value ?? User.Identity?.Name
            : null;

        var effectiveEmail = identityEmail ?? email;

        if (string.IsNullOrWhiteSpace(effectiveEmail))
        {
            return BadRequest(new { message = "An authenticated identity or email query parameter is required." });
        }

        var record = await _onboardingService.GetByEmailAsync(effectiveEmail, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        await _onboardingService.RecalculateCompletionAsync(record.Id, cancellationToken);

        var portal = await _portalService.GetPortalAsync(effectiveEmail, cancellationToken);
        return portal is null ? NotFound() : Ok(portal);
    }
}
