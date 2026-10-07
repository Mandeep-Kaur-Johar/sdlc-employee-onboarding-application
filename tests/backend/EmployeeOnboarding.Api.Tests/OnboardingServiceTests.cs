using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeOnboarding.Api.Tests;

/// <summary>
/// User Story 2788: an onboarding record is created from an accepted offer.
/// User Story 2789: progress, milestones, overdue tasks and completion percentage.
/// </summary>
public class OnboardingServiceTests
{
    [Fact]
    public async Task CreateFromAcceptedOffer_CreatesRecordWithEmployeeDetailsAndStatus()
    {
        using var host = new TestHost();

        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("ada.lovelace@contoso.example", created.CandidateEmail);
        Assert.Equal("Ada Lovelace", created.FullName);
        Assert.Equal("Software Engineer", created.Role);
        Assert.Equal("Engineering", created.Department);
        Assert.Equal("Remote", created.Location);
        Assert.Contains(created.Status, OnboardingStatus.All);
    }

    [Fact]
    public async Task CreateFromAcceptedOffer_AlsoGeneratesTasksProvisioningAndTraining()
    {
        using var host = new TestHost();

        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var tasks = await host.Context.OnboardingTasks.Where(t => t.OnboardingRecordId == created.Id).ToListAsync();
        var provisioning = await host.Context.ProvisioningRequests.Where(p => p.OnboardingRecordId == created.Id).ToListAsync();
        var training = await host.Context.TrainingAssignments.Where(t => t.OnboardingRecordId == created.Id).ToListAsync();

        Assert.NotEmpty(tasks);
        Assert.Equal(5, provisioning.Count);
        Assert.Equal(4, training.Count);
    }

    [Fact]
    public async Task CreateFromAcceptedOffer_IsIdempotentForRedeliveredEvents()
    {
        using var host = new TestHost();
        var offer = TestData.Offer();

        var first = await host.OnboardingService.CreateFromAcceptedOfferAsync(offer);
        var second = await host.OnboardingService.CreateFromAcceptedOfferAsync(offer);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await host.Context.OnboardingRecords.CountAsync());
    }

    [Fact]
    public async Task CreateFromAcceptedOffer_SendsWelcomeNotificationToCandidate()
    {
        using var host = new TestHost();

        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var notifications = await host.NotificationService.GetForRecipientAsync(created.CandidateEmail);
        Assert.Contains(notifications, n => n.Kind == "Welcome");
    }

    [Fact]
    public async Task GetProgress_ReturnsMilestonesOverdueTasksAndCompletionPercentage()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer(startInDays: -5));

        var progress = await host.OnboardingService.GetProgressAsync(created.Id);

        Assert.NotNull(progress);
        Assert.Equal(created.Id, progress!.OnboardingRecordId);
        Assert.Equal(5, progress.Milestones.Count);
        Assert.True(progress.TotalTasks > 0);
        Assert.True(progress.OverdueTasks > 0);
        Assert.InRange(progress.CompletionPercentage, 0, 100);
    }

    [Fact]
    public async Task RecalculateCompletion_ReachesHundredPercentWhenEverythingIsDone()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        foreach (var task in await host.Context.OnboardingTasks.Where(t => t.OnboardingRecordId == created.Id).ToListAsync())
        {
            await host.TaskGenerationService.UpdateStatusAsync(task.Id, OnboardingTaskStatus.Completed);
        }

        foreach (var request in await host.Context.ProvisioningRequests.Where(p => p.OnboardingRecordId == created.Id).ToListAsync())
        {
            await host.ProvisioningService.UpdateStatusAsync(request.Id, new DTOs.UpdateProvisioningStatusDto
            {
                Status = ProvisioningStatus.Completed,
                UpdatedBySystem = "ITSM"
            });
        }

        foreach (var assignment in await host.Context.TrainingAssignments.Where(t => t.OnboardingRecordId == created.Id).ToListAsync())
        {
            await host.TrainingService.CompleteAsync(assignment.Id, new DTOs.CompleteTrainingDto { ScorePercentage = 100 });
        }

        foreach (var documentType in host.Options.Value.RequiredDocumentTypes)
        {
            using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var document = await host.DocumentService.UploadAsync(
                created.Id, documentType, $"{documentType}.pdf", "application/pdf", 4, content, created.CandidateEmail);

            await host.DocumentService.ReviewAsync(document.Id, new DTOs.DocumentReviewRequestDto
            {
                Decision = DocumentStatus.Approved,
                ReviewerEmail = "hr-compliance@contoso.example"
            });
        }

        var percentage = await host.OnboardingService.RecalculateCompletionAsync(created.Id);

        Assert.Equal(100, percentage);
        var record = await host.Context.OnboardingRecords.FirstAsync(r => r.Id == created.Id);
        Assert.Equal(OnboardingStatus.Completed, record.Status);
    }

    [Fact]
    public async Task UpdateStatus_RejectsUnknownStatus()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        await Assert.ThrowsAsync<ArgumentException>(
            () => host.OnboardingService.UpdateStatusAsync(created.Id, "NotARealStatus"));
    }

    [Fact]
    public async Task GetDetail_ReturnsNullForUnknownRecord()
    {
        using var host = new TestHost();
        Assert.Null(await host.OnboardingService.GetDetailAsync(Guid.NewGuid()));
    }
}
