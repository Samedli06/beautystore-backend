namespace SmartTeam.Domain.Entities;

public enum CreditRequestStatus
{
    Pending = 0,
    Contacted = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// Represents a credit purchase request submitted by a user via Identity Card (Şəxsiyyət vəsiqəsi).
/// This is a lead-generation entity.
/// When admin sets Status = Approved, a real paid Order is created automatically from the snapshotted items
/// and dispatched to Azerpost. The resulting OrderId is stored in ConvertedOrderId.
/// </summary>
public class CreditRequest
{
    public Guid Id { get; set; }

    /// <summary>The authenticated user who submitted the request.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The Order that was automatically created when admin approved this request.
    /// Null until the request is Approved.
    /// </summary>
    public Guid? ConvertedOrderId { get; set; }

    /// <summary>Full name provided by the user at the time of the request.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Phone number provided by the user at the time of the request.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Snapshot of the total cart amount at the time of the request.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Current workflow status. Default is Pending.</summary>
    public CreditRequestStatus Status { get; set; } = CreditRequestStatus.Pending;

    /// <summary>Admin-only notes (e.g. contact result, reason for rejection).</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public List<CreditRequestItem> Items { get; set; } = new();
}

/// <summary>
/// A snapshot of a cart item captured at credit-request time.
/// Product data (name, price) is preserved here — NOT resolved from live product records.
/// </summary>
public class CreditRequestItem
{
    public Guid Id { get; set; }
    public Guid CreditRequestId { get; set; }

    /// <summary>Reference to the original product (not required to exist).</summary>
    public Guid ProductId { get; set; }

    /// <summary>Product name at the time of the request (snapshot).</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Product SKU at the time of the request (snapshot).</summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>Unit price at the time of the request (snapshot).</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>UnitPrice * Quantity at the time of the request (snapshot).</summary>
    public decimal TotalPrice { get; set; }

    // Navigation property
    public CreditRequest? CreditRequest { get; set; }
}
