using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

// ─── Request DTOs ────────────────────────────────────────────────────────────

/// <summary>
/// Payload sent by the user when submitting a credit (identity-card) purchase request.
/// Cart contents are read server-side from the authenticated user's active cart.
/// </summary>
public class CreateCreditRequestDto
{
    /// <summary>Full name of the applicant (as shown on identity card).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Contact phone number of the applicant.</summary>
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// Payload sent by an admin to update the status of a credit request.
/// </summary>
public class UpdateCreditRequestStatusDto
{
    public CreditRequestStatus Status { get; set; }

    /// <summary>Optional admin note (e.g. call result, rejection reason).</summary>
    public string? Notes { get; set; }
}

// ─── Response DTOs ───────────────────────────────────────────────────────────

public class CreditRequestItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreditRequestDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    /// <summary>String representation of <see cref="CreditRequestStatus"/>.</summary>
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    /// <summary>
    /// The Order ID created automatically when this request was Approved.
    /// Null until approval.
    /// </summary>
    public Guid? ConvertedOrderId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<CreditRequestItemDto> Items { get; set; } = new();
}
