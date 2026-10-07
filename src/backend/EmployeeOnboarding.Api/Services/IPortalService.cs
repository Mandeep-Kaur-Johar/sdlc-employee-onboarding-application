using EmployeeOnboarding.Api.DTOs;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Builds the personalized onboarding portal payload (User Story 2800).
/// </summary>
public interface IPortalService
{
    Task<PortalDto?> GetPortalAsync(string employeeEmail, CancellationToken cancellationToken = default);
}
