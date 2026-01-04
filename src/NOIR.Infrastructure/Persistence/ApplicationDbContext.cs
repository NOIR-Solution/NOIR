namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// The application database context with Identity, MultiTenant, and Soft Delete support.
/// Uses convention-based configuration for consistent entity setup.
/// Implements IMultiTenantDbContext for Finbuckle multi-tenant query filter support.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IUnitOfWork, IMultiTenantDbContext
{
    private readonly IMultiTenantContextAccessor<TenantInfo>? _tenantContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMultiTenantContextAccessor<TenantInfo>? tenantContextAccessor)
        : base(options)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    // IMultiTenantDbContext implementation
    public TenantInfo? TenantInfo => _tenantContextAccessor?.MultiTenantContext?.TenantInfo;
    public TenantMismatchMode TenantMismatchMode => TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode => TenantNotSetMode.Throw;

    // Domain entities
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ResourceShare> ResourceShares => Set<ResourceShare>();

    // Hierarchical Audit Logging entities
    public DbSet<HttpRequestAuditLog> HttpRequestAuditLogs => Set<HttpRequestAuditLog>();
    public DbSet<HandlerAuditLog> HandlerAuditLogs => Set<HandlerAuditLog>();
    public DbSet<EntityAuditLog> EntityAuditLogs => Set<EntityAuditLog>();

    /// <summary>
    /// Configures global type conventions.
    /// This reduces repetitive configuration and ensures consistency.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Default string length (prevents nvarchar(max) everywhere)
        configurationBuilder
            .Properties<string>()
            .AreUnicode(true)
            .HaveMaxLength(500);

        // Decimal precision for monetary values
        configurationBuilder
            .Properties<decimal>()
            .HavePrecision(18, 2);

        // Ensure DateTimeOffset is stored as UTC
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<Conventions.UtcDateTimeOffsetConverter>();

        configurationBuilder
            .Properties<DateTimeOffset?>()
            .HaveConversion<Conventions.NullableUtcDateTimeOffsetConverter>();

        // Store enums as strings (more readable in database)
        configurationBuilder
            .Properties<Enum>()
            .HaveConversion<string>();

        // Add string length by property name convention
        configurationBuilder.Conventions.Add(_ => new Conventions.StringLengthByNameConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-apply all IEntityTypeConfiguration classes from this assembly
        // This discovers RefreshTokenConfiguration, AuditLogConfiguration, etc.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure multi-tenant query filters
        ConfigureMultiTenancy(modelBuilder);
    }

    /// <summary>
    /// Configures multi-tenant query filters for all entities implementing ITenantEntity.
    /// Uses Finbuckle.MultiTenant for automatic tenant filtering.
    /// All data belongs to a tenant - TenantId is required.
    /// Default tenant "default" is always seeded and available via StaticStrategy fallback.
    /// Audit log entities are excluded - they store TenantId for filtering but don't require it.
    /// </summary>
    private void ConfigureMultiTenancy(ModelBuilder modelBuilder)
    {
        // Audit log types should not enforce tenant requirement
        // They store TenantId for filtering but allow null for system-level operations
        var auditLogTypes = new HashSet<Type>
        {
            typeof(HttpRequestAuditLog),
            typeof(HandlerAuditLog),
            typeof(EntityAuditLog)
        };

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Skip audit log entities from strict multi-tenant enforcement
                if (auditLogTypes.Contains(entityType.ClrType))
                {
                    continue;
                }

                // Apply Finbuckle multi-tenant query filter
                // This automatically filters queries by current tenant
                modelBuilder.Entity(entityType.ClrType).IsMultiTenant();
            }
        }
    }

    #region IUnitOfWork Transaction Support

    private IDbContextTransaction? _currentTransaction;

    /// <inheritdoc />
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <inheritdoc />
    public async Task<Domain.Interfaces.IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException(
                "A transaction is already in progress. Commit or rollback the current transaction before starting a new one.");
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
        return new DbTransactionWrapper(_currentTransaction);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is currently in progress.");
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is currently in progress.");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    #endregion
}
