namespace SmartTeam.Application.DTOs;

public class DashboardStatisticsDto
{
    public RevenueStatsDto Revenue { get; set; } = new();
    public OrderStatsDto Orders { get; set; } = new();
    public CustomerStatsDto Customers { get; set; } = new();
    public ProductStatsDto Products { get; set; } = new();
    public GrowthStatsDto Growth { get; set; } = new();
    public List<TrendDto> Trends { get; set; } = new();
}

public class RevenueStatsDto
{
    public decimal Today { get; set; }
    public decimal ThisMonth { get; set; }
    public decimal Total { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class OrderStatsDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Delivered { get; set; }
}

public class CustomerStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
}

public class ProductStatsDto
{
    public int Total { get; set; }
    public int LowStock { get; set; }
    public List<TopProductDto> TopSellingProducts { get; set; } = new();
}

public class TopProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SoldCount { get; set; }
}

public class GrowthStatsDto
{
    public double RevenuePercentage { get; set; }
    public double OrdersPercentage { get; set; }
    public double CustomersPercentage { get; set; }
}

public class TrendDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}
