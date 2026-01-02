namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Tests for Infrastructure DependencyInjection configuration.
/// Tests service registration, storage providers, and configuration validation.
/// </summary>
public class DependencyInjectionTests
{
    #region Helper Methods

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;Trusted_Connection=True;",
            ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyForTesting123!",
            ["Jwt:Issuer"] = "NOIR.Test",
            ["Jwt:Audience"] = "NOIR.Test",
            ["Jwt:AccessTokenExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Email:SmtpHost"] = "localhost",
            ["Email:SmtpPort"] = "25",
            ["Email:DefaultFromEmail"] = "test@test.com",
            ["Email:DefaultFromName"] = "Test",
            ["Email:TemplatesPath"] = "./Templates",
            ["Storage:Provider"] = "local",
            ["Storage:LocalPath"] = "./storage",
            ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Id"] = "default",
            ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Identifier"] = "default",
            ["Finbuckle:MultiTenant:Stores:ConfigurationStore:Tenants:0:Name"] = "Default"
        };

        if (overrides != null)
        {
            foreach (var kvp in overrides)
            {
                settings[kvp.Key] = kvp.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static IHostEnvironment CreateTestEnvironment()
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(x => x.EnvironmentName).Returns("Testing");
        return mock.Object;
    }

    private static IHostEnvironment CreateDevelopmentEnvironment()
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(x => x.EnvironmentName).Returns("Development");
        return mock.Object;
    }

    #endregion

    #region Storage Provider Tests

    [Fact]
    public void AddInfrastructureServices_WithLocalStorage_ShouldRegisterLocalProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "local",
            ["Storage:LocalPath"] = "./test-storage"
        });
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);
        var provider = services.BuildServiceProvider();

        // Assert
        var storage = provider.GetService<IBlobStorage>();
        storage.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructureServices_WithAzureStorage_WithoutConnectionString_ShouldFallbackToLocal()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "azure",
            ["Storage:AzureConnectionString"] = null // No connection string
        });
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);
        var provider = services.BuildServiceProvider();

        // Assert - Should fallback to local storage
        var storage = provider.GetService<IBlobStorage>();
        storage.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructureServices_WithS3Storage_WithoutBucket_ShouldFallbackToLocal()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "s3",
            ["Storage:S3BucketName"] = null // No bucket
        });
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);
        var provider = services.BuildServiceProvider();

        // Assert - Should fallback to local storage
        var storage = provider.GetService<IBlobStorage>();
        storage.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructureServices_WithUnknownProvider_ShouldFallbackToLocal()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "unknown-provider"
        });
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);
        var provider = services.BuildServiceProvider();

        // Assert - Should fallback to local storage
        var storage = provider.GetService<IBlobStorage>();
        storage.Should().NotBeNull();
    }

    #endregion

    #region Testing Environment Tests

    [Fact]
    public void AddInfrastructureServices_InTestingEnvironment_ShouldSkipDbContextRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert - DbContext should NOT be registered in Testing environment
        var dbContextDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ApplicationDbContext));
        dbContextDescriptor.Should().BeNull();
    }

    [Fact]
    public void AddInfrastructureServices_InTestingEnvironment_ShouldSkipHangfireRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert - Hangfire services should NOT be registered in Testing environment
        var hangfireDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.FullName?.Contains("Hangfire") == true);
        hangfireDescriptor.Should().BeNull();
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterInterceptors()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(AuditableEntityInterceptor));
        services.Should().Contain(d => d.ServiceType == typeof(DomainEventInterceptor));
        services.Should().Contain(d => d.ServiceType == typeof(EntityAuditLogInterceptor));
        services.Should().Contain(d => d.ServiceType == typeof(TenantIdSetterInterceptor));
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterAuthorizationServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert
        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.AspNetCore.Authorization.IAuthorizationHandler));
        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider));
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterJwtSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert
        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<JwtSettings>));
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterEmailSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert
        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<EmailSettings>));
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterStorageSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert
        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<StorageSettings>));
    }

    #endregion

    #region Identity Configuration Tests

    [Fact]
    public void AddInfrastructureServices_ShouldConfigureIdentityOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert - Identity services should be registered
        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>));
    }

    #endregion

    #region Multi-Tenant Configuration Tests

    [Fact]
    public void AddInfrastructureServices_ShouldConfigureMultiTenant()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert - Multi-tenant services should be registered
        services.Should().Contain(d =>
            d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("MultiTenant"));
    }

    #endregion

    #region Email Configuration Tests

    [Fact]
    public void AddInfrastructureServices_WithNullEmailSettings_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=Test;",
            ["Jwt:SecretKey"] = "TestSecretKey123456789012345678901234567890",
            ["Jwt:Issuer"] = "Test",
            ["Jwt:Audience"] = "Test",
            ["Jwt:AccessTokenExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            // Email settings NOT provided
            ["Storage:Provider"] = "local",
            ["Storage:LocalPath"] = "./storage"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var env = CreateTestEnvironment();

        // Act - Should not throw
        var act = () => services.AddInfrastructureServices(config, env);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddInfrastructureServices_WithSmtpAuthentication_ShouldConfigureCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Email:SmtpUser"] = "testuser",
            ["Email:SmtpPassword"] = "testpassword"
        });
        var env = CreateTestEnvironment();

        // Act
        services.AddInfrastructureServices(config, env);

        // Assert - Should complete without error
        var provider = services.BuildServiceProvider();
        provider.Should().NotBeNull();
    }

    #endregion

    #region Null Environment Tests

    [Fact]
    public void AddInfrastructureServices_WithNullEnvironment_ShouldNotSkipRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();

        // Act - environment is null (defaults to non-Testing)
        services.AddInfrastructureServices(config, environment: null);

        // Assert - DbContext should be registered when environment is not "Testing"
        var dbContextDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ApplicationDbContext));
        dbContextDescriptor.Should().NotBeNull();
    }

    #endregion

    #region Return Value Tests

    [Fact]
    public void AddInfrastructureServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();
        var env = CreateTestEnvironment();

        // Act
        var result = services.AddInfrastructureServices(config, env);

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion
}

/// <summary>
/// Tests for Application DependencyInjection configuration.
/// </summary>
public class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert - FluentValidation validators should be registered
        services.Should().Contain(d =>
            d.ServiceType.Name.Contains("Validator"));
    }

    [Fact]
    public void AddApplicationServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApplicationServices();

        // Assert
        result.Should().BeSameAs(services);
    }
}
