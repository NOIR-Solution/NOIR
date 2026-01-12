namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EmailChangeOtp aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class EmailChangeOtpRepository : Repository<EmailChangeOtp, Guid>, IScopedService
{
    public EmailChangeOtpRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
