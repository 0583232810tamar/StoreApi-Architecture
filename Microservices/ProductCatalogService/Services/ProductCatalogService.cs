using ProductCatalogService.Common;
using ProductCatalogService.DTOs;
using ProductCatalogService.Entities;
using ProductCatalogService.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace ProductCatalogService.Services;

public class ProductCatalogService : IProductCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string ProductByIdPrefix = "products:id:";

    private readonly IProductCatalogRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _productByIdTtl;
    private readonly ILogger<ProductCatalogService> _logger;

    public ProductCatalogService(
        IProductCatalogRepository productRepository,
        ICategoryRepository categoryRepository,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<ProductCatalogService> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _cache = cache;
        _logger = logger;
        _productByIdTtl = TimeSpan.FromSeconds(configuration.GetValue<int>("Cache:ProductByIdTtlSeconds", 120));
    }

    public async Task<IReadOnlyCollection<ProductCatalogResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(MapToResponseDto).ToList();
    }

    public async Task<ProductCatalogResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(id))
        {
            return null;
        }

        var cacheKey = GetProductByIdCacheKey(id);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            _logger.LogDebug("Cache HIT for product id {ProductId}", id);
            return JsonSerializer.Deserialize<ProductCatalogResponseDto>(cached, JsonOptions);
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var response = MapToResponseDto(product);
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _productByIdTtl
            },
            cancellationToken);

        _logger.LogDebug("Cache SET for product id {ProductId}", id);
        return response;
    }

    public async Task<IReadOnlyCollection<ProductCatalogResponseDto>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(categoryId))
        {
            return Array.Empty<ProductCatalogResponseDto>();
        }

        var products = await _productRepository.GetByCategoryAsync(categoryId, cancellationToken);
        return products.Select(MapToResponseDto).ToList();
    }

    public async Task<IReadOnlyCollection<ProductCatalogResponseDto>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<ProductCatalogResponseDto>();
        }

        var products = await _productRepository.SearchByNameAsync(searchTerm, cancellationToken);
        return products.Select(MapToResponseDto).ToList();
    }

    public async Task<ProductCatalogResponseDto> CreateAsync(ProductCatalogCreateDto createDto, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(createDto.CategoryId))
        {
            throw new ArgumentException("CategoryId is not a valid ObjectId.");
        }

        var category = await _categoryRepository.GetByIdAsync(createDto.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new KeyNotFoundException($"Category with ID '{createDto.CategoryId}' does not exist.");
        }

        var now = DateTime.UtcNow;
        var document = new ProductDocument
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            CategoryId = createDto.CategoryId,
            CategoryName = category.Name,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        var created = await _productRepository.CreateAsync(document, cancellationToken);
        return MapToResponseDto(created);
    }

    public async Task<ProductCatalogResponseDto?> UpdateAsync(string id, ProductCatalogUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(id))
        {
            return null;
        }

        var existing = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (updateDto.Name is not null)
        {
            existing.Name = updateDto.Name;
        }

        if (updateDto.Description is not null)
        {
            existing.Description = updateDto.Description;
        }

        if (updateDto.Price.HasValue)
        {
            existing.Price = updateDto.Price.Value;
        }

        if (updateDto.CategoryId is not null)
        {
            if (!MongoId.IsValid(updateDto.CategoryId))
            {
                throw new ArgumentException("CategoryId is not a valid ObjectId.");
            }

            var category = await _categoryRepository.GetByIdAsync(updateDto.CategoryId, cancellationToken);
            if (category is null)
            {
                throw new KeyNotFoundException($"Category with ID '{updateDto.CategoryId}' does not exist.");
            }

            existing.CategoryId = category.Id;
            existing.CategoryName = category.Name;
        }

        if (updateDto.IsActive.HasValue)
        {
            existing.IsActive = updateDto.IsActive.Value;
        }

        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _productRepository.UpdateAsync(existing, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await _cache.RemoveAsync(GetProductByIdCacheKey(id), cancellationToken);
        _logger.LogDebug("Cache INVALIDATE for product id {ProductId}", id);

        return MapToResponseDto(updated);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoId.IsValid(id))
        {
            return false;
        }

        return await _productRepository.DeleteAsync(id, cancellationToken);
    }

    private static ProductCatalogResponseDto MapToResponseDto(ProductDocument document)
    {
        return new ProductCatalogResponseDto
        {
            Id = document.Id,
            LegacySqlId = document.LegacySqlId,
            Name = document.Name,
            Description = document.Description,
            Price = document.Price,
            CategoryId = document.CategoryId,
            CategoryName = document.CategoryName,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            IsActive = document.IsActive
        };
    }

    private static string GetProductByIdCacheKey(string id) => $"{ProductByIdPrefix}{id}";
}
