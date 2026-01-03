namespace NOIR.Domain.Interfaces;

/// <summary>
/// Read-only repository interface for querying entities.
/// Use this when you only need to read data without modifications.
/// Does NOT expose IQueryable - use specifications for complex queries.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public interface IReadRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    #region Basic Queries

    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity with the specified ID exists.
    /// </summary>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of entities.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Predicate-based Queries

    /// <summary>
    /// Gets the first entity matching the predicate, or null.
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the single entity matching the predicate, or null.
    /// Throws if more than one entity matches.
    /// </summary>
    Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all entities matching the predicate.
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate.
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the predicate.
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Specification-based Queries

    /// <summary>
    /// Gets the first entity matching the specification, or null.
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities matching the specification.
    /// </summary>
    Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the specification.
    /// </summary>
    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specification.
    /// </summary>
    Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    #endregion

    #region Projection Queries

    /// <summary>
    /// Gets the first entity matching the specification with projection.
    /// </summary>
    Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities matching the specification with projection.
    /// </summary>
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Generic repository interface for aggregate roots with full CRUD and soft delete operations.
/// All aggregate roots are auditable with soft delete enabled by default:
/// - Remove() triggers soft delete via AuditableEntityInterceptor
/// - Global query filters exclude soft-deleted entities automatically
/// - Use IgnoreQueryFilters() in specifications to include deleted records
/// </summary>
/// <typeparam name="TEntity">The aggregate root type (inherits IAuditableEntity).</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    #region Create

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities.
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    #endregion

    #region Soft Delete (Default)

    /// <summary>
    /// Soft deletes the entity by setting IsDeleted = true.
    /// The entity remains in the database but is excluded from normal queries.
    /// </summary>
    void Remove(TEntity entity);

    /// <summary>
    /// Soft deletes multiple entities.
    /// </summary>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Gets an entity by ID, including soft-deleted entities.
    /// Bypasses the global query filter.
    /// </summary>
    Task<TEntity?> GetByIdIncludingDeletedAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities, including soft-deleted ones.
    /// Bypasses the global query filter.
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only soft-deleted entities.
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetDeletedOnlyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted entity by setting IsDeleted = false.
    /// </summary>
    Task RestoreAsync(TEntity entity, CancellationToken cancellationToken = default);

    #endregion

    #region Hard Delete (GDPR Compliance)

    /// <summary>
    /// Permanently deletes an entity from the database.
    /// Use for GDPR "right to be forgotten" compliance.
    /// WARNING: This cannot be undone.
    /// </summary>
    Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    #endregion

    #region Bulk Operations (Specification-based - Native EF Core)

    /// <summary>
    /// Bulk deletes entities matching the specification (hard delete).
    /// Uses ExecuteDeleteAsync for optimal performance - bypasses change tracking.
    /// NOTE: This does NOT trigger soft delete - entities are permanently removed.
    /// WARNING: This cannot be undone. Use for cleanup operations or GDPR compliance.
    /// </summary>
    /// <param name="specification">The specification to filter entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of entities deleted.</returns>
    Task<int> BulkDeleteAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk soft-deletes entities matching the specification.
    /// Sets IsDeleted = true, DeletedAt, and DeletedBy on matching entities.
    /// Uses ExecuteUpdateAsync for optimal performance.
    /// </summary>
    /// <param name="specification">The specification to filter entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of entities soft-deleted.</returns>
    Task<int> BulkSoftDeleteAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    #endregion

    #region Bulk Operations (Collection-based - High Performance)

    /// <summary>
    /// Bulk inserts entities using SqlBulkCopy for maximum performance.
    /// 10-15x faster than AddRange for large datasets (1000+ records).
    /// NOTE: Bypasses change tracking and interceptors (no audit logging).
    /// Use within a transaction for data consistency.
    /// </summary>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="config">Optional configuration for batch size, timeout, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BulkInsertAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates entities using MERGE for maximum performance.
    /// 4-5x faster than UpdateRange for large datasets.
    /// NOTE: Bypasses change tracking and interceptors (no audit logging).
    /// Entities must have their primary keys set.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="config">Optional configuration. Use UpdateByProperties for custom matching.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BulkUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts or updates entities (upsert) based on key matching.
    /// If entity exists (by PK or UpdateByProperties), it's updated; otherwise inserted.
    /// Ideal for sync scenarios where you don't know if records exist.
    /// NOTE: Bypasses change tracking and interceptors.
    /// </summary>
    /// <param name="entities">The entities to upsert.</param>
    /// <param name="config">Optional configuration. Use UpdateByProperties for business key matching.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BulkInsertOrUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk deletes the provided entities by their primary keys.
    /// Uses bulk delete for maximum performance.
    /// NOTE: This is HARD delete - entities are permanently removed.
    /// WARNING: This cannot be undone.
    /// </summary>
    /// <remarks>
    /// This differs from <see cref="BulkDeleteAsync(ISpecification{TEntity}, CancellationToken)"/>
    /// which deletes by specification. Use this method when you have a collection of entities to delete.
    /// </remarks>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="config">Optional configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BulkDeleteEntitiesAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes the database table with the provided collection.
    /// Inserts new entities, updates existing ones, and deletes those not in the collection.
    /// Useful for full sync scenarios (e.g., importing data from external systems).
    /// WARNING: Entities not in the collection will be DELETED (hard delete).
    /// </summary>
    /// <param name="entities">The complete collection to sync to.</param>
    /// <param name="config">Optional configuration. Use UpdateByProperties for business key matching.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BulkSyncAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk reads entities by their keys for efficient large-scale lookups.
    /// More efficient than multiple GetByIdAsync calls for large datasets.
    /// </summary>
    /// <param name="entities">Entities with keys set (other properties populated from DB).</param>
    /// <param name="config">Optional configuration. Use UpdateByProperties to match by business keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entities with all properties populated from the database.</returns>
    Task<IReadOnlyList<TEntity>> BulkReadAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default);

    #endregion
}
