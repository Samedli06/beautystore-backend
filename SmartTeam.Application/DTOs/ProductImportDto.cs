namespace SmartTeam.Application.DTOs;

public class ProductImportDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? SKU { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductImportResultDto
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ProductImportErrorDto> RowErrors { get; set; } = new();
    public List<ProductImportDetailDto> CreatedProducts { get; set; } = new();
}

public class ProductImportErrorDto
{
    public int RowNumber { get; set; }
    public string? ProductName { get; set; }
    public string Error { get; set; } = string.Empty;
}

public class ProductImportDetailDto
{
    public string Name { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}
