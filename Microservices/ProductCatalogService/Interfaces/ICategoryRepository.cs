using ProductCatalogService.Entities;

namespace ProductCatalogService.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<CategoryDocument>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<CategoryDocument?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<CategoryDocument> CreateAsync(CategoryDocument document, CancellationToken cancellationToken = default);
    Task<CategoryDocument?> UpdateAsync(CategoryDocument document, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
