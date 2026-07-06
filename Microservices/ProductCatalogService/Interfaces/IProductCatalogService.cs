using ProductCatalogService.DTOs;

namespace ProductCatalogService.Interfaces;

public interface IProductCatalogService
{
    Task<IReadOnlyCollection<ProductCatalogResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductCatalogResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductCatalogResponseDto>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductCatalogResponseDto>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<ProductCatalogResponseDto> CreateAsync(ProductCatalogCreateDto createDto, CancellationToken cancellationToken = default);
    Task<ProductCatalogResponseDto?> UpdateAsync(string id, ProductCatalogUpdateDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
