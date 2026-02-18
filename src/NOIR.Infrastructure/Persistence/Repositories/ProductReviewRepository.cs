namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ProductReview aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class ProductReviewRepository : Repository<ProductReview, Guid>, IRepository<ProductReview, Guid>, IScopedService
{
    public ProductReviewRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
