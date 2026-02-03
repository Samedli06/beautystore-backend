using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public WalletController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    /// <summary>
    /// Get user wallet balance
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WalletDto>> GetWallet(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
             return Unauthorized();
        }

        var wallet = await _loyaltyService.GetWalletAsync(userId, cancellationToken);
        return Ok(wallet);
    }

    /// <summary>
    /// Get user wallet transaction history
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(List<WalletTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WalletTransactionDto>>> GetTransactions(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
             return Unauthorized();
        }

        var transactions = await _loyaltyService.GetWalletTransactionsAsync(userId, cancellationToken);
        return Ok(transactions);
    }
}
