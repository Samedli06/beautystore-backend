namespace SmartTeam.Domain.Entities;

/// <summary>
/// Bank/card specific installment option
/// </summary>
public class InstallmentOption
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Bank or card provider name (e.g., "Kapital Bank", "AccessBank")
    /// </summary>
    public string BankName { get; set; } = string.Empty;
    
    /// <summary>
    /// Installment period in months (e.g., 2, 3, 6, 9, 12, 18, 24)
    /// </summary>
    public int InstallmentPeriod { get; set; }
    
    /// <summary>
    /// Interest percentage for this installment period
    /// </summary>
    public decimal InterestPercentage { get; set; }
    
    /// <summary>
    /// Enable or disable this specific installment option
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Minimum amount for this specific option (optional override of global minimum)
    /// </summary>
    public decimal? MinimumAmount { get; set; }
    
    /// <summary>
    /// Display order for frontend
    /// </summary>
    public int DisplayOrder { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
