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

            // Create order from cart immediately
            var order = await _orderService.CreateOrderFromCartAsync(userId, createOrderDto, cancellationToken);
            
            if (order == null)
            {
                return BadRequest(new { error = "Cart is empty or could not create order" });
            }

            // Update order status to PaymentInitiated (effectively "Unpaid")
            await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.PaymentInitiated, cancellationToken);

            // Create payment record linked to order
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PendingOrderId = null, 
                EpointTransactionId = $"TEMP_{Guid.NewGuid()}", 
                Amount = order.TotalAmount,
                Currency = "AZN",
                Status = PaymentStatus.Initiated,
                PaymentMethod = "Epoint",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Payment>().AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Link payment to order
            await _orderService.LinkPaymentToOrderAsync(order.Id, payment.Id, cancellationToken);

            // Create payment request to Epoint using actual Order ID
            var epointResponse = await _epointService.CreatePaymentRequestAsync(
                order.Id,
                order.TotalAmount,
                $"Order {order.OrderNumber}",
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
        [FromQuery(Name = "transaction_id")] string? transactionId,
        CancellationToken cancellationToken)
    {
        // Log all query parameters to debug parameter names
        var queryParams = string.Join(", ", Request.Query.Select(k => $"{k.Key}={k.Value}"));
        _logger.LogInformation("PaymentSuccess called with query: {Query}", queryParams);

        // Try multiple parameter name variations
        if (string.IsNullOrEmpty(orderId))
        {
            orderId = Request.Query["order"].ToString();
            if (string.IsNullOrEmpty(orderId)) orderId = Request.Query["orderId"].ToString();
            if (string.IsNullOrEmpty(orderId)) orderId = Request.Query["order_number"].ToString();
            if (string.IsNullOrEmpty(orderId)) orderId = Request.Query["orderNumber"].ToString();
        }
        
        if (string.IsNullOrEmpty(transactionId))
        {
            transactionId = Request.Query["transaction"].ToString();
            if (string.IsNullOrEmpty(transactionId)) transactionId = Request.Query["transactionId"].ToString();
            if (string.IsNullOrEmpty(transactionId)) transactionId = Request.Query["trans_id"].ToString();
        }

        _logger.LogInformation("PaymentSuccess - Final values: order_id={OrderId}, transaction_id={TransactionId}", orderId, transactionId);

        // =================================================================================
        // FAILSAFE: Update status here in case server-to-server callback failed (common in dev/localhost)
        // =================================================================================
        if (Guid.TryParse(orderId, out var parsedOrderId))
        {
            try 
            {
                var order = await _orderService.GetOrderByIdAsync(parsedOrderId, cancellationToken);
                if (order != null && order.Status != OrderStatus.Paid.ToString())
                {
                    var payment = await _unitOfWork.Repository<Payment>().FirstOrDefaultAsync(p => p.OrderId == parsedOrderId, cancellationToken);
                    if (payment != null)
                    {
                        // If transaction ID is provided, verify it matches
                        // If not provided (Epoint doesn't always send it), still process if payment exists
                        bool shouldProcess = string.IsNullOrEmpty(transactionId) || 
                                           payment.EpointTransactionId == transactionId || 
                                           payment.EpointTransactionId?.StartsWith("TEMP") == true;
                        
                        if (shouldProcess)
                        {
                            _logger.LogInformation("Failsafe: Auto-completing order {OrderId} in success redirect", parsedOrderId);
                            
                            // Update Payment
                            payment.Status = PaymentStatus.Completed;
                            if (!string.IsNullOrEmpty(transactionId))
                            {
                                payment.EpointTransactionId = transactionId; // Update if provided
                            }
                            payment.CompletedAt = DateTime.UtcNow;
                            _unitOfWork.Repository<Payment>().Update(payment);
                            
                            // Update Order (this will reduce stock)
                            await _orderService.UpdateOrderStatusAsync(parsedOrderId, OrderStatus.Paid, cancellationToken);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error executing failsafe status update in PaymentSuccess");
            }
        }
        // =================================================================================

        // URL encode parameters to ensure they're preserved
        var encodedOrderId = Uri.EscapeDataString(orderId ?? "");
        var encodedTransactionId = Uri.EscapeDataString(transactionId ?? "");

        // Redirect to frontend success page with status
        var frontendUrl = $"https://gunaybeauty.com/payment/success?orderId={encodedOrderId}&transactionId={encodedTransactionId}&status=paid";
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
        [FromQuery(Name = "message")] string? message,
        CancellationToken cancellationToken)
    {
        // Log all query parameters
        var queryParams = string.Join(", ", Request.Query.Select(k => $"{k.Key}={k.Value}"));
        _logger.LogInformation("PaymentError called with query: {Query}", queryParams);

        // Try multiple parameter name variations
        if (string.IsNullOrEmpty(orderId))
        {
            orderId = Request.Query["order"].ToString();
            if (string.IsNullOrEmpty(orderId)) orderId = Request.Query["orderId"].ToString();
        }
        
        if (string.IsNullOrEmpty(transactionId))
        {
            transactionId = Request.Query["transaction"].ToString();
            if (string.IsNullOrEmpty(transactionId)) transactionId = Request.Query["transactionId"].ToString();
        }

        // =================================================================================
        // FAILSAFE: Update status here in case server-to-server callback failed
        // =================================================================================
        if (Guid.TryParse(orderId, out var parsedOrderId))
        {
            try 
            {
                var payment = await _unitOfWork.Repository<Payment>().FirstOrDefaultAsync(p => p.OrderId == parsedOrderId, cancellationToken);
                if (payment != null && payment.Status != PaymentStatus.Failed && payment.Status != PaymentStatus.Completed)
                {
                    _logger.LogInformation("Failsafe: Marking order {OrderId} as Failed in error redirect", parsedOrderId);
                    
                    payment.Status = PaymentStatus.Failed;
                    payment.ErrorMessage = message;
                    _unitOfWork.Repository<Payment>().Update(payment);
                    
                    await _orderService.UpdateOrderStatusAsync(parsedOrderId, OrderStatus.Failed, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error executing failsafe status update in PaymentError");
            }
        }
        // =================================================================================

        // URL encode parameters
        var encodedOrderId = Uri.EscapeDataString(orderId ?? "");
        var encodedTransactionId = Uri.EscapeDataString(transactionId ?? "");
        var encodedMessage = Uri.EscapeDataString(message ?? "");

        // Redirect to frontend error page with status
        var frontendUrl = $"https://gunaybeauty.com/payment/error?orderId={encodedOrderId}&transactionId={encodedTransactionId}&message={encodedMessage}&status=failed";
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

            // Parse order ID
            if (!Guid.TryParse(callback.order_id, out var orderId))
            {
                _logger.LogWarning("Invalid order ID in callback: {OrderId}", callback.order_id);
                return BadRequest(new { error = "Invalid order ID" });
            }

            // Get order
            var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", orderId);
                return BadRequest(new { error = "Order not found" });
            }

            // Get payment
            var payment = await _unitOfWork.Repository<Payment>()
                .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
            
            // If payment record doesn't exist (should not happen if initiated correctly), create one or log error
            if (payment == null)
            {
                _logger.LogWarning("Payment record not found for order: {OrderId}. Creating new record based on callback.", orderId);
                // Creating a fallback payment record if original initiation failed to save
                payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    EpointTransactionId = callback.transaction_id ?? $"UNKNOWN_{Guid.NewGuid()}",
                    Amount = callback.amount, 
                    Currency = callback.currency,
                    Status = PaymentStatus.Initiated,
                    PaymentMethod = "Epoint",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<Payment>().AddAsync(payment, cancellationToken);
            }

            // Update payment transaction ID
            if (!string.IsNullOrEmpty(callback.transaction_id))
            {
                payment.EpointTransactionId = callback.transaction_id;
            }

            // Process based on payment status
            if (callback.status.ToLower() == "success" || callback.status.ToLower() == "completed")
            {
                _logger.LogInformation("Payment successful for order: {OrderId}", orderId);
                
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                
                await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Paid, cancellationToken);
            }
            else if (callback.status.ToLower() == "failed" || callback.status.ToLower() == "error")
            {
                _logger.LogWarning("Payment failed for order: {OrderId}", orderId);
                
                payment.Status = PaymentStatus.Failed;
                payment.ErrorMessage = callback.message;
                
                await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Failed, cancellationToken);
            }
            else if (callback.status.ToLower() == "cancelled")
            {
                _logger.LogWarning("Payment cancelled for order: {OrderId}", orderId);
                
                payment.Status = PaymentStatus.Cancelled;
                
                await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled, cancellationToken);
            }

            payment.ResponseData = JsonSerializer.Serialize(callback);
            _unitOfWork.Repository<Payment>().Update(payment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment callback processed successfully for order: {OrderId}", orderId);

            return Ok(new { message = "Callback processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Epoint callback");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
