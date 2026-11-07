using System.Linq.Expressions;
using EarnPlatform.Data;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly IDbContextFactory<ApplicationDbContext> Factory;

    public GenericRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        Factory = factory;
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        await context.Set<T>().AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        context.Set<T>().Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}