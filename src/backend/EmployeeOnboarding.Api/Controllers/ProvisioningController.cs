using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>
/// IT provisioning endpoints (User Story 2797, 2798).
/// </summary>
[ApiController]
[Route("api/provisioning")]
[Produces("application/json")]
public class ProvisioningController : ControllerBase
{
    private readonly IProvisioningService _provisioningService;
    private readonly IOnboardingService _onboardingService;

    public ProvisioningController(IProvisioningService provisioningService, IOnboardingService onboardingService)
    {
        _provisioningService = provisioningService;
        _onboardingService = onboardingService;
    }

    /// <summary>
    /// Generates provisioning requests for the required systems and equipment
    /// (User Story 2797).
    /// </summary>
    [HttpPost("requests/{onboardingRecordId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ProvisioningRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProvisioningRequestDto>>> CreateDefaults(Guid onboardingRecordId, CancellationToken cancellationToken)
    {
        var requests = await _provisioningService.CreateDefaultRequestsAsync(onboardingRecordId, cancellationToken);
        if (requests.Count == 0)
        {
            var summary = await _provisioningService.GetSummaryAsync(onboardingRecordId, cancellationToken);
            if (summary is null)
            {
                return NotFound();
            }
        }

        await _onboardingService.RecalculateCompletionAsync(onboardingRecordId, cancellationToken);
        return Ok(requests);
    }

    /// <summary>Creates an additional provisioning request (User Story 2797).</summary>
    [HttpPost("requests/{onboardingRecordId:guid}/custom")]
    [ProducesResponseType(typeof(ProvisioningRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProvisioningRequestDto>> CreateCustom(
        Guid onboardingRecordId,
        [FromBody] CreateProvisioningRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await _provisioningService.CreateRequestAsync(onboardingRecordId, request, cancellationToken);
        await _onboardingService.RecalculateCompletionAsync(onboardingRecordId, cancellationToken);
        return CreatedAtAction(nameof(GetSummary), new { onboardingRecordId }, created);
    }

    /// <summary>Returns provisioning status and day-one readiness (User Story 2798).</summary>
    [HttpGet("onboarding/{onboardingRecordId:guid}")]
    [ProducesResponseType(typeof(ProvisioningSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProvisioningSummaryDto>> GetSummary(Guid onboardingRecordId, CancellationToken cancellationToken)
    {
        var summary = await _provisioningService.GetSummaryAsync(onboardingRecordId, cancellationToken);
        return summary is null ? NotFound() : Ok(summary);
    }

    /// <summary>Returns all open provisioning requests across onboarding records (User Story 2798).</summary>
    [HttpGet("open")]
    [ProducesResponseType(typeof(IReadOnlyList<ProvisioningRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProvisioningRequestDto>>> GetOpen(CancellationToken cancellationToken)
    {
        var open = await _provisioningService.GetOpenRequestsAsync(cancellationToken);
        return Ok(open);
    }

    /// <summary>
    /// Status callback used by integrated provisioning systems (User Story 2798).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ProvisioningRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProvisioningRequestDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateProvisioningStatusDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _provisioningService.UpdateStatusAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        await _onboardingService.RecalculateCompletionAsync(updated.OnboardingRecordId, cancellationToken);
        return Ok(updated);
    }
}
