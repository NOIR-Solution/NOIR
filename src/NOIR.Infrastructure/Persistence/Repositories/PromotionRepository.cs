namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Promotion aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class PromotionRepository : Repository<Domain.Entities.Promotion.Promotion, Guid>, IRepository<Domain.Entities.Promotion.Promotion, Guid>, IScopedService
{
    public PromotionRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
