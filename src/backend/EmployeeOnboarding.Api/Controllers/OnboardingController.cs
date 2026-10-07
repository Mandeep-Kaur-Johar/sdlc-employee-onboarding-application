using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>
/// Onboarding lifecycle endpoints (User Story 2788, 2789).
/// </summary>
[ApiController]
[Route("api/onboarding")]
[Produces("application/json")]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    /// <summary>
    /// Creates an onboarding record when the HR system reports an accepted offer
    /// (User Story 2788).
    /// </summary>
    [HttpPost("offer-accepted")]
    [ProducesResponseType(typeof(OnboardingRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OnboardingRecordDto>> CreateFromAcceptedOffer(
        [FromBody] AcceptedOfferEventDto offer,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await _onboardingService.CreateFromAcceptedOfferAsync(offer, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Lists onboarding records for the HR dashboard (User Story 2789).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OnboardingRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OnboardingRecordDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        var records = await _onboardingService.GetRecordsAsync(status, department, cancellationToken);
        return Ok(records);
    }

    /// <summary>Returns the full onboarding detail for one record (User Story 2789).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OnboardingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OnboardingDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _onboardingService.GetDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>
    /// Returns status, milestones, overdue tasks and completion percentage
    /// (User Story 2789).
    /// </summary>
    [HttpGet("{id:guid}/progress")]
    [ProducesResponseType(typeof(OnboardingProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OnboardingProgressDto>> GetProgress(Guid id, CancellationToken cancellationToken)
    {
        await _onboardingService.RecalculateCompletionAsync(id, cancellationToken);
        var progress = await _onboardingService.GetProgressAsync(id, cancellationToken);
        return progress is null ? NotFound() : Ok(progress);
    }

    /// <summary>Updates the lifecycle status of an onboarding record.</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(OnboardingRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OnboardingRecordDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateOnboardingStatusDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _onboardingService.UpdateStatusAsync(id, request.Status, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
}
