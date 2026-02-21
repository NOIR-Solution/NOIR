namespace NOIR.Application.UnitTests.Modules;

public class ModuleCatalogTests
{
    private readonly ModuleCatalog _catalog;

    public ModuleCatalogTests()
    {
        // Collect all IModuleDefinition implementations from the Application assembly
        var moduleTypes = typeof(NOIR.Application.Modules.ModuleNames).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IModuleDefinition).IsAssignableFrom(t));

        var modules = moduleTypes.Select(t => (IModuleDefinition)Activator.CreateInstance(t)!);
        _catalog = new ModuleCatalog(modules);
    }

    [Fact]
    public void GetAllModules_ShouldReturn30Modules()
    {
        // Act
        var modules = _catalog.GetAllModules();

        // Assert
        modules.Should().HaveCount(30);
    }

    [Fact]
    public void GetAllModules_ShouldHaveNoDuplicateNames()
    {
        // Act
        var modules = _catalog.GetAllModules();
        var names = modules.Select(m => m.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetAllModules_ShouldHave8CoreModules()
    {
        // Act
        var coreModules = _catalog.GetAllModules().Where(m => m.IsCore).ToList();

        // Assert
        coreModules.Should().HaveCount(8);
    }

    [Fact]
    public void GetAllModules_ShouldHave22ToggleableModules()
    {
        // Act
        var toggleable = _catalog.GetAllModules().Where(m => !m.IsCore).ToList();

        // Assert
        toggleable.Should().HaveCount(22);
    }

    [Fact]
    public void GetModule_WithValidName_ShouldReturnModule()
    {
        // Act
        var module = _catalog.GetModule("Ecommerce.Products");

        // Assert
        module.Should().NotBeNull();
        module!.Name.Should().Be("Ecommerce.Products");
    }

    [Fact]
    public void GetModule_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var module = _catalog.GetModule("NonExistent.Module");

        // Assert
        module.Should().BeNull();
    }

    [Fact]
    public void GetModule_ShouldBeCaseInsensitive()
    {
        // Act
        var module = _catalog.GetModule("ecommerce.products");

        // Assert
        module.Should().NotBeNull();
    }

    [Fact]
    public void IsCore_WithCoreModule_ShouldReturnTrue()
    {
        // Act & Assert
        _catalog.IsCore("Core.Auth").Should().BeTrue();
        _catalog.IsCore("Core.Users").Should().BeTrue();
        _catalog.IsCore("Core.Dashboard").Should().BeTrue();
    }

    [Fact]
    public void IsCore_WithNonCoreModule_ShouldReturnFalse()
    {
        // Act & Assert
        _catalog.IsCore("Ecommerce.Products").Should().BeFalse();
        _catalog.IsCore("Content.Blog").Should().BeFalse();
    }

    [Fact]
    public void Exists_WithModuleName_ShouldReturnTrue()
    {
        // Act & Assert
        _catalog.Exists("Ecommerce.Products").Should().BeTrue();
    }

    [Fact]
    public void Exists_WithFeatureName_ShouldReturnTrue()
    {
        // Act & Assert
        _catalog.Exists("Ecommerce.Products.Variants").Should().BeTrue();
    }

    [Fact]
    public void Exists_WithInvalidName_ShouldReturnFalse()
    {
        // Act & Assert
        _catalog.Exists("Nonexistent").Should().BeFalse();
    }

    [Fact]
    public void GetParentModuleName_WithChildFeature_ShouldReturnParent()
    {
        // Act
        var parent = _catalog.GetParentModuleName("Ecommerce.Products.Variants");

        // Assert
        parent.Should().Be("Ecommerce.Products");
    }

    [Fact]
    public void GetParentModuleName_WithTopLevelModule_ShouldReturnNull()
    {
        // Act
        var parent = _catalog.GetParentModuleName("Core.Auth");

        // Assert
        parent.Should().BeNull();
    }

    [Fact]
    public void GetFeature_WithValidName_ShouldReturnFeature()
    {
        // Act
        var feature = _catalog.GetFeature("Ecommerce.Products.Variants");

        // Assert
        feature.Should().NotBeNull();
        feature!.Name.Should().Be("Ecommerce.Products.Variants");
    }

    [Fact]
    public void GetAllModules_ShouldBeOrderedBySortOrder()
    {
        // Act
        var modules = _catalog.GetAllModules();
        var sortOrders = modules.Select(m => m.SortOrder).ToList();

        // Assert
        sortOrders.Should().BeInAscendingOrder();
    }

    [Fact]
    public void AllFeatureNames_ShouldBeUnique()
    {
        // Act
        var featureNames = _catalog.GetAllModules()
            .SelectMany(m => m.Features)
            .Select(f => f.Name)
            .ToList();

        // Assert
        featureNames.Should().OnlyHaveUniqueItems();
    }
}
