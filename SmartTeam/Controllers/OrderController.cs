using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")] // api/v1/Order
[Route("api/v1/Orders")]        // api/v1/Orders (Explicit match)
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPdfService _pdfService;
    private readonly IAzerpostService _azerpostService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, IPdfService pdfService, IAzerpostService azerpostService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _pdfService = pdfService;
        _azerpostService = azerpostService;
        _logger = logger;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{orderId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", orderId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get order by order number
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderByNumber(string orderNumber, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber, cancellationToken);
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderNumber}", orderNumber);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all orders for the authenticated user
    /// </summary>
    [HttpGet("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<OrderDto>>> GetMyOrders(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, cancellationToken);
            return Ok(orders);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user orders");
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
        return userId;
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResultDto<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<OrderDto>>> GetAllOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderService.GetAllOrdersPagedAsync(page, pageSize, status, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all orders");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all paid orders (Admin only)
    /// </summary>
    [HttpGet("admin/paid")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OrderDto>>> GetPaidOrders(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _orderService.GetPaidOrdersAsync(cancellationToken);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paid orders");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, request.Status, cancellationToken);
            return Ok(updatedOrder);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Order not found: {OrderId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download order receipt as PDF
    /// </summary>
    [HttpGet("{id}/pdf")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadOrderPdf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            var pdfBytes = await _pdfService.GenerateOrderReceiptAsync(order, cancellationToken);
            var fileName = $"qaimə-{order.OrderNumber}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for order {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Manually dispatch an order to Azerpost (Admin only).
    /// Use this if the automatic trigger on payment failed.
    /// </summary>
    [HttpPost("{id}/send-to-azerpost")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendToAzerpost(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
            if (order == null)
                return NotFound(new { error = "Order not found" });

            if (!string.IsNullOrEmpty(order.AzerpostOrderId))
                return Ok(new { message = "Order already dispatched to Azerpost", azerpostOrderId = order.AzerpostOrderId });

            var result = await _orderService.DispatchToAzerpostAsync(id, cancellationToken);
            if (string.IsNullOrEmpty(result.TrackingId))
                return BadRequest(new { error = $"Azerpost rejected the order. Reason: {result.ErrorMessage ?? "Check logs for details."}" });

            return Ok(new { message = "Order dispatched to Azerpost successfully", azerpostOrderId = result.TrackingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching order {OrderId} to Azerpost", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get live Azerpost tracking status for an order (Admin only).
    /// </summary>
    [HttpGet("{id}/azerpost-status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAzerpostStatus(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
            if (order == null)
                return NotFound(new { error = "Order not found" });

            if (string.IsNullOrEmpty(order.AzerpostOrderId))
                return BadRequest(new { error = "This order has not been dispatched to Azerpost yet." });

            var status = await _azerpostService.GetPackageStatusAsync(order.OrderNumber, cancellationToken);
            if (status == null)
                return BadRequest(new { error = "Could not retrieve status from Azerpost." });

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Azerpost status for order {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
