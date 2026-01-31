using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IEpointService
{
    /// <summary>
    /// Generate signature for Epoint API request
    /// </summary>
    string GenerateSignature(string jsonData);
    
    /// <summary>
    /// Create payment request to Epoint
    /// </summary>
    Task<EpointPaymentResponse> CreatePaymentRequestAsync(Guid orderId, decimal amount, string? description, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verify callback signature from Epoint
    /// </summary>
    bool VerifyCallbackSignature(EpointCallbackDto callback);
}
