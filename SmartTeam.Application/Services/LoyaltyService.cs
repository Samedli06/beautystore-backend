using Microsoft.EntityFrameworkCore;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly IUnitOfWork _unitOfWork;
    private const string BONUS_PERCENTAGE_KEY = "Loyalty_BonusPercentage";

    public LoyaltyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> GetBonusPercentageAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _unitOfWork.Repository<AppSetting>()
            .FirstOrDefaultAsync(s => s.Key == BONUS_PERCENTAGE_KEY, cancellationToken);

        if (setting == null || !decimal.TryParse(setting.Value, out var percentage))
        {
            return 0;
        }

        return percentage;
    }

    public async Task SetBonusPercentageAsync(decimal percentage, CancellationToken cancellationToken = default)
    {
        var setting = await _unitOfWork.Repository<AppSetting>()
            .FirstOrDefaultAsync(s => s.Key == BONUS_PERCENTAGE_KEY, cancellationToken);

        bool isNew = false;
        if (setting == null)
        {
            isNew = true;
            setting = new AppSetting
            {
                Key = BONUS_PERCENTAGE_KEY,
                Category = "Loyalty"
            };
            await _unitOfWork.Repository<AppSetting>().AddAsync(setting, cancellationToken);
        }

        setting.Value = percentage.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        setting.UpdatedAt = DateTime.UtcNow;

        if (!isNew)
        {
            _unitOfWork.Repository<AppSetting>().Update(setting);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AwardBonusForOrderAsync(Guid userId, Guid orderId, decimal orderTotal, CancellationToken cancellationToken = default)
    {
        // 1. Check Bonus Percentage
        var percentage = await GetBonusPercentageAsync(cancellationToken);
        if (percentage <= 0) return;

        // 2. Idempotency Check: Verify if bonus already awarded for this order
        var existingTransaction = await _unitOfWork.Repository<WalletTransaction>()
            .FirstOrDefaultAsync(t => t.OrderId == orderId && t.Type == WalletTransactionType.Earned, cancellationToken);
        
        if (existingTransaction != null) return; // Already awarded

        // 3. Get or Create Wallet
        var wallet = await _unitOfWork.Repository<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        bool isNewWallet = false;
        if (wallet == null)
        {
            isNewWallet = true;
            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0
            };
            await _unitOfWork.Repository<Wallet>().AddAsync(wallet, cancellationToken);
            // Must save to get Wallet ID if it's generated, but usually GUID is set by us or auto-gen. 
            // If GUID is auto-gen by DB, we might need SaveChanges here.
            // But usually we can set Id = Guid.NewGuid() if standard.
            // Let's assume EF Core handles graph.
        }

        // 4. Calculate Bonus
        var bonusAmount = Math.Round(orderTotal * percentage / 100, 2);

        if (bonusAmount <= 0) return;

        // 5. Update Wallet
        var balanceBefore = wallet.Balance;
        var balanceAfter = wallet.Balance + bonusAmount;
        
        wallet.Balance = balanceAfter;
        wallet.UpdatedAt = DateTime.UtcNow;

        if (!isNewWallet)
        {
            _unitOfWork.Repository<Wallet>().Update(wallet);
        }

        // 6. Create Transaction
        var transaction = new WalletTransaction
        {
            Wallet = wallet, // Link to object directly to ensure FK is set correctly after insert
            OrderId = orderId,
            Amount = bonusAmount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Type = WalletTransactionType.Earned,
            Description = $"Bonus ({percentage}%) for Order",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<WalletTransaction>().AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<WalletDto> GetWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _unitOfWork.Repository<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet == null)
        {
            return new WalletDto { Balance = 0, UserId = userId };
        }

        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance
        };
    }

    public async Task<List<WalletTransactionDto>> GetWalletTransactionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _unitOfWork.Repository<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet == null) return new List<WalletTransactionDto>();

        var transactions = await _unitOfWork.Repository<WalletTransaction>()
             .FindAsync(t => t.WalletId == wallet.Id, cancellationToken);
             
        // Ensure ordering. FindAsync might return unsorted.
        // Repository pattern might not support OrderBy in FindAsync.
        // We sort in memory for now (list shouldn't be massive per user).

        return transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Type = (int)t.Type,
                TypeText = t.Type.ToString(),
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                OrderId = t.OrderId
            }).ToList();
    }
}
