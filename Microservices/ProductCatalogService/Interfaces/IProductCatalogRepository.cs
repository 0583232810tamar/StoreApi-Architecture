using ProductCatalogService.Entities;

namespace ProductCatalogService.Interfaces;

public interface IProductCatalogRepository
{
    Task<IReadOnlyCollection<ProductDocument>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductDocument>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductDocument>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<ProductDocument> CreateAsync(ProductDocument document, CancellationToken cancellationToken = default);
    Task<ProductDocument?> UpdateAsync(ProductDocument document, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
