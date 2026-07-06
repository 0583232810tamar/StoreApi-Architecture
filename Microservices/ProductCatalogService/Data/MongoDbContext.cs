using MongoDB.Driver;
using ProductCatalogService.Configuration;
using ProductCatalogService.Entities;

namespace ProductCatalogService.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    public MongoDbContext(IMongoDatabase database, MongoDbSettings settings)
    {
        _database = database;
        _settings = settings;
    }

    public IMongoCollection<ProductDocument> Products => _database.GetCollection<ProductDocument>(_settings.ProductsCollectionName);

    public IMongoCollection<CategoryDocument> Categories => _database.GetCollection<CategoryDocument>(_settings.CategoriesCollectionName);

    public IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
    {
        return _database.GetCollection<TDocument>(collectionName);
    }
}
