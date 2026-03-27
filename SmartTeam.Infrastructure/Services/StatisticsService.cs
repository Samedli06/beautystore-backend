using Microsoft.EntityFrameworkCore;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public StatisticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddSeconds(-1);
        var equivalentDayLastMonth = now.AddMonths(-1);

        // 1. Orders and Revenue
        var allOrders = await _unitOfWork.Repository<Order>().GetAllAsync(ct);
        var deliveredOrders = allOrders.Where(o => o.Status == OrderStatus.Delivered).ToList();

        var revenue = new RevenueStatsDto
        {
            Today = deliveredOrders.Where(o => o.CreatedAt.Date == today).Sum(o => o.TotalAmount),
            ThisMonth = deliveredOrders.Where(o => o.CreatedAt >= startOfMonth).Sum(o => o.TotalAmount),
            Total = deliveredOrders.Sum(o => o.TotalAmount),
            AverageOrderValue = deliveredOrders.Any() ? deliveredOrders.Average(o => o.TotalAmount) : 0
        };

        var orders = new OrderStatsDto
        {
            Total = allOrders.Count(),
            Pending = allOrders.Count(o => o.Status == OrderStatus.Pending),
            Confirmed = allOrders.Count(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Processing),
            Delivered = allOrders.Count(o => o.Status == OrderStatus.Delivered)
        };

        // 2. Customers
        var allUsers = await _unitOfWork.Repository<User>().GetAllAsync(ct);
        var customers = new CustomerStatsDto
        {
            Total = allUsers.Count(),
            Active = allOrders.Select(o => o.UserId).Distinct().Count()
        };

        // 3. Products
        var allProducts = await _unitOfWork.Repository<Product>().GetAllAsync(ct);
        var orderItems = await _unitOfWork.Repository<OrderItem>().GetAllAsync(ct);

        var topSellingProducts = orderItems
            .GroupBy(oi => oi.ProductId)
            .OrderByDescending(g => g.Sum(oi => oi.Quantity))
            .Take(10)
            .Select(g => new TopProductDto
            {
                Id = g.Key,
                Name = allProducts.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "Unknown",
                SoldCount = g.Sum(oi => oi.Quantity)
            })
            .ToList();

        var productStats = new ProductStatsDto
        {
            Total = allProducts.Count(),
            LowStock = allProducts.Count(p => p.StockQuantity < 10),
            TopSellingProducts = topSellingProducts
        };

        // 4. Growth (%)
        // Current Month Data (up to now)
        var currentMonthRevenue = deliveredOrders.Where(o => o.CreatedAt >= startOfMonth).Sum(o => o.TotalAmount);
        var currentMonthOrders = allOrders.Count(o => o.CreatedAt >= startOfMonth);
        var currentMonthCustomers = allUsers.Count(u => u.CreatedAt >= startOfMonth);

        // Equivalent Last Month Data (up to equivalent day last month)
        var lastMonthRevenue = deliveredOrders.Where(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt <= equivalentDayLastMonth).Sum(o => o.TotalAmount);
        var lastMonthOrders = allOrders.Count(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt <= equivalentDayLastMonth);
        var lastMonthCustomers = allUsers.Count(u => u.CreatedAt >= startOfLastMonth && u.CreatedAt <= equivalentDayLastMonth);

        var growth = new GrowthStatsDto
        {
            RevenuePercentage = CalculateGrowthPercentage(currentMonthRevenue, lastMonthRevenue),
            OrdersPercentage = CalculateGrowthPercentage(currentMonthOrders, lastMonthOrders),
            CustomersPercentage = CalculateGrowthPercentage(currentMonthCustomers, lastMonthCustomers)
        };

        // 5. Trends (Last 30 Days)
        var last30Days = Enumerable.Range(0, 30)
            .Select(i => today.AddDays(-i))
            .OrderBy(d => d)
            .Select(date => new TrendDto
            {
                Date = date,
                Amount = deliveredOrders.Where(o => o.CreatedAt.Date == date).Sum(o => o.TotalAmount)
            })
            .ToList();

        return new DashboardStatisticsDto
        {
            Revenue = revenue,
            Orders = orders,
            Customers = customers,
            Products = productStats,
            Growth = growth,
            Trends = last30Days
        };
    }

    private double CalculateGrowthPercentage(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100.0 : 0.0;
        return (double)((current - previous) / previous * 100);
    }

    private double CalculateGrowthPercentage(int current, int previous)
    {
        if (previous == 0) return current > 0 ? 100.0 : 0.0;
        return (double)(current - previous) / previous * 100;
    }
}
