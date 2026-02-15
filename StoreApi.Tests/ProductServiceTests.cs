using Xunit;
using Moq;
using StoreApi.Services;
using StoreApi.Interfaces;
using StoreApi.Models;
using StoreApi.DTOs;
using Microsoft.Extensions.Logging;

namespace StoreApi.Tests;

/// <summary>
/// Unit tests for ProductService using Moq for dependency isolation.
/// Follows Arrange-Act-Assert pattern and XUnit best practices.
/// </summary>
[Trait("Category", "Unit")]
public class ProductServiceTests : IDisposable
{
    private readonly Mock<IProductRepository> _mockProductRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        // Setup - shared across all tests in this class
        _mockProductRepo = new Mock<IProductRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        _service = new ProductService(_mockProductRepo.Object, _mockCategoryRepo.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Teardown - called after each test
    }

    #region GetAllProductsAsync Tests

    [Fact]
    public async Task GetAllProductsAsync_WhenProductsExist_ReturnsListOfProductDtos()
    {
        // Arrange
        var products = CreateTestProducts(2);
        _mockProductRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetAllProductsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockProductRepo.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsAsync_WhenNoProducts_ReturnsEmptyList()
    {
        // Arrange
        _mockProductRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _service.GetAllProductsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetProductByIdAsync Tests

    [Fact]
    public async Task GetProductByIdAsync_WithValidId_ReturnsProductDto()
    {
        // Arrange
        var product = CreateTestProduct(1, "Test Product", 15.99m);
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetProductByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal(15.99m, result.Price);
        Assert.Equal("Test Category", result.CategoryName);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public async Task GetProductByIdAsync_WithVariousValidIds_CallsRepositoryCorrectly(int id)
    {
        // Arrange
        var product = CreateTestProduct(id, $"Product {id}", 10.00m);
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetProductByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        _mockProductRepo.Verify(repo => repo.GetByIdAsync(id), Times.Once);
    }

    #endregion

    #region SearchProductsByNameAsync Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SearchProductsByNameAsync_WithEmptyOrWhitespace_ReturnsEmptyList(string? searchTerm)
    {
        // Act
        var result = await _service.SearchProductsByNameAsync(searchTerm!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockProductRepo.Verify(repo => repo.SearchByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SearchProductsByNameAsync_WithValidTerm_ReturnsMatchingProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProduct(1, "Laptop Pro", 999.99m),
            CreateTestProduct(2, "Laptop Basic", 499.99m)
        };
        _mockProductRepo.Setup(repo => repo.SearchByNameAsync("Laptop"))
            .ReturnsAsync(products);

        // Act
        var result = await _service.SearchProductsByNameAsync("Laptop");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Contains("Laptop", p.Name));
    }

    #endregion

    #region CreateProductAsync Tests

    [Fact]
    public async Task CreateProductAsync_WithValidData_ReturnsCreatedProductDto()
    {
        // Arrange
        var createDto = new ProductCreateDto
        {
            Name = "New Product",
            Description = "Description",
            Price = 25.99m,
            Stock = 7,
            CategoryId = 1
        };

        _mockCategoryRepo.Setup(repo => repo.ExistsAsync(1))
            .ReturnsAsync(true);

        _mockProductRepo.Setup(repo => repo.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => new Product
            {
                Id = 1,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                CategoryId = p.CategoryId,
                Category = new Category { Id = 1, Name = "Test Category" }
            });

        // Act
        var result = await _service.CreateProductAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Product", result.Name);
        Assert.Equal(25.99m, result.Price);
        _mockProductRepo.Verify(repo => repo.CreateAsync(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WithInvalidCategory_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new ProductCreateDto
        {
            Name = "New Product",
            Price = 25.99m,
            Stock = 7,
            CategoryId = 999 // Invalid category
        };

        _mockCategoryRepo.Setup(repo => repo.ExistsAsync(999))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateProductAsync(createDto));
        Assert.Contains("999", exception.Message);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(100.00)]
    [InlineData(9999.99)]
    public async Task CreateProductAsync_WithVariousPrices_CreatesProductCorrectly(decimal price)
    {
        // Arrange
        var createDto = new ProductCreateDto
        {
            Name = "Price Test Product",
            Price = price,
            Stock = 1,
            CategoryId = 1
        };

        _mockCategoryRepo.Setup(repo => repo.ExistsAsync(1)).ReturnsAsync(true);
        _mockProductRepo.Setup(repo => repo.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => new Product
            {
                Id = 1,
                Name = p.Name,
                Price = p.Price,
                CategoryId = p.CategoryId,
                Category = new Category { Id = 1, Name = "Test Category" }
            });

        // Act
        var result = await _service.CreateProductAsync(createDto);

        // Assert
        Assert.Equal(price, result.Price);
    }

    #endregion

    #region UpdateProductAsync Tests

    [Fact]
    public async Task UpdateProductAsync_WithValidId_ReturnsUpdatedProductDto()
    {
        // Arrange
        var existingProduct = CreateTestProduct(1, "Original Name", 10.00m);
        var updateDto = new ProductUpdateDto { Name = "Updated Name" };

        _mockProductRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingProduct);
        _mockProductRepo.Setup(repo => repo.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Act
        var result = await _service.UpdateProductAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task UpdateProductAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.UpdateProductAsync(999, new ProductUpdateDto());

        // Assert
        Assert.Null(result);
        _mockProductRepo.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WithInvalidCategoryId_ThrowsArgumentException()
    {
        // Arrange
        var existingProduct = CreateTestProduct(1, "Test", 10.00m);
        var updateDto = new ProductUpdateDto { CategoryId = 999 };

        _mockProductRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingProduct);
        _mockCategoryRepo.Setup(repo => repo.ExistsAsync(999))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateProductAsync(1, updateDto));
    }

    [Fact]
    public async Task UpdateProductAsync_WithPartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var existingProduct = CreateTestProduct(1, "Original Name", 10.00m);
        existingProduct.Stock = 50;
        var updateDto = new ProductUpdateDto { Price = 20.00m }; // Only update price

        _mockProductRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingProduct);
        _mockProductRepo.Setup(repo => repo.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Act
        var result = await _service.UpdateProductAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Original Name", result.Name); // Name unchanged
        Assert.Equal(20.00m, result.Price); // Price updated
        Assert.Equal(50, result.Stock); // Stock unchanged
    }

    #endregion

    #region DeleteProductAsync Tests

    [Fact]
    public async Task DeleteProductAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        _mockProductRepo.Setup(repo => repo.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteProductAsync(1);

        // Assert
        Assert.True(result);
        _mockProductRepo.Verify(repo => repo.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _mockProductRepo.Setup(repo => repo.DeleteAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteProductAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAllProductsPagedAsync Tests

    [Theory]
    [InlineData(1, 10, 25, 3)]   // Page 1, 10 items, 25 total = 3 pages
    [InlineData(1, 5, 12, 3)]    // Page 1, 5 items, 12 total = 3 pages
    [InlineData(2, 10, 15, 2)]   // Page 2, 10 items, 15 total = 2 pages
    public async Task GetAllProductsPagedAsync_CalculatesTotalPagesCorrectly(
        int pageNumber, int pageSize, int totalCount, int expectedTotalPages)
    {
        // Arrange
        var products = CreateTestProducts(Math.Min(pageSize, totalCount));
        var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };

        _mockProductRepo.Setup(repo => repo.GetAllPagedAsync(pageNumber, pageSize))
            .ReturnsAsync((products, totalCount));

        // Act
        var result = await _service.GetAllProductsPagedAsync(paginationParams);

        // Assert
        Assert.Equal(expectedTotalPages, result.TotalPages);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(pageNumber, result.PageNumber);
        Assert.Equal(pageSize, result.PageSize);
    }

    #endregion

    #region Helper Methods

    private static Product CreateTestProduct(int id, string name, decimal price)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = "Test Description",
            Price = price,
            Stock = 10,
            CategoryId = 1,
            Category = new Category { Id = 1, Name = "Test Category" },
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<Product> CreateTestProducts(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestProduct(i, $"Product {i}", 10.00m + i))
            .ToList();
    }

    #endregion
}
