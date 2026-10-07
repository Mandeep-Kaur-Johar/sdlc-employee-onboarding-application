using EmployeeOnboarding.Api.DTOs;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Secure document collection and HR review (User Story 2791, 2792).
/// </summary>
public interface IDocumentService
{
    Task<DocumentRecordDto> UploadAsync(
        Guid onboardingRecordId,
        string documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        string uploadedByEmail,
        CancellationToken cancellationToken = default);

    Task<DocumentComplianceDto?> GetComplianceAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentRecordDto>> GetPendingReviewAsync(CancellationToken cancellationToken = default);

    Task<DocumentRecordDto?> ReviewAsync(Guid documentId, DocumentReviewRequestDto request, CancellationToken cancellationToken = default);

    IReadOnlyList<string> RequiredDocumentTypes { get; }

    void ValidateUpload(string contentType, long sizeBytes);
}
