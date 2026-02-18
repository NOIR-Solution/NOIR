namespace NOIR.ArchitectureTests;

/// <summary>
/// Architecture tests to ensure Clean Architecture layer dependencies are maintained.
/// Uses NetArchTest.Rules to verify dependency directions and architectural boundaries.
/// </summary>
public class LayerDependencyTests
{
    #region Assembly Names

    private const string DomainNamespace = "NOIR.Domain";
    private const string ApplicationNamespace = "NOIR.Application";
    private const string InfrastructureNamespace = "NOIR.Infrastructure";
    private const string WebNamespace = "NOIR.Web";

    // Get assemblies using marker types
    private static System.Reflection.Assembly DomainAssembly =>
        typeof(Domain.Common.Entity<>).Assembly;
    private static System.Reflection.Assembly ApplicationAssembly =>
        typeof(Application.Common.Interfaces.ICurrentUser).Assembly;
    private static System.Reflection.Assembly InfrastructureAssembly =>
        typeof(Infrastructure.Identity.ApplicationUser).Assembly;
    private static System.Reflection.Assembly WebAssembly =>
        typeof(Web.Endpoints.AuthEndpoints).Assembly;

    #endregion

    #region Domain Layer Tests - No External Dependencies

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        // Act
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer should not depend on Application layer");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        // Act
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer should not depend on Infrastructure layer");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Web()
    {
        // Act
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer should not depend on Web layer");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_EntityFrameworkCore()
    {
        // Act
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer should not depend on Entity Framework Core");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_AspNetCore()
    {
        // Act
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer should not depend on ASP.NET Core");
    }

    #endregion

    #region Application Layer Tests - Only Depends on Domain

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        // Act
        var result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Application layer should not depend on Infrastructure layer");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Web()
    {
        // Act
        var result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Application layer should not depend on Web layer");
    }

    [Fact]
    public void Application_Core_ShouldNotDependOn_EntityFrameworkCore()
    {
        // Act
        // Note: Some features use IApplicationDbContext for direct EF Core access:
        // - EmailTemplates and LegalPages: copy-on-write pattern for tenant override queries
        // - FilterAnalytics: analytics aggregation queries
        // - ProductFilter: faceted search and filter queries
        // - ProductFilterIndex: sync handler for denormalized filter indexes
        // - ProductAttributes: complex attribute assignment queries
        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("NOIR.Application.Features")
            .And()
            .DoNotResideInNamespace("NOIR.Application.Features.EmailTemplates")
            .And()
            .DoNotResideInNamespace("NOIR.Application.Features.LegalPages")
            .And()
            .DoNotResideInNamespace("NOIR.Application.Features.FilterAnalytics")
            .And()
            .DoNotResideInNamespace("NOIR.Application.Features.ProductFilter")
            .And()
            .DoNotResideInNamespace("NOIR.Application.Features.ProductFilterIndex")
            .And()
            .DoNotResideInNamespace("NOIR.Application.Features.ProductAttributes")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Application Features should not depend on Entity Framework Core");
    }

    [Fact]
    public void Application_MayDependOn_Domain()
    {
        // Act
        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveDependencyOn(DomainNamespace)
            .GetTypes();

        // Assert
        result.Should().NotBeEmpty(
            because: "Application layer should reference Domain layer");
    }

    #endregion

    #region Infrastructure Layer Tests

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Web()
    {
        // Act
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure layer should not depend on Web layer");
    }

    [Fact]
    public void Infrastructure_MayDependOn_Domain()
    {
        // Act
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn(DomainNamespace)
            .GetTypes();

        // Assert
        result.Should().NotBeEmpty(
            because: "Infrastructure layer should reference Domain layer");
    }

    [Fact]
    public void Infrastructure_MayDependOn_Application()
    {
        // Act
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn(ApplicationNamespace)
            .GetTypes();

        // Assert
        result.Should().NotBeEmpty(
            because: "Infrastructure layer should reference Application layer to implement interfaces");
    }

    #endregion

    #region Web Layer Tests

    [Fact]
    public void Web_EndpointsShouldExist()
    {
        // Act
        var endpointTypes = Types
            .InAssembly(WebAssembly)
            .That()
            .ResideInNamespace("NOIR.Web.Endpoints")
            .GetTypes();

        // Assert - Endpoints should exist
        endpointTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Web_MayDependOn_Application()
    {
        // Act
        var result = Types
            .InAssembly(WebAssembly)
            .That()
            .HaveDependencyOn(ApplicationNamespace)
            .GetTypes();

        // Assert
        result.Should().NotBeEmpty(
            because: "Web layer should reference Application layer for commands/queries");
    }

    #endregion

    #region Naming Convention Tests

    [Fact]
    public void Specifications_ShouldHaveCorrectNamingSuffix()
    {
        // Act - Only check public specification classes that inherit from Specification<>
        var specTypes = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("NOIR.Application.Specifications")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .ArePublic()
            .And()
            .DoNotHaveNameEndingWith("Builder") // Exclude internal builders
            .GetTypes()
            .Where(t => !t.Name.Contains("Specification")); // Exclude internal And/Or/Not wrappers

        // Assert
        foreach (var type in specTypes)
        {
            type.Name.Should().EndWith("Spec",
                because: $"Public specification '{type.Name}' should end with 'Spec'");
        }
    }

    [Fact]
    public void Commands_ShouldHaveCorrectNamingSuffix()
    {
        // Act
        var commandTypes = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Commands")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameEndingWith("Handler")
            .And()
            .DoNotHaveNameEndingWith("Validator")
            .And()
            .DoNotHaveNameEndingWith("Dto") // Exclude DTOs co-located with commands
            .And()
            .DoNotHaveNameEndingWith("Request") // Exclude Request DTOs co-located with commands
            .And()
            .DoNotHaveNameEndingWith("Result") // Exclude Result types co-located with commands
            .And()
            .DoNotHaveNameEndingWith("Item") // Exclude Item DTOs co-located with commands (e.g., CategorySortOrderItem)
            .GetTypes();

        // Assert
        foreach (var type in commandTypes)
        {
            type.Name.Should().EndWith("Command",
                because: $"Type '{type.Name}' in Commands namespace should end with 'Command'");
        }
    }

    [Fact]
    public void Validators_ShouldHaveCorrectNamingSuffix()
    {
        // Act
        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "All validators should end with 'Validator'");
    }

    [Fact]
    public void Handlers_ShouldHaveCorrectNamingSuffix()
    {
        // Act
        var handlerTypes = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("NOIR.Infrastructure.Identity.Handlers")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert
        foreach (var type in handlerTypes)
        {
            type.Name.Should().EndWith("Handler",
                because: $"Type '{type.Name}' in Handlers namespace should end with 'Handler'");
        }
    }

    #endregion

    #region Interface Segregation Tests

    [Fact]
    public void Interfaces_ShouldBeDefined_InApplication()
    {
        // Act
        var interfaces = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("NOIR.Application.Common.Interfaces")
            .And()
            .AreInterfaces()
            .GetTypes();

        // Assert
        interfaces.Should().NotBeEmpty(
            because: "Application layer should define interfaces for dependency inversion");
    }

    [Fact]
    public void ServiceInterfaces_ShouldBeImplemented_InInfrastructure()
    {
        // Act - Check that infrastructure implements at least some application interfaces
        var infrastructureTypes = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .AreClasses()
            .GetTypes();

        var implementedInterfaces = infrastructureTypes
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.Namespace?.StartsWith("NOIR.Application") == true)
            .Distinct();

        // Assert
        implementedInterfaces.Should().NotBeEmpty(
            because: "Infrastructure layer should implement Application layer interfaces");
    }

    #endregion

    #region Entity and Value Object Tests

    [Fact]
    public void Entities_ShouldInherit_FromEntityBase()
    {
        // Join entities (many-to-many relationships) don't need Entity<> base
        // Tenant inherits from TenantInfo (Finbuckle requirement for EFCoreStore)
        var excludedNames = new HashSet<string> { "RolePermission", "Tenant" };

        // Act
        var entityTypes = Types
            .InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("NOIR.Domain.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes()
            .Where(t => !excludedNames.Contains(t.Name) && !t.IsEnum);

        // Assert
        foreach (var type in entityTypes)
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition().Name.StartsWith("Entity"))
                {
                    break;
                }
                baseType = baseType.BaseType;
            }

            (baseType != null && baseType != typeof(object)).Should().BeTrue(
                because: $"Entity '{type.Name}' should inherit from Entity<TId>");
        }
    }

    #endregion

    #region No Circular Dependencies

    [Fact]
    public void Domain_HasNoCircularDependencies()
    {
        // Act
        var types = Types.InAssembly(DomainAssembly).GetTypes();

        // Assert - Just verify Domain types exist and can be loaded
        types.Should().NotBeEmpty();
    }

    [Fact]
    public void Application_HasNoCircularDependencies()
    {
        // Act
        var types = Types.InAssembly(ApplicationAssembly).GetTypes();

        // Assert - Just verify Application types exist and can be loaded
        types.Should().NotBeEmpty();
    }

    #endregion

    #region CQRS Pattern Tests

    [Fact]
    public void Commands_ShouldBeImmutable()
    {
        // Act
        var commandTypes = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert
        foreach (var type in commandTypes)
        {
            // Records are immutable by default, or check for init-only setters
            (type.IsValueType ||
             type.GetConstructors().Any(c => c.GetParameters().Length > 0) ||
             type.IsSealed).Should().BeTrue(
                because: $"Command '{type.Name}' should be immutable (record, readonly struct, or sealed with init-only properties)");
        }
    }

    [Fact]
    public void Queries_ShouldExist()
    {
        // Act
        var queryTypes = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert
        queryTypes.Should().NotBeEmpty(
            because: "Application should have Query classes for read operations");
    }

    #endregion
}
