using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IEpointService _epointService;
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IEpointService epointService,
        IOrderService orderService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentController> logger)
    {
        _epointService = epointService;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Initiate payment for user's cart
    /// </summary>
    [HttpPost("initiate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EpointPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EpointPaymentResponse>> InitiatePayment(
        [FromBody] CreateOrderDto createOrderDto,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid? userId = null;
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (userIdClaim != null && Guid.TryParse(userIdClaim, out var parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }
            }
            catch { }

            // Get cart to calculate total and create snapshot
            var cartService = HttpContext.RequestServices.GetRequiredService<ICartService>();
            var cart = await cartService.GetUserCartAsync(userId, cancellationToken);
            
            if (cart == null || !cart.Items.Any())
            {
                return BadRequest(new { error = "Cart is empty" });
            }

            // Create pending order (not actual order yet)
            var pendingOrderId = Guid.NewGuid();
            var pendingOrder = new PendingOrder
            {
                Id = pendingOrderId,
                UserId = userId,
                CartSnapshot = JsonSerializer.Serialize(cart),
                CustomerInfo = JsonSerializer.Serialize(createOrderDto),
                TotalAmount = cart.FinalAmount,
                PromoCode = cart.AppliedPromoCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1) // Expire after 1 hour
            };

            await _unitOfWork.Repository<PendingOrder>().AddAsync(pendingOrder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create payment record linked to pending order
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                PendingOrderId = pendingOrderId,
                OrderId = null, // No order yet
                EpointTransactionId = $"TEMP_{Guid.NewGuid()}", // Temporary ID until Epoint response
                Amount = cart.FinalAmount,
                Currency = "AZN",
                Status = PaymentStatus.Initiated,
                PaymentMethod = "Epoint",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Payment>().AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create payment request to Epoint (use PendingOrder ID)
            var epointResponse = await _epointService.CreatePaymentRequestAsync(
                pendingOrderId,
                cart.FinalAmount,
                $"Payment for order",
                cancellationToken);

            // Update payment with Epoint transaction ID
            if (!string.IsNullOrEmpty(epointResponse.transaction_id))
            {
                payment.EpointTransactionId = epointResponse.transaction_id;
                payment.ResponseData = JsonSerializer.Serialize(epointResponse);
                _unitOfWork.Repository<Payment>().Update(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Ok(epointResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Success callback from Epoint (user is redirected here)
    /// </summary>
    [HttpGet("success")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentSuccess(
        [FromQuery(Name = "order_id")] string? orderId,
        [FromQuery(Name = "transaction_id")] string? transactionId)
    {
        // Log all query parameters to debug parameter names
        var queryParams = string.Join(", ", Request.Query.Select(k => $"{k.Key}={k.Value}"));
        _logger.LogInformation("PaymentSuccess called with query: {Query}", queryParams);
        _logger.LogInformation("PaymentSuccess - order_id: {OrderId}, transaction_id: {TransactionId}", orderId, transactionId);

        // Try multiple parameter name variations
        if (string.IsNullOrEmpty(orderId))
        {
            orderId = Request.Query["order"].ToString();
            if (string.IsNullOrEmpty(orderId))
                orderId = Request.Query["orderId"].ToString();
            if (string.IsNullOrEmpty(orderId))
                orderId = Request.Query["order_number"].ToString();
            if (string.IsNullOrEmpty(orderId))
                orderId = Request.Query["orderNumber"].ToString();
        }
        
        if (string.IsNullOrEmpty(transactionId))
        {
            transactionId = Request.Query["transaction"].ToString();
            if (string.IsNullOrEmpty(transactionId))
                transactionId = Request.Query["transactionId"].ToString();
            if (string.IsNullOrEmpty(transactionId))
                transactionId = Request.Query["trans_id"].ToString();
        }

        _logger.LogInformation("PaymentSuccess - Final values: order_id={OrderId}, transaction_id={TransactionId}", orderId, transactionId);

        // URL encode parameters to ensure they're preserved
        var encodedOrderId = Uri.EscapeDataString(orderId ?? "");
        var encodedTransactionId = Uri.EscapeDataString(transactionId ?? "");

        // Redirect to frontend success page
        var frontendUrl = $"https://gunaybeauty.com/payment/success?orderId={encodedOrderId}&transactionId={encodedTransactionId}";
        _logger.LogInformation("Redirecting to: {Url}", frontendUrl);
        return Redirect(frontendUrl);
    }

    /// <summary>
    /// Error callback from Epoint (user is redirected here)
    /// </summary>
    [HttpGet("error")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentError(
        [FromQuery(Name = "order_id")] string? orderId,
        [FromQuery(Name = "transaction_id")] string? transactionId,
        [FromQuery(Name = "message")] string? message)
    {
        // Log all query parameters
        var queryParams = string.Join(", ", Request.Query.Select(k => $"{k.Key}={k.Value}"));
        _logger.LogInformation("PaymentError called with query: {Query}", queryParams);

        // Try multiple parameter name variations
        if (string.IsNullOrEmpty(orderId))
        {
            orderId = Request.Query["order"].ToString();
            if (string.IsNullOrEmpty(orderId))
                orderId = Request.Query["orderId"].ToString();
        }
        
        if (string.IsNullOrEmpty(transactionId))
        {
            transactionId = Request.Query["transaction"].ToString();
            if (string.IsNullOrEmpty(transactionId))
                transactionId = Request.Query["transactionId"].ToString();
        }

        // URL encode parameters
        var encodedOrderId = Uri.EscapeDataString(orderId ?? "");
        var encodedTransactionId = Uri.EscapeDataString(transactionId ?? "");
        var encodedMessage = Uri.EscapeDataString(message ?? "");

        // Redirect to frontend error page
        var frontendUrl = $"https://gunaybeauty.com/payment/error?orderId={encodedOrderId}&transactionId={encodedTransactionId}&message={encodedMessage}";
        return Redirect(frontendUrl);
    }

    /// <summary>
    /// Result callback from Epoint (server-to-server notification)
    /// </summary>
    [HttpPost("result")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PaymentResult([FromBody] EpointCallbackDto callback, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received Epoint callback: {Callback}", JsonSerializer.Serialize(callback));

            // Verify signature
            if (!_epointService.VerifyCallbackSignature(callback))
            {
                _logger.LogWarning("Invalid signature in Epoint callback");
                return BadRequest(new { error = "Invalid signature" });
            }

            // Parse pending order ID (not order ID anymore!)
            if (!Guid.TryParse(callback.order_id, out var pendingOrderId))
            {
                _logger.LogWarning("Invalid pending order ID in callback: {OrderId}", callback.order_id);
                return BadRequest(new { error = "Invalid pending order ID" });
            }

            // Get pending order
            var pendingOrder = await _unitOfWork.Repository<PendingOrder>()
                .FirstOrDefaultAsync(p => p.Id == pendingOrderId, cancellationToken);
            
            if (pendingOrder == null)
            {
                _logger.LogWarning("Pending order not found: {PendingOrderId}", pendingOrderId);
                return BadRequest(new { error = "Pending order not found" });
            }

            // Get payment
            var payment = await _unitOfWork.Repository<Payment>()
                .FirstOrDefaultAsync(p => p.PendingOrderId == pendingOrderId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for pending order: {PendingOrderId}", pendingOrderId);
                return BadRequest(new { error = "Payment not found" });
            }

            // Update payment transaction ID if provided
            if (!string.IsNullOrEmpty(callback.transaction_id))
            {
                payment.EpointTransactionId = callback.transaction_id;
            }

            // Process based on payment status
            if (callback.status.ToLower() == "success" || callback.status.ToLower() == "completed")
            {
                _logger.LogInformation("Payment successful for pending order: {PendingOrderId}", pendingOrderId);
                
                // Deserialize cart and customer info
                var cart = JsonSerializer.Deserialize<CartDto>(pendingOrder.CartSnapshot);
                var customerInfo = JsonSerializer.Deserialize<CreateOrderDto>(pendingOrder.CustomerInfo);
                
                if (cart == null || customerInfo == null)
                {
                    _logger.LogError("Failed to deserialize pending order data");
                    return BadRequest(new { error = "Invalid pending order data" });
                }

                // NOW create the actual order
                var order = await _orderService.CreateOrderFromCartAsync(
                    pendingOrder.UserId, 
                    customerInfo, 
                    cancellationToken);

                _logger.LogInformation("Order created: {OrderId} from pending order: {PendingOrderId}", 
                    order.Id, pendingOrderId);

                // Update payment to link to actual order
                payment.OrderId = order.Id;
                payment.PendingOrderId = null;
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                payment.ResponseData = JsonSerializer.Serialize(callback);
                _unitOfWork.Repository<Payment>().Update(payment);

                // Update order status to Paid
                await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Paid, cancellationToken);
                
                // Link payment to order
                await _orderService.LinkPaymentToOrderAsync(order.Id, payment.Id, cancellationToken);

                // Delete pending order (no longer needed)
                _unitOfWork.Repository<PendingOrder>().Remove(pendingOrder);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment completed successfully. Order: {OrderId}, Payment: {PaymentId}", 
                    order.Id, payment.Id);
            }
            else if (callback.status.ToLower() == "failed" || callback.status.ToLower() == "error")
            {
                _logger.LogWarning("Payment failed for pending order: {PendingOrderId}", pendingOrderId);
                
                // Mark payment as failed
                payment.Status = PaymentStatus.Failed;
                payment.ErrorMessage = callback.message;
                payment.ResponseData = JsonSerializer.Serialize(callback);
                _unitOfWork.Repository<Payment>().Update(payment);

                // Delete pending order (no order will be created)
                _unitOfWork.Repository<PendingOrder>().Remove(pendingOrder);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment failed. Pending order deleted: {PendingOrderId}", pendingOrderId);
            }
            else if (callback.status.ToLower() == "cancelled")
            {
                _logger.LogWarning("Payment cancelled for pending order: {PendingOrderId}", pendingOrderId);
                
                payment.Status = PaymentStatus.Cancelled;
                payment.ResponseData = JsonSerializer.Serialize(callback);
                _unitOfWork.Repository<Payment>().Update(payment);

                // Delete pending order
                _unitOfWork.Repository<PendingOrder>().Remove(pendingOrder);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogWarning("Unknown payment status: {Status}", callback.status);
                payment.ErrorMessage = $"Unknown status: {callback.status}";
                payment.ResponseData = JsonSerializer.Serialize(callback);
                _unitOfWork.Repository<Payment>().Update(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Ok(new { message = "Callback processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Epoint callback");
            return BadRequest(new { error = ex.Message });
        }
    }
}
