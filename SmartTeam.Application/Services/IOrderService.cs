using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Services;

public interface IOrderService
{
    /// <summary>
    /// Create order from user's cart
    /// </summary>
    Task<OrderDto> CreateOrderFromCartAsync(Guid? userId, CreateOrderDto createOrderDto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get order by ID
    /// </summary>
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get order by order number
    /// </summary>
    Task<OrderDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all orders for a user
    /// </summary>
    Task<List<OrderDto>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update order status
    /// </summary>
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Link payment to order
    /// </summary>
    Task LinkPaymentToOrderAsync(Guid orderId, Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    Task<List<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all paid orders (Admin only)
    /// </summary>
    Task<List<OrderDto>> GetPaidOrdersAsync(CancellationToken cancellationToken = default);
}
