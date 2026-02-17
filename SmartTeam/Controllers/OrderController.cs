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
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
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
}
