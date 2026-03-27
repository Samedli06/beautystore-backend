using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IPdfService
{
    /// <summary>
    /// Generates a PDF receipt for a single order
    /// </summary>
    Task<byte[]> GenerateOrderReceiptAsync(OrderDto order, CancellationToken ct = default);

    /// <summary>
    /// Generates a bulk PDF containing multiple orders, each starting on a new page
    /// </summary>
    Task<byte[]> GenerateBulkOrderReceiptsAsync(List<OrderDto> orders, CancellationToken ct = default);
}
