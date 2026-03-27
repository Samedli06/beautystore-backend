using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Get current cart minimum amount (Public)
    /// </summary>
    [HttpGet("cart-minimum-amount")]
    [ProducesResponseType(typeof(CartMinimumAmountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartMinimumAmountDto>> GetCartMinimumAmount(CancellationToken cancellationToken)
    {
        var amount = await _settingsService.GetCartMinimumAmountAsync(cancellationToken);
        return Ok(new CartMinimumAmountDto { MinimumAmount = amount });
    }

    /// <summary>
    /// Update cart minimum amount (Admin only)
    /// </summary>
    [HttpPut("cart-minimum-amount")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateCartMinimumAmount([FromBody] CartMinimumAmountDto dto, CancellationToken cancellationToken)
    {
        if (dto.MinimumAmount < 0)
        {
            return BadRequest(new { error = "Minimum amount cannot be negative" });
        }

        await _settingsService.UpdateCartMinimumAmountAsync(dto.MinimumAmount, cancellationToken);
        return Ok(new { message = "Cart minimum amount updated successfully" });
    }
}
