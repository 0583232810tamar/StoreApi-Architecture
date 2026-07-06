using MongoDB.Driver;
using ProductCatalogService.Configuration;
using ProductCatalogService.Data;
using ProductCatalogService.Entities;
using ProductCatalogService.Interfaces;

namespace ProductCatalogService.Repositories;

public class CategoryRepository : GenericRepository<CategoryDocument>, ICategoryRepository
{
    private readonly MongoDbContext _context;

    public CategoryRepository(MongoDbContext context, MongoDbSettings settings) : base(context, settings)
    {
        _context = context;
    }

    public async Task<CategoryDocument?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.Find(x => x.Name == name).FirstOrDefaultAsync(cancellationToken);
    }
}
