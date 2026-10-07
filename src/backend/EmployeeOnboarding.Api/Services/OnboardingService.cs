using EmployeeOnboarding.Api.Configuration;
using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Creates onboarding records from accepted offer events and computes the
/// progress view used by the HR dashboard (User Story 2788, 2789).
/// </summary>
public class OnboardingService : IOnboardingService
{
    private readonly OnboardingDbContext _context;
    private readonly ITaskGenerationService _taskGenerationService;
    private readonly IProvisioningService _provisioningService;
    private readonly INotificationService _notificationService;
    private readonly OnboardingOptions _options;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        OnboardingDbContext context,
        ITaskGenerationService taskGenerationService,
        IProvisioningService provisioningService,
        INotificationService notificationService,
        IOptions<OnboardingOptions> options,
        ILogger<OnboardingService> logger)
    {
        _context = context;
        _taskGenerationService = taskGenerationService;
        _provisioningService = provisioningService;
        _notificationService = notificationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OnboardingRecordDto> CreateFromAcceptedOfferAsync(AcceptedOfferEventDto offer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(offer);

        // Idempotency: the HR system may redeliver the accepted-offer event.
        var existing = await _context.OnboardingRecords
            .FirstOrDefaultAsync(r => r.CandidateEmail == offer.CandidateEmail && r.StartDate == offer.StartDate, cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation("Accepted-offer event ignored as duplicate for {Email}", offer.CandidateEmail);
            return Map(existing);
        }

        var record = new OnboardingRecord
        {
            CandidateEmail = offer.CandidateEmail.Trim(),
            FirstName = offer.FirstName.Trim(),
            LastName = offer.LastName.Trim(),
            Role = offer.Role.Trim(),
            Department = offer.Department.Trim(),
            Location = offer.Location.Trim(),
            StartDate = offer.StartDate,
            ManagerEmail = offer.ManagerEmail?.Trim(),
            OfferReference = offer.OfferReference?.Trim(),
            Status = OnboardingStatus.Initiated,
            CompletionPercentage = 0
        };

        record.Milestones.AddRange(BuildMilestones(record));
        record.TrainingAssignments.AddRange(BuildTrainingAssignments(record));

        _context.OnboardingRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        await _taskGenerationService.GenerateForRecordAsync(record.Id, cancellationToken);
        await _provisioningService.CreateDefaultRequestsAsync(record.Id, cancellationToken);

        await _notificationService.SendAsync(
            record.CandidateEmail,
            "Welcome",
            "Welcome to your onboarding portal",
            $"Hello {record.FirstName}, your onboarding has started. Sign in to the onboarding portal to complete your tasks, upload documents and finish orientation before {record.StartDate:yyyy-MM-dd}.",
            record.Id,
            cancellationToken: cancellationToken);

        await RecalculateCompletionAsync(record.Id, cancellationToken);

        var created = await _context.OnboardingRecords.FirstAsync(r => r.Id == record.Id, cancellationToken);
        _logger.LogInformation("Created onboarding record {RecordId} from accepted offer", created.Id);
        return Map(created);
    }

    public async Task<IReadOnlyList<OnboardingRecordDto>> GetRecordsAsync(string? status, string? department, CancellationToken cancellationToken = default)
    {
        var query = _context.OnboardingRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(r => r.Department == department);
        }

        var records = await query.OrderBy(r => r.StartDate).ToListAsync(cancellationToken);
        return records.Select(Map).ToList();
    }

    public async Task<OnboardingDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await LoadFullAsync(id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new OnboardingDetailDto
        {
            Record = Map(record),
            Progress = BuildProgress(record, today),
            Tasks = record.Tasks.OrderBy(t => t.DueDate).Select(t => TaskGenerationService.Map(t, today)).ToList(),
            Documents = record.Documents.OrderBy(d => d.DocumentType).Select(DocumentService.Map).ToList(),
            ProvisioningRequests = record.ProvisioningRequests.OrderBy(p => p.SystemName).Select(ProvisioningService.Map).ToList(),
            TrainingAssignments = record.TrainingAssignments.OrderBy(t => t.DueDate).Select(TrainingService.Map).ToList()
        };
    }

    public async Task<OnboardingProgressDto?> GetProgressAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await LoadFullAsync(id, cancellationToken);
        return record is null ? null : BuildProgress(record, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<OnboardingRecordDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var record = await _context.OnboardingRecords
            .FirstOrDefaultAsync(r => r.CandidateEmail == email, cancellationToken);

        return record is null ? null : Map(record);
    }

    public async Task<OnboardingRecordDto?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        if (!OnboardingStatus.IsValid(status))
        {
            throw new ArgumentException($"'{status}' is not a valid onboarding status.", nameof(status));
        }

        var record = await _context.OnboardingRecords.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.Status = status;
        record.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(record);
    }

    /// <summary>
    /// Recomputes the completion percentage across tasks, documents,
    /// provisioning and training, and advances the lifecycle status.
    /// </summary>
    public async Task<int> RecalculateCompletionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await LoadFullAsync(id, cancellationToken);
        if (record is null)
        {
            return 0;
        }

        var total = record.Tasks.Count
                  + record.ProvisioningRequests.Count
                  + record.TrainingAssignments.Count
                  + _options.RequiredDocumentTypes.Length;

        var completed = record.Tasks.Count(t => t.Status == OnboardingTaskStatus.Completed)
                      + record.ProvisioningRequests.Count(p => p.Status == ProvisioningStatus.Completed)
                      + record.TrainingAssignments.Count(t => t.Status == TrainingStatus.Completed)
                      + record.Documents.Count(d => d.Status == DocumentStatus.Approved
                            && _options.RequiredDocumentTypes.Contains(d.DocumentType, StringComparer.OrdinalIgnoreCase));

        var percentage = total == 0 ? 0 : (int)Math.Round(completed * 100.0 / total, MidpointRounding.AwayFromZero);
        percentage = Math.Clamp(percentage, 0, 100);

        record.CompletionPercentage = percentage;
        record.UpdatedAtUtc = DateTime.UtcNow;

        if (percentage >= 100)
        {
            record.Status = OnboardingStatus.Completed;
        }
        else if (record.Status == OnboardingStatus.Initiated && percentage > 0)
        {
            record.Status = OnboardingStatus.InProgress;
        }

        UpdateMilestones(record);

        await _context.SaveChangesAsync(cancellationToken);
        return percentage;
    }

    private void UpdateMilestones(OnboardingRecord record)
    {
        var requiredApproved = record.Documents.Count(d => d.Status == DocumentStatus.Approved
            && _options.RequiredDocumentTypes.Contains(d.DocumentType, StringComparer.OrdinalIgnoreCase));

        SetMilestone(record, "Offer Accepted", true);
        SetMilestone(record, "Documents Verified", requiredApproved >= _options.RequiredDocumentTypes.Length);
        SetMilestone(record, "IT Provisioning Complete",
            record.ProvisioningRequests.Count > 0 && record.ProvisioningRequests.All(p => p.Status == ProvisioningStatus.Completed));
        SetMilestone(record, "Orientation Complete",
            record.TrainingAssignments.Where(t => t.IsMandatory).All(t => t.Status == TrainingStatus.Completed));
        SetMilestone(record, "Day One Ready", record.CompletionPercentage >= 100);
    }

    private static void SetMilestone(OnboardingRecord record, string name, bool isComplete)
    {
        var milestone = record.Milestones.FirstOrDefault(m => m.Name == name);
        if (milestone is null)
        {
            return;
        }

        milestone.Status = isComplete ? "Completed" : "Pending";
        milestone.CompletedAtUtc = isComplete ? milestone.CompletedAtUtc ?? DateTime.UtcNow : null;
    }

    private static List<Milestone> BuildMilestones(OnboardingRecord record) => new()
    {
        new Milestone { Name = "Offer Accepted", Status = "Completed", TargetDate = record.StartDate.AddDays(-14), SortOrder = 1, CompletedAtUtc = DateTime.UtcNow },
        new Milestone { Name = "Documents Verified", Status = "Pending", TargetDate = record.StartDate.AddDays(-3), SortOrder = 2 },
        new Milestone { Name = "IT Provisioning Complete", Status = "Pending", TargetDate = record.StartDate.AddDays(-1), SortOrder = 3 },
        new Milestone { Name = "Orientation Complete", Status = "Pending", TargetDate = record.StartDate.AddDays(14), SortOrder = 4 },
        new Milestone { Name = "Day One Ready", Status = "Pending", TargetDate = record.StartDate, SortOrder = 5 }
    };

    private static List<TrainingAssignment> BuildTrainingAssignments(OnboardingRecord record) =>
        OnboardingDbSeeder.DefaultCourses
            .Select(course => new TrainingAssignment
            {
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                IsMandatory = course.Mandatory,
                Status = TrainingStatus.NotStarted,
                DueDate = record.StartDate.AddDays(course.OffsetDays)
            })
            .ToList();

    private Task<OnboardingRecord?> LoadFullAsync(Guid id, CancellationToken cancellationToken) =>
        _context.OnboardingRecords
            .Include(r => r.Tasks)
            .Include(r => r.Documents).ThenInclude(d => d.Reviews)
            .Include(r => r.ProvisioningRequests)
            .Include(r => r.TrainingAssignments)
            .Include(r => r.Milestones)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    internal OnboardingProgressDto BuildProgress(OnboardingRecord record, DateOnly today) => new()
    {
        OnboardingRecordId = record.Id,
        EmployeeName = record.FullName,
        Status = record.Status,
        StartDate = record.StartDate,
        CompletionPercentage = record.CompletionPercentage,
        TotalTasks = record.Tasks.Count,
        CompletedTasks = record.Tasks.Count(t => t.Status == OnboardingTaskStatus.Completed),
        OverdueTasks = record.Tasks.Count(t => t.IsOverdue(today)),
        PendingDocuments = record.Documents.Count(d => d.Status == DocumentStatus.Submitted),
        ApprovedDocuments = record.Documents.Count(d => d.Status == DocumentStatus.Approved),
        RejectedDocuments = record.Documents.Count(d => d.Status == DocumentStatus.Rejected),
        PendingProvisioning = record.ProvisioningRequests.Count(p => p.Status != ProvisioningStatus.Completed),
        CompletedProvisioning = record.ProvisioningRequests.Count(p => p.Status == ProvisioningStatus.Completed),
        CompletedTraining = record.TrainingAssignments.Count(t => t.Status == TrainingStatus.Completed),
        TotalTraining = record.TrainingAssignments.Count,
        Milestones = record.Milestones
            .OrderBy(m => m.SortOrder)
            .Select(m => new MilestoneDto
            {
                Id = m.Id,
                Name = m.Name,
                Status = m.Status,
                TargetDate = m.TargetDate,
                CompletedAtUtc = m.CompletedAtUtc,
                SortOrder = m.SortOrder
            })
            .ToList()
    };

    internal static OnboardingRecordDto Map(OnboardingRecord r) => new()
    {
        Id = r.Id,
        CandidateEmail = r.CandidateEmail,
        FirstName = r.FirstName,
        LastName = r.LastName,
        FullName = r.FullName,
        Role = r.Role,
        Department = r.Department,
        Location = r.Location,
        StartDate = r.StartDate,
        ManagerEmail = r.ManagerEmail,
        OfferReference = r.OfferReference,
        Status = r.Status,
        CompletionPercentage = r.CompletionPercentage,
        CreatedAtUtc = r.CreatedAtUtc,
        UpdatedAtUtc = r.UpdatedAtUtc
    };
}
