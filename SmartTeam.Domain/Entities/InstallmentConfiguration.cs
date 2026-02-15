namespace SmartTeam.Domain.Entities;

/// <summary>
/// Global installment payment configuration
/// </summary>
public class InstallmentConfiguration
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Enable or disable installment payments globally
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Minimum order amount to qualify for installment payments
    /// </summary>
    public decimal MinimumAmount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
