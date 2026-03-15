namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// DI verification tests for repository registrations.
/// Ensures all domain entity repositories can be resolved from the DI container.
/// </summary>
[Collection("Integration")]
public class RepositoryDiVerificationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RepositoryDiVerificationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region HR Repositories

    [Fact]
    public async Task EmployeeRepository_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var repository = services.GetRequiredService<IRepository<NOIR.Domain.Entities.Hr.Employee, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task DepartmentRepository_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var repository = services.GetRequiredService<IRepository<NOIR.Domain.Entities.Hr.Department, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region CRM Repositories

    [Fact]
    public async Task CrmContactRepository_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var repository = services.GetRequiredService<IRepository<NOIR.Domain.Entities.Crm.CrmContact, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task CrmCompanyRepository_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var repository = services.GetRequiredService<IRepository<NOIR.Domain.Entities.Crm.CrmCompany, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task CrmActivityRepository_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var repository = services.GetRequiredService<IRepository<NOIR.Domain.Entities.Crm.CrmActivity, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task LeadRepository_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var repository = services.GetRequiredService<IRepository<NOIR.Domain.Entities.Crm.Lead, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task PipelineRepository_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var repository = services.GetRequiredService<IRepository<NOIR.Domain.Entities.Crm.Pipeline, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion
}
