namespace NOIR.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory that uses SQL Server LocalDB for integration testing.
/// Provides realistic database testing with actual SQL Server behavior.
/// </summary>
public class LocalDbWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"NOIR_Test_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private Respawner? _respawner;
    private SqlConnection? _dbConnection;

    public string ConnectionString => _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

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

                // Identity - use development-friendly password policy for testing
                ["Identity:Password:RequireDigit"] = "false",
                ["Identity:Password:RequireLowercase"] = "false",
                ["Identity:Password:RequireUppercase"] = "false",
                ["Identity:Password:RequireNonAlphanumeric"] = "false",
                ["Identity:Password:RequiredLength"] = "6",
                ["Identity:Password:RequiredUniqueChars"] = "1",

                // Multi-tenant configuration - same as production with "default" tenant
                // StaticStrategy uses "default" as fallback for non-HTTP contexts (seeding, background jobs)
                ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Id"] = "default",
                ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Identifier"] = "default",
                ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Name"] = "Default Tenant",
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

            // Add memory cache
            services.AddMemoryCache();

            // Register EF Core interceptors (same as production)
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor>();
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.DomainEventInterceptor>();
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.EntityAuditLogInterceptor>();
            services.AddScoped<NOIR.Infrastructure.Persistence.Interceptors.TenantIdSetterInterceptor>();

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

    public async Task InitializeAsync()
    {
        // Accessing Services triggers app startup which runs the seeder
        // The seeder handles database creation (migrations or EnsureCreated)
        _ = Services;

        // Set up Respawner for cleaning the database between tests
        _dbConnection = new SqlConnection(_connectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = new Respawn.Graph.Table[]
            {
                "__EFMigrationsHistory",
                "AspNetRoles" // Keep roles as they're seeded
            }
        });
    }

    /// <summary>
    /// Resets the database to a clean state, keeping only the seeded data.
    /// Call this between tests to ensure test isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner != null && _dbConnection != null)
        {
            await _respawner.ResetAsync(_dbConnection);
        }
    }

    public new async Task DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        // Drop the test database
        try
        {
            using var masterConnection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
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
    /// Gets a scoped service from the DI container.
    /// </summary>
    public T GetRequiredService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
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
            var tenant = new TenantInfo(tenantId, tenantId, "Test Tenant");
            tenantSetter.MultiTenantContext = new MultiTenantContext<TenantInfo>(tenant);
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
            var tenant = new TenantInfo(tenantId, tenantId, "Test Tenant");
            tenantSetter.MultiTenantContext = new MultiTenantContext<TenantInfo>(tenant);
        }

        return await func(services);
    }

    /// <summary>
    /// Executes an action within a scoped service provider.
    /// </summary>
    [Obsolete("Use ExecuteWithTenantAsync for proper multi-tenant support")]
    public async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action)
    {
        await ExecuteWithTenantAsync(action);
    }
}

/// <summary>
/// Collection fixture for sharing the LocalDB WebApplicationFactory across tests.
/// </summary>
[CollectionDefinition("LocalDb")]
public class LocalDbTestCollection : ICollectionFixture<LocalDbWebApplicationFactory>
{
}
