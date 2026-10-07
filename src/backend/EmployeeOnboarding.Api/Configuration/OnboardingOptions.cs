namespace EmployeeOnboarding.Api.Configuration;

/// <summary>
/// Application configuration for onboarding behaviour. Values are supplied by
/// appsettings or environment variables. No secrets are stored in source.
/// </summary>
public class OnboardingOptions
{
    public const string SectionName = "Onboarding";

    /// <summary>Document types that must be collected before compliance is complete (User Story 2791).</summary>
    public string[] RequiredDocumentTypes { get; set; } =
    {
        "PhotoIdentification",
        "RightToWork",
        "SignedContract",
        "BankDetails",
        "TaxDeclaration"
    };

    /// <summary>Maximum accepted upload size in bytes (NFR: 10 MB).</summary>
    public long MaxUploadSizeBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedContentTypes { get; set; } =
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg"
    };

    /// <summary>Systems and equipment provisioned for every new employee (User Story 2797).</summary>
    public string[] DefaultProvisioningSystems { get; set; } =
    {
        "Email Account",
        "Identity Directory Account",
        "Laptop",
        "VPN Access",
        "HR Self Service"
    };

    /// <summary>Days before a due date at which a reminder is sent (User Story 2795).</summary>
    public int ReminderLeadDays { get; set; } = 2;

    /// <summary>Days overdue after which a task is escalated to the manager (User Story 2795).</summary>
    public int EscalationThresholdDays { get; set; } = 3;

    /// <summary>Root folder used by the local document storage provider.</summary>
    public string LocalDocumentStorageRoot { get; set; } = "App_Data/documents";

    /// <summary>Blob container name used when Azure Blob Storage is configured.</summary>
    public string BlobContainerName { get; set; } = "onboarding-documents";
}
