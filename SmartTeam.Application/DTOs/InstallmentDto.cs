namespace SmartTeam.Application.DTOs;

/// <summary>
/// Global installment configuration DTO
/// </summary>
public class InstallmentConfigurationDto
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
    public decimal MinimumAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for updating installment configuration
/// </summary>
public class UpdateInstallmentConfigurationDto
{
    public bool IsEnabled { get; set; }
    public decimal MinimumAmount { get; set; }
}

/// <summary>
/// Bank/card specific installment option DTO
/// </summary>
public class InstallmentOptionDto
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public int InstallmentPeriod { get; set; }
    public decimal InterestPercentage { get; set; }
    public bool IsActive { get; set; }
    public decimal? MinimumAmount { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating or updating installment option
/// </summary>
public class CreateInstallmentOptionDto
{
    public string BankName { get; set; } = string.Empty;
    public int InstallmentPeriod { get; set; }
    public decimal InterestPercentage { get; set; }
    public bool IsActive { get; set; }
    public decimal? MinimumAmount { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for installment calculation result
/// </summary>
public class InstallmentCalculationDto
{
    public Guid InstallmentOptionId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public int InstallmentPeriod { get; set; }
    public decimal InterestPercentage { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
}

/// <summary>
/// DTO for customer's installment selection during checkout
/// </summary>
public class InstallmentSelectionDto
{
    public Guid InstallmentOptionId { get; set; }
}
