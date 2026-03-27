using Microsoft.EntityFrameworkCore;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private const string CART_MINIMUM_AMOUNT_KEY = "Cart_MinimumAmount";

    public SettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> GetCartMinimumAmountAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _unitOfWork.Repository<AppSetting>()
            .FirstOrDefaultAsync(s => s.Key == CART_MINIMUM_AMOUNT_KEY, cancellationToken);

        if (setting == null || !decimal.TryParse(setting.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
        {
            return 0;
        }

        return amount;
    }

    public async Task UpdateCartMinimumAmountAsync(decimal amount, CancellationToken cancellationToken = default)
    {
        var setting = await _unitOfWork.Repository<AppSetting>()
            .FirstOrDefaultAsync(s => s.Key == CART_MINIMUM_AMOUNT_KEY, cancellationToken);

        bool isNew = false;
        if (setting == null)
        {
            isNew = true;
            setting = new AppSetting
            {
                Key = CART_MINIMUM_AMOUNT_KEY,
                Category = "Cart",
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AppSetting>().AddAsync(setting, cancellationToken);
        }

        setting.Value = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        setting.UpdatedAt = DateTime.UtcNow;

        if (!isNew)
        {
            _unitOfWork.Repository<AppSetting>().Update(setting);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
