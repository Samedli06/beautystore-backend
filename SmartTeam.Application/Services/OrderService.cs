using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace SmartTeam.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IInstallmentService _installmentService;
    private readonly IAzerpostService _azerpostService;
    private readonly IExpargoService _expargoService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICartService cartService,
        IProductService productService,
        ILoyaltyService loyaltyService,
        IInstallmentService installmentService,
        IAzerpostService azerpostService,
        IExpargoService expargoService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cartService = cartService;
        _productService = productService;
        _loyaltyService = loyaltyService;
        _installmentService = installmentService;
        _azerpostService = azerpostService;
        _expargoService = expargoService;
        _serviceScopeFactory = serviceScopeFactory;
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

        // Handle installment selection if provided
        decimal finalAmount = cartDto.FinalAmount;
        int? installmentPeriod = null;
        decimal? installmentInterestPercentage = null;
        decimal? installmentInterestAmount = null;
        decimal? originalAmount = null;

        if (createOrderDto.InstallmentOptionId.HasValue)
        {
            // Validate installment selection
            var isValid = await _installmentService.ValidateInstallmentSelectionAsync(
                cartDto.FinalAmount, 
                createOrderDto.InstallmentOptionId.Value, 
                cancellationToken);

            if (!isValid)
            {
                throw new InvalidOperationException("Invalid installment option selected");
            }

            // Calculate installment details
            var installmentCalculation = await _installmentService.CalculateInstallmentDetailsAsync(
                cartDto.FinalAmount,
                createOrderDto.InstallmentOptionId.Value,
                cancellationToken);

            // Store original amount and calculate new total with interest
            originalAmount = cartDto.FinalAmount;
            installmentPeriod = installmentCalculation.InstallmentPeriod;
            installmentInterestPercentage = installmentCalculation.InterestPercentage;
            installmentInterestAmount = installmentCalculation.InterestAmount;
            finalAmount = installmentCalculation.TotalAmount;
        }

        // ── Parse and validate ShippingMethod ───────────────────────────────────────
        if (!Enum.TryParse<ShippingMethod>(createOrderDto.ShippingMethod, ignoreCase: true, out var shippingMethod))
        {
            throw new ArgumentException(
                $"Invalid ShippingMethod '{createOrderDto.ShippingMethod}'. " +
                "Accepted values: Azerpost, Expargo, FreeDelivery.");
        }

        // Expargo and FreeDelivery require contact details
        if (shippingMethod == ShippingMethod.Expargo || shippingMethod == ShippingMethod.FreeDelivery)
        {
            if (string.IsNullOrWhiteSpace(createOrderDto.CustomerName))
                throw new ArgumentException("CustomerName (Ad Soyad) is required for this delivery type.");
            if (string.IsNullOrWhiteSpace(createOrderDto.CustomerPhone))
                throw new ArgumentException("CustomerPhone is required for this delivery type.");
            if (string.IsNullOrWhiteSpace(createOrderDto.ShippingAddress))
                throw new ArgumentException("ShippingAddress (delivery address) is required for this delivery type.");
        }

        // Create order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            UserId = user.Id,
            SubTotal = cartDto.SubTotal,
            DiscountAmount = cartDto.PromoCodeDiscountAmount,
            TotalAmount = finalAmount,
            PromoCode = cartDto.AppliedPromoCode,
            PromoCodeDiscountPercentage = cartDto.PromoCodeDiscountPercentage,
            InstallmentPeriod = installmentPeriod,
            InstallmentInterestPercentage = installmentInterestPercentage,
            InstallmentInterestAmount = installmentInterestAmount,
            OriginalAmount = originalAmount,
            Status = OrderStatus.Pending,
            CustomerName = createOrderDto.CustomerName,
            CustomerEmail = createOrderDto.CustomerEmail,
            CustomerPhone = createOrderDto.CustomerPhone,
            ShippingAddress = createOrderDto.ShippingAddress,
            Notes = createOrderDto.Notes,
            // Azerpost delivery fields (kept as-is; ignored for Expargo/FreeDelivery)
            DeliveryPostCode = createOrderDto.DeliveryPostCode,
            UserPassport = createOrderDto.UserPassport,
            TotalWeightKg = cartDto.TotalWeightKg,
            PackageWeight = cartDto.TotalWeightKg > 0 ? cartDto.TotalWeightKg : (createOrderDto.PackageWeight > 0 ? createOrderDto.PackageWeight : 0.1m),
            Fragile = createOrderDto.Fragile,
            DeliveryType = (SmartTeam.Domain.Entities.AzerpostDeliveryType)createOrderDto.DeliveryType,
            ShippingMethod = shippingMethod,
            CreatedAt = DateTime.UtcNow
        };

        // ── Delivery fee calculation ─────────────────────────────────────────────
        if (shippingMethod == ShippingMethod.Azerpost)
        {
            // Existing Azerpost flow: completely unchanged
            var azerpostResult = await _azerpostService.CreateOrderWithFeeAsync(order, cancellationToken);
            if (!string.IsNullOrEmpty(azerpostResult.TrackingId))
            {
                order.AzerpostOrderId = azerpostResult.TrackingId;
                order.DeliveryFee = azerpostResult.DeliveryFee;
                order.TotalAmount += azerpostResult.DeliveryFee;
            }
        }
        else if (shippingMethod == ShippingMethod.Expargo)
        {
            // Weight-based fee from admin-configured pricing rules
            var feeResult = await _expargoService.CalculateDeliveryFeeAsync(cartDto.TotalWeightKg, cancellationToken);
            order.DeliveryFee = feeResult.DeliveryFee;
            order.TotalAmount += feeResult.DeliveryFee;
        }
        else // FreeDelivery
        {
            // No delivery fee; TotalAmount stays at cart total
            order.DeliveryFee = 0m;
        }

        // Handle Wallet Usage
        decimal walletDeduction = 0;
        if (createOrderDto.WalletAmountToUse.HasValue && createOrderDto.WalletAmountToUse.Value > 0 && user.Id != Guid.Parse("00000000-0000-0000-0000-000000000001"))
        {
            var wallet = await _loyaltyService.GetWalletAsync(user.Id, cancellationToken);
            if (wallet != null)
            {
                if (wallet.Balance < createOrderDto.WalletAmountToUse.Value)
                {
                    throw new InvalidOperationException($"Insufficient wallet balance. Available: {wallet.Balance}, Requested: {createOrderDto.WalletAmountToUse.Value}");
                }

                // Use the requested amount, but don't exceed order total
                walletDeduction = Math.Min(createOrderDto.WalletAmountToUse.Value, order.TotalAmount);
                
                order.WalletAmountUsed = walletDeduction;
                order.TotalAmount -= walletDeduction;
            }
        }

        // Check if fully paid via wallet
        if (order.TotalAmount == 0 && walletDeduction > 0)
        {
            order.Status = OrderStatus.Paid;
        }

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

        // Deduct from wallet if used
        if (walletDeduction > 0)
        {
            await _loyaltyService.DeductBalanceAsync(user.Id, order.Id, walletDeduction, cancellationToken);
        }

        // If order is paid (fully via wallet), reduce stock immediately
        if (order.Status == OrderStatus.Paid)
        {
             // Clear cart items from stock
             foreach (var cartItem in cartDto.Items)
             {
                 await _productService.ReduceStockAsync(cartItem.ProductId, cartItem.Quantity, cancellationToken);
             }

             // Notify Azerpost that it has been paid (fully by wallet) — only if Azerpost delivery
             if (!string.IsNullOrEmpty(order.AzerpostOrderId))
             {
                 await _azerpostService.UpdateVendorPaymentStatusAsync(order.AzerpostOrderId, true, cancellationToken);
             }
        }

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
        // Use GetByIdAsync (FindAsync) so EF returns the already-tracked entity if one exists in
        // the current DbContext scope, avoiding the "entity with same key already tracked" conflict.
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        // ── Step 1: Reduce stock ─────────────────────────────────────────────
        if (status == OrderStatus.Paid && order.Status != OrderStatus.Paid)
        {
            var orderItems = await _unitOfWork.Repository<OrderItem>()
                .FindAsync(oi => oi.OrderId == orderId, cancellationToken);

            foreach (var item in orderItems)
            {
                try
                {
                    await _productService.ReduceStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] ReduceStockAsync failed for product {item.ProductId}: {ex.Message}");
                }
            }
        }

        // ── Step 2: Save order status IMMEDIATELY with CancellationToken.None ─
        // We MUST save before any external HTTP call (Azerpost) which can hang
        // for 100 seconds and cause the request CancellationToken to fire,
        // which would make SaveChangesAsync throw OperationCanceledException.
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // ── Step 3: Side-effects AFTER the status is committed ───────────────
        // All of these use CancellationToken.None to avoid being killed by
        // an already-expired request token.
        if (status == OrderStatus.Paid)
        {
            // Loyalty bonus
            try
            {
                await _loyaltyService.AwardBonusForOrderAsync(order.UserId, order.Id, order.TotalAmount, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] AwardBonusForOrderAsync failed for order {orderId}: {ex.Message}");
            }

            // Notify Azerpost that payment is complete (Fire-and-forget background task)
            if (!string.IsNullOrEmpty(order.AzerpostOrderId))
            {
                var azerpostOrderId = order.AzerpostOrderId;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var azerpostSvc = scope.ServiceProvider.GetRequiredService<IAzerpostService>();
                        await azerpostSvc.UpdateVendorPaymentStatusAsync(azerpostOrderId, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARN] Background Azerpost update failed for package {azerpostOrderId}: {ex.Message}");
                    }
                });
            }
        }

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

    public async Task<PagedResultDto<OrderDto>> GetAllOrdersPagedAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        // Add console logging for debugging
        Console.WriteLine($"GetAllOrdersPagedAsync called with page={page}, pageSize={pageSize}, status={status}");

        var query = await _unitOfWork.Repository<Order>().GetAllAsync(cancellationToken);
        var orders = query.AsQueryable();

        // Apply status filter
        if (!string.IsNullOrEmpty(status))
        {
            if (status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                orders = orders.Where(o => o.Status == OrderStatus.Paid);
            }
            else if (status.Equals("Unpaid", StringComparison.OrdinalIgnoreCase))
            {
                // Unpaid includes Pending and PaymentInitiated
                orders = orders.Where(o => 
                    o.Status == OrderStatus.Pending || 
                    o.Status == OrderStatus.PaymentInitiated);
            }
            else if (status.Equals("Error", StringComparison.OrdinalIgnoreCase) || 
                     status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                orders = orders.Where(o => o.Status == OrderStatus.Failed);
            }
            else if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            {
                orders = orders.Where(o => o.Status == parsedStatus);
            }
        }

        // Calculate total count
        var totalCount = orders.Count();
        Console.WriteLine($"Total count: {totalCount}");

        // Apply pagination
        var pagedOrders = orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Map to DTOs
        var orderDtos = new List<OrderDto>();
        foreach (var order in pagedOrders)
        {
            orderDtos.Add(await MapOrderToDto(order, cancellationToken));
        }

        // Calculate total pages
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        Console.WriteLine($"Total pages: {totalPages}");

        return new PagedResultDto<OrderDto>
        {
            Items = orderDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
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

    public async Task<(string? TrackingId, string? ErrorMessage)> DispatchToAzerpostAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new ArgumentException("Order not found");

        if (!string.IsNullOrEmpty(order.AzerpostOrderId))
            return (order.AzerpostOrderId, null); // Already dispatched

        var azerpostResult = await _azerpostService.CreateOrderWithFeeAsync(order, cancellationToken);
        if (!string.IsNullOrEmpty(azerpostResult.TrackingId))
        {
            order.AzerpostOrderId = azerpostResult.TrackingId;
            order.DeliveryFee = azerpostResult.DeliveryFee;
            order.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return (azerpostResult.TrackingId, azerpostResult.ErrorMessage);
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
                    InstallmentPeriod = payment.InstallmentPeriod,
                    InstallmentInterestAmount = payment.InstallmentInterestAmount,
                    OriginalAmount = payment.OriginalAmount,
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
            InstallmentPeriod = order.InstallmentPeriod,
            InstallmentInterestPercentage = order.InstallmentInterestPercentage,
            InstallmentInterestAmount = order.InstallmentInterestAmount,
            OriginalAmount = order.OriginalAmount,
            Status = order.Status.ToString(),
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            ShippingAddress = order.ShippingAddress,
            WalletAmountUsed = order.WalletAmountUsed,
            DeliveryPostCode = order.DeliveryPostCode,
            UserPassport = order.UserPassport,
            PackageWeight = order.PackageWeight,
            TotalWeightKg = order.TotalWeightKg,
            DeliveryFee = order.DeliveryFee,
            Fragile = order.Fragile,
            DeliveryType = (int)order.DeliveryType,
            AzerpostOrderId = order.AzerpostOrderId,
            ShippingMethod = order.ShippingMethod.ToString(),
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
