namespace SmartTeam.Domain.Entities;

public enum WalletTransactionType
{
    Earned = 1,
    Spent = 2,
    Adjustment = 3
}

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid? OrderId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public WalletTransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wallet Wallet { get; set; }
    // Order navigation property is optional, depends on if Order is in same context, usually yes.
    public Order? Order { get; set; }
}
