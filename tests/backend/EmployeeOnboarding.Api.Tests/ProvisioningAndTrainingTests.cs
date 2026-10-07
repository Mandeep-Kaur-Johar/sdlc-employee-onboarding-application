using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeOnboarding.Api.Tests;

/// <summary>
/// User Story 2797 and 2798: provisioning requests are created and tracked.
/// User Story 2801: training completion is recorded and visible.
/// </summary>
public class ProvisioningAndTrainingTests
{
    [Fact]
    public async Task CreateDefaultRequests_CreatesRequestsForEverySystemAndEquipmentItem()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var summary = await host.ProvisioningService.GetSummaryAsync(record.Id);

        Assert.NotNull(summary);
        Assert.Equal(5, summary!.TotalRequests);
        Assert.Contains(summary.Requests, r => r.SystemName == "Laptop" && r.ItemType == "Equipment");
        Assert.Contains(summary.Requests, r => r.SystemName == "Email Account" && r.ItemType == "Account");
        Assert.All(summary.Requests, r => Assert.Equal(ProvisioningStatus.Requested, r.Status));
    }

    [Fact]
    public async Task CreateDefaultRequests_IsIdempotent()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        await host.ProvisioningService.CreateDefaultRequestsAsync(record.Id);
        var count = await host.Context.ProvisioningRequests.CountAsync(p => p.OnboardingRecordId == record.Id);

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task UpdateStatus_IsVisibleInTheSummaryAndSetsDayOneReadiness()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var requests = await host.Context.ProvisioningRequests
            .Where(p => p.OnboardingRecordId == record.Id)
            .ToListAsync();

        foreach (var request in requests)
        {
            await host.ProvisioningService.UpdateStatusAsync(request.Id, new UpdateProvisioningStatusDto
            {
                Status = ProvisioningStatus.Completed,
                ExternalTicketId = "ITSM-9001",
                UpdatedBySystem = "ServiceNow"
            });
        }

        var summary = await host.ProvisioningService.GetSummaryAsync(record.Id);

        Assert.True(summary!.IsDayOneReady);
        Assert.Equal(100, summary.CompletionPercentage);
        Assert.All(summary.Requests, r => Assert.Equal("ServiceNow", r.UpdatedBySystem));
    }

    [Fact]
    public async Task UpdateStatus_RejectsInvalidProvisioningStatus()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());
        var request = await host.Context.ProvisioningRequests.FirstAsync(p => p.OnboardingRecordId == record.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => host.ProvisioningService.UpdateStatusAsync(
            request.Id, new UpdateProvisioningStatusDto { Status = "Unknown" }));
    }

    [Fact]
    public async Task GetOpenRequests_ExcludesCompletedRequests()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());
        var request = await host.Context.ProvisioningRequests.FirstAsync(p => p.OnboardingRecordId == record.Id);

        await host.ProvisioningService.UpdateStatusAsync(request.Id, new UpdateProvisioningStatusDto
        {
            Status = ProvisioningStatus.Completed
        });

        var open = await host.ProvisioningService.GetOpenRequestsAsync();

        Assert.DoesNotContain(open, r => r.Id == request.Id);
    }

    [Fact]
    public async Task CompleteTraining_RecordsCompletionAndNotifiesManagerAndHr()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());
        var assignment = await host.Context.TrainingAssignments
            .FirstAsync(t => t.OnboardingRecordId == record.Id && t.CourseCode == "ORI-101");

        var completed = await host.TrainingService.CompleteAsync(assignment.Id, new CompleteTrainingDto { ScorePercentage = 92 });

        Assert.Equal(TrainingStatus.Completed, completed!.Status);
        Assert.Equal(92, completed.ScorePercentage);
        Assert.NotNull(completed.CompletedAtUtc);

        var managerNotifications = await host.NotificationService.GetForRecipientAsync("grace.hopper@contoso.example");
        var hrNotifications = await host.NotificationService.GetForRecipientAsync("hr-coordinator@contoso.example");

        Assert.Contains(managerNotifications, n => n.Kind == "TrainingCompleted");
        Assert.Contains(hrNotifications, n => n.Kind == "TrainingCompleted");
    }

    [Fact]
    public async Task StartTraining_MovesAssignmentToInProgress()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());
        var assignment = await host.Context.TrainingAssignments.FirstAsync(t => t.OnboardingRecordId == record.Id);

        var started = await host.TrainingService.StartAsync(assignment.Id);

        Assert.Equal(TrainingStatus.InProgress, started!.Status);
    }

    [Fact]
    public async Task CompleteTraining_RejectsOutOfRangeScore()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());
        var assignment = await host.Context.TrainingAssignments.FirstAsync(t => t.OnboardingRecordId == record.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => host.TrainingService.CompleteAsync(
            assignment.Id, new CompleteTrainingDto { ScorePercentage = 140 }));
    }
}
