using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeOnboarding.Api.Tests;

/// <summary>
/// User Story 2794: tasks are created automatically based on role, location and
/// department.
/// </summary>
public class TaskGenerationServiceTests
{
    [Fact]
    public void SelectTemplates_MatchesWildcardRoleDepartmentAndLocation()
    {
        using var host = new TestHost();

        var record = new OnboardingRecord
        {
            Role = "Account Executive",
            Department = "Sales",
            Location = "Onsite",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            CandidateEmail = "sales.hire@contoso.example",
            FirstName = "Sam",
            LastName = "Rivera"
        };

        var templates = host.Context.TaskTemplates.ToList();
        var selected = host.TaskGenerationService.SelectTemplates(templates, record);

        Assert.Contains(selected, t => t.Title == "Sign employment contract");
        Assert.Contains(selected, t => t.Title == "Prepare workstation and building access");
        Assert.Contains(selected, t => t.Title == "Complete sales territory handover");
        Assert.DoesNotContain(selected, t => t.Title == "Ship remote working equipment");
        Assert.DoesNotContain(selected, t => t.Title == "Set up engineering development environment");
    }

    [Fact]
    public void SelectTemplates_AppliesRemoteLocationRule()
    {
        using var host = new TestHost();

        var record = new OnboardingRecord
        {
            Role = "Software Engineer",
            Department = "Engineering",
            Location = "Remote",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            CandidateEmail = "eng.hire@contoso.example",
            FirstName = "Eng",
            LastName = "Hire"
        };

        var selected = host.TaskGenerationService.SelectTemplates(host.Context.TaskTemplates.ToList(), record);

        Assert.Contains(selected, t => t.Title == "Ship remote working equipment");
        Assert.Contains(selected, t => t.Title == "Set up engineering development environment");
        Assert.DoesNotContain(selected, t => t.Title == "Prepare workstation and building access");
    }

    [Fact]
    public async Task GenerateForRecord_SetsDueDatesRelativeToStartDate()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var record = await host.Context.OnboardingRecords.FirstAsync(r => r.Id == created.Id);
        var contractTask = await host.Context.OnboardingTasks
            .FirstAsync(t => t.OnboardingRecordId == created.Id && t.Title == "Sign employment contract");

        Assert.Equal(record.StartDate.AddDays(-10), contractTask.DueDate);
        Assert.Equal(OnboardingTaskStatus.Pending, contractTask.Status);
    }

    [Fact]
    public async Task GenerateForRecord_IsIdempotentAndDoesNotDuplicateTasks()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var before = await host.Context.OnboardingTasks.CountAsync(t => t.OnboardingRecordId == created.Id);
        await host.TaskGenerationService.GenerateForRecordAsync(created.Id);
        var after = await host.Context.OnboardingTasks.CountAsync(t => t.OnboardingRecordId == created.Id);

        Assert.Equal(before, after);
    }

    [Fact]
    public async Task GenerateForRecord_AssignsTasksToTheCorrectRoleMailbox()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var itTask = await host.Context.OnboardingTasks
            .FirstAsync(t => t.OnboardingRecordId == created.Id && t.AssigneeRole == AppRoles.ITAdmin);
        var employeeTask = await host.Context.OnboardingTasks
            .FirstAsync(t => t.OnboardingRecordId == created.Id && t.AssigneeRole == AppRoles.Employee);

        Assert.Equal("it-provisioning@contoso.example", itTask.AssignedToEmail);
        Assert.Equal(created.CandidateEmail, employeeTask.AssignedToEmail);
    }

    [Fact]
    public async Task UpdateStatus_RejectsInvalidStatus()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());
        var task = await host.Context.OnboardingTasks.FirstAsync(t => t.OnboardingRecordId == created.Id);

        await Assert.ThrowsAsync<ArgumentException>(
            () => host.TaskGenerationService.UpdateStatusAsync(task.Id, "Nope"));
    }

    [Fact]
    public async Task GetTasks_FiltersOverdueOnly()
    {
        using var host = new TestHost();
        var created = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer(startInDays: -30));

        var overdue = await host.TaskGenerationService.GetTasksAsync(created.Id, null, null, overdueOnly: true);

        Assert.NotEmpty(overdue);
        Assert.All(overdue, t => Assert.True(t.IsOverdue));
    }
}
