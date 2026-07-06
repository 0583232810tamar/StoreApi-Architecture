using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.DTOs;
using ProductCatalogService.Interfaces;

namespace ProductCatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductCatalogService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductCatalogService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductCatalogResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductCatalogResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductCatalogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductCatalogResponseDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound(new { message = $"Product with ID '{id}' not found." });
        }

        return Ok(product);
    }

    [HttpGet("category/{categoryId}")]
    [ProducesResponseType(typeof(IEnumerable<ProductCatalogResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductCatalogResponseDto>>> GetByCategory(string categoryId, CancellationToken cancellationToken)
    {
        var products = await _productService.GetByCategoryAsync(categoryId, cancellationToken);
        return Ok(products);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductCatalogResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ProductCatalogResponseDto>>> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Search term cannot be empty." });
        }

        var products = await _productService.SearchByNameAsync(name, cancellationToken);
        return Ok(products);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductCatalogResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductCatalogResponseDto>> Create([FromBody] ProductCatalogCreateDto createDto, CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(createDto, cancellationToken);
        _logger.LogInformation("Product created with ID: {ProductId}", created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductCatalogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductCatalogResponseDto>> Update(string id, [FromBody] ProductCatalogUpdateDto updateDto, CancellationToken cancellationToken)
    {
        var updated = await _productService.UpdateAsync(id, updateDto, cancellationToken);
        if (updated is null)
        {
            return NotFound(new { message = $"Product with ID '{id}' not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _productService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Product with ID '{id}' not found." });
        }

        return NoContent();
    }
}
