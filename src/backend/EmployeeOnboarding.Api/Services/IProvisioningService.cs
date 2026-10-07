using EmployeeOnboarding.Api.DTOs;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// IT provisioning request creation and tracking (User Story 2797, 2798).
/// </summary>
public interface IProvisioningService
{
    Task<IReadOnlyList<ProvisioningRequestDto>> CreateDefaultRequestsAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default);

    Task<ProvisioningRequestDto> CreateRequestAsync(Guid onboardingRecordId, CreateProvisioningRequestDto request, CancellationToken cancellationToken = default);

    Task<ProvisioningSummaryDto?> GetSummaryAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProvisioningRequestDto>> GetOpenRequestsAsync(CancellationToken cancellationToken = default);

    Task<ProvisioningRequestDto?> UpdateStatusAsync(Guid requestId, UpdateProvisioningStatusDto update, CancellationToken cancellationToken = default);
}
