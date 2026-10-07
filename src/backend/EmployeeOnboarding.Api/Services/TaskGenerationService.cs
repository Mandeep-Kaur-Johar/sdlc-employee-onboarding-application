using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Template rule engine that creates onboarding tasks automatically based on the
/// employee role, department and location (User Story 2794) and notifies the
/// assignee (User Story 2795).
/// </summary>
public class TaskGenerationService : ITaskGenerationService
{
    private readonly OnboardingDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TaskGenerationService> _logger;

    public TaskGenerationService(
        OnboardingDbContext context,
        INotificationService notificationService,
        ILogger<TaskGenerationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OnboardingTaskDto>> GenerateForRecordAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _context.OnboardingRecords
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == onboardingRecordId, cancellationToken);

        if (record is null)
        {
            return Array.Empty<OnboardingTaskDto>();
        }

        var templates = await _context.TaskTemplates
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        var selected = SelectTemplates(templates, record);
        var existingTemplateIds = record.Tasks
            .Where(t => t.TaskTemplateId.HasValue)
            .Select(t => t.TaskTemplateId!.Value)
            .ToHashSet();

        var created = new List<OnboardingTask>();

        foreach (var template in selected)
        {
            if (existingTemplateIds.Contains(template.Id))
            {
                continue;
            }

            var task = new OnboardingTask
            {
                OnboardingRecordId = record.Id,
                TaskTemplateId = template.Id,
                Title = template.Title,
                Description = template.Description,
                Category = template.Category,
                AssigneeRole = template.AssigneeRole,
                AssignedToEmail = ResolveAssignee(template.AssigneeRole, record),
                DueDate = record.StartDate.AddDays(template.OffsetDays),
                Status = OnboardingTaskStatus.Pending
            };

            created.Add(task);
        }

        if (created.Count > 0)
        {
            _context.OnboardingTasks.AddRange(created);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var task in created)
            {
                await _notificationService.SendAsync(
                    task.AssignedToEmail,
                    "TaskAssigned",
                    $"New onboarding task: {task.Title}",
                    $"You have been assigned '{task.Title}' for {record.FullName}. It is due on {task.DueDate:yyyy-MM-dd}.",
                    record.Id,
                    task.Id,
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Generated {Count} onboarding tasks for record {RecordId}", created.Count, record.Id);
        }

        var all = await _context.OnboardingTasks
            .Where(t => t.OnboardingRecordId == record.Id)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return all.Select(t => Map(t, today)).ToList();
    }

    public async Task<IReadOnlyList<OnboardingTaskDto>> GetTasksAsync(
        Guid? onboardingRecordId,
        string? assignedToEmail,
        string? status,
        bool overdueOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OnboardingTasks.AsQueryable();

        if (onboardingRecordId.HasValue)
        {
            query = query.Where(t => t.OnboardingRecordId == onboardingRecordId.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedToEmail))
        {
            query = query.Where(t => t.AssignedToEmail == assignedToEmail);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (overdueOnly)
        {
            query = query.Where(t => t.Status != OnboardingTaskStatus.Completed && t.DueDate < today);
        }

        var tasks = await query.OrderBy(t => t.DueDate).ToListAsync(cancellationToken);
        return tasks.Select(t => Map(t, today)).ToList();
    }

    public async Task<OnboardingTaskDto?> UpdateStatusAsync(Guid taskId, string status, CancellationToken cancellationToken = default)
    {
        if (!OnboardingTaskStatus.IsValid(status))
        {
            throw new ArgumentException($"'{status}' is not a valid task status.", nameof(status));
        }

        var task = await _context.OnboardingTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.Status = status;
        task.CompletedAtUtc = status == OnboardingTaskStatus.Completed ? DateTime.UtcNow : null;

        if (status == OnboardingTaskStatus.Completed)
        {
            task.IsEscalated = false;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Map(task, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>
    /// Applies the role, department and location matching rules. A template value
    /// of null, empty or "*" matches any employee attribute.
    /// </summary>
    public IReadOnlyList<TaskTemplate> SelectTemplates(IEnumerable<TaskTemplate> templates, OnboardingRecord record)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(record);

        return templates
            .Where(t => Matches(t.Role, record.Role)
                     && Matches(t.Department, record.Department)
                     && Matches(t.Location, record.Location))
            .OrderBy(t => t.OffsetDays)
            .ThenBy(t => t.Title, StringComparer.Ordinal)
            .ToList();
    }

    private static bool Matches(string? templateValue, string employeeValue)
    {
        if (string.IsNullOrWhiteSpace(templateValue) || templateValue == "*")
        {
            return true;
        }

        return string.Equals(templateValue, employeeValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveAssignee(string assigneeRole, OnboardingRecord record) => assigneeRole switch
    {
        AppRoles.Employee => record.CandidateEmail,
        AppRoles.Manager => record.ManagerEmail ?? record.CandidateEmail,
        AppRoles.HRSpecialist => "hr-compliance@contoso.example",
        AppRoles.HRCoordinator => "hr-coordinator@contoso.example",
        AppRoles.ITAdmin => "it-provisioning@contoso.example",
        _ => record.CandidateEmail
    };

    internal static OnboardingTaskDto Map(OnboardingTask t, DateOnly today) => new()
    {
        Id = t.Id,
        OnboardingRecordId = t.OnboardingRecordId,
        Title = t.Title,
        Description = t.Description,
        Category = t.Category,
        AssignedToEmail = t.AssignedToEmail,
        AssigneeRole = t.AssigneeRole,
        DueDate = t.DueDate,
        Status = t.Status,
        IsOverdue = t.IsOverdue(today),
        IsEscalated = t.IsEscalated,
        CompletedAtUtc = t.CompletedAtUtc
    };
}
