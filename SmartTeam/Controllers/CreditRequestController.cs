using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using System.Security.Claims;

namespace SmartTeam.Controllers;

/// <summary>
/// Credit request controller — Şəxsiyyət vəsiqəsi ilə kredit.
/// This is a standalone lead-generation system. It does NOT create orders or trigger payments.
/// Admin reviews every request manually and contacts the user.
/// </summary>
[ApiController]
[Route("api/v1/CreditRequests")]
public class CreditRequestController : ControllerBase
{
    private readonly ICreditRequestService _creditRequestService;
    private readonly ILogger<CreditRequestController> _logger;

    public CreditRequestController(
        ICreditRequestService creditRequestService,
        ILogger<CreditRequestController> logger)
    {
        _creditRequestService = creditRequestService;
        _logger = logger;
    }

    // ─── User Endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// [User] Submit a credit purchase request using the currently authenticated user's cart.
    /// Provide full name and phone number. Cart contents are snapshotted automatically.
    /// The cart is NOT cleared — the user may still proceed to normal checkout.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CreditRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreditRequestDto>> SubmitCreditRequest(
        [FromBody] CreateCreditRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest(new { error = "Full name is required." });

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest(new { error = "Phone number is required." });

            var userId = GetCurrentUserId();
            var result = await _creditRequestService.CreateAsync(userId, dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetCreditRequest),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            // e.g. cart is empty
            _logger.LogWarning(ex, "Credit request validation failed for user.");
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "User not authenticated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting credit request.");
            return BadRequest(new { error = ex.Message });
        }
    }

    // ─── Admin Endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// [Admin] Get a paginated list of all credit requests.
    /// Use ?status=Pending|Contacted|Approved|Rejected to filter.
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResultDto<CreditRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<CreditRequestDto>>> GetAllCreditRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _creditRequestService.GetAllPagedAsync(page, pageSize, status, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching credit requests list.");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Get a single credit request by ID including the full product list.
    /// </summary>
    [HttpGet("admin/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreditRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreditRequestDto>> GetCreditRequest(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _creditRequestService.GetByIdAsync(id, cancellationToken);
            if (result == null)
                return NotFound(new { error = $"Credit request '{id}' not found." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching credit request {Id}.", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Update the status of a credit request (Pending → Contacted / Approved / Rejected).
    /// Optionally include admin notes.
    /// </summary>
    [HttpPut("admin/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreditRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreditRequestDto>> UpdateCreditRequestStatus(
        Guid id,
        [FromBody] UpdateCreditRequestStatusDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _creditRequestService.UpdateStatusAsync(id, dto.Status, dto.Notes, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Credit request not found: {Id}.", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credit request status for {Id}.", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim == null || !Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("User not authenticated.");

        return userId;
    }
}
