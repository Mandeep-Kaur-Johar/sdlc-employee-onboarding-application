using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOnboarding.Api.Data;

/// <summary>
/// Seeds the task templates that drive automatic task generation (User Story 2794)
/// and the default orientation catalogue (User Story 2801).
/// </summary>
public static class OnboardingDbSeeder
{
    public static readonly IReadOnlyList<(string CourseCode, string CourseName, bool Mandatory, int OffsetDays)>
        DefaultCourses = new List<(string, string, bool, int)>
        {
            ("ORI-101", "Company Orientation", true, 2),
            ("SEC-201", "Information Security Awareness", true, 5),
            ("HRP-301", "Code of Conduct and HR Policies", true, 7),
            ("DEI-401", "Diversity, Equity and Inclusion", false, 14)
        };

    public static async Task SeedAsync(OnboardingDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.TaskTemplates.AnyAsync(cancellationToken))
        {
            return;
        }

        var templates = new List<TaskTemplate>
        {
            new()
            {
                Title = "Sign employment contract",
                Description = "Review and electronically sign the employment contract.",
                Category = "Documentation",
                Role = "*",
                Department = "*",
                Location = "*",
                OffsetDays = -10,
                AssigneeRole = AppRoles.Employee
            },
            new()
            {
                Title = "Upload identity and right-to-work documents",
                Description = "Upload passport or national ID and right-to-work evidence.",
                Category = "Documentation",
                Role = "*",
                Department = "*",
                Location = "*",
                OffsetDays = -7,
                AssigneeRole = AppRoles.Employee
            },
            new()
            {
                Title = "Verify compliance documents",
                Description = "Review submitted documents and record the approval decision.",
                Category = "Compliance",
                Role = "*",
                Department = "*",
                Location = "*",
                OffsetDays = -3,
                AssigneeRole = AppRoles.HRSpecialist
            },
            new()
            {
                Title = "Raise IT provisioning requests",
                Description = "Create laptop, account and access requests for day-one readiness.",
                Category = "Provisioning",
                Role = "*",
                Department = "*",
                Location = "*",
                OffsetDays = -5,
                AssigneeRole = AppRoles.ITAdmin
            },
            new()
            {
                Title = "Prepare workstation and building access",
                Description = "Prepare the desk, badge and building access for the office location.",
                Category = "Facilities",
                Role = "*",
                Department = "*",
                Location = "Onsite",
                OffsetDays = -2,
                AssigneeRole = AppRoles.ITAdmin
            },
            new()
            {
                Title = "Ship remote working equipment",
                Description = "Ship laptop, headset and peripherals to the remote home address.",
                Category = "Facilities",
                Role = "*",
                Department = "*",
                Location = "Remote",
                OffsetDays = -8,
                AssigneeRole = AppRoles.ITAdmin
            },
            new()
            {
                Title = "Schedule manager welcome meeting",
                Description = "Book the day-one welcome session with the hiring manager.",
                Category = "Orientation",
                Role = "*",
                Department = "*",
                Location = "*",
                OffsetDays = 0,
                AssigneeRole = AppRoles.Manager
            },
            new()
            {
                Title = "Complete company orientation",
                Description = "Complete the mandatory orientation and training curriculum.",
                Category = "Orientation",
                Role = "*",
                Department = "*",
                Location = "*",
                OffsetDays = 5,
                AssigneeRole = AppRoles.Employee
            },
            new()
            {
                Title = "Set up engineering development environment",
                Description = "Install the toolchain, clone repositories and verify build access.",
                Category = "Role Readiness",
                Role = "Software Engineer",
                Department = "Engineering",
                Location = "*",
                OffsetDays = 1,
                AssigneeRole = AppRoles.Employee
            },
            new()
            {
                Title = "Complete sales territory handover",
                Description = "Review assigned accounts, quota and CRM territory configuration.",
                Category = "Role Readiness",
                Role = "*",
                Department = "Sales",
                Location = "*",
                OffsetDays = 3,
                AssigneeRole = AppRoles.Manager
            }
        };

        context.TaskTemplates.AddRange(templates);
        await context.SaveChangesAsync(cancellationToken);
    }
}
