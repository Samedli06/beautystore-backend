using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    /// <summary>Get all active brands (Public).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BrandDto>>> GetBrands(CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _brandService.GetAllBrandsAsync(includeInactive: false, cancellationToken);
            return Ok(brands);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to retrieve brands. Please try again later or contact support." });
        }
    }

    /// <summary>Get all brands including inactive (Admin only).</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BrandDto>>> GetBrandsAdmin(CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _brandService.GetAllBrandsAsync(includeInactive: true, cancellationToken);
            return Ok(brands);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to retrieve brands. Please try again later or contact support." });
        }
    }

    /// <summary>Get brand by ID (Public). Cannot retrieve inactive brands.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> GetBrandById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandService.GetBrandByIdAsync(id, includeInactive: false, cancellationToken);
            if (brand == null)
                return NotFound(new { error = "Brand not found.", message = $"No brand with ID '{id}' could be found." });
            return Ok(brand);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to retrieve brand. Please try again later or contact support." });
        }
    }

    /// <summary>Get brand by ID including inactive (Admin only).</summary>
    [HttpGet("admin/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> GetBrandByIdAdmin(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandService.GetBrandByIdAsync(id, includeInactive: true, cancellationToken);
            if (brand == null)
                return NotFound(new { error = "Brand not found.", message = $"No brand with ID '{id}' could be found." });
            return Ok(brand);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to retrieve brand. Please try again later or contact support." });
        }
    }

    /// <summary>
    /// Get brand by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> GetBrandBySlug(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandService.GetBrandBySlugAsync(slug, cancellationToken);
            if (brand == null)
            {
                return NotFound(new { error = "Brand not found.", message = "The requested brand could not be found." });
            }
            return Ok(brand);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve brand.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Create a new brand (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> CreateBrand([FromBody] CreateBrandDto createBrandDto, CancellationToken cancellationToken)
    {
        try
        {
            if (createBrandDto == null)
                return BadRequest(new { error = "Invalid request.", message = "Brand data cannot be null." });

            if (string.IsNullOrWhiteSpace(createBrandDto.Name))
                return BadRequest(new { error = "Validation failed.", message = "Brand name is required and cannot be empty." });

            var brand = await _brandService.CreateBrandAsync(createBrandDto, cancellationToken);
            return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, brand);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Validation failed.", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "Duplicate brand name.", message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to create brand. Please try again later or contact support." });
        }
    }

    /// <summary>
    /// Create a new brand with image upload (Admin only)
    /// </summary>
    [HttpPost("with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> CreateBrandWithImage(
        string name,
        int sortOrder,
        IFormFile imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { error = "Validation failed.", message = "Brand name is required and cannot be empty." });

            if (imageFile == null || imageFile.Length == 0)
                return BadRequest(new { error = "Validation failed.", message = "Brand logo image is required." });

            var createBrandDto = new CreateBrandWithImageDto { Name = name, SortOrder = sortOrder };

            var brand = await _brandService.CreateBrandWithImageAsync(createBrandDto, imageFile, cancellationToken);
            return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, brand);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Validation failed.", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "Duplicate brand name.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to create brand. Please try again later or contact support.", details = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Update an existing brand (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> UpdateBrand(Guid id, [FromBody] UpdateBrandDto updateBrandDto, CancellationToken cancellationToken)
    {
        try
        {
            if (updateBrandDto == null)
                return BadRequest(new { error = "Invalid request.", message = "Brand data cannot be null." });

            if (string.IsNullOrWhiteSpace(updateBrandDto.Name))
                return BadRequest(new { error = "Validation failed.", message = "Brand name is required and cannot be empty." });

            var brand = await _brandService.UpdateBrandAsync(id, updateBrandDto, cancellationToken);
            return Ok(brand);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = "Brand not found.", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "Duplicate brand name.", message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to update brand. Please try again later or contact support." });
        }
    }

    /// <summary>
    /// Update an existing brand with image (Admin only)
    /// </summary>
    /// <remarks>
    /// Accepts multipart/form-data with 'brandData' as JSON string and optional 'imageFile'.
    /// Use this endpoint when you need to update both brand data and image simultaneously.
    /// </remarks>
    [HttpPut("{id:guid}/with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<ActionResult<BrandDto>> UpdateBrandWithImage(
        Guid id,
        [FromForm] string brandData,
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brandData))
                return BadRequest(new { error = "Invalid request.", message = "'brandData' field is required and must contain valid JSON." });

            UpdateBrandWithImageDto updateBrandDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                updateBrandDto = System.Text.Json.JsonSerializer.Deserialize<UpdateBrandWithImageDto>(brandData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest(new { error = "Invalid JSON format.", message = $"Could not parse 'brandData': {ex.Message}" });
            }

            if (updateBrandDto == null)
                return BadRequest(new { error = "Invalid request.", message = "Failed to parse brand data. Ensure the JSON is well-formed." });

            if (string.IsNullOrWhiteSpace(updateBrandDto.Name))
                return BadRequest(new { error = "Validation failed.", message = "Brand name is required and cannot be empty." });

            var updatedBrand = await _brandService.UpdateBrandWithImageAsync(id, updateBrandDto, imageFile, cancellationToken);
            return Ok(updatedBrand);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = "Brand not found.", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "Duplicate brand name.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to update brand. Please try again later or contact support.", details = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Delete a brand (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _brandService.DeleteBrandAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(new { error = "Brand not found.", message = "The requested brand could not be found." });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to delete brand.", message = "Please try again later or contact support if the issue persists." });
        }
    }


    /// <summary>
    /// Get all brands with pagination
    /// </summary>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PagedResultDto<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<BrandDto>>> GetBrandsPaginated([FromQuery] BrandPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _brandService.GetBrandsPaginatedAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve brands.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Paginated brand search with sorting and filters.
    /// </summary>
    /// <remarks>
    /// For authenticated admins, this endpoint also allows including inactive brands.
    /// </remarks>
    [HttpGet("search")]
    [ProducesResponseType(typeof(BrandSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandSearchResultDto>> SearchBrands(
        [FromQuery] BrandSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Only admins can include inactive brands
            if (!User.IsInRole("Admin"))
            {
                request.IncludeInactive = false;
            }

            var result = await _brandService.SearchBrandsPagedAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid search parameters.", message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to search brands. Please try again later or contact support." });
        }
    }

    /// <summary>
    /// Paginated brand search with sorting and filters (public — active brands only).
    /// </summary>
    /// <remarks>
    /// Query parameters:
    /// - q: partial name match (optional — omitting returns all)
    /// - page / pageSize: pagination (default 1 / 20)
    /// - sortBy: name | sortorder | createdat | productcount (default: name)
    /// - sortOrder: asc | desc (default: asc)
    /// - hasProducts: true = brands with products only, false = empty brands only
    /// </remarks>
    [HttpGet("search/paged")]
    [ProducesResponseType(typeof(BrandSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandSearchResultDto>> SearchBrandsPaged(
        [FromQuery] BrandSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Public endpoint always returns active brands only
            request.IncludeInactive = false;

            var result = await _brandService.SearchBrandsPagedAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid search parameters.", message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to search brands. Please try again later or contact support." });
        }
    }

    /// <summary>
    /// Paginated admin brand search — can include inactive brands (Admin only).
    /// </summary>
    /// <remarks>
    /// Same parameters as /search/paged, plus:
    /// - includeInactive: true = include inactive brands (default false)
    /// </remarks>
    [HttpGet("search/admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BrandSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandSearchResultDto>> SearchBrandsAdmin(
        [FromQuery] BrandSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _brandService.SearchBrandsPagedAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid search parameters.", message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Unexpected error.", message = "Failed to search brands. Please try again later or contact support." });
        }
    }
}
