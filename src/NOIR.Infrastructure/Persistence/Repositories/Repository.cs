namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation using Entity Framework Core.
/// Implements specification pattern with soft delete as default behavior.
///
/// All entities must inherit from <see cref="AggregateRoot{TId}"/>, which includes:
/// - <see cref="IAuditableEntity"/> for audit fields (CreatedAt, ModifiedAt, DeletedAt, etc.)
/// - Domain event support via <see cref="IDomainEvent"/>
/// - Soft delete with automatic global query filtering
///
/// The <see cref="AuditableEntityInterceptor"/> automatically manages:
/// - Setting CreatedAt/CreatedBy on insert
/// - Setting ModifiedAt/ModifiedBy on update
/// - Converting Delete to soft delete (IsDeleted = true, DeletedAt, DeletedBy)
///
/// Based on best practices from:
/// - Microsoft eShopOnWeb reference architecture
/// - Ardalis.Specification patterns
/// - Milan Jovanović's implementation guides
/// </summary>
/// <typeparam name="TEntity">The aggregate root type (inherits AggregateRoot which includes IAuditableEntity).</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Abstract repository - tested via integration tests using DbContext")]
public abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    protected readonly ApplicationDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ICurrentUser CurrentUser;
    protected readonly IDateTime DateTime;

    protected Repository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
        CurrentUser = currentUser;
        DateTime = dateTime;
    }

    #region Basic Queries

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(cancellationToken);
    }

    #endregion

    #region Predicate-based Queries

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return predicate is null
            ? await DbSet.AnyAsync(cancellationToken)
            : await DbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(predicate, cancellationToken);
    }

    #endregion

    #region Specification-based Queries

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);
        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);
        return await query.AnyAsync(cancellationToken);
    }

    #endregion

    #region Projection Queries

    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    #endregion

    #region Create Operations

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    #endregion

    #region Update Operations

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }

    #endregion

    #region Soft Delete Operations (Default)

    /// <summary>
    /// Soft deletes the entity. The AuditableEntityInterceptor sets IsDeleted = true.
    /// The entity remains in the database but is excluded from normal queries.
    /// </summary>
    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    /// <summary>
    /// Soft deletes multiple entities.
    /// </summary>
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Gets an entity by ID, including soft-deleted entities.
    /// Bypasses the global query filter.
    /// </summary>
    public virtual async Task<TEntity?> GetByIdIncludingDeletedAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Gets all entities, including soft-deleted ones.
    /// Bypasses the global query filter.
    /// </summary>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets only soft-deleted entities.
    /// </summary>
    public virtual async Task<IReadOnlyList<TEntity>> GetDeletedOnlyAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Restores a soft-deleted entity by setting IsDeleted = false.
    /// </summary>
    public virtual Task RestoreAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = DbContext.Entry(entity);

        entry.Property(nameof(IAuditableEntity.IsDeleted)).CurrentValue = false;
        entry.Property(nameof(IAuditableEntity.DeletedAt)).CurrentValue = null;
        entry.Property(nameof(IAuditableEntity.DeletedBy)).CurrentValue = null;
        entry.Property(nameof(IAuditableEntity.ModifiedAt)).CurrentValue = DateTime.UtcNow;
        entry.Property(nameof(IAuditableEntity.ModifiedBy)).CurrentValue = CurrentUser.UserId;

        return Task.CompletedTask;
    }

    #endregion

    #region Hard Delete (GDPR Compliance)

    /// <summary>
    /// Permanently deletes an entity from the database.
    /// Uses ExecuteDeleteAsync to bypass the soft delete interceptor.
    /// WARNING: This cannot be undone. Use for GDPR "right to be forgotten" compliance only.
    /// </summary>
    public virtual async Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet
            .IgnoreQueryFilters()
            .Where(e => e.Id.Equals(entity.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Bulk deletes entities matching the specification (hard delete).
    /// Uses ExecuteDeleteAsync for optimal performance - bypasses change tracking.
    /// NOTE: This does NOT trigger soft delete - entities are permanently removed.
    /// WARNING: This cannot be undone. Use for cleanup operations or GDPR compliance.
    /// </summary>
    public virtual async Task<int> BulkDeleteAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);

        // Apply IgnoreQueryFilters if specified (to also delete soft-deleted entities)
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Bulk soft-deletes entities matching the specification.
    /// Sets IsDeleted = true, DeletedAt, and DeletedBy on matching entities.
    /// Uses ExecuteUpdateAsync for optimal performance - bypasses change tracking.
    /// NOTE: This does NOT trigger interceptors or domain events.
    /// </summary>
    public virtual async Task<int> BulkSoftDeleteAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var userId = CurrentUser.UserId;

        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);

        return await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, utcNow)
                .SetProperty(e => e.DeletedBy, userId)
                .SetProperty(e => e.ModifiedAt, utcNow)
                .SetProperty(e => e.ModifiedBy, userId),
            cancellationToken);
    }

    #endregion
}
