using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InstallmentController : ControllerBase
{
    private readonly IInstallmentService _installmentService;
    private readonly ILogger<InstallmentController> _logger;

    public InstallmentController(
        IInstallmentService installmentService,
        ILogger<InstallmentController> logger)
    {
        _installmentService = installmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get available installment options for a given order amount (Public)
    /// </summary>
    [HttpGet("options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<InstallmentOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InstallmentOptionDto>>> GetInstallmentOptions(
        [FromQuery] decimal amount,
        CancellationToken cancellationToken)
    {
        try
        {
            if (amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than zero" });
            }

            var options = await _installmentService.GetActiveInstallmentOptionsAsync(amount, cancellationToken);
            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installment options for amount: {Amount}", amount);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculate installment details for a specific option and amount (Public)
    /// </summary>
    [HttpGet("calculate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InstallmentCalculationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InstallmentCalculationDto>> CalculateInstallment(
        [FromQuery] decimal amount,
        [FromQuery] Guid optionId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than zero" });
            }

            var calculation = await _installmentService.CalculateInstallmentDetailsAsync(amount, optionId, cancellationToken);
            return Ok(calculation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating installment for amount: {Amount}, optionId: {OptionId}", amount, optionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get global installment configuration (Admin)
    /// </summary>
    [HttpGet("configuration")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(InstallmentConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstallmentConfigurationDto>> GetConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _installmentService.GetInstallmentConfigurationAsync(cancellationToken);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installment configuration");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update global installment configuration (Admin)
    /// </summary>
    [HttpPut("configuration")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(InstallmentConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InstallmentConfigurationDto>> UpdateConfiguration(
        [FromBody] UpdateInstallmentConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (dto.MinimumAmount < 0)
            {
                return BadRequest(new { error = "Minimum amount cannot be negative" });
            }

            var config = await _installmentService.UpdateInstallmentConfigurationAsync(dto, cancellationToken);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating installment configuration");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all installment options (Admin)
    /// </summary>
    [HttpGet("admin/options")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<InstallmentOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InstallmentOptionDto>>> GetAllOptions(CancellationToken cancellationToken)
    {
        try
        {
            var options = await _installmentService.GetAllInstallmentOptionsAsync(cancellationToken);
            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all installment options");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create new installment option (Admin)
    /// </summary>
    [HttpPost("admin/options")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(InstallmentOptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InstallmentOptionDto>> CreateOption(
        [FromBody] CreateInstallmentOptionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.BankName))
            {
                return BadRequest(new { error = "Bank name is required" });
            }

            if (dto.InstallmentPeriod <= 0)
            {
                return BadRequest(new { error = "Installment period must be greater than zero" });
            }

            if (dto.InterestPercentage < 0)
            {
                return BadRequest(new { error = "Interest percentage cannot be negative" });
            }

            var option = await _installmentService.CreateInstallmentOptionAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetAllOptions), new { id = option.Id }, option);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating installment option");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update installment option (Admin)
    /// </summary>
    [HttpPut("admin/options/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(InstallmentOptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstallmentOptionDto>> UpdateOption(
        Guid id,
        [FromBody] CreateInstallmentOptionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.BankName))
            {
                return BadRequest(new { error = "Bank name is required" });
            }

            if (dto.InstallmentPeriod <= 0)
            {
                return BadRequest(new { error = "Installment period must be greater than zero" });
            }

            if (dto.InterestPercentage < 0)
            {
                return BadRequest(new { error = "Interest percentage cannot be negative" });
            }

            var option = await _installmentService.UpdateInstallmentOptionAsync(id, dto, cancellationToken);
            return Ok(option);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating installment option: {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete installment option (Admin)
    /// </summary>
    [HttpDelete("admin/options/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOption(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _installmentService.DeleteInstallmentOptionAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting installment option: {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
