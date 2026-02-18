namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for InventoryReceipt entities.
/// </summary>
public sealed class InventoryReceiptRepository : Repository<InventoryReceipt, Guid>, IRepository<InventoryReceipt, Guid>, IScopedService
{
    public InventoryReceiptRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
