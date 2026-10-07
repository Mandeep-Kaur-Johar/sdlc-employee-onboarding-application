using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>
/// Task generation and workflow endpoints (User Story 2794, 2795).
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskGenerationService _taskService;
    private readonly IOnboardingService _onboardingService;

    public TasksController(ITaskGenerationService taskService, IOnboardingService onboardingService)
    {
        _taskService = taskService;
        _onboardingService = onboardingService;
    }

    /// <summary>
    /// Generates tasks from templates based on role, location and department
    /// (User Story 2794).
    /// </summary>
    [HttpPost("generate/{onboardingRecordId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<OnboardingTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OnboardingTaskDto>>> Generate(Guid onboardingRecordId, CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GenerateForRecordAsync(onboardingRecordId, cancellationToken);
        if (tasks.Count == 0)
        {
            var exists = await _onboardingService.GetProgressAsync(onboardingRecordId, cancellationToken);
            if (exists is null)
            {
                return NotFound();
            }
        }

        await _onboardingService.RecalculateCompletionAsync(onboardingRecordId, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>Returns tasks with optional filters including overdue only (User Story 2794, 2795).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OnboardingTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OnboardingTaskDto>>> GetTasks(
        [FromQuery] Guid? onboardingRecordId,
        [FromQuery] string? assignedToEmail,
        [FromQuery] string? status,
        [FromQuery] bool overdueOnly = false,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _taskService.GetTasksAsync(onboardingRecordId, assignedToEmail, status, overdueOnly, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>Updates a task status (User Story 2794).</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(OnboardingTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OnboardingTaskDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateTaskStatusDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _taskService.UpdateStatusAsync(id, request.Status, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        await _onboardingService.RecalculateCompletionAsync(updated.OnboardingRecordId, cancellationToken);
        return Ok(updated);
    }
}
