namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Wishlist aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class WishlistRepository : Repository<Domain.Entities.Wishlist.Wishlist, Guid>, IRepository<Domain.Entities.Wishlist.Wishlist, Guid>, IScopedService
{
    public WishlistRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
