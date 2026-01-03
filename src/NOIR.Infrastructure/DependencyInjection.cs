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
        // Configure Multi-Tenancy with Finbuckle
        services.AddMultiTenant<TenantInfo>()
            .WithHeaderStrategy("X-Tenant")  // Detect tenant from header
            .WithClaimStrategy("tenant_id")  // Or from JWT claim
            .WithStaticStrategy("default")   // Fallback for non-HTTP contexts (like seeding)
            .WithConfigurationStore();       // Store tenants in appsettings.json

        // Register EF Core interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<EntityAuditLogInterceptor>();
        services.AddScoped<TenantIdSetterInterceptor>();

        // Skip DbContext registration in Testing - tests configure their own database
        if (!isTesting)
        {
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

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings from configuration
            options.Password.RequireDigit = identitySettings.Password.RequireDigit;
            options.Password.RequireLowercase = identitySettings.Password.RequireLowercase;
            options.Password.RequireUppercase = identitySettings.Password.RequireUppercase;
            options.Password.RequireNonAlphanumeric = identitySettings.Password.RequireNonAlphanumeric;
            options.Password.RequiredLength = identitySettings.Password.RequiredLength;
            options.Password.RequiredUniqueChars = identitySettings.Password.RequiredUniqueChars;

            // User settings
            options.User.RequireUniqueEmail = true;

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

        // Auto-register services using Scrutor via marker interfaces
        // Services implement IScopedService, ITransientService, or ISingletonService
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationDbContext>()

            // Register IScopedService implementations
            .AddClasses(c => c.AssignableTo<IScopedService>(), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            // Register ITransientService implementations
            .AddClasses(c => c.AssignableTo<ITransientService>(), publicOnly: false)
            .AsImplementedInterfaces()
            .WithTransientLifetime()

            // Register ISingletonService implementations
            .AddClasses(c => c.AssignableTo<ISingletonService>(), publicOnly: false)
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        );

        // Configure job notification settings
        services.Configure<JobNotificationSettings>(
            configuration.GetSection(JobNotificationSettings.SectionName));

        // Configure Localization settings
        services.Configure<LocalizationSettings>(
            configuration.GetSection(LocalizationSettings.SectionName));

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
        services
            .AddFluentEmail(emailSettings.DefaultFromEmail, emailSettings.DefaultFromName)
            .AddRazorRenderer(emailSettings.TemplatesPath)
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
