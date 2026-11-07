using System.Linq.Expressions;

namespace EarnPlatform.Repositories.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Adds the entity and saves immediately (short-lived context).</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Updates the entity and saves immediately (short-lived context).</summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Removes the entity and saves immediately (short-lived context).</summary>
    Task RemoveAsync(T entity, CancellationToken cancellationToken = default);
}