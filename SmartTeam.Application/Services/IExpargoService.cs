using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IExpargoService
{
    // ── Admin CRUD ────────────────────────────────────────────────────────────

    /// <summary>Create a new weight-based pricing rule.</summary>
    Task<ExpargoWeightRuleDto> CreateRuleAsync(CreateExpargoWeightRuleDto dto, CancellationToken cancellationToken = default);

    /// <summary>Update an existing pricing rule.</summary>
    Task<ExpargoWeightRuleDto> UpdateRuleAsync(Guid id, UpdateExpargoWeightRuleDto dto, CancellationToken cancellationToken = default);

    /// <summary>Delete a pricing rule by ID.</summary>
    Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Get all pricing rules (active and inactive).</summary>
    Task<List<ExpargoWeightRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default);

    // ── Calculation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Calculate the Expargo delivery fee for a given total cart weight.
    /// Throws <see cref="InvalidOperationException"/> if no matching active rule is found.
    /// </summary>
    Task<ExpargoDeliveryFeeDto> CalculateDeliveryFeeAsync(decimal totalWeightKg, CancellationToken cancellationToken = default);
}
