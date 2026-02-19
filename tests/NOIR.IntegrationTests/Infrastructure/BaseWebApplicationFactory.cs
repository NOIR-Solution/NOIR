using Testcontainers.MsSql;

namespace NOIR.IntegrationTests.Infrastructure;

/// <summary>
/// Base WebApplicationFactory for integration testing using SQL Server.
/// Database strategy (in priority order):
///   1. NOIR_USE_LOCALDB=true → Windows LocalDB
///   2. NOIR_TEST_SQL_CONNECTION env var → external SQL Server
///   3. Testcontainers → auto-provisions a Docker SQL Server container
/// </summary>
public abstract class BaseWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected readonly string _databaseName = $"NOIR_Test_{Guid.NewGuid():N}";
    protected string _connectionString = null!;
    protected string _masterConnectionString = null!;
    protected MsSqlContainer? _container;

    private static bool UseLocalDb
    {
        get
        {
            var forceLocalDb = Environment.GetEnvironmentVariable("NOIR_USE_LOCALDB");
            if (bool.TryParse(forceLocalDb, out var useLocal))
            {
                return useLocal;
            }

            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);
        }
    }

    private static string? ExternalConnectionString =>
        Environment.GetEnvironmentVariable("NOIR_TEST_SQL_CONNECTION");

    private async Task EnsureDatabaseAsync()
    {
        if (UseLocalDb)
        {
            _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            _masterConnectionString = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
        }
        else if (ExternalConnectionString != null)
        {
            _connectionString = $"{ExternalConnectionString};Database={_databaseName}";
            _masterConnectionString = $"{ExternalConnectionString};Database=master";
        }
        else
        {
            // Auto-provision SQL Server via Testcontainers (Docker required)
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Test@12345!")
                .Build();

            await _container.StartAsync();

            var baseConnection = _container.GetConnectionString();
            // MsSql container returns connection string with Database=master; replace for our test DB
            var csBuilder = new SqlConnectionStringBuilder(baseConnection)
            {
                InitialCatalog = _databaseName
            };
            _connectionString = csBuilder.ConnectionString;

            csBuilder.InitialCatalog = "master";
            _masterConnectionString = csBuilder.ConnectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set Testing environment
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Database connection
                ["ConnectionStrings:DefaultConnection"] = _connectionString,

                // Rate limiting
                ["RateLimiting:PermitLimit"] = "1000",
                ["RateLimiting:AuthPermitLimit"] = "100",

                // JWT Settings - must match appsettings.json for consistent token generation/validation
                ["JwtSettings:Secret"] = "NOIRSecretKeyForJWTAuthenticationMustBeAtLeast32Characters!",
                ["JwtSettings:Issuer"] = "NOIR.API",
                ["JwtSettings:Audience"] = "NOIR.Client",
                ["JwtSettings:ExpirationInMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationInDays"] = "7",

                // Identity - use development-friendly password policy for testing
                ["Identity:Password:RequireDigit"] = "false",
                ["Identity:Password:RequireLowercase"] = "false",
                ["Identity:Password:RequireUppercase"] = "false",
                ["Identity:Password:RequireNonAlphanumeric"] = "false",
                ["Identity:Password:RequiredLength"] = "6",
                ["Identity:Password:RequiredUniqueChars"] = "1",

                // Platform settings - for seeding platform admin and default tenant
                ["Platform:PlatformAdmin:Email"] = "platform@noir.local",
                ["Platform:PlatformAdmin:Password"] = "Platform123!",
                ["Platform:PlatformAdmin:FirstName"] = "Platform",
                ["Platform:PlatformAdmin:LastName"] = "Administrator",
                ["Platform:DefaultTenant:Enabled"] = "true",
                ["Platform:DefaultTenant:Identifier"] = "default",
                ["Platform:DefaultTenant:Name"] = "Default Tenant",
                ["Platform:DefaultTenant:Admin:Enabled"] = "true",
                ["Platform:DefaultTenant:Admin:Email"] = "admin@noir.local",
                ["Platform:DefaultTenant:Admin:Password"] = "123qwe",
                ["Platform:DefaultTenant:Admin:FirstName"] = "Tenant",
                ["Platform:DefaultTenant:Admin:LastName"] = "Administrator",

                // Multi-tenant configuration - same as production with "default" tenant
                // StaticStrategy uses "default" as fallback for non-HTTP contexts (seeding, background jobs)
                ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Id"] = "default",
                ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Identifier"] = "default",
                ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Name"] = "Default Tenant",

                // Cookie Settings - for cookie-based authentication testing
                ["CookieSettings:AccessTokenCookieName"] = "noir.access",
                ["CookieSettings:RefreshTokenCookieName"] = "noir.refresh",
                ["CookieSettings:SameSiteMode"] = "Strict",
                ["CookieSettings:Path"] = "/",
                ["CookieSettings:SecureInProduction"] = "false", // Allow non-secure in testing
            });
        });

        builder.ConfigureServices(services =>
        {
            // Override Identity password options for testing (simpler passwords allowed)
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
            });

            // Remove existing DbContext registrations
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Also remove the DbContext registration itself
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove TenantStoreDbContext registrations (registered in DependencyInjection.cs)
            var tenantStoreDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TenantStoreDbContext>));
            if (tenantStoreDescriptor != null)
            {
                services.Remove(tenantStoreDescriptor);
            }

            var tenantStoreDbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TenantStoreDbContext));
            if (tenantStoreDbContextDescriptor != null)
            {
                services.Remove(tenantStoreDbContextDescriptor);
            }

            // Add memory cache
            services.AddMemoryCache();

            // Register EF Core interceptors (same as production)
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor>();
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.DomainEventInterceptor>();
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.EntityAuditLogInterceptor>();
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.TenantIdSetterInterceptor>();

            // Add TenantStoreDbContext for Finbuckle EFCoreStore
            services.AddDbContext<TenantStoreDbContext>(options =>
            {
                options.UseSqlServer(_connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(TenantStoreDbContext).Assembly.FullName);
                });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Add SQL Server for testing with multi-tenant support
            // Use the factory overload to inject IMultiTenantContextAccessor
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                // Add all interceptors like production
                options.AddInterceptors(
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.TenantIdSetterInterceptor>(),
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor>(),
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.DomainEventInterceptor>(),
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.EntityAuditLogInterceptor>());

                options.UseSqlServer(_connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                // Suppress pending model changes warning for hand-written migrations
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            // Register interfaces
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<Domain.Interfaces.IUnitOfWork>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
        });
    }

    public virtual async Task InitializeAsync()
    {
        // Provision the database (Testcontainers, LocalDB, or external)
        await EnsureDatabaseAsync();

        // Accessing Services triggers app startup which runs the seeder
        // The seeder handles database creation and migrations
        _ = Services;
    }

    /// <summary>
    /// Drops the test database. Only needed for LocalDB/external — container disposal handles Docker.
    /// </summary>
    protected async Task DropTestDatabaseAsync()
    {
        if (_container == null)
        {
            try
            {
                await using var masterConnection = new SqlConnection(_masterConnectionString);
                await masterConnection.OpenAsync();

                await using var cmd = masterConnection.CreateCommand();
                cmd.CommandText = $@"
                    IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{_databaseName}')
                    BEGIN
                        ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{_databaseName}];
                    END";
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    public new virtual async Task DisposeAsync()
    {
        await DropTestDatabaseAsync();

        await base.DisposeAsync();

        // Stop the Testcontainers SQL Server
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates an HTTP client configured for integration testing.
    /// Includes X-Tenant header for multi-tenant resolution.
    /// </summary>
    public HttpClient CreateTestClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        // Add tenant header for multi-tenant resolution
        client.DefaultRequestHeaders.Add("X-Tenant", "default");
        return client;
    }

    /// <summary>
    /// Creates an HTTP client with authentication header.
    /// Includes X-Tenant header for multi-tenant resolution.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateTestClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Executes an action within a scoped service provider with tenant context set.
    /// Use this for direct database access in tests.
    /// </summary>
    public async Task ExecuteWithTenantAsync(Func<IServiceProvider, Task> action, string tenantId = "default")
    {
        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Set tenant context for multi-tenant query filters
        var tenantSetter = services.GetService<IMultiTenantContextSetter>();
        if (tenantSetter != null)
        {
            var tenant = new Tenant(tenantId, tenantId, "Test Tenant");
            tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(tenant);
        }

        await action(services);
    }

    /// <summary>
    /// Executes a function within a scoped service provider with tenant context set.
    /// Use this for direct database access in tests.
    /// </summary>
    public async Task<T> ExecuteWithTenantAsync<T>(Func<IServiceProvider, Task<T>> func, string tenantId = "default")
    {
        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Set tenant context for multi-tenant query filters
        var tenantSetter = services.GetService<IMultiTenantContextSetter>();
        if (tenantSetter != null)
        {
            var tenant = new Tenant(tenantId, tenantId, "Test Tenant");
            tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(tenant);
        }

        return await func(services);
    }
}
