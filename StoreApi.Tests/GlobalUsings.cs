global using Xunit;

// UserServiceTests.cs - Unit tests for UserService
// This file contains tests following XUnit best practices with data-driven testing

using Moq;
using StoreApi.Services;
using StoreApi.Interfaces;
using StoreApi.Models;
using StoreApi.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace StoreApi.Tests;

/// <summary>
/// Unit tests for UserService with comprehensive coverage.
/// Uses constructor for shared setup and IDisposable for cleanup.
/// </summary>
[Trait("Category", "Unit")]
public class UserServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<UserService>>();

        // Setup configuration for token expiry
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(x => x.Value).Returns("60");
        _mockConfiguration.Setup(x => x.GetValue<int>(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(60);

        _service = new UserService(
            _mockUserRepo.Object,
            _mockTokenService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_WhenUsersExist_ReturnsUserDtoList()
    {
        // Arrange
        var users = CreateTestUsers(3);
        _mockUserRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetAllUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockUserRepo.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenNoUsers_ReturnsEmptyList()
    {
        // Arrange
        _mockUserRepo.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _service.GetAllUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUserDto()
    {
        // Arrange
        var user = CreateTestUser(1, "john@example.com");
        _mockUserRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _mockUserRepo.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task GetUserByIdAsync_VariousIds_CallsRepositoryCorrectly(int id)
    {
        // Arrange
        var user = CreateTestUser(id, $"user{id}@example.com");
        _mockUserRepo.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        _mockUserRepo.Verify(repo => repo.GetByIdAsync(id), Times.Once);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_ReturnsCreatedUserDto()
    {
        // Arrange
        var createDto = new UserCreateDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "password123",
            Phone = "555-1234",
            Address = "123 Main St",
            Role = UserRole.Customer
        };

        _mockUserRepo.Setup(repo => repo.EmailExistsAsync("jane@example.com"))
            .ReturnsAsync(false);

        _mockUserRepo.Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => new User
            {
                Id = 1,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PasswordHash = u.PasswordHash,
                Phone = u.Phone,
                Address = u.Address,
                Role = u.Role,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _service.CreateUserAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Equal("Customer", result.Role);
        _mockUserRepo.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new UserCreateDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "password123"
        };

        _mockUserRepo.Setup(repo => repo.EmailExistsAsync("existing@example.com"))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateUserAsync(createDto));
        Assert.Contains("existing@example.com", exception.Message);
        Assert.Contains("already registered", exception.Message);
    }

    [Theory]
    [InlineData(UserRole.Customer, "Customer")]
    [InlineData(UserRole.Manager, "Manager")]
    [InlineData(UserRole.Admin, "Admin")]
    public async Task CreateUserAsync_WithVariousRoles_AssignsRoleCorrectly(UserRole role, string expectedRoleString)
    {
        // Arrange
        var createDto = new UserCreateDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = $"{role.ToString().ToLower()}@example.com",
            Password = "password123",
            Role = role
        };

        _mockUserRepo.Setup(repo => repo.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockUserRepo.Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => new User
            {
                Id = 1,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _service.CreateUserAsync(createDto);

        // Assert
        Assert.Equal(expectedRoleString, result.Role);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithValidId_ReturnsUpdatedUserDto()
    {
        // Arrange
        var existingUser = CreateTestUser(1, "old@example.com");
        var updateDto = new UserUpdateDto { FirstName = "Updated" };

        _mockUserRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingUser);
        _mockUserRepo.Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _service.UpdateUserAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.FirstName);
    }

    [Fact]
    public async Task UpdateUserAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _mockUserRepo.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateUserAsync(999, new UserUpdateDto());

        // Assert
        Assert.Null(result);
        _mockUserRepo.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WithDuplicateEmail_ThrowsArgumentException()
    {
        // Arrange
        var existingUser = CreateTestUser(1, "old@example.com");
        var updateDto = new UserUpdateDto { Email = "taken@example.com" };

        _mockUserRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingUser);
        _mockUserRepo.Setup(repo => repo.EmailExistsAsync("taken@example.com"))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateUserAsync(1, updateDto));
    }

    [Fact]
    public async Task UpdateUserAsync_WithSameEmail_DoesNotThrowException()
    {
        // Arrange
        var existingUser = CreateTestUser(1, "same@example.com");
        var updateDto = new UserUpdateDto { Email = "same@example.com", FirstName = "Updated" };

        _mockUserRepo.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingUser);
        _mockUserRepo.Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _service.UpdateUserAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        // EmailExistsAsync should NOT be called when email hasn't changed
        _mockUserRepo.Verify(repo => repo.EmailExistsAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        _mockUserRepo.Setup(repo => repo.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteUserAsync(1);

        // Assert
        Assert.True(result);
        _mockUserRepo.Verify(repo => repo.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _mockUserRepo.Setup(repo => repo.DeleteAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteUserAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var user = CreateTestUser(1, "john@example.com");
        user.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("validpassword"));

        _mockUserRepo.Setup(repo => repo.GetByEmailAsync("john@example.com"))
            .ReturnsAsync(user);
        _mockTokenService.Setup(ts => ts.GenerateToken(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        // Act
        var result = await _service.AuthenticateAsync("john@example.com", "validpassword");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-jwt-token", result.Token);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn); // 60 minutes * 60 seconds
        Assert.NotNull(result.User);
        Assert.Equal("john@example.com", result.User.Email);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidEmail_ReturnsNull()
    {
        // Arrange
        _mockUserRepo.Setup(repo => repo.GetByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.AuthenticateAsync("nonexistent@example.com", "password");

        // Assert
        Assert.Null(result);
        _mockTokenService.Verify(
            ts => ts.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = CreateTestUser(1, "john@example.com");
        user.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("correctpassword"));

        _mockUserRepo.Setup(repo => repo.GetByEmailAsync("john@example.com"))
            .ReturnsAsync(user);

        // Act
        var result = await _service.AuthenticateAsync("john@example.com", "wrongpassword");

        // Assert
        Assert.Null(result);
        _mockTokenService.Verify(
            ts => ts.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(int id, string email)
    {
        return new User
        {
            Id = id,
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            PasswordHash = "hashedpassword",
            Phone = "555-1234",
            Address = "123 Test St",
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<User> CreateTestUsers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestUser(i, $"user{i}@example.com"))
            .ToList();
    }

    #endregion
}

/// <summary>
/// Demonstrates ClassData attribute for complex test scenarios.
/// </summary>
public class UserRoleTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { UserRole.Customer, "Customer", false };
        yield return new object[] { UserRole.Manager, "Manager", true };
        yield return new object[] { UserRole.Admin, "Admin", true };
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

[Trait("Category", "Unit")]
public class UserRoleAuthorizationTests
{
    [Theory]
    [ClassData(typeof(UserRoleTestData))]
    public void UserRole_HasExpectedPrivileges(UserRole role, string expectedName, bool canManageProducts)
    {
        // Assert
        Assert.Equal(expectedName, role.ToString());
        Assert.Equal(canManageProducts, role >= UserRole.Manager);
    }
}