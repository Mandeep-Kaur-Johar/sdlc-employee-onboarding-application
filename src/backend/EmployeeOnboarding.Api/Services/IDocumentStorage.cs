namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Abstraction over secure document storage. The production implementation
/// targets a private Azure Blob Storage container; the local implementation
/// writes to a protected application data folder (User Story 2791).
/// </summary>
public interface IDocumentStorage
{
    Task<string> SaveAsync(Guid onboardingRecordId, string fileName, Stream content, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
