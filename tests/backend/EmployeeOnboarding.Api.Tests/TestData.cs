using EmployeeOnboarding.Api.DTOs;

namespace EmployeeOnboarding.Api.Tests;

public static class TestData
{
    public static AcceptedOfferEventDto Offer(
        string email = "ada.lovelace@contoso.example",
        string role = "Software Engineer",
        string department = "Engineering",
        string location = "Remote",
        int startInDays = 14) => new()
    {
        CandidateEmail = email,
        FirstName = "Ada",
        LastName = "Lovelace",
        Role = role,
        Department = department,
        Location = location,
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(startInDays),
        ManagerEmail = "grace.hopper@contoso.example",
        OfferReference = "OFFER-10045"
    };
}
