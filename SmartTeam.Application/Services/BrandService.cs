using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileUploadService _fileUploadService;

    public BrandService(IUnitOfWork unitOfWork, IMapper mapper, IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileUploadService = fileUploadService;
    }

    public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync(CancellationToken cancellationToken = default)
        => await GetAllBrandsAsync(includeInactive: false, cancellationToken);

    public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var brands = await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken);

        var filtered = includeInactive
            ? brands.OrderBy(b => b.SortOrder)
            : brands.Where(b => b.IsActive).OrderBy(b => b.SortOrder);

        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var activeProducts = products.Where(p => p.IsActive).ToList();

        var brandDtos = _mapper.Map<IEnumerable<BrandDto>>(filtered);

        foreach (var brandDto in brandDtos)
            brandDto.ProductCount = activeProducts.Count(p => p.BrandId == brandDto.Id);

        return brandDtos;
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await GetBrandByIdAsync(id, includeInactive: false, cancellationToken);

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);

        if (brand == null)
            return null;

        if (!includeInactive && !brand.IsActive)
            return null;

        var brandDto = _mapper.Map<BrandDto>(brand);

        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == id && p.IsActive);

        return brandDto;
    }

    public async Task<BrandDto?> GetBrandBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive, cancellationToken);
        
        if (brand == null)
            return null;

        var brandDto = _mapper.Map<BrandDto>(brand);
        
        // Calculate product count
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == brand.Id && p.IsActive);
        
        return brandDto;
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, CancellationToken cancellationToken = default)
    {
        var brandName = createBrandDto.Name.Trim();

        // Ensure all required fields are explicitly set
        if (string.IsNullOrEmpty(brandName))
            throw new ArgumentException("Brand name cannot be empty.");

        // Check for duplicate brand name (case-insensitive)
        var existingBrand = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Name.ToLower() == brandName.ToLower(), cancellationToken);
        if (existingBrand != null)
            throw new InvalidOperationException($"A brand with the name '{brandName}' already exists.");

        var brandSlug = GenerateSlug(brandName);
        if (string.IsNullOrEmpty(brandSlug))
            throw new ArgumentException("Brand slug cannot be generated from the given name.");

        var brandToSave = new Brand
        {
            Id = Guid.NewGuid(),
            Name = brandName,
            Slug = brandSlug,
            LogoUrl = createBrandDto.LogoUrl,
            IsActive = true,
            SortOrder = createBrandDto.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        await _unitOfWork.Repository<Brand>().AddAsync(brandToSave, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var brandDto = _mapper.Map<BrandDto>(brandToSave);
        brandDto.ProductCount = 0;

        return brandDto;
    }

    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace(",", "-")
            .Replace("(", "-")
            .Replace(")", "-")
            .Replace("[", "-")
            .Replace("]", "-")
            .Replace("{", "-")
            .Replace("}", "-")
            .Replace("&", "and")
            .Replace("@", "at")
            .Replace("#", "hash")
            .Replace("$", "dollar")
            .Replace("%", "percent")
            .Replace("^", "caret")
            .Replace("*", "star")
            .Replace("+", "plus")
            .Replace("=", "equals")
            .Replace("?", "question")
            .Replace("!", "exclamation")
            .Replace("|", "pipe")
            .Replace("\\", "-")
            .Replace("/", "-")
            .Replace(":", "-")
            .Replace(";", "-")
            .Replace("\"", "-")
            .Replace("'", "-")
            .Replace("<", "-")
            .Replace(">", "-")
            .Replace("~", "-")
            .Replace("`", "-")
            .Replace(" ", "-")
            .Trim('-')
            .Trim();
    }

    public async Task<BrandDto> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);
        if (brand == null)
            throw new ArgumentException($"Brand with ID '{id}' was not found.");

        var newName = updateBrandDto.Name.Trim();

        // Check for duplicate name (case-insensitive, exclude current brand)
        var duplicate = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Name.ToLower() == newName.ToLower() && b.Id != id, cancellationToken);
        if (duplicate != null)
            throw new InvalidOperationException($"A brand with the name '{newName}' already exists.");

        _mapper.Map(updateBrandDto, brand);
        brand.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Brand>().Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var brandDto = _mapper.Map<BrandDto>(brand);

        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == id && p.IsActive);

        return brandDto;
    }

    public async Task<bool> DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);
        if (brand == null)
            return false;

        _unitOfWork.Repository<Brand>().Remove(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }



    public async Task<BrandDto> CreateBrandWithImageAsync(CreateBrandWithImageDto createBrandDto, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var brandName = createBrandDto.Name.Trim();

        if (string.IsNullOrEmpty(brandName))
            throw new ArgumentException("Brand name cannot be empty.");

        // Check for duplicate brand name (case-insensitive)
        var existingBrand = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Name.ToLower() == brandName.ToLower(), cancellationToken);
        if (existingBrand != null)
            throw new InvalidOperationException($"A brand with the name '{brandName}' already exists.");

        // Validate image file
        if (!_fileUploadService.IsValidImageFile(imageFile))
            throw new ArgumentException("Invalid image file format. Allowed formats: JPG, JPEG, PNG, GIF, WebP.");

        var brandSlug = GenerateSlug(brandName);
        if (string.IsNullOrEmpty(brandSlug))
            throw new ArgumentException("Brand slug cannot be generated from the given name.");

        // Upload the image
        var logoUrl = await _fileUploadService.UploadFileAsync(imageFile, "brands");

        var brandId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO Brand (Id, Name, Slug, LogoUrl, IsActive, SortOrder, CreatedAt, UpdatedAt) 
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})";

        await _unitOfWork.ExecuteSqlRawAsync(sql,
            brandId,
            brandName,
            brandSlug,
            logoUrl,
            1, // IsActive = true
            createBrandDto.SortOrder,
            createdAt,
            null!);

        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(brandId, cancellationToken);
        if (brand == null)
            throw new InvalidOperationException("Brand was saved but could not be retrieved. Please try again.");

        var brandDto = _mapper.Map<BrandDto>(brand);
        brandDto.ProductCount = 0;

        return brandDto;
    }

    public async Task<BrandDto> UpdateBrandWithImageAsync(Guid id, UpdateBrandWithImageDto updateBrandDto, IFormFile? imageFile, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);
        if (brand == null)
            throw new ArgumentException($"Brand with ID '{id}' was not found.");

        var newName = updateBrandDto.Name.Trim();

        // Check for duplicate name (case-insensitive, exclude current brand)
        var duplicate = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Name.ToLower() == newName.ToLower() && b.Id != id, cancellationToken);
        if (duplicate != null)
            throw new InvalidOperationException($"A brand with the name '{newName}' already exists.");

        brand.Name = newName;
        brand.Slug = GenerateSlug(newName);
        brand.IsActive = updateBrandDto.IsActive;
        brand.SortOrder = updateBrandDto.SortOrder;
        brand.UpdatedAt = DateTime.UtcNow;

        if (imageFile != null && imageFile.Length > 0)
        {
            if (!_fileUploadService.IsValidImageFile(imageFile))
                throw new ArgumentException("Invalid image file format. Allowed formats: JPG, JPEG, PNG, GIF, WebP.");

            var logoUrl = await _fileUploadService.UploadFileAsync(imageFile, "brands");
            brand.LogoUrl = logoUrl;
        }

        _unitOfWork.Repository<Brand>().Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var brandDto = _mapper.Map<BrandDto>(brand);

        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == id && p.IsActive);

        return brandDto;
    }

    public async Task<PagedResultDto<BrandDto>> GetBrandsPaginatedAsync(BrandPaginationRequestDto request, CancellationToken cancellationToken = default)
    {
        var brands = await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken);
        var filteredBrands = brands.Where(b => b.IsActive).AsQueryable();
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filteredBrands = filteredBrands.Where(b => b.Name.ToLower().Contains(searchTerm));
        }
        
        // Apply active status filter
        if (request.IsActive.HasValue)
        {
            filteredBrands = filteredBrands.Where(b => b.IsActive == request.IsActive.Value);
        }
        
        // Apply sorting
        filteredBrands = ApplyBrandSorting(filteredBrands, request.SortBy, request.SortOrder);
        
        // Get total count before pagination
        var totalCount = filteredBrands.Count();
        
        // Apply pagination
        var pagedBrands = filteredBrands
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Map to DTOs
        var brandDtos = _mapper.Map<IEnumerable<BrandDto>>(pagedBrands);
        
        // Get all products to calculate counts
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var activeProducts = products.Where(p => p.IsActive);
        
        // Calculate product count for each brand
        foreach (var brandDto in brandDtos)
        {
            brandDto.ProductCount = activeProducts.Count(p => p.BrandId == brandDto.Id);
        }
        
        return CreatePagedResult(brandDtos, request.Page, request.PageSize, totalCount);
    }

    private IQueryable<Brand> ApplyBrandSorting(IQueryable<Brand> brands, string? sortBy, string sortOrder)
    {
        return sortBy?.ToLower() switch
        {
            "name" => sortOrder.ToLower() == "desc" 
                ? brands.OrderByDescending(b => b.Name)
                : brands.OrderBy(b => b.Name),
            "sortorder" => sortOrder.ToLower() == "desc"
                ? brands.OrderByDescending(b => b.SortOrder)
                : brands.OrderBy(b => b.SortOrder),
            "createdat" => sortOrder.ToLower() == "desc"
                ? brands.OrderByDescending(b => b.CreatedAt)
                : brands.OrderBy(b => b.CreatedAt),
            _ => brands.OrderBy(b => b.SortOrder)
        };
    }

    private PagedResultDto<BrandDto> CreatePagedResult(IEnumerable<BrandDto> items, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        return new PagedResultDto<BrandDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<BrandSearchResultDto> SearchBrandsPagedAsync(BrandSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        var allBrands = await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken);

        // --- Active status filter ---
        var filtered = request.IncludeInactive
            ? allBrands.AsQueryable()
            : allBrands.Where(b => b.IsActive).AsQueryable();

        // --- Name search (partial, case-insensitive) ---
        var query = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.ToLower();
            filtered = filtered.Where(b => b.Name.ToLower().Contains(term));
        }

        // --- Calculate product counts first (needed for hasProducts filter & sorting) ---
        var allProducts = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var activeProductsList = allProducts.Where(p => p.IsActive).ToList();

        var brandList = filtered.ToList();
        var brandDtos = _mapper.Map<List<BrandDto>>(brandList);

        foreach (var dto in brandDtos)
            dto.ProductCount = activeProductsList.Count(p => p.BrandId == dto.Id);

        // --- HasProducts filter ---
        if (request.HasProducts.HasValue)
        {
            brandDtos = request.HasProducts.Value
                ? brandDtos.Where(b => b.ProductCount > 0).ToList()
                : brandDtos.Where(b => b.ProductCount == 0).ToList();
        }

        // --- Sorting ---
        var isDesc = request.SortOrder?.ToLower() == "desc";
        brandDtos = request.SortBy?.ToLower() switch
        {
            "sortorder"    => isDesc ? brandDtos.OrderByDescending(b => b.SortOrder).ToList()    : brandDtos.OrderBy(b => b.SortOrder).ToList(),
            "createdat"    => isDesc ? brandDtos.OrderByDescending(b => b.CreatedAt).ToList()    : brandDtos.OrderBy(b => b.CreatedAt).ToList(),
            "productcount" => isDesc ? brandDtos.OrderByDescending(b => b.ProductCount).ToList() : brandDtos.OrderBy(b => b.ProductCount).ToList(),
            _              => isDesc ? brandDtos.OrderByDescending(b => b.Name).ToList()         : brandDtos.OrderBy(b => b.Name).ToList()
        };

        // --- Pagination ---
        var totalCount = brandDtos.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var paged = brandDtos
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BrandSearchResultDto
        {
            Items          = paged,
            TotalCount     = totalCount,
            Page           = request.Page,
            PageSize       = request.PageSize,
            TotalPages     = totalPages,
            HasNextPage    = request.Page < totalPages,
            HasPreviousPage = request.Page > 1,
            Query          = string.IsNullOrWhiteSpace(query) ? null : query
        };
    }
}
