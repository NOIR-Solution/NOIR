namespace NOIR.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface for coordinating persistence operations.
/// Implements both IDisposable and IAsyncDisposable for proper async cleanup.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
