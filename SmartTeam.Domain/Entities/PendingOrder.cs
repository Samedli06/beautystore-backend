namespace SmartTeam.Domain.Entities;

/// <summary>
/// Temporary storage for order data before payment confirmation
/// </summary>
public class PendingOrder
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    
    // JSON snapshot of cart data
    public string CartSnapshot { get; set; } = string.Empty;
    
    // JSON snapshot of customer information
    public string CustomerInfo { get; set; } = string.Empty;
    
    public decimal TotalAmount { get; set; }
    public string? PromoCode { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
}
