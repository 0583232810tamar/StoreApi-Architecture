using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StoreApi.Data;
using StoreApi.DTOs;
using StoreApi.Interfaces;
using StoreApi.Models;
using StoreApi.Repositories;
using StoreApi.Services;

namespace StoreApi.Tests;


[Trait("Category", "Integration")]
public class ProductServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductService _productService;

    public ProductServiceIntegrationTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
            .Options;

        _context = new ApplicationDbContext(options);

        // Setup repositories with real DbContext
        _productRepository = new ProductRepository(_context);
        _categoryRepository = new CategoryRepository(_context);

        // Setup service with real repositories
        var mockLogger = new Mock<ILogger<ProductService>>();
        _productService = new ProductService(_productRepository, _categoryRepository, mockLogger.Object);

        // Seed test data
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            Description = "Electronic devices",
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);

        var products = new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Laptop",
                Description = "Gaming laptop",
                Price = 999.99m,
                Stock = 10,
                CategoryId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Mouse",
                Description = "Wireless mouse",
                Price = 29.99m,
                Stock = 50,
                CategoryId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Name == "Laptop");
        Assert.Contains(result, p => p.Name == "Mouse");
    }

    [Fact]
    public async Task GetProductByIdAsync_WithValidId_ShouldReturnProduct()
    {
        // Act
        var result = await _productService.GetProductByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal(999.99m, result.Price);
        Assert.Equal("Electronics", result.CategoryName);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _productService.GetProductByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProductAsync_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var createDto = new ProductCreateDto
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 89.99m,
            Stock = 25,
            CategoryId = 1
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Keyboard", result.Name);
        Assert.Equal(89.99m, result.Price);
        Assert.Equal("Electronics", result.CategoryName);

        // Verify it was saved to database
        var savedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Name == "Keyboard");
        Assert.NotNull(savedProduct);
        Assert.Equal(25, savedProduct.Stock);
    }

    [Fact]
    public async Task CreateProductAsync_WithInvalidCategory_ShouldThrowException()
    {
        // Arrange
        var createDto = new ProductCreateDto
        {
            Name = "Invalid Product",
            Description = "This should fail",
            Price = 50.00m,
            Stock = 5,
            CategoryId = 999 // Non-existent category
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _productService.CreateProductAsync(createDto)
        );

        Assert.Contains("Category with ID 999 does not exist", exception.Message);
    }

    [Fact]
    public async Task UpdateProductAsync_WithValidData_ShouldUpdateProduct()
    {
        // Arrange
        var updateDto = new ProductUpdateDto
        {
            Name = "Updated Laptop",
            Price = 1099.99m,
            Stock = 15
        };

        // Act
        var result = await _productService.UpdateProductAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Laptop", result.Name);
        Assert.Equal(1099.99m, result.Price);

        // Verify database was updated
        var updatedProduct = await _context.Products.FindAsync(1);
        Assert.Equal("Updated Laptop", updatedProduct!.Name);
        Assert.Equal(15, updatedProduct.Stock);
    }

    [Fact]
    public async Task UpdateProductAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var updateDto = new ProductUpdateDto
        {
            Name = "Should Not Update"
        };

        // Act
        var result = await _productService.UpdateProductAsync(999, updateDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProductAsync_WithInvalidCategory_ShouldThrowException()
    {
        // Arrange
        var updateDto = new ProductUpdateDto
        {
            CategoryId = 999 // Non-existent category
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _productService.UpdateProductAsync(1, updateDto)
        );
    }

    [Fact]
    public async Task DeleteProductAsync_WithValidId_ShouldDeleteProduct()
    {
        // Act
        var result = await _productService.DeleteProductAsync(1);

        // Assert
        Assert.True(result);

        // Verify it's deleted from database
        var deletedProduct = await _context.Products.FindAsync(1);
        Assert.Null(deletedProduct);

        // Verify other products still exist
        var remainingProducts = await _context.Products.ToListAsync();
        Assert.Single(remainingProducts);
    }

    [Fact]
    public async Task DeleteProductAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _productService.DeleteProductAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateMultipleProducts_ShouldAllBeSaved()
    {
        // Arrange
        var product1 = new ProductCreateDto
        {
            Name = "Monitor",
            Description = "4K Monitor",
            Price = 399.99m,
            Stock = 15,
            CategoryId = 1
        };

        var product2 = new ProductCreateDto
        {
            Name = "Webcam",
            Description = "HD Webcam",
            Price = 79.99m,
            Stock = 30,
            CategoryId = 1
        };

        // Act
        await _productService.CreateProductAsync(product1);
        await _productService.CreateProductAsync(product2);

        // Assert
        var allProducts = await _productService.GetAllProductsAsync();
        Assert.Equal(4, allProducts.Count()); // 2 seeded + 2 new
        Assert.Contains(allProducts, p => p.Name == "Monitor");
        Assert.Contains(allProducts, p => p.Name == "Webcam");
    }

    [Fact]
    public async Task UpdateProduct_PartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange - Only update price
        var updateDto = new ProductUpdateDto
        {
            Price = 899.99m
            // Name, Stock, etc. are null - should not be updated
        };

        // Get original product
        var originalProduct = await _productService.GetProductByIdAsync(1);
        var originalName = originalProduct!.Name;
        var originalStock = originalProduct.Stock;

        // Act
        var result = await _productService.UpdateProductAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(899.99m, result.Price); // Updated
        Assert.Equal(originalName, result.Name); // Not changed
        Assert.Equal(originalStock, result.Stock); // Not changed
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
