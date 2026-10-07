using EmployeeOnboarding.Api.DTOs;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Sends and records onboarding notifications, reminders and escalations
/// (User Story 2792, 2795).
/// </summary>
public interface INotificationService
{
    Task<NotificationDto> SendAsync(
        string recipient,
        string kind,
        string subject,
        string body,
        Guid? onboardingRecordId = null,
        Guid? onboardingTaskId = null,
        Guid? documentRecordId = null,
        CancellationToken cancellationToken = default);

    Task<ReminderRunResultDto> RunRemindersAsync(DateOnly today, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetForRecipientAsync(string recipient, CancellationToken cancellationToken = default);
}
