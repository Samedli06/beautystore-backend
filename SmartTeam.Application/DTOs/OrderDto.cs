using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PromoCode { get; set; }
    public decimal? PromoCodeDiscountPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    public int? InstallmentPeriod { get; set; }
    public decimal? InstallmentInterestPercentage { get; set; }
    public decimal? InstallmentInterestAmount { get; set; }
    public decimal? OriginalAmount { get; set; }
    public decimal WalletAmountUsed { get; set; }
    // Azerpost delivery fields
    public string? DeliveryPostCode { get; set; }
    public string? UserPassport { get; set; }
    public decimal PackageWeight { get; set; }
    public decimal TotalWeightKg { get; set; }
    public decimal DeliveryFee { get; set; }
    public bool Fragile { get; set; }
    /// <summary>0 = post_office_lcl, 1 = home_delivery_lcl</summary>
    public int DeliveryType { get; set; }
    public string? AzerpostOrderId { get; set; }
    /// <summary>Top-level delivery method: Azerpost | Expargo | FreeDelivery</summary>
    public string ShippingMethod { get; set; } = "Azerpost";
    public List<OrderItemDto> Items { get; set; } = new();
    public PaymentDto? Payment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public string EpointTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Epoint";
    public int? InstallmentPeriod { get; set; }
    public decimal? InstallmentInterestAmount { get; set; }
    public decimal? OriginalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    public Guid? InstallmentOptionId { get; set; }
    public decimal? WalletAmountToUse { get; set; }
    // Azerpost delivery fields
    /// <summary>Azerbaijan postal code e.g. "AZ1045". Required for Azerpost delivery.</summary>
    public string? DeliveryPostCode { get; set; }
    /// <summary>Customer passport / ID number required by Azerpost.</summary>
    public string? UserPassport { get; set; }
    /// <summary>Package weight in kg. Defaults to 0.1 kg if not provided.</summary>
    public decimal PackageWeight { get; set; } = 0.1m;
    /// <summary>Set true if the shipment contains fragile items.</summary>
    public bool Fragile { get; set; }
    /// <summary>
    /// Azerpost delivery type:
    /// 0 = post_office_lcl (customer picks up from nearest post office)
    /// 1 = home_delivery_lcl (delivered to customer's address)
    /// Defaults to 0 (post office pickup).
    /// </summary>
    public int DeliveryType { get; set; } = 0;

    // ── Top-level delivery provider ──────────────────────────────────────────
    /// <summary>
    /// Which delivery provider to use.
    /// Accepted values (case-insensitive): "Azerpost", "Expargo", "FreeDelivery".
    /// Required. Expargo and FreeDelivery require CustomerName, CustomerPhone, ShippingAddress.
    /// </summary>
    public string ShippingMethod { get; set; } = "Azerpost";
}

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}
