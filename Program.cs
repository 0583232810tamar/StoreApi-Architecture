using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using StackExchange.Redis;
using StoreApi.Data;
using StoreApi.Interfaces;
using StoreApi.Repositories;
using StoreApi.Services;
using StoreApi.Middleware;
using Serilog;

// Configure Serilog
// Log.Logger = new LoggerConfiguration()
//     .ReadFrom.Configuration(new ConfigurationBuilder()
//         .AddJsonFile("appsettings.json")
//         .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
//         .Build())
//     .Enrich.FromLogContext()
//     .WriteTo.Console()
//     .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
//     .CreateLogger();

try
{
    Log.Information("Starting Store API application");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Add Serilog
    builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {token}"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Register DapperContext for ADO.NET/Dapper repositories
builder.Services.AddSingleton<DapperContext>();

// Register Redis
// IConnectionMultiplexer is the low-level connection used by:
//   - CacheService (pattern-based key invalidation via SCAN)
//   - RateLimitingMiddleware (atomic INCR counters — works across replicas)
// AbortOnConnectFail=false lets the app start even if Redis is temporarily unavailable.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
redisConfig.AbortOnConnectFail = false;
var redisMultiplexer = ConnectionMultiplexer.Connect(redisConfig);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

// AddStackExchangeRedisCache registers IDistributedCache backed by Redis.
// InstanceName prefixes every key ("StoreApi:products:all") to namespace
// this app's keys from other apps sharing the same Redis instance.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult((IConnectionMultiplexer)redisMultiplexer);
    options.InstanceName = builder.Configuration.GetValue<string>("Cache:InstanceName", "StoreApi:");
});

// CacheService is Singleton: IDistributedCache and IConnectionMultiplexer are both thread-safe singletons.
builder.Services.AddSingleton<ICacheService, CacheService>();

// Register Repositories (Scoped - one instance per request)
// Note: Choose ONE implementation for IProductRepository:
//   - ProductRepository: Uses Entity Framework Core (default)
//   - ProductAdoRepository: Uses raw ADO.NET
//   - ProductDapperRepository: Uses Dapper micro-ORM
// To switch, uncomment the desired implementation below:
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// builder.Services.AddScoped<IProductRepository, ProductAdoRepository>();  // ADO.NET
// builder.Services.AddScoped<IProductRepository, ProductDapperRepository>(); // Dapper
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register Services (Scoped - one instance per request)
// Concrete types are registered first so the cached decorators can resolve the inner service.
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Cached decorators wrap the concrete services and implement the same interface.
// Controllers depend on IProductService / ICategoryService and transparently get caching.
builder.Services.AddScoped<ICategoryService>(sp => new CachedCategoryService(
    sp.GetRequiredService<CategoryService>(),
    sp.GetRequiredService<ICacheService>(),
    sp.GetRequiredService<ILogger<CachedCategoryService>>(),
    sp.GetRequiredService<IConfiguration>()));

builder.Services.AddScoped<IProductService>(sp => new CachedProductService(
    sp.GetRequiredService<ProductService>(),
    sp.GetRequiredService<ICacheService>(),
    sp.GetRequiredService<ILogger<CachedProductService>>(),
    sp.GetRequiredService<IConfiguration>()));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Log.Debug("JWT token validated for user {UserId}", userId);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Configure JSON options to handle circular references
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// Auto-apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    Log.Information("Database migrations applied successfully");
}

// Configure the HTTP request pipeline


// 1. Request Logging
app.UseRequestLogging();

//2. Rate Limiting (optional - comment out if not needed)
app.UseRateLimiting();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("Store API is now running");
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }
