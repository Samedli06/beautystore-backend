using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class CreditRequestService : ICreditRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    private readonly IAzerpostService _azerpostService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CreditRequestService> _logger;

    public CreditRequestService(
        IUnitOfWork unitOfWork,
        ICartService cartService,
        IProductService productService,
        IAzerpostService azerpostService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CreditRequestService> logger)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
        _productService = productService;
        _azerpostService = azerpostService;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<CreditRequestDto> CreateAsync(
        Guid? userId,
        CreateCreditRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // ── Validation ────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ArgumentException("Ad və soyad tələb olunur.");

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            throw new ArgumentException("Telefon nömrəsi tələb olunur.");

        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Kredit müraciəti göndərmək üçün istifadəçi sistemə daxil olmalıdır.");

        // ── Read cart ─────────────────────────────────────────────────────────
        var cart = await _cartService.GetUserCartAsync(userId, cancellationToken);

        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Səbət boşdur. Kredit müraciəti göndərməzdən əvvəl məhsullar əlavə edin.");

        // ── Build aggregate ───────────────────────────────────────────────────
        var creditRequest = new CreditRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            TotalAmount = cart.FinalAmount,
            Status = CreditRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Snapshot each cart item — do NOT rely on live product data later
        foreach (var item in cart.Items)
        {
            creditRequest.Items.Add(new CreditRequestItem
            {
                Id = Guid.NewGuid(),
                CreditRequestId = creditRequest.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSku = item.ProductSku,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                TotalPrice = item.TotalPrice
            });
        }

        // ── Persist ───────────────────────────────────────────────────────────
        await _unitOfWork.Repository<CreditRequest>().AddAsync(creditRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // NOTE: Cart is intentionally NOT cleared — user may still check out normally.

        return MapToDto(creditRequest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<CreditRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var creditRequest = await _unitOfWork.Repository<CreditRequest>().GetByIdAsync(id, cancellationToken);
        if (creditRequest == null) return null;

        var items = await _unitOfWork.Repository<CreditRequestItem>()
            .FindAsync(i => i.CreditRequestId == id, cancellationToken);

        creditRequest.Items = items.ToList();
        return MapToDto(creditRequest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAllPagedAsync
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<PagedResultDto<CreditRequestDto>> GetAllPagedAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var allRequests = await _unitOfWork.Repository<CreditRequest>().GetAllAsync(cancellationToken);
        var query = allRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<CreditRequestStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(cr => cr.Status == parsedStatus);
        }

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paged = query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<CreditRequestDto>();
        foreach (var cr in paged)
        {
            var items = await _unitOfWork.Repository<CreditRequestItem>()
                .FindAsync(i => i.CreditRequestId == cr.Id, cancellationToken);

            cr.Items = items.ToList();
            dtos.Add(MapToDto(cr));
        }

        return new PagedResultDto<CreditRequestDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateStatusAsync — main logic: Approved → create paid Order + Azerpost
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<CreditRequestDto> UpdateStatusAsync(
        Guid id,
        CreditRequestStatus status,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var creditRequest = await _unitOfWork.Repository<CreditRequest>().GetByIdAsync(id, cancellationToken);
        if (creditRequest == null)
            throw new ArgumentException($"'{id}' ID-li kredit müraciəti tapılmadı.");

        // Prevent double-conversion (idempotent guard)
        if (status == CreditRequestStatus.Approved && creditRequest.ConvertedOrderId.HasValue)
            throw new InvalidOperationException(
                $"Bu kredit müraciəti artıq təsdiqlənib və {creditRequest.ConvertedOrderId} nömrəli sifarişə çevrilib.");

        creditRequest.Status = status;
        creditRequest.UpdatedAt = DateTime.UtcNow;

        if (notes != null)
            creditRequest.Notes = notes.Trim();

        // ── When Approved: create a real paid order from the snapshotted items ─
        if (status == CreditRequestStatus.Approved)
        {
            // Load items (generic repo doesn't eager-load navigation props)
            var items = await _unitOfWork.Repository<CreditRequestItem>()
                .FindAsync(i => i.CreditRequestId == id, cancellationToken);

            creditRequest.Items = items.ToList();

            if (!creditRequest.Items.Any())
                throw new InvalidOperationException("Məhsulu olmayan kredit müraciətini təsdiqləmək mümkün deyil.");

            // Resolve the user (needed for CustomerEmail on the order)
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(creditRequest.UserId, cancellationToken);
            var customerEmail = user?.Email ?? string.Empty;

            // Generate a unique order number
            var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

            // Build the Order — Status is Paid immediately (credit was approved = payment confirmed)
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                UserId = creditRequest.UserId,
                SubTotal = creditRequest.TotalAmount,
                DiscountAmount = 0m,
                TotalAmount = creditRequest.TotalAmount,
                Status = OrderStatus.Paid,
                CustomerName = creditRequest.FullName,
                CustomerPhone = creditRequest.PhoneNumber,
                CustomerEmail = customerEmail,
                Notes = $"Kredit müraciəti təsdiqləndi. Müraciət ID: {creditRequest.Id}",
                // Azerpost defaults — admin can update via dispatch endpoint if needed
                PackageWeight = 0.1m,
                Fragile = false,
                DeliveryType = AzerpostDeliveryType.PostOffice,
                CreatedAt = DateTime.UtcNow
            };

            // Copy snapshotted items → OrderItems
            foreach (var item in creditRequest.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductSku = item.ProductSku,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Persist the order
            await _unitOfWork.Repository<Order>().AddAsync(order, cancellationToken);

            // Link the order back to the credit request
            creditRequest.ConvertedOrderId = order.Id;

            // Save everything in one transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── Reduce stock (best-effort, non-blocking) ──────────────────────
            foreach (var item in creditRequest.Items)
            {
                try
                {
                    await _productService.ReduceStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "ReduceStockAsync failed for product {ProductId} on credit approval {CreditRequestId}.",
                        item.ProductId, id);
                }
            }

            // ── Azerpost dispatch (fire-and-forget background task) ───────────
            var orderIdToDispatch = order.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var azerpostSvc = scope.ServiceProvider.GetRequiredService<IAzerpostService>();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var freshOrder = await uow.Repository<Order>()
                        .GetByIdAsync(orderIdToDispatch, CancellationToken.None);

                    if (freshOrder != null && string.IsNullOrEmpty(freshOrder.AzerpostOrderId))
                    {
                        var azerpostId = await azerpostSvc.CreateOrderAsync(freshOrder, CancellationToken.None);
                        if (!string.IsNullOrEmpty(azerpostId))
                        {
                            freshOrder.AzerpostOrderId = azerpostId;
                            freshOrder.UpdatedAt = DateTime.UtcNow;
                            await uow.SaveChangesAsync(CancellationToken.None);

                            _logger.LogInformation(
                                "Credit-approved order {OrderId} dispatched to Azerpost: {AzerpostId}.",
                                orderIdToDispatch, azerpostId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Background Azerpost dispatch failed for credit-approved order {OrderId}.",
                        orderIdToDispatch);
                }
            });
        }
        else
        {
            // Non-Approved status update: just save the status change
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Re-load items for the response
            var items = await _unitOfWork.Repository<CreditRequestItem>()
                .FindAsync(i => i.CreditRequestId == id, cancellationToken);
            creditRequest.Items = items.ToList();
        }

        return MapToDto(creditRequest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a unique order number in the format CR-YYYYMMDD-XXXX,
    /// where CR prefix distinguishes credit-approved orders from regular online orders.
    /// </summary>
    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var rng = new Random();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var number = $"CR-{date}-{rng.Next(1000, 9999)}";
            var exists = await _unitOfWork.Repository<Order>()
                .FirstOrDefaultAsync(o => o.OrderNumber == number, cancellationToken);

            if (exists == null) return number;
        }

        // Guaranteed-unique fallback
        return $"CR-{date}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static CreditRequestDto MapToDto(CreditRequest cr) =>
        new()
        {
            Id = cr.Id,
            UserId = cr.UserId,
            FullName = cr.FullName,
            PhoneNumber = cr.PhoneNumber,
            TotalAmount = cr.TotalAmount,
            Status = cr.Status.ToString(),
            Notes = cr.Notes,
            ConvertedOrderId = cr.ConvertedOrderId,
            CreatedAt = cr.CreatedAt,
            UpdatedAt = cr.UpdatedAt,
            Items = cr.Items.Select(i => new CreditRequestItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductSku = i.ProductSku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
}
