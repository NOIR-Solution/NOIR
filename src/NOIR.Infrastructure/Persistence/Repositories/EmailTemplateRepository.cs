namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EmailTemplate aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class EmailTemplateRepository : Repository<EmailTemplate, Guid>, IScopedService
{
    public EmailTemplateRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
