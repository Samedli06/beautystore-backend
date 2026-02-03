using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public LoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    /// <summary>
    /// Get current loyalty bonus percentage (Admin only)
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LoyaltySettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoyaltySettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var percentage = await _loyaltyService.GetBonusPercentageAsync(cancellationToken);
        return Ok(new LoyaltySettingsDto { BonusPercentage = percentage });
    }

    /// <summary>
    /// Update loyalty bonus percentage (Admin only)
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateSettings([FromBody] LoyaltySettingsDto settings, CancellationToken cancellationToken)
    {
        if (settings.BonusPercentage < 0 || settings.BonusPercentage > 100)
            return BadRequest(new { error = "Percentage must be between 0 and 100" });

        await _loyaltyService.SetBonusPercentageAsync(settings.BonusPercentage, cancellationToken);
        return Ok(new { message = "Loyalty settings updated successfully" });
    }


}
