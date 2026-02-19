namespace NOIR.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory that uses SQL Server with Respawn for database reset.
/// Extends <see cref="BaseWebApplicationFactory"/> with Respawn support for test isolation.
/// </summary>
public class LocalDbWebApplicationFactory : BaseWebApplicationFactory
{
    private Respawner? _respawner;
    private SqlConnection? _dbConnection;

    public string ConnectionString => _connectionString;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Set up Respawner for cleaning the database between tests
        _dbConnection = new SqlConnection(_connectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = new Respawn.Graph.Table[]
            {
                "__EFMigrationsHistory",
                "AspNetRoles",
                "AspNetRoleClaims",
                "AspNetUsers",
                "AspNetUserRoles",
                "Tenants"
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

    public override async Task DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        await base.DisposeAsync();
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
