# Employee Onboarding Application

Implementation repository for Azure DevOps **Epic 2786 - Employee Onboarding Application**.

A centralized application that automates pre-boarding, onboarding task management, document
collection, approvals, IT provisioning, and employee orientation tracking.

| Layer | Technology |
|---|---|
| Frontend | React 18, TypeScript, Vite, React Router, Axios |
| Backend | ASP.NET Core 8 Web API (C#), EF Core 8 |
| Data | Azure SQL Database (EF Core in-memory provider for local/CI) |
| Documents | Azure Blob Storage (local file-system provider for local/CI) |
| Backend tests | xUnit |
| Frontend tests | Vitest + React Testing Library |
| CI | GitHub Actions (`.github/workflows/developer-validation.yml`) |

## Repository layout

```text
src/
  backend/
    EmployeeOnboarding.sln
    EmployeeOnboarding.Api/        # Controllers, Services, Models, DTOs, Data, Middleware
  frontend/                        # React + TypeScript SPA
tests/
  backend/EmployeeOnboarding.Api.Tests/
docs/architecture/epic-2786/       # HLD and rendered architecture diagrams
.github/workflows/                 # CI validation
```

## Running the backend

```bash
cd src/backend
dotnet restore EmployeeOnboarding.sln
dotnet build EmployeeOnboarding.sln --configuration Release
dotnet run --project EmployeeOnboarding.Api
```

The API listens on `http://localhost:5080`. Swagger UI is available at `/swagger` in Development.

With no `ConnectionStrings:OnboardingDatabase` value the API uses the EF Core in-memory provider
and seeds the task templates automatically. Supply an Azure SQL connection string through
configuration, environment variables or Azure Key Vault to use a relational database.
**No secrets are stored in this repository.**

## Running the frontend

```bash
cd src/frontend
npm install
npm run dev
```

The dev server runs on `http://localhost:5173` and proxies `/api` to the backend.
Set `VITE_API_BASE_URL` to target a deployed API instead.

## Running the tests

```bash
# Backend
dotnet test src/backend/EmployeeOnboarding.sln

# Frontend
cd src/frontend
npm run lint
npm run build
npm test -- --run
```

## API surface

| Method | Route | User Story |
|---|---|---|
| POST | `/api/onboarding/offer-accepted` | 2788 |
| GET | `/api/onboarding` | 2789 |
| GET | `/api/onboarding/{id}` | 2789 |
| GET | `/api/onboarding/{id}/progress` | 2789 |
| PUT | `/api/onboarding/{id}/status` | 2789 |
| GET | `/api/documents/required-types` | 2791 |
| POST | `/api/documents/upload` | 2791 |
| GET | `/api/documents/onboarding/{id}` | 2791 |
| GET | `/api/documents/pending-review` | 2792 |
| POST | `/api/documents/{id}/review` | 2792 |
| POST | `/api/tasks/generate/{onboardingRecordId}` | 2794 |
| GET | `/api/tasks` | 2794, 2795 |
| PUT | `/api/tasks/{id}/status` | 2794 |
| POST | `/api/notifications/run-reminders` | 2795 |
| GET | `/api/notifications?recipient=` | 2795 |
| POST | `/api/provisioning/requests/{onboardingRecordId}` | 2797 |
| POST | `/api/provisioning/requests/{onboardingRecordId}/custom` | 2797 |
| GET | `/api/provisioning/onboarding/{id}` | 2798 |
| GET | `/api/provisioning/open` | 2798 |
| PUT | `/api/provisioning/{id}/status` | 2798 |
| GET | `/api/portal/me` | 2800 |
| GET | `/api/training/onboarding/{id}` | 2801 |
| PUT | `/api/training/{id}/start` | 2801 |
| PUT | `/api/training/{id}/complete` | 2801 |
| GET | `/api/health` | operational |

## Acceptance Criteria traceability

| User Story | Acceptance Criterion | Implementation |
|---|---|---|
| 2788 Create onboarding record from accepted offer | Given an accepted offer, when the HR system sends the event, then an onboarding record is created with employee details and status | `OnboardingController.CreateFromAcceptedOffer`, `OnboardingService.CreateFromAcceptedOfferAsync` (idempotent on candidate email + start date), `OnboardingServiceTests` |
| 2789 Track onboarding progress | Dashboard displays status, milestones, overdue tasks, and completion percentage | `OnboardingService.BuildProgress` / `RecalculateCompletionAsync`, `DashboardPage`, `OnboardingDetailPage`, `MilestoneList`, `ProgressBar` |
| 2791 Upload required onboarding documents | Employee can upload required documents; system stores them securely and tracks completion | `DocumentsController.Upload`, `DocumentService.UploadAsync` + `ValidateUpload`, `IDocumentStorage`, `DocumentUploadForm`, `DocumentService.GetComplianceAsync` |
| 2792 Review and approve submitted documents | HR can approve or reject documents and employees receive notifications for resubmission | `DocumentsController.Review`, `DocumentService.ReviewAsync` (`DocumentReview` audit trail + `DocumentRejected` notification), `DocumentReviewPanel`, `DocumentsPage` |
| 2794 Generate onboarding tasks automatically | Tasks are created automatically based on role, location, and department | `TaskGenerationService.SelectTemplates` / `GenerateForRecordAsync`, `TaskTemplate`, `OnboardingDbSeeder`, `TaskGenerationServiceTests` |
| 2795 Receive task notifications and reminders | System sends notifications and escalations for overdue tasks | `NotificationService.RunRemindersAsync` (reminder lead window + escalation threshold), `NotificationsController.RunReminders`, `TasksPage` |
| 2797 Request IT assets and accounts | Provisioning requests are created and tracked for required systems and equipment | `ProvisioningService.CreateDefaultRequestsAsync` / `CreateRequestAsync`, `OnboardingOptions.DefaultProvisioningSystems` |
| 2798 Track provisioning completion | Provisioning status is visible and updated by integrated systems | `ProvisioningService.GetSummaryAsync` (`IsDayOneReady`), `ProvisioningController.UpdateStatus` callback, `ProvisioningPage` |
| 2800 Access onboarding portal | Employee can securely access assigned tasks, documents, and onboarding content | `PortalController.GetMyPortal` (identity-scoped), `PortalService.GetPortalAsync`, `PortalPage`, `AuthContext` + JWT interceptor |
| 2801 Complete orientation and training | Training completion is recorded and visible to HR and managers | `TrainingService.StartAsync` / `CompleteAsync` (manager + HR notifications), `TrainingController`, `OrientationPage` |

## Security notes

- HTTPS redirection and HSTS outside Development.
- JWT bearer token attached by the Axios request interceptor; tokens are never committed.
- Upload validation: allowed content types (PDF, PNG, JPEG) and a 10 MB limit, enforced on both
  the client and the server. Stored file names are server-generated, so user input never controls
  the storage path.
- Consistent RFC 7807 problem responses via `ExceptionHandlingMiddleware`; internal exception
  details are never returned to clients.
- Full audit trail on document reviews, provisioning status changes and notifications.

## Architecture

See [`docs/architecture/epic-2786/high-level-design.md`](docs/architecture/epic-2786/high-level-design.md)
and the rendered diagrams in the same folder.
