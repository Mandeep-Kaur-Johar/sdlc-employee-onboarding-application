using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;
using Xunit;

namespace EmployeeOnboarding.Api.Tests;

/// <summary>
/// User Story 2791: documents are uploaded securely and completion is tracked.
/// User Story 2792: HR approves or rejects and the employee is notified.
/// </summary>
public class DocumentServiceTests
{
    private static MemoryStream Pdf() => new(new byte[] { 0x25, 0x50, 0x44, 0x46 });

    [Fact]
    public async Task Upload_StoresDocumentOutsideTheDatabaseAndMarksItSubmitted()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var content = Pdf();
        var document = await host.DocumentService.UploadAsync(
            record.Id, "PhotoIdentification", "passport.pdf", "application/pdf", 4, content, record.CandidateEmail);

        Assert.Equal(DocumentStatus.Submitted, document.Status);
        Assert.Equal("passport.pdf", document.FileName);
        Assert.Equal(1, host.Storage.SavedCount);
    }

    [Fact]
    public async Task Upload_RejectsDisallowedContentType()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var content = Pdf();
        await Assert.ThrowsAsync<ArgumentException>(() => host.DocumentService.UploadAsync(
            record.Id, "PhotoIdentification", "malware.exe", "application/x-msdownload", 4, content, record.CandidateEmail));
    }

    [Fact]
    public async Task Upload_RejectsFilesLargerThanTenMegabytes()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var content = Pdf();
        await Assert.ThrowsAsync<ArgumentException>(() => host.DocumentService.UploadAsync(
            record.Id, "PhotoIdentification", "huge.pdf", "application/pdf", 11L * 1024 * 1024, content, record.CandidateEmail));
    }

    [Fact]
    public async Task Upload_RejectsUnknownOnboardingRecord()
    {
        using var host = new TestHost();
        using var content = Pdf();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => host.DocumentService.UploadAsync(
            Guid.NewGuid(), "PhotoIdentification", "passport.pdf", "application/pdf", 4, content, "someone@contoso.example"));
    }

    [Fact]
    public async Task GetCompliance_TracksMissingRequiredDocuments()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        var compliance = await host.DocumentService.GetComplianceAsync(record.Id);

        Assert.NotNull(compliance);
        Assert.Equal(5, compliance!.RequiredCount);
        Assert.Equal(0, compliance.CompletionPercentage);
        Assert.Equal(5, compliance.MissingDocumentTypes.Count);
    }

    [Fact]
    public async Task Review_ApprovesDocumentAndNotifiesEmployee()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var content = Pdf();
        var document = await host.DocumentService.UploadAsync(
            record.Id, "SignedContract", "contract.pdf", "application/pdf", 4, content, record.CandidateEmail);

        var reviewed = await host.DocumentService.ReviewAsync(document.Id, new DocumentReviewRequestDto
        {
            Decision = DocumentStatus.Approved,
            ReviewerEmail = "hr-compliance@contoso.example"
        });

        Assert.NotNull(reviewed);
        Assert.Equal(DocumentStatus.Approved, reviewed!.Status);

        var notifications = await host.NotificationService.GetForRecipientAsync(record.CandidateEmail);
        Assert.Contains(notifications, n => n.Kind == "DocumentApproved");
    }

    [Fact]
    public async Task Review_RejectsDocumentAndSendsResubmissionNotification()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var content = Pdf();
        var document = await host.DocumentService.UploadAsync(
            record.Id, "RightToWork", "visa.pdf", "application/pdf", 4, content, record.CandidateEmail);

        var reviewed = await host.DocumentService.ReviewAsync(document.Id, new DocumentReviewRequestDto
        {
            Decision = DocumentStatus.Rejected,
            Comments = "The document is expired. Please upload a current version.",
            ReviewerEmail = "hr-compliance@contoso.example"
        });

        Assert.Equal(DocumentStatus.Rejected, reviewed!.Status);

        var notifications = await host.NotificationService.GetForRecipientAsync(record.CandidateEmail);
        var resubmission = notifications.First(n => n.Kind == "DocumentRejected");
        Assert.Contains("resubmit", resubmission.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expired", resubmission.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Review_RequiresCommentsWhenRejecting()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var content = Pdf();
        var document = await host.DocumentService.UploadAsync(
            record.Id, "BankDetails", "bank.pdf", "application/pdf", 4, content, record.CandidateEmail);

        await Assert.ThrowsAsync<ArgumentException>(() => host.DocumentService.ReviewAsync(document.Id, new DocumentReviewRequestDto
        {
            Decision = DocumentStatus.Rejected,
            ReviewerEmail = "hr-compliance@contoso.example"
        }));
    }

    [Fact]
    public async Task Review_RejectsUnknownDecisionValue()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var content = Pdf();
        var document = await host.DocumentService.UploadAsync(
            record.Id, "TaxDeclaration", "tax.pdf", "application/pdf", 4, content, record.CandidateEmail);

        await Assert.ThrowsAsync<ArgumentException>(() => host.DocumentService.ReviewAsync(document.Id, new DocumentReviewRequestDto
        {
            Decision = "Maybe",
            ReviewerEmail = "hr-compliance@contoso.example"
        }));
    }

    [Fact]
    public async Task GetPendingReview_ReturnsSubmittedDocumentsOnly()
    {
        using var host = new TestHost();
        var record = await host.OnboardingService.CreateFromAcceptedOfferAsync(TestData.Offer());

        using var first = Pdf();
        var approved = await host.DocumentService.UploadAsync(
            record.Id, "SignedContract", "a.pdf", "application/pdf", 4, first, record.CandidateEmail);
        await host.DocumentService.ReviewAsync(approved.Id, new DocumentReviewRequestDto
        {
            Decision = DocumentStatus.Approved,
            ReviewerEmail = "hr-compliance@contoso.example"
        });

        using var second = Pdf();
        await host.DocumentService.UploadAsync(
            record.Id, "BankDetails", "b.pdf", "application/pdf", 4, second, record.CandidateEmail);

        var pending = await host.DocumentService.GetPendingReviewAsync();

        Assert.Single(pending);
        Assert.Equal("BankDetails", pending[0].DocumentType);
    }
}
