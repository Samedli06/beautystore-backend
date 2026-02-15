namespace SmartTeam.Domain.Entities;

public enum PaymentStatus
{
    Pending = 0,
    Initiated = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? PendingOrderId { get; set; }
    public string EpointTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public PaymentStatus Status { get; set; }
    public string PaymentMethod { get; set; } = "Epoint";
    
    // Store request/response data for debugging and verification
    public string? RequestData { get; set; }
    public string? ResponseData { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Installment payment fields
    public int? InstallmentPeriod { get; set; }
    public decimal? InstallmentInterestAmount { get; set; }
    public decimal? OriginalAmount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public Order? Order { get; set; }
    public PendingOrder? PendingOrder { get; set; }
}

