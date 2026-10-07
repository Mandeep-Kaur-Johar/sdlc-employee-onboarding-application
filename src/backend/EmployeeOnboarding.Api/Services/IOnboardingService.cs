using EmployeeOnboarding.Api.DTOs;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Onboarding lifecycle operations (User Story 2788, 2789).
/// </summary>
public interface IOnboardingService
{
    Task<OnboardingRecordDto> CreateFromAcceptedOfferAsync(AcceptedOfferEventDto offer, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OnboardingRecordDto>> GetRecordsAsync(string? status, string? department, CancellationToken cancellationToken = default);

    Task<OnboardingDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OnboardingProgressDto?> GetProgressAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OnboardingRecordDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<OnboardingRecordDto?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);

    Task<int> RecalculateCompletionAsync(Guid id, CancellationToken cancellationToken = default);
}
