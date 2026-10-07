using EmployeeOnboarding.Api.Configuration;
using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Persists every notification as an auditable record and applies the reminder
/// and escalation rules for overdue onboarding tasks (User Story 2795).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly OnboardingDbContext _context;
    private readonly OnboardingOptions _options;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        OnboardingDbContext context,
        IOptions<OnboardingOptions> options,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<NotificationDto> SendAsync(
        string recipient,
        string kind,
        string subject,
        string body,
        Guid? onboardingRecordId = null,
        Guid? onboardingTaskId = null,
        Guid? documentRecordId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("A notification recipient is required.", nameof(recipient));
        }

        var notification = new Notification
        {
            Recipient = recipient,
            Kind = kind,
            Channel = "Email",
            Subject = Truncate(subject, 300),
            Body = Truncate(body, 2000),
            OnboardingRecordId = onboardingRecordId,
            OnboardingTaskId = onboardingTaskId,
            DocumentRecordId = documentRecordId,
            SentAtUtc = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification {Kind} queued for {Recipient}", kind, recipient);
        return Map(notification);
    }

    public async Task<ReminderRunResultDto> RunRemindersAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var result = new ReminderRunResultDto { ExecutedAtUtc = DateTime.UtcNow };

        var openTasks = await _context.OnboardingTasks
            .Include(t => t.OnboardingRecord)
            .Where(t => t.Status != OnboardingTaskStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var task in openTasks)
        {
            var daysUntilDue = task.DueDate.DayNumber - today.DayNumber;

            if (daysUntilDue < 0)
            {
                var daysOverdue = -daysUntilDue;
                var reminder = await SendAsync(
                    task.AssignedToEmail,
                    "Reminder",
                    $"Overdue onboarding task: {task.Title}",
                    $"The task '{task.Title}' was due on {task.DueDate:yyyy-MM-dd} and is {daysOverdue} day(s) overdue.",
                    task.OnboardingRecordId,
                    task.Id,
                    cancellationToken: cancellationToken);

                result.RemindersSent++;
                result.Notifications.Add(reminder);

                if (daysOverdue >= _options.EscalationThresholdDays && !task.IsEscalated)
                {
                    var escalationRecipient = task.OnboardingRecord?.ManagerEmail;
                    if (!string.IsNullOrWhiteSpace(escalationRecipient))
                    {
                        var escalation = await SendAsync(
                            escalationRecipient,
                            "Escalation",
                            $"Escalation: onboarding task overdue by {daysOverdue} days",
                            $"The task '{task.Title}' assigned to {task.AssignedToEmail} is {daysOverdue} day(s) overdue and has been escalated.",
                            task.OnboardingRecordId,
                            task.Id,
                            cancellationToken: cancellationToken);

                        result.EscalationsSent++;
                        result.Notifications.Add(escalation);
                    }

                    task.IsEscalated = true;
                }

                task.LastReminderSentUtc = DateTime.UtcNow;
            }
            else if (daysUntilDue <= _options.ReminderLeadDays)
            {
                var reminder = await SendAsync(
                    task.AssignedToEmail,
                    "Reminder",
                    $"Onboarding task due soon: {task.Title}",
                    $"The task '{task.Title}' is due on {task.DueDate:yyyy-MM-dd}. Please complete it before the due date.",
                    task.OnboardingRecordId,
                    task.Id,
                    cancellationToken: cancellationToken);

                result.RemindersSent++;
                result.Notifications.Add(reminder);
                task.LastReminderSentUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForRecipientAsync(string recipient, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.Recipient == recipient)
            .OrderByDescending(n => n.SentAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return notifications.Select(Map).ToList();
    }

    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) ? string.Empty :
        value.Length <= maxLength ? value : value[..maxLength];

    internal static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        Recipient = n.Recipient,
        Channel = n.Channel,
        Kind = n.Kind,
        Subject = n.Subject,
        Body = n.Body,
        SentAtUtc = n.SentAtUtc
    };
}
