namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for ShippingProvider entities.
/// </summary>
public sealed class ShippingProviderRepository : Repository<ShippingProvider, Guid>, IRepository<ShippingProvider, Guid>, IScopedService
{
    public ShippingProviderRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
