namespace SmartTeam.Application.DTOs;

// ── Admin CRUD DTOs ──────────────────────────────────────────────────────────

public class CreateExpargoWeightRuleDto
{
    /// <summary>Minimum weight in kg (inclusive). Must be >= 0.</summary>
    public decimal MinWeight { get; set; }

    /// <summary>
    /// Maximum weight in kg (inclusive). Leave null for an open-ended rule
    /// (the last band, e.g. "5 kg and above").
    /// </summary>
    public decimal? MaxWeight { get; set; }

    /// <summary>Flat delivery fee in AZN for this weight band.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Extra cost per kg above MinWeight — only used when MaxWeight is null.
    /// Example: MinWeight=5, BasePrice=5, AdditionalPricePerKg=1 → 7 kg costs 5 + (7-5)*1 = 7 AZN.
    /// </summary>
    public decimal AdditionalPricePerKg { get; set; } = 0m;
}

public class UpdateExpargoWeightRuleDto
{
    public decimal MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public decimal BasePrice { get; set; }
    public decimal AdditionalPricePerKg { get; set; } = 0m;
    public bool IsActive { get; set; } = true;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public class ExpargoWeightRuleDto
{
    public Guid Id { get; set; }
    public decimal MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public decimal BasePrice { get; set; }
    public decimal AdditionalPricePerKg { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Returned by the /calculate endpoint and used internally during order creation.</summary>
public class ExpargoDeliveryFeeDto
{
    /// <summary>Total cart weight used for the calculation (kg).</summary>
    public decimal TotalWeightKg { get; set; }

    /// <summary>Calculated Expargo delivery fee in AZN.</summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>The rule that was matched (for transparency).</summary>
    public ExpargoWeightRuleDto? MatchedRule { get; set; }
}
