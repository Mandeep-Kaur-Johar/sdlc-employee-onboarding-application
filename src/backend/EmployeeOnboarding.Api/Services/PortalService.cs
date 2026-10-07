using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Returns only the signed-in employee's own tasks, documents and orientation
/// content so the portal is both personalized and least-privilege
/// (User Story 2800).
/// </summary>
public class PortalService : IPortalService
{
    private readonly OnboardingDbContext _context;
    private readonly IOnboardingService _onboardingService;
    private readonly IDocumentService _documentService;

    public PortalService(
        OnboardingDbContext context,
        IOnboardingService onboardingService,
        IDocumentService documentService)
    {
        _context = context;
        _onboardingService = onboardingService;
        _documentService = documentService;
    }

    public async Task<PortalDto?> GetPortalAsync(string employeeEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            return null;
        }

        var record = await _context.OnboardingRecords
            .Include(r => r.Tasks)
            .Include(r => r.Documents).ThenInclude(d => d.Reviews)
            .Include(r => r.TrainingAssignments)
            .Include(r => r.Milestones)
            .FirstOrDefaultAsync(r => r.CandidateEmail == employeeEmail, cancellationToken);

        if (record is null)
        {
            return null;
        }

        var detail = await _onboardingService.GetDetailAsync(record.Id, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new PortalDto
        {
            Record = detail.Record,
            Progress = detail.Progress,
            MyTasks = record.Tasks
                .Where(t => string.Equals(t.AssignedToEmail, employeeEmail, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.DueDate)
                .Select(t => TaskGenerationService.Map(t, today))
                .ToList(),
            MyDocuments = record.Documents
                .OrderBy(d => d.DocumentType)
                .Select(DocumentService.Map)
                .ToList(),
            MyTraining = record.TrainingAssignments
                .OrderBy(t => t.DueDate)
                .Select(TrainingService.Map)
                .ToList(),
            RequiredDocumentTypes = _documentService.RequiredDocumentTypes.ToList()
        };
    }
}
