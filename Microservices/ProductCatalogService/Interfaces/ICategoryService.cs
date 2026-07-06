using ProductCatalogService.DTOs;

namespace ProductCatalogService.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyCollection<CategoryResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto> CreateAsync(CategoryCreateDto createDto, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto?> UpdateAsync(string id, CategoryUpdateDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
