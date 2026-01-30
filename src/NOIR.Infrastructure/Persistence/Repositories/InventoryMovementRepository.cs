namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for InventoryMovement entities.
/// </summary>
public sealed class InventoryMovementRepository : Repository<InventoryMovement, Guid>, IRepository<InventoryMovement, Guid>, IScopedService
{
    public InventoryMovementRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
