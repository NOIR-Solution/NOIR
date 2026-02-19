namespace NOIR.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory for integration testing using SQL Server.
/// Thin subclass of <see cref="BaseWebApplicationFactory"/> — all shared logic lives in the base.
/// </summary>
public class CustomWebApplicationFactory : BaseWebApplicationFactory
{
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
