using EmployeeOnboarding.Api.Configuration;
using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Stores uploaded documents securely, tracks checklist completion
/// (User Story 2791) and applies the HR approval workflow with resubmission
/// notifications (User Story 2792).
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly OnboardingDbContext _context;
    private readonly IDocumentStorage _storage;
    private readonly INotificationService _notificationService;
    private readonly OnboardingOptions _options;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        OnboardingDbContext context,
        IDocumentStorage storage,
        INotificationService notificationService,
        IOptions<OnboardingOptions> options,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _storage = storage;
        _notificationService = notificationService;
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyList<string> RequiredDocumentTypes => _options.RequiredDocumentTypes;

    public void ValidateUpload(string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentException("The uploaded file is empty.", nameof(sizeBytes));
        }

        if (sizeBytes > _options.MaxUploadSizeBytes)
        {
            throw new ArgumentException(
                $"The uploaded file exceeds the maximum size of {_options.MaxUploadSizeBytes / (1024 * 1024)} MB.",
                nameof(sizeBytes));
        }

        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", _options.AllowedContentTypes)}.",
                nameof(contentType));
        }
    }

    public async Task<DocumentRecordDto> UploadAsync(
        Guid onboardingRecordId,
        string documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        string uploadedByEmail,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new ArgumentException("A document type is required.", nameof(documentType));
        }

        ValidateUpload(contentType, sizeBytes);

        var record = await _context.OnboardingRecords
            .FirstOrDefaultAsync(r => r.Id == onboardingRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Onboarding record '{onboardingRecordId}' was not found.");

        var storagePath = await _storage.SaveAsync(record.Id, fileName, content, cancellationToken);

        var document = new DocumentRecord
        {
            OnboardingRecordId = record.Id,
            DocumentType = documentType.Trim(),
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StoragePath = storagePath,
            Status = DocumentStatus.Submitted,
            UploadedByEmail = uploadedByEmail,
            UploadedAtUtc = DateTime.UtcNow
        };

        _context.DocumentRecords.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            "hr-compliance@contoso.example",
            "DocumentSubmitted",
            $"Document submitted for review: {document.DocumentType}",
            $"{record.FullName} submitted '{document.DocumentType}' for compliance review.",
            record.Id,
            documentRecordId: document.Id,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Stored document {DocumentId} of type {DocumentType} for record {RecordId}",
            document.Id, document.DocumentType, record.Id);

        return Map(document);
    }

    public async Task<DocumentComplianceDto?> GetComplianceAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.OnboardingRecords.AnyAsync(r => r.Id == onboardingRecordId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var documents = await _context.DocumentRecords
            .Include(d => d.Reviews)
            .Where(d => d.OnboardingRecordId == onboardingRecordId)
            .ToListAsync(cancellationToken);

        var approvedTypes = documents
            .Where(d => d.Status == DocumentStatus.Approved)
            .Select(d => d.DocumentType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = _options.RequiredDocumentTypes
            .Where(t => !approvedTypes.Contains(t))
            .ToList();

        var required = _options.RequiredDocumentTypes.Length;
        var approvedRequired = required - missing.Count;

        return new DocumentComplianceDto
        {
            OnboardingRecordId = onboardingRecordId,
            RequiredCount = required,
            SubmittedCount = documents.Count(d => d.Status == DocumentStatus.Submitted),
            ApprovedCount = documents.Count(d => d.Status == DocumentStatus.Approved),
            RejectedCount = documents.Count(d => d.Status == DocumentStatus.Rejected),
            CompletionPercentage = required == 0 ? 100 : (int)Math.Round(approvedRequired * 100.0 / required, MidpointRounding.AwayFromZero),
            MissingDocumentTypes = missing,
            Documents = documents.OrderBy(d => d.DocumentType).Select(Map).ToList()
        };
    }

    public async Task<IReadOnlyList<DocumentRecordDto>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _context.DocumentRecords
            .Include(d => d.Reviews)
            .Where(d => d.Status == DocumentStatus.Submitted)
            .OrderBy(d => d.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return documents.Select(Map).ToList();
    }

    public async Task<DocumentRecordDto?> ReviewAsync(Guid documentId, DocumentReviewRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var decision = request.Decision?.Trim() ?? string.Empty;
        if (!string.Equals(decision, DocumentStatus.Approved, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(decision, DocumentStatus.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The review decision must be 'Approved' or 'Rejected'.", nameof(request));
        }

        var isRejection = string.Equals(decision, DocumentStatus.Rejected, StringComparison.OrdinalIgnoreCase);

        if (isRejection && string.IsNullOrWhiteSpace(request.Comments))
        {
            throw new ArgumentException("Comments are required when a document is rejected.", nameof(request));
        }

        var document = await _context.DocumentRecords
            .Include(d => d.Reviews)
            .Include(d => d.OnboardingRecord)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return null;
        }

        var normalized = isRejection ? DocumentStatus.Rejected : DocumentStatus.Approved;

        var review = new DocumentReview
        {
            DocumentRecordId = document.Id,
            ReviewerEmail = request.ReviewerEmail,
            Decision = normalized,
            Comments = request.Comments,
            ReviewedAtUtc = DateTime.UtcNow
        };

        document.Reviews.Add(review);
        document.Status = normalized;
        await _context.SaveChangesAsync(cancellationToken);

        var employeeEmail = document.OnboardingRecord?.CandidateEmail ?? document.UploadedByEmail;

        if (isRejection)
        {
            await _notificationService.SendAsync(
                employeeEmail,
                "DocumentRejected",
                $"Action required: resubmit {document.DocumentType}",
                $"Your '{document.DocumentType}' document was rejected. Reason: {request.Comments}. Please upload a corrected document.",
                document.OnboardingRecordId,
                documentRecordId: document.Id,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _notificationService.SendAsync(
                employeeEmail,
                "DocumentApproved",
                $"Document approved: {document.DocumentType}",
                $"Your '{document.DocumentType}' document has been approved. No further action is required.",
                document.OnboardingRecordId,
                documentRecordId: document.Id,
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Document {DocumentId} reviewed as {Decision} by {Reviewer}",
            document.Id, normalized, request.ReviewerEmail);

        return Map(document);
    }

    internal static DocumentRecordDto Map(DocumentRecord d)
    {
        var latest = d.Reviews.OrderByDescending(r => r.ReviewedAtUtc).FirstOrDefault();

        return new DocumentRecordDto
        {
            Id = d.Id,
            OnboardingRecordId = d.OnboardingRecordId,
            DocumentType = d.DocumentType,
            FileName = d.FileName,
            ContentType = d.ContentType,
            SizeBytes = d.SizeBytes,
            Status = d.Status,
            UploadedByEmail = d.UploadedByEmail,
            UploadedAtUtc = d.UploadedAtUtc,
            LatestReviewComments = latest?.Comments,
            LatestReviewerEmail = latest?.ReviewerEmail,
            LatestReviewedAtUtc = latest?.ReviewedAtUtc
        };
    }
}
