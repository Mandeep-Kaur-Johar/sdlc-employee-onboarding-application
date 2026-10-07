using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnboarding.Api.Controllers;

/// <summary>
/// Document collection and compliance endpoints (User Story 2791, 2792).
/// </summary>
[ApiController]
[Route("api/documents")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IOnboardingService _onboardingService;

    public DocumentsController(IDocumentService documentService, IOnboardingService onboardingService)
    {
        _documentService = documentService;
        _onboardingService = onboardingService;
    }

    /// <summary>Returns the required document checklist (User Story 2791).</summary>
    [HttpGet("required-types")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> GetRequiredTypes() => Ok(_documentService.RequiredDocumentTypes);

    /// <summary>
    /// Uploads a document securely and tracks checklist completion
    /// (User Story 2791).
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentRecordDto>> Upload(
        [FromForm] Guid onboardingRecordId,
        [FromForm] string documentType,
        [FromForm] string uploadedByEmail,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A non-empty file is required." });
        }

        await using var stream = file.OpenReadStream();

        var created = await _documentService.UploadAsync(
            onboardingRecordId,
            documentType,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            uploadedByEmail,
            cancellationToken);

        await _onboardingService.RecalculateCompletionAsync(onboardingRecordId, cancellationToken);

        return CreatedAtAction(nameof(GetForOnboarding), new { onboardingRecordId = created.OnboardingRecordId }, created);
    }

    /// <summary>Returns documents and compliance completion for a record (User Story 2791).</summary>
    [HttpGet("onboarding/{onboardingRecordId:guid}")]
    [ProducesResponseType(typeof(DocumentComplianceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentComplianceDto>> GetForOnboarding(Guid onboardingRecordId, CancellationToken cancellationToken)
    {
        var compliance = await _documentService.GetComplianceAsync(onboardingRecordId, cancellationToken);
        return compliance is null ? NotFound() : Ok(compliance);
    }

    /// <summary>Returns the HR review queue (User Story 2792).</summary>
    [HttpGet("pending-review")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentRecordDto>>> GetPendingReview(CancellationToken cancellationToken)
    {
        var pending = await _documentService.GetPendingReviewAsync(cancellationToken);
        return Ok(pending);
    }

    /// <summary>
    /// Approves or rejects a document and notifies the employee when a
    /// resubmission is required (User Story 2792).
    /// </summary>
    [HttpPost("{id:guid}/review")]
    [ProducesResponseType(typeof(DocumentRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentRecordDto>> Review(
        Guid id,
        [FromBody] DocumentReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var reviewed = await _documentService.ReviewAsync(id, request, cancellationToken);
        if (reviewed is null)
        {
            return NotFound();
        }

        await _onboardingService.RecalculateCompletionAsync(reviewed.OnboardingRecordId, cancellationToken);
        return Ok(reviewed);
    }
}
