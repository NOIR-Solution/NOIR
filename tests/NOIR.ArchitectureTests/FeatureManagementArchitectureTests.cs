namespace NOIR.ArchitectureTests;

/// <summary>
/// Architecture tests for the Feature Management module definition system.
/// Validates that all IModuleDefinition implementations follow required patterns.
/// </summary>
public class FeatureManagementArchitectureTests
{
    private static Assembly ApplicationAssembly =>
        typeof(Application.Common.Interfaces.ICurrentUser).Assembly;

    private static Type ModuleDefinitionInterface =>
        typeof(Domain.Interfaces.IModuleDefinition);

    private static IEnumerable<Type> GetModuleDefinitionTypes() =>
        Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(ModuleDefinitionInterface)
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes();

    [Fact]
    public void AllModuleDefinitions_ShouldHaveUniqueNames()
    {
        // Arrange
        var moduleTypes = GetModuleDefinitionTypes().ToList();
        moduleTypes.Should().NotBeEmpty("there should be module definitions in the Application assembly");

        // Act - instantiate each and get the Name
        var names = new List<(string Name, string TypeName)>();
        foreach (var type in moduleTypes)
        {
            var instance = (Domain.Interfaces.IModuleDefinition)Activator.CreateInstance(type)!;
            names.Add((instance.Name, type.Name));
        }

        // Assert - all names should be unique
        var duplicates = names
            .GroupBy(n => n.Name)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}' used by: {string.Join(", ", g.Select(x => x.TypeName))}")
            .ToList();

        duplicates.Should().BeEmpty(
            because: "all IModuleDefinition implementations must have unique Name values");
    }

    [Fact]
    public void AllModuleDefinitions_ShouldImplementISingletonService()
    {
        // Arrange
        var singletonServiceType = typeof(Application.Common.Interfaces.ISingletonService);

        // Act
        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(ModuleDefinitionInterface)
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .ImplementInterface(singletonServiceType)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "all IModuleDefinition implementations must also implement ISingletonService for DI auto-registration");
    }

    [Fact]
    public void AllModuleNames_ShouldFollowDotNotationConvention()
    {
        // Arrange
        var moduleTypes = GetModuleDefinitionTypes().ToList();
        moduleTypes.Should().NotBeEmpty("there should be module definitions in the Application assembly");

        // Act & Assert - each module name should contain exactly one dot (Category.Name format)
        foreach (var type in moduleTypes)
        {
            var instance = (Domain.Interfaces.IModuleDefinition)Activator.CreateInstance(type)!;
            var dotCount = instance.Name.Count(c => c == '.');

            dotCount.Should().Be(1,
                because: $"module '{instance.Name}' (type: {type.Name}) should follow 'Category.Name' format with exactly one dot");
        }
    }

    [Fact]
    public void ModuleDefinitionCount_ShouldBe30()
    {
        // Act
        var moduleTypes = GetModuleDefinitionTypes().ToList();

        // Assert
        moduleTypes.Should().HaveCount(30,
            because: "there should be exactly 30 module definitions (8 Core + 14 Ecommerce + 3 Content + 3 Platform + 2 Analytics)");
    }

    [Fact]
    public void CoreModules_ShouldHaveIsCoreTrue()
    {
        // Arrange
        var coreModuleNames = new HashSet<string>
        {
            "Core.Auth", "Core.Users", "Core.Roles", "Core.Permissions",
            "Core.Dashboard", "Core.Settings", "Core.Audit", "Core.Notifications"
        };

        var moduleTypes = GetModuleDefinitionTypes().ToList();

        // Act & Assert
        foreach (var type in moduleTypes)
        {
            var instance = (Domain.Interfaces.IModuleDefinition)Activator.CreateInstance(type)!;

            if (coreModuleNames.Contains(instance.Name))
            {
                instance.IsCore.Should().BeTrue(
                    because: $"module '{instance.Name}' is a core module and must have IsCore = true");
            }
            else
            {
                instance.IsCore.Should().BeFalse(
                    because: $"module '{instance.Name}' is not a core module and must have IsCore = false");
            }
        }
    }

    [Fact]
    public void AllModuleDefinitions_ShouldBeSealed()
    {
        // Act
        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(ModuleDefinitionInterface)
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "all IModuleDefinition implementations should be sealed for performance and clarity");
    }
}
