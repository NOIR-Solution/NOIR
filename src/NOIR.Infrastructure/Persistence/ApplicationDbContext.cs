namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// The application database context with Identity, MultiTenant, and Soft Delete support.
/// Uses convention-based configuration for consistent entity setup.
/// Implements IMultiTenantDbContext for Finbuckle multi-tenant query filter support.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IApplicationDbContext, IUnitOfWork, IMultiTenantDbContext
{
    private readonly IMultiTenantContextAccessor<Tenant>? _tenantContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMultiTenantContextAccessor<Tenant>? tenantContextAccessor)
        : base(options)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    // IMultiTenantDbContext implementation - Tenant inherits from TenantInfo
    public ITenantInfo? TenantInfo => _tenantContextAccessor?.MultiTenantContext?.TenantInfo;
    public TenantMismatchMode TenantMismatchMode => TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode => TenantNotSetMode.Throw;

    // Domain entities
    // Note: Tenants are managed by TenantStoreDbContext for Finbuckle EFCoreStore
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<EmailChangeOtp> EmailChangeOtps => Set<EmailChangeOtp>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<LegalPage> LegalPages => Set<LegalPage>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ResourceShare> ResourceShares => Set<ResourceShare>();
    public DbSet<PermissionTemplate> PermissionTemplates => Set<PermissionTemplate>();
    public DbSet<PermissionTemplateItem> PermissionTemplateItems => Set<PermissionTemplateItem>();

    // Multi-tenant platform entities (platform-level, not tenant-scoped)
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    // Notification entities
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // Hierarchical Audit Logging entities
    public DbSet<HttpRequestAuditLog> HttpRequestAuditLogs => Set<HttpRequestAuditLog>();
    public DbSet<HandlerAuditLog> HandlerAuditLogs => Set<HandlerAuditLog>();
    public DbSet<EntityAuditLog> EntityAuditLogs => Set<EntityAuditLog>();

    // Blog CMS entities
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();
    public DbSet<PostTag> PostTags => Set<PostTag>();

    // Payment entities
    public DbSet<PaymentGateway> PaymentGateways => Set<PaymentGateway>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentWebhookLog> PaymentWebhookLogs => Set<PaymentWebhookLog>();
    public DbSet<PaymentOperationLog> PaymentOperationLogs => Set<PaymentOperationLog>();
    public DbSet<Refund> Refunds => Set<Refund>();

    // Payment installments (Phase 7)
    public DbSet<PaymentInstallment> PaymentInstallments => Set<PaymentInstallment>();

    // Shipping entities
    public DbSet<ShippingProvider> ShippingProviders => Set<ShippingProvider>();
    public DbSet<ShippingOrder> ShippingOrders => Set<ShippingOrder>();
    public DbSet<ShippingTrackingEvent> ShippingTrackingEvents => Set<ShippingTrackingEvent>();
    public DbSet<ShippingWebhookLog> ShippingWebhookLogs => Set<ShippingWebhookLog>();

    // E-commerce entities (Phase 8)
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Brand> Brands => Set<Brand>();

    // Product Attribute entities (Phase 8 - Product Attribute System)
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
    public DbSet<ProductAttributeAssignment> ProductAttributeAssignments => Set<ProductAttributeAssignment>();
    public DbSet<ProductFilterIndex> ProductFilterIndexes => Set<ProductFilterIndex>();

    // Shopping Cart entities (Phase 8 - Sprint 3)
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    // Wishlist entities
    public DbSet<Domain.Entities.Wishlist.Wishlist> Wishlists => Set<Domain.Entities.Wishlist.Wishlist>();
    public DbSet<Domain.Entities.Wishlist.WishlistItem> WishlistItems => Set<Domain.Entities.Wishlist.WishlistItem>();

    // Customer entities
    public DbSet<Domain.Entities.Customer.Customer> Customers => Set<Domain.Entities.Customer.Customer>();
    public DbSet<Domain.Entities.Customer.CustomerAddress> CustomerAddresses => Set<Domain.Entities.Customer.CustomerAddress>();

    // Inventory Receipt entities
    public DbSet<InventoryReceipt> InventoryReceipts => Set<InventoryReceipt>();
    public DbSet<InventoryReceiptItem> InventoryReceiptItems => Set<InventoryReceiptItem>();

    // Review entities
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ReviewMedia> ReviewMedia => Set<ReviewMedia>();

    // Order Note entities
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();

    // Customer Group entities
    public DbSet<CustomerGroupMembership> CustomerGroupMemberships => Set<CustomerGroupMembership>();

    // Analytics entities (Phase 7)
    public DbSet<FilterAnalyticsEvent> FilterAnalyticsEvents => Set<FilterAnalyticsEvent>();

    // Promotion entities
    public DbSet<Domain.Entities.Promotion.Promotion> Promotions => Set<Domain.Entities.Promotion.Promotion>();
    public DbSet<Domain.Entities.Promotion.PromotionProduct> PromotionProducts => Set<Domain.Entities.Promotion.PromotionProduct>();
    public DbSet<Domain.Entities.Promotion.PromotionCategory> PromotionCategories => Set<Domain.Entities.Promotion.PromotionCategory>();
    public DbSet<Domain.Entities.Promotion.PromotionUsage> PromotionUsages => Set<Domain.Entities.Promotion.PromotionUsage>();

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
    /// Audit log entities and RefreshToken are excluded - they store TenantId for filtering but don't require it.
    /// </summary>
    private void ConfigureMultiTenancy(ModelBuilder modelBuilder)
    {
        // Types that should not enforce tenant requirement via query filter
        // - Audit logs: Store TenantId for filtering but allow null for system-level operations
        // - RefreshToken: User-scoped (not tenant-scoped) - platform admins need sessions too
        var excludedTypes = new HashSet<Type>
        {
            typeof(HttpRequestAuditLog),
            typeof(HandlerAuditLog),
            typeof(EntityAuditLog),
            typeof(RefreshToken)
        };

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Skip excluded types from strict multi-tenant enforcement
                if (excludedTypes.Contains(entityType.ClrType))
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
