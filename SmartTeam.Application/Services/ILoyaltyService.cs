using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface ILoyaltyService
{
    Task<decimal> GetBonusPercentageAsync(CancellationToken cancellationToken = default);
    Task SetBonusPercentageAsync(decimal percentage, CancellationToken cancellationToken = default);
    Task AwardBonusForOrderAsync(Guid userId, Guid orderId, decimal orderTotal, CancellationToken cancellationToken = default);
    Task<WalletDto> GetWalletAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<WalletTransactionDto>> GetWalletTransactionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
