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
}
