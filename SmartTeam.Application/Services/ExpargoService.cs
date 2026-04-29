using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class ExpargoService : IExpargoService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpargoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── Admin CRUD ────────────────────────────────────────────────────────────

    public async Task<ExpargoWeightRuleDto> CreateRuleAsync(
        CreateExpargoWeightRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateRuleDto(dto.MinWeight, dto.MaxWeight, dto.BasePrice, dto.AdditionalPricePerKg);

        var rule = new ExpargoWeightPricingRule
        {
            Id                    = Guid.NewGuid(),
            MinWeight             = dto.MinWeight,
            MaxWeight             = dto.MaxWeight,
            BasePrice             = dto.BasePrice,
            AdditionalPricePerKg  = dto.AdditionalPricePerKg,
            IsActive              = true,
            CreatedAt             = DateTime.UtcNow
        };

        await _unitOfWork.Repository<ExpargoWeightPricingRule>().AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(rule);
    }

    public async Task<ExpargoWeightRuleDto> UpdateRuleAsync(
        Guid id,
        UpdateExpargoWeightRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        var rule = await _unitOfWork.Repository<ExpargoWeightPricingRule>().GetByIdAsync(id, cancellationToken)
            ?? throw new ArgumentException($"Expargo pricing rule {id} not found.");

        ValidateRuleDto(dto.MinWeight, dto.MaxWeight, dto.BasePrice, dto.AdditionalPricePerKg);

        rule.MinWeight            = dto.MinWeight;
        rule.MaxWeight            = dto.MaxWeight;
        rule.BasePrice            = dto.BasePrice;
        rule.AdditionalPricePerKg = dto.AdditionalPricePerKg;
        rule.IsActive             = dto.IsActive;
        rule.UpdatedAt            = DateTime.UtcNow;

        _unitOfWork.Repository<ExpargoWeightPricingRule>().Update(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(rule);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _unitOfWork.Repository<ExpargoWeightPricingRule>().GetByIdAsync(id, cancellationToken)
            ?? throw new ArgumentException($"Expargo pricing rule {id} not found.");

        _unitOfWork.Repository<ExpargoWeightPricingRule>().Remove(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ExpargoWeightRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _unitOfWork.Repository<ExpargoWeightPricingRule>().GetAllAsync(cancellationToken);
        return rules.OrderBy(r => r.MinWeight).Select(MapToDto).ToList();
    }

    // ── Fee Calculation ───────────────────────────────────────────────────────

    public async Task<ExpargoDeliveryFeeDto> CalculateDeliveryFeeAsync(
        decimal totalWeightKg,
        CancellationToken cancellationToken = default)
    {
        if (totalWeightKg < 0)
            throw new ArgumentException("Total weight cannot be negative.");

        var allRules = await _unitOfWork.Repository<ExpargoWeightPricingRule>()
            .FindAsync(r => r.IsActive, cancellationToken);

        // Sort rules: fixed-range rules first (ascending MinWeight), open-ended last
        var sortedRules = allRules
            .OrderBy(r => r.MinWeight)
            .ThenBy(r => r.MaxWeight.HasValue ? 0 : 1)  // fixed before open-ended
            .ToList();

        ExpargoWeightPricingRule? matched = null;
        decimal fee = 0m;

        foreach (var rule in sortedRules)
        {
            bool aboveMin = totalWeightKg >= rule.MinWeight;

            if (rule.MaxWeight.HasValue)
            {
                // Fixed range: MinWeight <= weight <= MaxWeight
                if (aboveMin && totalWeightKg <= rule.MaxWeight.Value)
                {
                    matched = rule;
                    fee = rule.BasePrice;
                    break;
                }
            }
            else
            {
                // Open-ended: weight >= MinWeight (no upper bound)
                if (aboveMin)
                {
                    matched = rule;
                    var extraKg = totalWeightKg - rule.MinWeight;
                    fee = rule.BasePrice + extraKg * rule.AdditionalPricePerKg;
                    break;
                }
            }
        }

        if (matched == null)
        {
            throw new InvalidOperationException(
                $"No active Expargo pricing rule found for weight {totalWeightKg:F3} kg. " +
                "Please ask admin to configure pricing rules before using Expargo delivery.");
        }

        return new ExpargoDeliveryFeeDto
        {
            TotalWeightKg = totalWeightKg,
            DeliveryFee   = Math.Round(fee, 2),
            MatchedRule   = MapToDto(matched)
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ValidateRuleDto(decimal minWeight, decimal? maxWeight, decimal basePrice, decimal additionalPricePerKg)
    {
        if (minWeight < 0)
            throw new ArgumentException("MinWeight must be >= 0.");

        if (maxWeight.HasValue && maxWeight.Value <= minWeight)
            throw new ArgumentException("MaxWeight must be greater than MinWeight.");

        if (basePrice < 0)
            throw new ArgumentException("BasePrice must be >= 0.");

        if (additionalPricePerKg < 0)
            throw new ArgumentException("AdditionalPricePerKg must be >= 0.");
    }

    private static ExpargoWeightRuleDto MapToDto(ExpargoWeightPricingRule rule) =>
        new()
        {
            Id                   = rule.Id,
            MinWeight            = rule.MinWeight,
            MaxWeight            = rule.MaxWeight,
            BasePrice            = rule.BasePrice,
            AdditionalPricePerKg = rule.AdditionalPricePerKg,
            IsActive             = rule.IsActive,
            CreatedAt            = rule.CreatedAt,
            UpdatedAt            = rule.UpdatedAt
        };
}
