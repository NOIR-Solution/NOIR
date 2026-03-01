namespace NOIR.ArchitectureTests;

/// <summary>
/// Architecture tests to verify that all custom repository implementations
/// in the Infrastructure layer follow the required patterns for DI registration.
/// Ensures each repository implements IScopedService for Scrutor auto-registration
/// and the corresponding IRepository&lt;TEntity, TId&gt; interface.
/// </summary>
public class RepositoryDiVerificationTests
{
    private static Assembly InfrastructureAssembly =>
        typeof(Infrastructure.Identity.ApplicationUser).Assembly;

    private static Assembly DomainAssembly =>
        typeof(Domain.Common.Entity<>).Assembly;

    private static IEnumerable<Type> GetCustomRepositoryTypes() =>
        Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("NOIR.Infrastructure.Persistence.Repositories")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameEndingWith("Repository`2") // Exclude base generic Repository<,>
            .GetTypes()
            .Where(t => t.Name != "Repository" && !t.IsGenericTypeDefinition
                && t.Name != "ShippingWebhookLogRepository"); // Specialized repo, not IRepository<T,TId>

    [Fact]
    public void AllCustomRepositories_ShouldImplementIScopedService()
    {
        // Arrange
        var scopedServiceType = typeof(Application.Common.Interfaces.IScopedService);
        var repoTypes = GetCustomRepositoryTypes().ToList();

        repoTypes.Should().NotBeEmpty(
            because: "there should be custom repository implementations in Infrastructure.Persistence.Repositories");

        // Assert
        foreach (var type in repoTypes)
        {
            scopedServiceType.IsAssignableFrom(type).Should().BeTrue(
                because: $"Repository '{type.Name}' must implement IScopedService for Scrutor auto-registration");
        }
    }

    [Fact]
    public void AllCustomRepositories_ShouldBeSealed()
    {
        // Arrange
        var repoTypes = GetCustomRepositoryTypes().ToList();

        // Assert
        foreach (var type in repoTypes)
        {
            type.IsSealed.Should().BeTrue(
                because: $"Repository '{type.Name}' should be sealed for performance and clarity");
        }
    }

    [Fact]
    public void AllCustomRepositories_ShouldImplementIRepository()
    {
        // Arrange
        var repoTypes = GetCustomRepositoryTypes().ToList();
        var iRepositoryGenericDef = typeof(Domain.Interfaces.IRepository<,>);

        // Assert
        foreach (var type in repoTypes)
        {
            var implementsIRepo = type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == iRepositoryGenericDef);

            implementsIRepo.Should().BeTrue(
                because: $"Repository '{type.Name}' must implement IRepository<TEntity, TId>");
        }
    }

    [Fact]
    public void CustomRepositoryCount_ShouldBeAtLeast9ForHrCrmPm()
    {
        // Act - verify that the HR, CRM, and PM repositories exist
        var repoTypes = GetCustomRepositoryTypes().ToList();
        var repoNames = repoTypes.Select(t => t.Name).ToList();

        // Assert - HR repositories
        repoNames.Should().Contain("EmployeeRepository",
            because: "Employee aggregate root needs a custom repository");
        repoNames.Should().Contain("DepartmentRepository",
            because: "Department aggregate root needs a custom repository");

        // Assert - CRM repositories
        repoNames.Should().Contain("CrmContactRepository",
            because: "CrmContact aggregate root needs a custom repository");
        repoNames.Should().Contain("CrmCompanyRepository",
            because: "CrmCompany aggregate root needs a custom repository");
        repoNames.Should().Contain("CrmActivityRepository",
            because: "CrmActivity aggregate root needs a custom repository");
        repoNames.Should().Contain("LeadRepository",
            because: "Lead aggregate root needs a custom repository");
        repoNames.Should().Contain("PipelineRepository",
            because: "Pipeline aggregate root needs a custom repository");

        // Assert - PM repositories
        repoNames.Should().Contain("ProjectRepository",
            because: "Project aggregate root needs a custom repository");
        repoNames.Should().Contain("ProjectTaskRepository",
            because: "ProjectTask aggregate root needs a custom repository");
    }

    [Theory]
    [InlineData("EmployeeRepository", "NOIR.Domain.Entities.Hr.Employee")]
    [InlineData("DepartmentRepository", "NOIR.Domain.Entities.Hr.Department")]
    [InlineData("CrmContactRepository", "NOIR.Domain.Entities.Crm.CrmContact")]
    [InlineData("CrmCompanyRepository", "NOIR.Domain.Entities.Crm.CrmCompany")]
    [InlineData("CrmActivityRepository", "NOIR.Domain.Entities.Crm.CrmActivity")]
    [InlineData("LeadRepository", "NOIR.Domain.Entities.Crm.Lead")]
    [InlineData("PipelineRepository", "NOIR.Domain.Entities.Crm.Pipeline")]
    [InlineData("ProjectRepository", "NOIR.Domain.Entities.Pm.Project")]
    [InlineData("ProjectTaskRepository", "NOIR.Domain.Entities.Pm.ProjectTask")]
    public void Repository_ShouldImplementCorrectIRepositoryInterface(
        string repositoryName, string entityFullName)
    {
        // Arrange
        var repoType = GetCustomRepositoryTypes()
            .FirstOrDefault(t => t.Name == repositoryName);

        repoType.Should().NotBeNull(
            because: $"Repository '{repositoryName}' should exist in Infrastructure.Persistence.Repositories");

        var entityType = DomainAssembly.GetType(entityFullName);
        entityType.Should().NotBeNull(
            because: $"Entity '{entityFullName}' should exist in the Domain assembly");

        // Act - check IRepository<Entity, Guid> specifically
        var expectedInterface = typeof(Domain.Interfaces.IRepository<,>).MakeGenericType(entityType!, typeof(Guid));

        // Assert
        expectedInterface.IsAssignableFrom(repoType).Should().BeTrue(
            because: $"Repository '{repositoryName}' must implement IRepository<{entityType!.Name}, Guid>");
    }
}
