namespace NOIR.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory for integration testing using SQL Server.
/// Supports both LocalDB (Windows) and Docker SQL Server (macOS/Linux).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"NOIR_Test_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private string _masterConnectionString = null!;

    // Allow explicit override via environment variable for edge cases (e.g., WSL with LocalDB)
    private static bool UseLocalDb
    {
        get
        {
            var forceLocalDb = Environment.GetEnvironmentVariable("NOIR_USE_LOCALDB");
            if (bool.TryParse(forceLocalDb, out var useLocal))
            {
                return useLocal;
            }

            // Default: LocalDB on Windows, Docker elsewhere
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (UseLocalDb)
        {
            // Windows: Use LocalDB
            _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            _masterConnectionString = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
        }
        else
        {
            // macOS/Linux: Use Docker SQL Server (or override via environment variable)
            var baseConnection = Environment.GetEnvironmentVariable("NOIR_TEST_SQL_CONNECTION")
                ?? "Server=localhost,1433;User Id=sa;Password=coffee123@@;TrustServerCertificate=True";
            _connectionString = $"{baseConnection};Database={_databaseName}";
            _masterConnectionString = $"{baseConnection};Database=master";
        }

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
                options.UseSqlServer(_connectionString);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Add SQL Server LocalDB for testing with multi-tenant support
            // Use the factory overload to inject IMultiTenantContextAccessor
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                // Add all interceptors like production
                options.AddInterceptors(
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.TenantIdSetterInterceptor>(),
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor>(),
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.DomainEventInterceptor>(),
                    sp.GetRequiredService<NOIR.Infrastructure.Persistence.Interceptors.EntityAuditLogInterceptor>());

                options.UseSqlServer(_connectionString);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Register interfaces
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<Domain.Interfaces.IUnitOfWork>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
        });
    }

    public Task InitializeAsync()
    {
        // Accessing Services triggers app startup which runs the seeder
        // The seeder handles database creation and migrations (MigrateAsync for SQL, EnsureCreatedAsync for InMemory)
        _ = Services;
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        // Drop the test database
        try
        {
            using var masterConnection = new SqlConnection(_masterConnectionString);
            await masterConnection.OpenAsync();

            // Force close all connections and drop database
            using var cmd = masterConnection.CreateCommand();
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

        await base.DisposeAsync();
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

/// <summary>
/// Collection fixture for sharing the WebApplicationFactory across tests.
/// Improves test performance by reusing the same server instance.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition].
}
