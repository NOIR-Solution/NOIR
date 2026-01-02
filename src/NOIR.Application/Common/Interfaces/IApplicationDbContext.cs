namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Interface for the application database context.
/// Defines the contract for data access in the application layer.
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
