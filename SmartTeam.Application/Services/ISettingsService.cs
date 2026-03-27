using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface ISettingsService
{
    Task<decimal> GetCartMinimumAmountAsync(CancellationToken cancellationToken = default);
    Task UpdateCartMinimumAmountAsync(decimal amount, CancellationToken cancellationToken = default);
}
