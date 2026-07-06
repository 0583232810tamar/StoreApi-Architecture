using System.Linq.Expressions;
using MongoDB.Driver;
using ProductCatalogService.Configuration;
using ProductCatalogService.Data;
using ProductCatalogService.Entities;
using ProductCatalogService.Interfaces;

namespace ProductCatalogService.Repositories;

public class GenericRepository<TDocument> : IGenericRepository<TDocument> where TDocument : IMongoEntity
{
    private readonly IMongoCollection<TDocument> _collection;

    public GenericRepository(MongoDbContext context, MongoDbSettings settings)
    {
        _collection = context.GetCollection<TDocument>(ResolveCollectionName(settings));
    }

    public async Task<IReadOnlyCollection<TDocument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(FilterDefinition<TDocument>.Empty).ToListAsync(cancellationToken);
    }

    public async Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TDocument>> FindAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(predicate).ToListAsync(cancellationToken);
    }

    public async Task<TDocument> CreateAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document;
    }

    public async Task<TDocument?> UpdateAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        var result = await _collection.ReplaceOneAsync(x => x.Id == document.Id, document, cancellationToken: cancellationToken);
        if (result.MatchedCount == 0)
        {
            return default;
        }

        return document;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }

    private static string ResolveCollectionName(MongoDbSettings settings)
    {
        if (typeof(TDocument) == typeof(ProductDocument))
        {
            return settings.ProductsCollectionName;
        }

        if (typeof(TDocument) == typeof(CategoryDocument))
        {
            return settings.CategoriesCollectionName;
        }

        throw new InvalidOperationException($"No collection mapping defined for document type {typeof(TDocument).Name}.");
    }
}
