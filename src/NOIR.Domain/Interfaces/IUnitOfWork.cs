namespace NOIR.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface for coordinating persistence operations.
/// Implements both IDisposable and IAsyncDisposable for proper async cleanup.
/// Supports explicit transaction management for bulk operations.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    #region Transaction Management

    /// <summary>
    /// Begins a new database transaction.
    /// Use for coordinating multiple bulk operations that must succeed or fail together.
    /// </summary>
    /// <example>
    /// await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
    /// try
    /// {
    ///     await repo.BulkInsertAsync(items, config, ct);
    ///     await repo.BulkUpdateAsync(updates, config, ct);
    ///     await unitOfWork.CommitTransactionAsync(ct);
    /// }
    /// catch
    /// {
    ///     await unitOfWork.RollbackTransactionAsync(ct);
    ///     throw;
    /// }
    /// </example>
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// All changes made within the transaction become permanent.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// All changes made within the transaction are discarded.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    bool HasActiveTransaction { get; }

    #endregion
}

/// <summary>
/// Represents a database transaction.
/// Disposable wrapper for EF Core's IDbContextTransaction.
/// </summary>
public interface IDbTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    Guid TransactionId { get; }

    /// <summary>
    /// Commits all changes made within this transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards all changes made within this transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
