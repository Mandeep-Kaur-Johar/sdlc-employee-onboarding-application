using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>
/// Orientation and training endpoints (User Story 2801).
/// </summary>
[ApiController]
[Route("api/training")]
[Produces("application/json")]
public class TrainingController : ControllerBase
{
    private readonly ITrainingService _trainingService;
    private readonly IOnboardingService _onboardingService;

    public TrainingController(ITrainingService trainingService, IOnboardingService onboardingService)
    {
        _trainingService = trainingService;
        _onboardingService = onboardingService;
    }

    /// <summary>Returns training assignments visible to HR and managers (User Story 2801).</summary>
    [HttpGet("onboarding/{onboardingRecordId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<TrainingAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TrainingAssignmentDto>>> GetForOnboarding(Guid onboardingRecordId, CancellationToken cancellationToken)
    {
        var assignments = await _trainingService.GetForRecordAsync(onboardingRecordId, cancellationToken);
        return assignments is null ? NotFound() : Ok(assignments);
    }

    /// <summary>Marks a training assignment as started (User Story 2801).</summary>
    [HttpPut("{id:guid}/start")]
    [ProducesResponseType(typeof(TrainingAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingAssignmentDto>> Start(Guid id, CancellationToken cancellationToken)
    {
        var started = await _trainingService.StartAsync(id, cancellationToken);
        return started is null ? NotFound() : Ok(started);
    }

    /// <summary>Records training completion for HR and manager visibility (User Story 2801).</summary>
    [HttpPut("{id:guid}/complete")]
    [ProducesResponseType(typeof(TrainingAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingAssignmentDto>> Complete(
        Guid id,
        [FromBody] CompleteTrainingDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var completed = await _trainingService.CompleteAsync(id, request, cancellationToken);
        if (completed is null)
        {
            return NotFound();
        }

        await _onboardingService.RecalculateCompletionAsync(completed.OnboardingRecordId, cancellationToken);
        return Ok(completed);
    }
}
