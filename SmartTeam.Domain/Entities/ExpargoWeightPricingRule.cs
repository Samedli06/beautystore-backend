namespace SmartTeam.Domain.Entities;

/// <summary>
/// Admin-defined weight-based pricing rule for Expargo delivery.
/// Rules are evaluated in order: MinWeight (inclusive) to MaxWeight (inclusive).
/// A null MaxWeight indicates an open-ended rule (applies to all weights above MinWeight).
/// </summary>
public class ExpargoWeightPricingRule
{
    public Guid Id { get; set; }

    /// <summary>Minimum weight in kg (inclusive) for this rule to apply.</summary>
    public decimal MinWeight { get; set; }

    /// <summary>
    /// Maximum weight in kg (inclusive). Null means open-ended (no upper bound).
    /// For open-ended rules, AdditionalPricePerKg is used to calculate the extra cost.
    /// </summary>
    public decimal? MaxWeight { get; set; }

    /// <summary>Base delivery price in AZN for this weight band.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Additional cost per kg above MinWeight for open-ended rules (MaxWeight == null).
    /// E.g. if MinWeight=5, BasePrice=5, AdditionalPricePerKg=1 and weight=7 → fee = 5 + (7-5)*1 = 7 AZN.
    /// Ignored when MaxWeight is set (fixed-range rules).
    /// </summary>
    public decimal AdditionalPricePerKg { get; set; } = 0m;

    /// <summary>Whether this rule is active. Inactive rules are excluded from calculation.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
