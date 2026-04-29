using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Services;

public interface ICreditRequestService
{
    /// <summary>
    /// Creates a new credit request from the authenticated user's active cart.
    /// Snapshots all product data at the time of the request.
    /// Does NOT clear the cart, create an order, or touch the payment system.
    /// </summary>
    Task<CreditRequestDto> CreateAsync(
        Guid? userId,
        CreateCreditRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single credit request with its full item list (Admin use).
    /// </summary>
    Task<CreditRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated, optionally filtered list of all credit requests (Admin use).
    /// Supported filter values: "Pending", "Contacted", "Approved", "Rejected".
    /// </summary>
    Task<PagedResultDto<CreditRequestDto>> GetAllPagedAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a credit request and optionally sets an admin note (Admin use).
    /// </summary>
    Task<CreditRequestDto> UpdateStatusAsync(
        Guid id,
        CreditRequestStatus status,
        string? notes,
        CancellationToken cancellationToken = default);
}
