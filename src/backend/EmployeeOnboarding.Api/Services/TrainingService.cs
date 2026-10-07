using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Records orientation and training completion so that it is visible to HR and
/// managers (User Story 2801).
/// </summary>
public class TrainingService : ITrainingService
{
    private readonly OnboardingDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TrainingService> _logger;

    public TrainingService(
        OnboardingDbContext context,
        INotificationService notificationService,
        ILogger<TrainingService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TrainingAssignmentDto>?> GetForRecordAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.OnboardingRecords.AnyAsync(r => r.Id == onboardingRecordId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var assignments = await _context.TrainingAssignments
            .Where(t => t.OnboardingRecordId == onboardingRecordId)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

        return assignments.Select(Map).ToList();
    }

    public async Task<TrainingAssignmentDto?> StartAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.TrainingAssignments
            .FirstOrDefaultAsync(t => t.Id == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        if (assignment.Status == TrainingStatus.NotStarted)
        {
            assignment.Status = TrainingStatus.InProgress;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Map(assignment);
    }

    public async Task<TrainingAssignmentDto?> CompleteAsync(Guid assignmentId, CompleteTrainingDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ScorePercentage is < 0 or > 100)
        {
            throw new ArgumentException("The score must be between 0 and 100.", nameof(request));
        }

        var assignment = await _context.TrainingAssignments
            .Include(t => t.OnboardingRecord)
            .FirstOrDefaultAsync(t => t.Id == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        assignment.Status = TrainingStatus.Completed;
        assignment.ScorePercentage = request.ScorePercentage;
        assignment.CompletedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var record = assignment.OnboardingRecord;
        var managerEmail = record?.ManagerEmail;

        if (!string.IsNullOrWhiteSpace(managerEmail))
        {
            await _notificationService.SendAsync(
                managerEmail,
                "TrainingCompleted",
                $"Training completed: {assignment.CourseName}",
                $"{record!.FullName} completed '{assignment.CourseName}' ({assignment.CourseCode}) on {assignment.CompletedAtUtc:yyyy-MM-dd}.",
                assignment.OnboardingRecordId,
                cancellationToken: cancellationToken);
        }

        await _notificationService.SendAsync(
            "hr-coordinator@contoso.example",
            "TrainingCompleted",
            $"Training completed: {assignment.CourseName}",
            $"{record?.FullName ?? "An employee"} completed '{assignment.CourseName}' ({assignment.CourseCode}).",
            assignment.OnboardingRecordId,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Training assignment {AssignmentId} completed", assignment.Id);

        return Map(assignment);
    }

    internal static TrainingAssignmentDto Map(TrainingAssignment t) => new()
    {
        Id = t.Id,
        OnboardingRecordId = t.OnboardingRecordId,
        CourseCode = t.CourseCode,
        CourseName = t.CourseName,
        IsMandatory = t.IsMandatory,
        ContentUrl = t.ContentUrl,
        Status = t.Status,
        ScorePercentage = t.ScorePercentage,
        DueDate = t.DueDate,
        CompletedAtUtc = t.CompletedAtUtc
    };
}
