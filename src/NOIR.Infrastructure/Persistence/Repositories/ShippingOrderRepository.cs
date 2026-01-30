namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for ShippingOrder entities.
/// </summary>
public sealed class ShippingOrderRepository : Repository<ShippingOrder, Guid>, IRepository<ShippingOrder, Guid>, IScopedService
{
    public ShippingOrderRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
