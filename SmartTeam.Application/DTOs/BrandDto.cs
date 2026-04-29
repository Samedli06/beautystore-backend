using System.ComponentModel.DataAnnotations;

namespace SmartTeam.Application.DTOs;

public class BrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProductCount { get; set; }
}

public class CreateBrandDto
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateBrandDto
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}


public class CreateBrandWithImageDto
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateBrandWithImageDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Request parameters for brand search
/// </summary>
public class BrandSearchRequestDto
{
    /// <summary>Search term — matches brand name (partial, case-insensitive). Leave empty to return all.</summary>
    public string? Q { get; set; }

    /// <summary>Page number (1-based)</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    public int Page { get; set; } = 1;

    /// <summary>Results per page (1–100)</summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    /// <summary>Sort field: name | sortorder | createdat | productcount. Default: name.</summary>
    public string SortBy { get; set; } = "name";

    /// <summary>Sort direction: asc | desc</summary>
    public string SortOrder { get; set; } = "asc";

    /// <summary>When true, include inactive brands (admin use only)</summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>When true, only return brands that have at least one active product</summary>
    public bool? HasProducts { get; set; }
}

/// <summary>
/// Paginated brand search result
/// </summary>
public class BrandSearchResultDto
{
    public IEnumerable<BrandDto> Items { get; set; } = new List<BrandDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int Count => Items.Count();

    /// <summary>The search term that produced this result</summary>
    public string? Query { get; set; }
}