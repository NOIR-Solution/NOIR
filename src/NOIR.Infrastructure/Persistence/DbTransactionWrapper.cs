namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// Wrapper for EF Core's IDbContextTransaction that implements our domain IDbTransaction interface.
/// Provides a clean abstraction for transaction management with thread-safe disposal.
/// </summary>
internal sealed class DbTransactionWrapper : Domain.Interfaces.IDbTransaction
{
    private readonly IDbContextTransaction _transaction;
    private int _disposed; // 0 = not disposed, 1 = disposed (for thread-safe Interlocked operations)

    public DbTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <inheritdoc />
    public Guid TransactionId => _transaction.TransactionId;

    private bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        await _transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        await _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        // Thread-safe: only the first caller disposes
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _transaction.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        // Thread-safe: only the first caller disposes
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _transaction.DisposeAsync();
    }
}
