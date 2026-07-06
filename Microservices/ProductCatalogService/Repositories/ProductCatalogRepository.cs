using MongoDB.Driver;
using ProductCatalogService.Configuration;
using ProductCatalogService.Data;
using ProductCatalogService.Entities;
using ProductCatalogService.Interfaces;

namespace ProductCatalogService.Repositories;

public class ProductCatalogRepository : GenericRepository<ProductDocument>, IProductCatalogRepository
{
    private readonly MongoDbContext _context;

    public ProductCatalogRepository(MongoDbContext context, MongoDbSettings settings) : base(context, settings)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ProductDocument>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products.Find(x => x.CategoryId == categoryId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductDocument>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProductDocument>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
        return await _context.Products.Find(filter).ToListAsync(cancellationToken);
    }
}
