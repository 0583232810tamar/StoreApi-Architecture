using Microsoft.Extensions.Options;
using ProductCatalogService.Configuration;

namespace ProductCatalogService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureMongoDbSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException("MongoDbSettings.ConnectionString is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
            {
                throw new InvalidOperationException("MongoDbSettings.DatabaseName is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.ProductsCollectionName))
            {
                throw new InvalidOperationException("MongoDbSettings.ProductsCollectionName is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.CategoriesCollectionName))
            {
                throw new InvalidOperationException("MongoDbSettings.CategoriesCollectionName is required.");
            }

            return settings;
        });

        return services;
    }
}
