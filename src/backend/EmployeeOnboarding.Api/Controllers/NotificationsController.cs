using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>
/// Notification, reminder and escalation endpoints (User Story 2795).
/// </summary>
[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Runs the reminder and escalation sweep for overdue tasks (User Story 2795).</summary>
    [HttpPost("run-reminders")]
    [ProducesResponseType(typeof(ReminderRunResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReminderRunResultDto>> RunReminders(CancellationToken cancellationToken)
    {
        var result = await _notificationService.RunRemindersAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns the notification history for a recipient (User Story 2795).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetForRecipient(
        [FromQuery] string recipient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return BadRequest(new { message = "A recipient query parameter is required." });
        }

        var notifications = await _notificationService.GetForRecipientAsync(recipient, cancellationToken);
        return Ok(notifications);
    }
}
