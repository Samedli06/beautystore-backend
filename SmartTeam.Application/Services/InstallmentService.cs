using Microsoft.EntityFrameworkCore;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class InstallmentService : IInstallmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public InstallmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<InstallmentOptionDto>> GetActiveInstallmentOptionsAsync(decimal orderAmount, CancellationToken cancellationToken = default)
    {
        // Get global configuration
        var config = await GetOrCreateConfigurationAsync(cancellationToken);
        
        // If installments are disabled globally, return empty list
        if (!config.IsEnabled)
        {
            return new List<InstallmentOptionDto>();
        }
        
        // Check if order amount meets global minimum
        if (orderAmount < config.MinimumAmount)
        {
            return new List<InstallmentOptionDto>();
        }
        
        // Get all active options
        var allOptions = await _unitOfWork.Repository<InstallmentOption>()
            .FindAsync(o => o.IsActive, cancellationToken);
        
        // Filter by option-specific minimum amounts and sort
        var validOptions = allOptions
            .Where(o => !o.MinimumAmount.HasValue || orderAmount >= o.MinimumAmount.Value)
            .OrderBy(o => o.DisplayOrder)
            .ThenBy(o => o.InstallmentPeriod)
            .Select(o => new InstallmentOptionDto
            {
                Id = o.Id,
                BankName = o.BankName,
                InstallmentPeriod = o.InstallmentPeriod,
                InterestPercentage = o.InterestPercentage,
                IsActive = o.IsActive,
                MinimumAmount = o.MinimumAmount,
                DisplayOrder = o.DisplayOrder,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToList();
        
        return validOptions;
    }

    public async Task<InstallmentCalculationDto> CalculateInstallmentDetailsAsync(decimal amount, Guid installmentOptionId, CancellationToken cancellationToken = default)
    {
        var option = await _unitOfWork.Repository<InstallmentOption>()
            .FirstOrDefaultAsync(o => o.Id == installmentOptionId, cancellationToken);
        
        if (option == null)
        {
            throw new ArgumentException("Installment option not found", nameof(installmentOptionId));
        }
        
        // Calculate interest amount
        var interestAmount = amount * (option.InterestPercentage / 100);
        var totalAmount = amount + interestAmount;
        var monthlyPayment = totalAmount / option.InstallmentPeriod;
        
        return new InstallmentCalculationDto
        {
            InstallmentOptionId = option.Id,
            BankName = option.BankName,
            InstallmentPeriod = option.InstallmentPeriod,
            InterestPercentage = option.InterestPercentage,
            OriginalAmount = amount,
            InterestAmount = interestAmount,
            TotalAmount = totalAmount,
            MonthlyPayment = Math.Round(monthlyPayment, 2)
        };
    }

    public async Task<InstallmentConfigurationDto> GetInstallmentConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetOrCreateConfigurationAsync(cancellationToken);
        
        return new InstallmentConfigurationDto
        {
            Id = config.Id,
            IsEnabled = config.IsEnabled,
            MinimumAmount = config.MinimumAmount,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    public async Task<InstallmentConfigurationDto> UpdateInstallmentConfigurationAsync(UpdateInstallmentConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        var config = await GetOrCreateConfigurationAsync(cancellationToken);
        
        config.IsEnabled = dto.IsEnabled;
        config.MinimumAmount = dto.MinimumAmount;
        config.UpdatedAt = DateTime.UtcNow;
        
        _unitOfWork.Repository<InstallmentConfiguration>().Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new InstallmentConfigurationDto
        {
            Id = config.Id,
            IsEnabled = config.IsEnabled,
            MinimumAmount = config.MinimumAmount,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    public async Task<List<InstallmentOptionDto>> GetAllInstallmentOptionsAsync(CancellationToken cancellationToken = default)
    {
        var allOptions = await _unitOfWork.Repository<InstallmentOption>()
            .GetAllAsync(cancellationToken);
        
        var options = allOptions
            .OrderBy(o => o.DisplayOrder)
            .ThenBy(o => o.InstallmentPeriod)
            .ToList();
        
        return options.Select(o => new InstallmentOptionDto
        {
            Id = o.Id,
            BankName = o.BankName,
            InstallmentPeriod = o.InstallmentPeriod,
            InterestPercentage = o.InterestPercentage,
            IsActive = o.IsActive,
            MinimumAmount = o.MinimumAmount,
            DisplayOrder = o.DisplayOrder,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        }).ToList();
    }

    public async Task<InstallmentOptionDto> CreateInstallmentOptionAsync(CreateInstallmentOptionDto dto, CancellationToken cancellationToken = default)
    {
        var option = new InstallmentOption
        {
            Id = Guid.NewGuid(),
            BankName = dto.BankName,
            InstallmentPeriod = dto.InstallmentPeriod,
            InterestPercentage = dto.InterestPercentage,
            IsActive = dto.IsActive,
            MinimumAmount = dto.MinimumAmount,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };
        
        await _unitOfWork.Repository<InstallmentOption>().AddAsync(option, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new InstallmentOptionDto
        {
            Id = option.Id,
            BankName = option.BankName,
            InstallmentPeriod = option.InstallmentPeriod,
            InterestPercentage = option.InterestPercentage,
            IsActive = option.IsActive,
            MinimumAmount = option.MinimumAmount,
            DisplayOrder = option.DisplayOrder,
            CreatedAt = option.CreatedAt,
            UpdatedAt = option.UpdatedAt
        };
    }

    public async Task<InstallmentOptionDto> UpdateInstallmentOptionAsync(Guid id, CreateInstallmentOptionDto dto, CancellationToken cancellationToken = default)
    {
        var option = await _unitOfWork.Repository<InstallmentOption>()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        
        if (option == null)
        {
            throw new ArgumentException("Installment option not found", nameof(id));
        }
        
        option.BankName = dto.BankName;
        option.InstallmentPeriod = dto.InstallmentPeriod;
        option.InterestPercentage = dto.InterestPercentage;
        option.IsActive = dto.IsActive;
        option.MinimumAmount = dto.MinimumAmount;
        option.DisplayOrder = dto.DisplayOrder;
        option.UpdatedAt = DateTime.UtcNow;
        
        _unitOfWork.Repository<InstallmentOption>().Update(option);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new InstallmentOptionDto
        {
            Id = option.Id,
            BankName = option.BankName,
            InstallmentPeriod = option.InstallmentPeriod,
            InterestPercentage = option.InterestPercentage,
            IsActive = option.IsActive,
            MinimumAmount = option.MinimumAmount,
            DisplayOrder = option.DisplayOrder,
            CreatedAt = option.CreatedAt,
            UpdatedAt = option.UpdatedAt
        };
    }

    public async Task DeleteInstallmentOptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var option = await _unitOfWork.Repository<InstallmentOption>()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        
        if (option == null)
        {
            throw new ArgumentException("Installment option not found", nameof(id));
        }
        
        _unitOfWork.Repository<InstallmentOption>().Remove(option);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ValidateInstallmentSelectionAsync(decimal amount, Guid installmentOptionId, CancellationToken cancellationToken = default)
    {
        // Get global configuration
        var config = await GetOrCreateConfigurationAsync(cancellationToken);
        
        // Check if installments are enabled
        if (!config.IsEnabled)
        {
            return false;
        }
        
        // Check global minimum amount
        if (amount < config.MinimumAmount)
        {
            return false;
        }
        
        // Get the selected option
        var option = await _unitOfWork.Repository<InstallmentOption>()
            .FirstOrDefaultAsync(o => o.Id == installmentOptionId, cancellationToken);
        
        if (option == null || !option.IsActive)
        {
            return false;
        }
        
        // Check option-specific minimum amount
        if (option.MinimumAmount.HasValue && amount < option.MinimumAmount.Value)
        {
            return false;
        }
        
        return true;
    }

    private async Task<InstallmentConfiguration> GetOrCreateConfigurationAsync(CancellationToken cancellationToken)
    {
        var allConfigs = await _unitOfWork.Repository<InstallmentConfiguration>()
            .GetAllAsync(cancellationToken);
        
        var config = allConfigs.FirstOrDefault();
        
        if (config == null)
        {
            // Create default configuration
            config = new InstallmentConfiguration
            {
                Id = Guid.NewGuid(),
                IsEnabled = false,
                MinimumAmount = 100,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Repository<InstallmentConfiguration>().AddAsync(config, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        return config;
    }
}
