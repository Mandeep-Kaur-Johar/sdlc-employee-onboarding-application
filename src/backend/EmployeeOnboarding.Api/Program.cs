using EmployeeOnboarding.Api.Configuration;
using EmployeeOnboarding.Api.Data;
using EmployeeOnboarding.Api.Middleware;
using EmployeeOnboarding.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OnboardingOptions>(
    builder.Configuration.GetSection(OnboardingOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("OnboardingDatabase");

builder.Services.AddDbContext<OnboardingDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        // Developer and CI default. Production supplies the Azure SQL connection
        // string through configuration or Azure Key Vault.
        options.UseInMemoryDatabase("EmployeeOnboarding");
    }
    else
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    }
});

builder.Services.AddScoped<IDocumentStorage, LocalDocumentStorage>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITaskGenerationService, TaskGenerationService>();
builder.Services.AddScoped<IProvisioningService, ProvisioningService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IPortalService, PortalService>();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string DevCorsPolicy = "OnboardingSpa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(DevCorsPolicy);
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();

    // EnsureCreated is used for both providers. EF Core migrations are added in a
    // follow-up story once the Azure SQL deployment pipeline is provisioned.
    await context.Database.EnsureCreatedAsync();

    await OnboardingDbSeeder.SeedAsync(context);
}

await app.RunAsync();

/// <summary>Exposed so integration tests can reference the API entry point.</summary>
public partial class Program
{
}
