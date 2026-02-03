using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    private readonly ILoyaltyService _loyaltyService;

    public OrderService(IUnitOfWork unitOfWork, IMapper mapper, ICartService cartService, IProductService productService, ILoyaltyService loyaltyService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cartService = cartService;
        _productService = productService;
        _loyaltyService = loyaltyService;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(Guid? userId, CreateOrderDto createOrderDto, CancellationToken cancellationToken = default)
    {
        // Get user's cart
        var cartDto = await _cartService.GetUserCartAsync(userId, cancellationToken);

        if (cartDto == null || !cartDto.Items.Any())
        {
            throw new InvalidOperationException("Cart is empty");
        }

        // Validate user exists
        User? user = null;
        if (userId.HasValue)
        {
            user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }
        }
        else
        {
            // For anonymous users, use the anonymous user ID
            var anonymousUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            user = await _unitOfWork.Repository<User>().GetByIdAsync(anonymousUserId, cancellationToken);
        }

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

        // Create order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            UserId = user.Id,
            SubTotal = cartDto.SubTotal,
            DiscountAmount = cartDto.PromoCodeDiscountAmount,
            TotalAmount = cartDto.FinalAmount,
            PromoCode = cartDto.AppliedPromoCode,
            PromoCodeDiscountPercentage = cartDto.PromoCodeDiscountPercentage,
            Status = OrderStatus.Pending,
            CustomerName = createOrderDto.CustomerName,
            CustomerEmail = createOrderDto.CustomerEmail,
            CustomerPhone = createOrderDto.CustomerPhone,
            ShippingAddress = createOrderDto.ShippingAddress,
            Notes = createOrderDto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        // Create order items
        foreach (var cartItem in cartDto.Items)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                ProductSku = cartItem.ProductSku,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = cartItem.TotalPrice,
                CreatedAt = DateTime.UtcNow
            };
            order.OrderItems.Add(orderItem);
        }

        // Save order
        await _unitOfWork.Repository<Order>().AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Clear cart after order creation
        await _cartService.ClearCartAsync(userId, cancellationToken);

        return await MapOrderToDto(order, cancellationToken);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId, cancellationToken);
        if (order == null) return null;

        return await MapOrderToDto(order, cancellationToken);
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        
        if (order == null) return null;

        return await MapOrderToDto(order, cancellationToken);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.UserId == userId, cancellationToken);

        var orderDtos = new List<OrderDto>();
        foreach (var order in orders.OrderByDescending(o => o.CreatedAt))
        {
            orderDtos.Add(await MapOrderToDto(order, cancellationToken));
        }

        return orderDtos;
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdWithIncludesAsync(orderId, o => o.OrderItems);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        // Handle post-payment actions when transitioning to Paid status
        if (status == OrderStatus.Paid && order.Status != OrderStatus.Paid)
        {
            // Reduce stock for all items
            foreach (var item in order.OrderItems)
            {
                await _productService.ReduceStockAsync(item.ProductId, item.Quantity, cancellationToken);
            }
        }

        // Award Loyalty Bonus (Idempotent check inside service allows safe retries)
        if (status == OrderStatus.Paid)
        {
            await _loyaltyService.AwardBonusForOrderAsync(order.UserId, order.Id, order.TotalAmount, cancellationToken);
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Order>().Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapOrderToDto(order, cancellationToken);
    }

    public async Task LinkPaymentToOrderAsync(Guid orderId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        order.PaymentId = paymentId;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Order>().Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>()
            .GetAllAsync(cancellationToken);

        var orderDtos = new List<OrderDto>();
        foreach (var order in orders.OrderByDescending(o => o.CreatedAt))
        {
            orderDtos.Add(await MapOrderToDto(order, cancellationToken));
        }

        return orderDtos;
    }



    public async Task<List<OrderDto>> GetPaidOrdersAsync(CancellationToken cancellationToken = default)
    {
        // 1. Get all completed payments
        var payments = await _unitOfWork.Repository<Payment>()
            .FindAsync(p => p.Status == PaymentStatus.Completed, cancellationToken);
        
        var orderIds = payments.Select(p => p.OrderId).Distinct().ToList();

        // 2. Get orders associated with these payments
        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => orderIds.Contains(o.Id), cancellationToken);

        var orderDtos = new List<OrderDto>();
        foreach (var order in orders.OrderByDescending(o => o.CreatedAt))
        {
            orderDtos.Add(await MapOrderToDto(order, cancellationToken));
        }

        return orderDtos;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        // Generate order number: ORD-YYYYMMDD-XXXX
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random();
        var attempts = 0;
        const int maxAttempts = 10;

        while (attempts < maxAttempts)
        {
            var randomNumber = random.Next(1000, 9999);
            var orderNumber = $"ORD-{date}-{randomNumber}";

            var exists = await _unitOfWork.Repository<Order>()
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

            if (exists == null)
            {
                return orderNumber;
            }

            attempts++;
        }

        // Fallback to GUID if we can't generate unique number
        return $"ORD-{date}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    private async Task<OrderDto> MapOrderToDto(Order order, CancellationToken cancellationToken)
    {
        // Get order items
        var orderItems = await _unitOfWork.Repository<OrderItem>()
            .FindAsync(oi => oi.OrderId == order.Id, cancellationToken);

        // Get payment if exists
        PaymentDto? paymentDto = null;
        if (order.PaymentId.HasValue)
        {
            var payment = await _unitOfWork.Repository<Payment>().GetByIdAsync(order.PaymentId.Value, cancellationToken);
            if (payment != null)
            {
                paymentDto = new PaymentDto
                {
                    Id = payment.Id,
                    EpointTransactionId = payment.EpointTransactionId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status.ToString(),
                    PaymentMethod = payment.PaymentMethod,
                    CreatedAt = payment.CreatedAt,
                    CompletedAt = payment.CompletedAt
                };
            }
        }

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            PromoCode = order.PromoCode,
            PromoCodeDiscountPercentage = order.PromoCodeDiscountPercentage,
            Status = order.Status.ToString(),
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            ShippingAddress = order.ShippingAddress,
            Notes = order.Notes,
            Items = orderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                ProductSku = oi.ProductSku,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice
            }).ToList(),
            Payment = paymentDto,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
