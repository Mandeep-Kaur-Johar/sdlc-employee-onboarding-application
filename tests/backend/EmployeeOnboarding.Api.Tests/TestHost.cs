using EmployeeOnboarding.Api.Configuration;
using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EmployeeOnboarding.Api.Tests;

/// <summary>
/// Builds an isolated in-memory service graph for each test.
/// </summary>
public sealed class TestHost : IDisposable
{
    public TestHost()
    {
        var options = new DbContextOptionsBuilder<OnboardingDbContext>()
            .UseInMemoryDatabase($"onboarding-tests-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        Context = new OnboardingDbContext(options);
        Context.Database.EnsureCreated();
        OnboardingDbSeeder.SeedAsync(Context).GetAwaiter().GetResult();

        Options = Microsoft.Extensions.Options.Options.Create(new OnboardingOptions());

        Storage = new InMemoryDocumentStorage();
        NotificationService = new NotificationService(Context, Options, NullLogger<NotificationService>.Instance);
        TaskGenerationService = new TaskGenerationService(Context, NotificationService, NullLogger<TaskGenerationService>.Instance);
        ProvisioningService = new ProvisioningService(Context, NotificationService, Options, NullLogger<ProvisioningService>.Instance);
        TrainingService = new TrainingService(Context, NotificationService, NullLogger<TrainingService>.Instance);
        DocumentService = new DocumentService(Context, Storage, NotificationService, Options, NullLogger<DocumentService>.Instance);
        OnboardingService = new OnboardingService(
            Context,
            TaskGenerationService,
            ProvisioningService,
            NotificationService,
            Options,
            NullLogger<OnboardingService>.Instance);
        PortalService = new PortalService(Context, OnboardingService, DocumentService);
    }

    public OnboardingDbContext Context { get; }

    public IOptions<OnboardingOptions> Options { get; }

    public InMemoryDocumentStorage Storage { get; }

    public NotificationService NotificationService { get; }

    public TaskGenerationService TaskGenerationService { get; }

    public ProvisioningService ProvisioningService { get; }

    public TrainingService TrainingService { get; }

    public DocumentService DocumentService { get; }

    public OnboardingService OnboardingService { get; }

    public PortalService PortalService { get; }

    public void Dispose() => Context.Dispose();
}

/// <summary>Document storage double that keeps content in memory.</summary>
public sealed class InMemoryDocumentStorage : IDocumentStorage
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public int SavedCount => _files.Count;

    public async Task<string> SaveAsync(Guid onboardingRecordId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var path = $"{onboardingRecordId:N}/{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        _files[path] = buffer.ToArray();
        return path;
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(_files.TryGetValue(storagePath, out var bytes) ? new MemoryStream(bytes) : null);

    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_files.Remove(storagePath));
}
