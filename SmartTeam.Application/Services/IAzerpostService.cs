namespace SmartTeam.Application.Services;

public interface IAzerpostService
{
    /// <summary>
    /// Create a single delivery order in Azerpost for a paid order.
    /// Returns the Azerpost tracking ID (order_Id) on success, null on failure.
    /// </summary>
    Task<string?> CreateOrderAsync(SmartTeam.Domain.Entities.Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an Azerpost order and returns both the tracking ID, calculated delivery charge, and any error message.
    /// </summary>
    Task<(string? TrackingId, decimal DeliveryFee, string? ErrorMessage)> CreateOrderWithFeeAsync(SmartTeam.Domain.Entities.Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create multiple delivery orders in Azerpost in a single bulk call.
    /// Returns a dictionary of OrderNumber → AzerpostOrderId for successful entries.
    /// </summary>
    Task<Dictionary<string, string>> CreateBulkOrdersAsync(IEnumerable<SmartTeam.Domain.Entities.Order> orders, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify Azerpost that the vendor payment has been collected from the customer.
    /// </summary>
    Task<bool> UpdateVendorPaymentStatusAsync(string packageId, bool isPaid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the current tracking status of a package from Azerpost.
    /// </summary>
    Task<SmartTeam.Application.DTOs.AzerpostPackageStatus?> GetPackageStatusAsync(string packageId, CancellationToken cancellationToken = default);
}
