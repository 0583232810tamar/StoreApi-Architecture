using System.Linq.Expressions;

namespace ProductCatalogService.Interfaces;

public interface IGenericRepository<TDocument> where TDocument : IMongoEntity
{
    Task<IReadOnlyCollection<TDocument>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TDocument>> FindAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TDocument> CreateAsync(TDocument document, CancellationToken cancellationToken = default);
    Task<TDocument?> UpdateAsync(TDocument document, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
