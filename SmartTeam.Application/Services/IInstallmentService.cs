using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IInstallmentService
{
    /// <summary>
    /// Get active installment options available for the given order amount
    /// </summary>
    Task<List<InstallmentOptionDto>> GetActiveInstallmentOptionsAsync(decimal orderAmount, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate installment details (interest, total amount, monthly payment) for a given amount and option
    /// </summary>
    Task<InstallmentCalculationDto> CalculateInstallmentDetailsAsync(decimal amount, Guid installmentOptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get global installment configuration
    /// </summary>
    Task<InstallmentConfigurationDto> GetInstallmentConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update global installment configuration (Admin only)
    /// </summary>
    Task<InstallmentConfigurationDto> UpdateInstallmentConfigurationAsync(UpdateInstallmentConfigurationDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all installment options (Admin only)
    /// </summary>
    Task<List<InstallmentOptionDto>> GetAllInstallmentOptionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create new installment option (Admin only)
    /// </summary>
    Task<InstallmentOptionDto> CreateInstallmentOptionAsync(CreateInstallmentOptionDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update installment option (Admin only)
    /// </summary>
    Task<InstallmentOptionDto> UpdateInstallmentOptionAsync(Guid id, CreateInstallmentOptionDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete installment option (Admin only)
    /// </summary>
    Task DeleteInstallmentOptionAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate that the selected installment option is valid for the given amount
    /// </summary>
    Task<bool> ValidateInstallmentSelectionAsync(decimal amount, Guid installmentOptionId, CancellationToken cancellationToken = default);
}
