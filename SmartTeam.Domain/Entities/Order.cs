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

/// <summary>
/// Azerpost delivery type.
/// PostOffice (0) = customer picks up from nearest post office.
/// HomeDelivery (1) = delivered to the customer's door.
/// </summary>
public enum AzerpostDeliveryType
{
    PostOffice = 0,
    HomeDelivery = 1
}

/// <summary>
/// Top-level shipping / delivery provider selected at checkout.
/// Azerpost (0) = existing Azerpost integration (postal service).
/// Expargo   (1) = paid courier for regions outside Baku area.
/// FreeDelivery (2) = free delivery for Bakı / Abşeron / Sumqayıt.
/// </summary>
public enum ShippingMethod
{
    Azerpost    = 0,
    Expargo     = 1,
    FreeDelivery = 2
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
    
    // Azerpost delivery fields
    /// <summary>Azerbaijan postal code e.g. "AZ1045"</summary>
    public string? DeliveryPostCode { get; set; }
    /// <summary>Customer passport/ID number required by Azerpost</summary>
    public string? UserPassport { get; set; }
    /// <summary>Package weight in kg (default 0.1)</summary>
    public decimal PackageWeight { get; set; } = 0.1m;
    /// <summary>Whether the package contains fragile items</summary>
    public bool Fragile { get; set; }
    /// <summary>Azerpost delivery type: 0 = post office pickup, 1 = home delivery</summary>
    public AzerpostDeliveryType DeliveryType { get; set; } = AzerpostDeliveryType.PostOffice;
    /// <summary>Tracking ID returned by Azerpost after successful order creation</summary>
    public string? AzerpostOrderId { get; set; }

    /// <summary>
    /// Top-level delivery provider: Azerpost (0), Expargo (1), FreeDelivery (2).
    /// Defaults to Azerpost for backward compatibility with existing orders.
    /// </summary>
    public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Azerpost;
    
    /// <summary>Total cart weight (kg) snapshotted at order creation time.</summary>
    public decimal TotalWeightKg { get; set; }

    /// <summary>Delivery fee in AZN snapshotted at order creation. 0 for FreeDelivery.</summary>
    public decimal DeliveryFee { get; set; }
    
    // Installment payment fields
    public int? InstallmentPeriod { get; set; }
    public decimal? InstallmentInterestPercentage { get; set; }
    public decimal? InstallmentInterestAmount { get; set; }
    public decimal? OriginalAmount { get; set; }
    public decimal WalletAmountUsed { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
    public Payment? Payment { get; set; }
}
