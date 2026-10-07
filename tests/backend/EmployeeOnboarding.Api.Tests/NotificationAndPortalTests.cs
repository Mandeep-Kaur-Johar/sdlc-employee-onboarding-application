using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeOnboarding.Api.Tests;

/// <summary>
/// User Story 2795: reminders and escalations for overdue tasks.
/// User Story 2800: personalized and least-privilege employee portal.
/// </summary>
public class NotificationAndPortalTests
{
    [Fact]
    public async Task RunReminders_SendsRemindersForOverdueTasks()
    {
        using var host = new TestHost();
        await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer(startInDays: -20));

        var result = await host.NotificationService.RunRemindersAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.True(result.RemindersSent > 0);
        Assert.Contains(result.Notifications, n => n.Kind == "Reminder");
    }

    [Fact]
    public async Task RunReminders_EscalatesTasksOverdueBeyondTheThreshold()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer(startInDays: -30));

        var result = await host.NotificationService.RunRemindersAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.True(result.EscalationsSent > 0);
        var escalations = await host.NotificationService.GetForRecipientAsync("grace.hopper@contoso.example");
        Assert.Contains(escalations, n => n.Kind == "Escalation");

        var escalatedTasks = await host.Context.OnboardingTasks
            .Where(t => t.OnboardingRecordId == record.Id && t.IsEscalated)
            .ToListAsync();
        Assert.NotEmpty(escalatedTasks);
    }

    [Fact]
    public async Task RunReminders_DoesNotRemindCompletedTasks()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer(startInDays: -20));

        foreach (var task in await host.Context.OnboardingTasks.Where(t => t.OnboardingRecordId == record.Id).ToListAsync())
        {
            await host.TaskGenerationService.UpdateStatusAsync(task.Id, OnboardingTaskStatus.Completed);
        }

        var result = await host.NotificationService.RunRemindersAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Equal(0, result.RemindersSent);
        Assert.Equal(0, result.EscalationsSent);
    }

    [Fact]
    public async Task RunReminders_SendsUpcomingReminderWithinTheLeadWindow()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer(startInDays: 10));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var task = await host.Context.OnboardingTasks.FirstAsync(t => t.OnboardingRecordId == record.Id);
        task.DueDate = today.AddDays(1);
        await host.Context.SaveChangesAsync();

        var result = await host.NotificationService.RunRemindersAsync(today);

        Assert.Contains(result.Notifications, n => n.Subject.Contains("due soon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_RequiresARecipient()
    {
        using var host = new TestHost();

        await Assert.ThrowsAsync<ArgumentException>(() => host.NotificationService.SendAsync(
            string.Empty, "Reminder", "Subject", "Body"));
    }

    [Fact]
    public async Task GetPortal_ReturnsOnlyTheEmployeesOwnTasksDocumentsAndTraining()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var portal = await host.PortalService.GetPortalAsync(record.CandidateEmail);

        Assert.NotNull(portal);
        Assert.Equal(record.Id, portal!.Record.Id);
        Assert.NotEmpty(portal.MyTasks);
        Assert.All(portal.MyTasks, t => Assert.Equal(record.CandidateEmail, t.AssignedToEmail));
        Assert.Equal(4, portal.MyTraining.Count);
        Assert.Equal(5, portal.RequiredDocumentTypes.Count);
        Assert.NotEmpty(portal.Progress.Milestones);
    }

    [Fact]
    public async Task GetPortal_ReturnsNullForAnUnknownEmployee()
    {
        using var host = new TestHost();
        await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        Assert.Null(await host.PortalService.GetPortalAsync("stranger@contoso.example"));
        Assert.Null(await host.PortalService.GetPortalAsync(string.Empty));
    }
}
