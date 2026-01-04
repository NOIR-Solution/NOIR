namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PasswordResetOtp aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class PasswordResetOtpRepository : Repository<PasswordResetOtp, Guid>, IScopedService
{
    public PasswordResetOtpRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
