using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IStatisticsService
{
    /// <summary>
    /// Gets a comprehensive dashboard statistics report for Admin
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>DashboardStatisticsDto containing all metrics</returns>
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken ct = default);
}
