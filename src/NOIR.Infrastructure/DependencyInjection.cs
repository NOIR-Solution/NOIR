using NOIR.Application.Common.Extensions;

namespace NOIR.Infrastructure;

/// <summary>
/// Extension methods for configuring Infrastructure layer services.
/// Uses Scrutor for convention-based auto-registration via marker interfaces.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var isTesting = environment?.EnvironmentName == "Testing";

        // Configure Multi-Tenancy with Finbuckle using database-backed EFCoreStore
        // Tenant entity inherits from TenantInfo for Finbuckle compatibility
        // Using separate TenantStoreDbContext because EFCoreStore requires EFCoreStoreDbContext base class
        // NO FALLBACK STRATEGY: If tenant is null, it stays null everywhere for consistency
        // Database seeder explicitly sets tenant context when needed
        services.AddMultiTenant<Tenant>()
            .WithHeaderStrategy("X-Tenant")  // Detect tenant from header
            .WithClaimStrategy("tenant_id")  // Or from JWT claim
            .WithEFCoreStore<TenantStoreDbContext, Tenant>();  // Store tenants in dedicated DbContext

        // Register EF Core interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<EntityAuditLogInterceptor>();
        services.AddScoped<TenantIdSetterInterceptor>();

        // Skip DbContext registration in Testing - tests configure their own database
        if (!isTesting)
        {
            // Register TenantStoreDbContext for Finbuckle EFCoreStore
            services.AddDbContext<TenantStoreDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
            });

            // Register DbContext with SQL Server and multi-tenant support
            // Note: Using AddDbContext (not Pool) to support IMultiTenantContextAccessor injection
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(
                    sp.GetRequiredService<TenantIdSetterInterceptor>(),
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<DomainEventInterceptor>(),
                    sp.GetRequiredService<EntityAuditLogInterceptor>());

                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                        // Connection resiliency (retry on transient failures)
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);

                        // Performance optimizations
                        sqlOptions.CommandTimeout(30);
                        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });

                // Enable detailed errors and sensitive data logging only in development
#if DEBUG
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
                // TODO: [TECH-DEBT] Remove after EF Core Roslyn tooling fix (2026-01-25)
                // Issue: ReflectionTypeLoadException in dotnet ef migrations
                // See: docs/KNOWLEDGE_BASE.md#ef-core-migration-tooling-workaround
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
#endif
            });

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<IUnitOfWork>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
        }

        // Configure Identity with environment-aware password policy
        // Production: Strong policy (12+ chars, complexity requirements)
        // Development: Simple policy (6 chars, no complexity) for easier testing
        var identitySettings = configuration
            .GetSection(IdentitySettings.SectionName)
            .Get<IdentitySettings>() ?? new IdentitySettings();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password settings from configuration
            options.Password.RequireDigit = identitySettings.Password.RequireDigit;
            options.Password.RequireLowercase = identitySettings.Password.RequireLowercase;
            options.Password.RequireUppercase = identitySettings.Password.RequireUppercase;
            options.Password.RequireNonAlphanumeric = identitySettings.Password.RequireNonAlphanumeric;
            options.Password.RequiredLength = identitySettings.Password.RequiredLength;
            options.Password.RequiredUniqueChars = identitySettings.Password.RequiredUniqueChars;

            // User settings
            // In multi-tenant mode, email uniqueness is per-tenant, not global
            // Per-tenant uniqueness is enforced in CreateUserAsync/UpdateUserAsync
            options.User.RequireUniqueEmail = false;
            // Allow # in usernames for multi-tenant format: email#tenantId
            // Default is "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+"
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+#";

            // Lockout settings from configuration
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identitySettings.Lockout.DefaultLockoutTimeSpanMinutes);
            options.Lockout.MaxFailedAccessAttempts = identitySettings.Lockout.MaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = identitySettings.Lockout.AllowedForNewUsers;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Configure JWT Settings with validation (fail fast on startup)
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure permission-based authorization
        // Permissions are checked on each request against the database (with caching)
        // This allows real-time permission updates without requiring re-login
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Configure resource-based authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("resource:read", policy =>
                policy.Requirements.Add(new ResourcePermissionRequirement("read")))
            .AddPolicy("resource:edit", policy =>
                policy.Requirements.Add(new ResourcePermissionRequirement("edit")))
            .AddPolicy("resource:delete", policy =>
                policy.Requirements.Add(new ResourcePermissionRequirement("delete")))
            .AddPolicy("resource:share", policy =>
                policy.Requirements.Add(new ResourcePermissionRequirement("share")))
            .AddPolicy("resource:admin", policy =>
                policy.Requirements.Add(new ResourcePermissionRequirement("admin")));

        // Configure FusionCache (hybrid L1/L2 caching with stampede protection)
        // Default: In-memory only (L1). Optional: Add Redis for distributed cache (L2).
        services.AddFusionCaching(configuration);

        // Auto-register Infrastructure services using shared Scrutor extension
        services.ScanMarkerInterfaces(typeof(ApplicationDbContext).Assembly);

        // Explicitly register Application layer handlers that are used with [FromServices] injection
        services.AddScoped<NOIR.Application.Features.Auth.Commands.UploadAvatar.UploadAvatarCommandHandler>();
        services.AddScoped<NOIR.Application.Features.Auth.Commands.DeleteAvatar.DeleteAvatarCommandHandler>();

        // Configure job notification settings
        services.Configure<JobNotificationSettings>(
            configuration.GetSection(JobNotificationSettings.SectionName));

        // Configure Localization settings
        services.Configure<LocalizationSettings>(
            configuration.GetSection(LocalizationSettings.SectionName));

        // Configure Password Reset settings
        services.Configure<PasswordResetSettings>(
            configuration.GetSection(PasswordResetSettings.SectionName));

        // Configure Application settings (base URL, app name, etc.)
        services.Configure<ApplicationSettings>(
            configuration.GetSection(ApplicationSettings.SectionName));

        // Configure Payment settings
        services.Configure<PaymentSettings>(
            configuration.GetSection(PaymentSettings.SectionName));

        // Configure Payment Gateway providers (VNPay, MoMo, ZaloPay, COD)
        services.AddPaymentGatewayServices(configuration);

        // Configure Image Processing settings
        services.AddOptions<ImageProcessingSettings>()
            .Bind(configuration.GetSection(ImageProcessingSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register localization startup validator to validate resources at startup
        services.AddHostedService<LocalizationStartupValidator>();

        // Configure Hangfire for background jobs (skip in Testing - requires SQL Server)
        if (!isTesting)
        {
            // Register the job failure notification filter for DI
            services.AddSingleton<JobFailureNotificationFilter>();

            services.AddHangfire((sp, config) =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(
                        configuration.GetConnectionString("DefaultConnection"),
                        new SqlServerStorageOptions
                        {
                            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                            QueuePollInterval = TimeSpan.FromSeconds(15),
                            UseRecommendedIsolationLevel = true,
                            DisableGlobalLocks = true,
                            PrepareSchemaIfNecessary = true
                        });

                // Add global job failure notification filter
                var filter = sp.GetRequiredService<JobFailureNotificationFilter>();
                config.UseFilter(filter);
            });
            services.AddHangfireServer();
        }

        // Configure FluentEmail
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // FluentEmail's RazorRenderer requires an absolute path
        var templatesPath = Path.IsPathRooted(emailSettings.TemplatesPath)
            ? emailSettings.TemplatesPath
            : Path.Combine(Directory.GetCurrentDirectory(), emailSettings.TemplatesPath);

        services
            .AddFluentEmail(emailSettings.DefaultFromEmail, emailSettings.DefaultFromName)
            .AddRazorRenderer(templatesPath)
            .AddMailKitSender(new SmtpClientOptions
            {
                Server = emailSettings.SmtpHost,
                Port = emailSettings.SmtpPort,
                User = emailSettings.SmtpUser,
                Password = emailSettings.SmtpPassword,
                UseSsl = emailSettings.EnableSsl,
                RequiresAuthentication = !string.IsNullOrEmpty(emailSettings.SmtpUser)
            });

        // Register before-state resolvers for auditable DTOs
        // These are lazily applied on first use by WolverineBeforeStateProvider.EnsureInitialized()
        services.AddBeforeStateResolver<UserProfileDto, GetUserByIdQuery>(
            targetId => new GetUserByIdQuery(targetId.ToString()!));

        services.AddBeforeStateResolver<TenantDto, GetTenantByIdQuery>(
            targetId => new GetTenantByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<PostDto, GetPostQuery>(
            targetId => new GetPostQuery(Id: Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<PostCategoryDto, GetCategoryByIdQuery>(
            targetId => new GetCategoryByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<PostTagDto, GetTagByIdQuery>(
            targetId => new GetTagByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<EmailTemplateDto, GetEmailTemplateQuery>(
            targetId => new GetEmailTemplateQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<LegalPageDto, GetLegalPageQuery>(
            targetId => new GetLegalPageQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<ProductDto, GetProductByIdQuery>(
            targetId => new GetProductByIdQuery(Id: Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<ProductCategoryDto, GetProductCategoryByIdQuery>(
            targetId => new GetProductCategoryByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<BrandDto, GetBrandByIdQuery>(
            targetId => new GetBrandByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<OrderDto, GetOrderByIdQuery>(
            targetId => new GetOrderByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<ProductOptionDto, GetProductOptionByIdQuery>(
            targetId => new GetProductOptionByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<ProductOptionValueDto, GetProductOptionValueByIdQuery>(
            targetId => new GetProductOptionValueByIdQuery(Guid.Parse(targetId.ToString()!)));

        services.AddBeforeStateResolver<ShippingProviderDto, GetShippingProviderByIdQuery>(
            targetId => new GetShippingProviderByIdQuery(Guid.Parse(targetId.ToString()!)));

        // Settings DTOs use parameterless query resolvers (tenant-scoped singletons, no ID needed)
        services.AddSettingsBeforeStateResolver<SmtpSettingsDto, GetSmtpSettingsQuery>();
        services.AddSettingsBeforeStateResolver<BrandingSettingsDto, GetBrandingSettingsQuery>();
        services.AddSettingsBeforeStateResolver<ContactSettingsDto, GetContactSettingsQuery>();
        services.AddSettingsBeforeStateResolver<RegionalSettingsDto, GetRegionalSettingsQuery>();
        services.AddSettingsBeforeStateResolver<TenantSmtpSettingsDto, GetTenantSmtpSettingsQuery>();

        // Configure FluentStorage (Local, Azure, or S3)
        var storageSettings = configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>() ?? new StorageSettings();
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        var storage = storageSettings.Provider.ToLowerInvariant() switch
        {
            "azure" when !string.IsNullOrEmpty(storageSettings.AzureConnectionString) =>
                StorageFactory.Blobs.FromConnectionString(storageSettings.AzureConnectionString),
            "s3" when !string.IsNullOrEmpty(storageSettings.S3BucketName) =>
                StorageFactory.Blobs.FromConnectionString(
                    $"aws.s3://keyId={storageSettings.S3AccessKeyId};key={storageSettings.S3SecretAccessKey};bucket={storageSettings.S3BucketName};region={storageSettings.S3Region}"),
            _ => StorageFactory.Blobs.DirectoryFiles(Path.Combine(Directory.GetCurrentDirectory(), storageSettings.LocalPath))
        };
        services.AddSingleton(storage);

        return services;
    }
}
