using ProductCatalogService.Common;
using ProductCatalogService.DTOs;
using ProductCatalogService.Entities;
using ProductCatalogService.Interfaces;

namespace ProductCatalogService.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyCollection<CategoryResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        return categories.Select(MapToResponseDto).ToList();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(id))
        {
            return null;
        }

        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return category is null ? null : MapToResponseDto(category);
    }

    public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto createDto, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepository.GetByNameAsync(createDto.Name, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Category name '{createDto.Name}' already exists.");
        }

        var now = DateTime.UtcNow;
        var document = new CategoryDocument
        {
            Name = createDto.Name,
            Description = createDto.Description,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _categoryRepository.CreateAsync(document, cancellationToken);
        return MapToResponseDto(created);
    }

    public async Task<CategoryResponseDto?> UpdateAsync(string id, CategoryUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(id))
        {
            return null;
        }

        var existing = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Name) && !string.Equals(existing.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _categoryRepository.GetByNameAsync(updateDto.Name, cancellationToken);
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"Category name '{updateDto.Name}' already exists.");
            }

            existing.Name = updateDto.Name;
        }

        if (updateDto.Description is not null)
        {
            existing.Description = updateDto.Description;
        }

        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _categoryRepository.UpdateAsync(existing, cancellationToken);
        return updated is null ? null : MapToResponseDto(updated);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(id))
        {
            return false;
        }

        return await _categoryRepository.DeleteAsync(id, cancellationToken);
    }

    private static CategoryResponseDto MapToResponseDto(CategoryDocument document)
    {
        return new CategoryResponseDto
        {
            Id = document.Id,
            LegacySqlId = document.LegacySqlId,
            Name = document.Name,
            Description = document.Description,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
}
