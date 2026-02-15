# StoreApi - .NET 8 Web API Project

## Architecture Overview

**Three-Layer Architecture**: Controllers → Services → Repositories → Database
- **Controllers**: HTTP request handling, validation, response formatting
- **Services**: Business logic, DTO mapping, cross-repository orchestration
- **Repositories**: Data access layer with EF Core (also supports ADO.NET/Dapper)
- **DTOs**: Separate objects for Create/Update/Response operations

**Key Design Principles**:
- Always use interfaces (`IProductService`, `IProductRepository`) for testability
- Services return DTOs; repositories return domain models
- Controllers delegate all logic to services and remain thin
- All database operations use async/await

## Critical Architectural Patterns

### Repository Flexibility
The project supports **three data access patterns** (switch in `Program.cs` line 75-79):
```csharp
// Choose ONE implementation for IProductRepository:
builder.Services.AddScoped<IProductRepository, ProductRepository>();      // EF Core (default)
// builder.Services.AddScoped<IProductRepository, ProductAdoRepository>();  // Raw ADO.NET
// builder.Services.AddScoped<IProductRepository, ProductDapperRepository>(); // Dapper
```
When creating new repositories, follow the EF Core pattern unless specified otherwise.

### DTO Mapping Convention
Services must map between domain models and DTOs:
- **CreateDto** → Domain Model (incoming POST)
- **UpdateDto** → Domain Model (incoming PUT/PATCH with nullable properties)
- Domain Model → **ResponseDto** (outgoing responses)

Example from `ProductService.cs`:
```csharp
private static ProductResponseDto MapToResponseDto(Product product)
{
    return new ProductResponseDto
    {
        Id = product.Id,
        Name = product.Name,
        CategoryName = product.Category?.Name ?? string.Empty,
        // ...
    };
}
```

### Authentication & Authorization
- **JWT Bearer tokens** configured in `Program.cs` (lines 94-122)
- Uses `ITokenService` to generate tokens
- Login flow: `AuthController` → `UserService.AuthenticateAsync()` → returns `LoginResponseDto` with token
- Passwords are hashed using BCrypt (see `UserService.cs`)
- User roles: `Customer`, `Manager`, `Admin` (enum in `User.cs`)

## Exception Handling & Logging

**Strict Rules** (see `.github/instructions/logging try catch.instructions.md`):
- **Never use exceptions for validation or normal flow** - return proper result types instead
- **Lower layers throw; controllers handle** - Services throw `ArgumentException` for business validation; controllers catch and return `BadRequest`
- **Log once per failure at boundary layer** using structured logging:
  ```csharp
  _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
  ```
- **Never log sensitive data** (passwords, tokens, PII)
- Use Serilog (configured in `appsettings.json`) with file and console sinks

## Middleware Pipeline Order

Order matters! From `Program.cs` lines 152-158:
1. `UseRequestLogging()` - logs all HTTP requests
2. `UseRateLimiting()` - throttles requests (configurable in appsettings)
3. `UseAuthentication()` - validates JWT tokens
4. `UseAuthorization()` - enforces role-based access
5. `MapControllers()` - routes to API endpoints

## Pagination Pattern

Use `PaginationParams` and `PagedResult<T>` for any list endpoints:
```csharp
[HttpGet("paged")]
public async Task<ActionResult<PagedResult<ProductResponseDto>>> GetAllPaged(
    [FromQuery] PaginationParams paginationParams)
{
    var result = await _productService.GetAllProductsPagedAsync(paginationParams);
    return Ok(result);
}
```
Repository returns `(IEnumerable<T> Items, int TotalCount)` tuple; service converts to `PagedResult`.

## Database Context

- **ApplicationDbContext**: Main EF Core context for CRUD operations
- **DapperContext**: Registered as Singleton for Dapper/ADO.NET (see `Data/DapperContext.cs`)
- Connection string: `appsettings.json` → `"DefaultConnection"`
- Migrations: Use `dotnet ef migrations add <name>` and `dotnet ef database update`

### Entity Relationships
- `Product` → `Category` (many-to-one, DeleteBehavior.Restrict)
- `User` → `Orders` (one-to-many)
- `Order` → `OrderItems` (one-to-many)
- Always use `.Include()` to eagerly load navigation properties in repositories

## Development Workflows

### Running the Application
```bash
dotnet run --project StoreApi.csproj
# Or use the default launch profile (see Properties/launchSettings.json)
```
Swagger UI available at `https://localhost:<port>/swagger` (dev mode only)

### Running Tests
```bash
dotnet test StoreApi.Tests/StoreApi.Tests.csproj
```
Test categories:
- `[Trait("Category", "Unit")]` - unit tests with mocks (Moq framework)
- `[Trait("Category", "Integration")]` - tests with real database
- `[Trait("Category", "E2E")]` - end-to-end API tests

### Database Setup
1. ction string in `appsettings.json` or `appsettings.Development.json`
2. Apply migrations: `dotnet ef database update`
3. Seed data: Execute `Scripts/SeedData.sql` manually or create seeding logic

## Service Registration Pattern

All dependencies registered in `Program.cs` with **Scoped** lifetime:
```csharp
// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Services
builder.Services.AddScoped<IProductService, ProductService>();
```
Exception: `DapperContext` is Singleton because it's stateless.

## Common Patterns

### Creating New Entities
1. Create model in `Models/` with navigation properties
2. Create DTOs in `DTOs/` (Create, Update, Response)
3. Create repository interface in `Interfaces/` and implementation in `Repositories/`
4. Create service interface and implementation in `Services/` with DTO mapping
5. Create controller in `Controllers/` delegating to service
6. Register in `Program.cs` DI container
7. Update `ApplicationDbContext.cs` with DbSet and OnModelCreating configuration
8. Create migration

### Controller Return Types
- `200 OK`: `Ok(data)`
- `201 Created`: `CreatedAtAction(nameof(GetById), new { id }, data)`
- `204 No Content`: `NoContent()` (for DELETE)
- `400 Bad Request`: `BadRequest(new { message })`
- `404 Not Found`: `NotFound(new { message })`
- `401 Unauthorized`: `Unauthorized(new { message })`

### Validation
- Use data annotations in DTOs: `[Required]`, `[MaxLength]`, `[Range]`
- `[ApiController]` attribute enables automatic model state validation
- Business validation in services throws `ArgumentException`

## Configuration Files

- `appsettings.json`: Production settings (Serilog, connection strings, JWT config)
- `appsettings.Development.json`: Development overrides
- `Properties/launchSettings.json`: Launch profiles and environment variables
- Rate limiting config: `appsettings.json` → `"RateLimiting:RequestLimit"`

## Important Notes

- **Never expose domain models directly in API responses** - always use DTOs
- **Timestamps**: `CreatedAt` and `UpdatedAt` set automatically in repositories
- **Circular reference handling**: Configured in `Program.cs` with `ReferenceHandler.IgnoreCycles`
- **Partial class Program**: `public partial class Program { }` at end of `Program.cs` enables testing
- **Security**: JWT secret stored in `appsettings.json` (should be moved to user secrets/env vars for production)

## Existing Instruction Files

Follow these project-specific rules:
- `.github/instructions/dotnetInstruction.instructions.md` - comprehensive architecture guide
- `.github/instructions/logging try catch.instructions.md` - exception and logging rules
- `vscode-userdata:/.../design_system_rules.md.instructions.md` - UI/frontend conventions (if applicable)
