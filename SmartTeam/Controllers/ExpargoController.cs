using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

/// <summary>
/// Admin CRUD for Expargo weight-based delivery pricing rules,
/// and a public endpoint to preview the delivery fee for a given cart weight.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]  // api/v1/Expargo
public class ExpargoController : ControllerBase
{
    private readonly IExpargoService _expargoService;
    private readonly ILogger<ExpargoController> _logger;

    public ExpargoController(IExpargoService expargoService, ILogger<ExpargoController> logger)
    {
        _expargoService = expargoService;
        _logger = logger;
    }

    // ── Admin CRUD ────────────────────────────────────────────────────────────

    /// <summary>
    /// [Admin] Create a new weight-based pricing rule for Expargo delivery.
    /// </summary>
    /// <remarks>
    /// Example — fixed range (0–3 kg → 3.50 AZN):
    ///     { "minWeight": 0, "maxWeight": 3, "basePrice": 3.50, "additionalPricePerKg": 0 }
    ///
    /// Example — open-ended (5+ kg, +1 AZN per kg above 5):
    ///     { "minWeight": 5, "maxWeight": null, "basePrice": 5.00, "additionalPricePerKg": 1.00 }
    /// </remarks>
    [HttpPost("rules")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ExpargoWeightRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ExpargoWeightRuleDto>> CreateRule(
        [FromBody] CreateExpargoWeightRuleDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _expargoService.CreateRuleAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetAllRules), new { }, rule);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid Expargo rule creation request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Expargo pricing rule");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// [Public] Get all Expargo weight pricing rules.
    /// </summary>
    [HttpGet("rules")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ExpargoWeightRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExpargoWeightRuleDto>>> GetAllRules(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _expargoService.GetAllRulesAsync(cancellationToken);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Expargo pricing rules");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Update an existing Expargo weight pricing rule.
    /// </summary>
    [HttpPut("rules/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ExpargoWeightRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ExpargoWeightRuleDto>> UpdateRule(
        Guid id,
        [FromBody] UpdateExpargoWeightRuleDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _expargoService.UpdateRuleAsync(id, dto, cancellationToken);
            return Ok(rule);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Expargo rule update failed for {Id}", id);
            return ex.Message.Contains("not found")
                ? NotFound(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Expargo pricing rule {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Delete an Expargo weight pricing rule.
    /// </summary>
    [HttpDelete("rules/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _expargoService.DeleteRuleAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Expargo rule delete failed for {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Expargo pricing rule {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Fee Calculation (Public) ──────────────────────────────────────────────

    /// <summary>
    /// [Public] Preview the Expargo delivery fee for a given total cart weight.
    /// Frontend can call this before checkout to show the fee to the user.
    /// </summary>
    /// <param name="weight">Total cart weight in kg (e.g. 4.5)</param>
    [HttpGet("calculate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExpargoDeliveryFeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpargoDeliveryFeeDto>> CalculateFee(
        [FromQuery] decimal weight,
        CancellationToken cancellationToken)
    {
        try
        {
            if (weight < 0)
                return BadRequest(new { error = "Weight must be >= 0." });

            var result = await _expargoService.CalculateDeliveryFeeAsync(weight, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No matching Expargo rule for weight {Weight}", weight);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Expargo delivery fee for weight {Weight}", weight);
            return BadRequest(new { error = ex.Message });
        }
    }
}
