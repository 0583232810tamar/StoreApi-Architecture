using System.ComponentModel.DataAnnotations;

namespace ProductCatalogService.DTOs;

public class ProductCatalogCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    public string CategoryId { get; set; } = string.Empty;
}

public class ProductCatalogUpdateDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }

    public string? CategoryId { get; set; }

    public bool? IsActive { get; set; }
}

public class ProductCatalogResponseDto
{
    public string Id { get; set; } = string.Empty;
    public int? LegacySqlId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public int? AvailableQuantity { get; set; }
}
