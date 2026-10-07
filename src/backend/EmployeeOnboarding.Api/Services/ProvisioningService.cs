using EmployeeOnboarding.Api.Configuration;
using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Creates provisioning requests automatically for required systems and
/// equipment (User Story 2797) and exposes provisioning readiness that is
/// updated by integrated systems (User Story 2798).
/// </summary>
public class ProvisioningService : IProvisioningService
{
    private readonly OnboardingDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly OnboardingOptions _options;
    private readonly ILogger<ProvisioningService> _logger;

    public ProvisioningService(
        OnboardingDbContext context,
        INotificationService notificationService,
        IOptions<OnboardingOptions> options,
        ILogger<ProvisioningService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProvisioningRequestDto>> CreateDefaultRequestsAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _context.OnboardingRecords
            .Include(r => r.ProvisioningRequests)
            .FirstOrDefaultAsync(r => r.Id == onboardingRecordId, cancellationToken);

        if (record is null)
        {
            return Array.Empty<ProvisioningRequestDto>();
        }

        var existing = record.ProvisioningRequests
            .Select(p => p.SystemName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = new List<ProvisioningRequest>();

        foreach (var systemName in _options.DefaultProvisioningSystems)
        {
            if (existing.Contains(systemName))
            {
                continue;
            }

            created.Add(new ProvisioningRequest
            {
                OnboardingRecordId = record.Id,
                SystemName = systemName,
                ItemType = IsEquipment(systemName) ? "Equipment" : "Account",
                Details = $"Provision '{systemName}' for {record.FullName} ({record.Role}, {record.Department}, {record.Location}).",
                Status = ProvisioningStatus.Requested,
                RequiredByDate = record.StartDate.AddDays(-1)
            });
        }

        if (created.Count > 0)
        {
            _context.ProvisioningRequests.AddRange(created);
            await _context.SaveChangesAsync(cancellationToken);

            await _notificationService.SendAsync(
                "it-provisioning@contoso.example",
                "ProvisioningRequested",
                $"{created.Count} provisioning request(s) raised for {record.FullName}",
                $"Provisioning requests were generated for {record.FullName} starting {record.StartDate:yyyy-MM-dd}: {string.Join(", ", created.Select(c => c.SystemName))}.",
                record.Id,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Created {Count} provisioning requests for record {RecordId}", created.Count, record.Id);
        }

        var all = await _context.ProvisioningRequests
            .Where(p => p.OnboardingRecordId == record.Id)
            .OrderBy(p => p.SystemName)
            .ToListAsync(cancellationToken);

        return all.Select(Map).ToList();
    }

    public async Task<ProvisioningRequestDto> CreateRequestAsync(Guid onboardingRecordId, CreateProvisioningRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = await _context.OnboardingRecords
            .FirstOrDefaultAsync(r => r.Id == onboardingRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Onboarding record '{onboardingRecordId}' was not found.");

        var entity = new ProvisioningRequest
        {
            OnboardingRecordId = record.Id,
            ItemType = request.ItemType.Trim(),
            SystemName = request.SystemName.Trim(),
            Details = request.Details,
            Status = ProvisioningStatus.Requested,
            RequiredByDate = record.StartDate.AddDays(-1)
        };

        _context.ProvisioningRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            "it-provisioning@contoso.example",
            "ProvisioningRequested",
            $"Provisioning request raised: {entity.SystemName}",
            $"A '{entity.ItemType}' provisioning request for '{entity.SystemName}' was raised for {record.FullName}.",
            record.Id,
            cancellationToken: cancellationToken);

        return Map(entity);
    }

    public async Task<ProvisioningSummaryDto?> GetSummaryAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.OnboardingRecords.AnyAsync(r => r.Id == onboardingRecordId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var requests = await _context.ProvisioningRequests
            .Where(p => p.OnboardingRecordId == onboardingRecordId)
            .OrderBy(p => p.SystemName)
            .ToListAsync(cancellationToken);

        var completed = requests.Count(p => p.Status == ProvisioningStatus.Completed);

        return new ProvisioningSummaryDto
        {
            OnboardingRecordId = onboardingRecordId,
            TotalRequests = requests.Count,
            CompletedRequests = completed,
            FailedRequests = requests.Count(p => p.Status == ProvisioningStatus.Failed),
            CompletionPercentage = requests.Count == 0 ? 0 : (int)Math.Round(completed * 100.0 / requests.Count, MidpointRounding.AwayFromZero),
            IsDayOneReady = requests.Count > 0 && completed == requests.Count,
            Requests = requests.Select(Map).ToList()
        };
    }

    public async Task<IReadOnlyList<ProvisioningRequestDto>> GetOpenRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.ProvisioningRequests
            .Where(p => p.Status != ProvisioningStatus.Completed)
            .OrderBy(p => p.RequiredByDate)
            .ToListAsync(cancellationToken);

        return requests.Select(Map).ToList();
    }

    public async Task<ProvisioningRequestDto?> UpdateStatusAsync(Guid requestId, UpdateProvisioningStatusDto update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (!ProvisioningStatus.IsValid(update.Status))
        {
            throw new ArgumentException($"'{update.Status}' is not a valid provisioning status.", nameof(update));
        }

        var entity = await _context.ProvisioningRequests
            .FirstOrDefaultAsync(p => p.Id == requestId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = update.Status;
        entity.ExternalTicketId = update.ExternalTicketId ?? entity.ExternalTicketId;
        entity.UpdatedBySystem = update.UpdatedBySystem ?? entity.UpdatedBySystem;
        entity.CompletedAtUtc = update.Status == ProvisioningStatus.Completed ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Provisioning request {RequestId} updated to {Status} by {System}",
            entity.Id, entity.Status, entity.UpdatedBySystem ?? "manual");

        return Map(entity);
    }

    private static bool IsEquipment(string systemName) =>
        systemName.Contains("Laptop", StringComparison.OrdinalIgnoreCase)
        || systemName.Contains("Phone", StringComparison.OrdinalIgnoreCase)
        || systemName.Contains("Monitor", StringComparison.OrdinalIgnoreCase)
        || systemName.Contains("Badge", StringComparison.OrdinalIgnoreCase);

    internal static ProvisioningRequestDto Map(ProvisioningRequest p) => new()
    {
        Id = p.Id,
        OnboardingRecordId = p.OnboardingRecordId,
        ItemType = p.ItemType,
        SystemName = p.SystemName,
        Details = p.Details,
        ExternalTicketId = p.ExternalTicketId,
        Status = p.Status,
        RequiredByDate = p.RequiredByDate,
        CreatedAtUtc = p.CreatedAtUtc,
        CompletedAtUtc = p.CompletedAtUtc,
        UpdatedBySystem = p.UpdatedBySystem
    };
}
