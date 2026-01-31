namespace SmartTeam.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    PaymentInitiated = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7,
    Failed = 8
}

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PromoCode { get; set; }
    public decimal? PromoCodeDiscountPercentage { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? PaymentId { get; set; }
    
    // Customer information
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
    public Payment? Payment { get; set; }
}
