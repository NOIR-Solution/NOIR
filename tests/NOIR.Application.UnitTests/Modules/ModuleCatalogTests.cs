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
    public void GetAllModules_ShouldReturn32Modules()
    {
        // Act
        var modules = _catalog.GetAllModules();

        // Assert
        modules.Count().ShouldBe(35);
    }

    [Fact]
    public void GetAllModules_ShouldHaveNoDuplicateNames()
    {
        // Act
        var modules = _catalog.GetAllModules();
        var names = modules.Select(m => m.Name).ToList();

        // Assert
        names.ShouldBeUnique();
    }

    [Fact]
    public void GetAllModules_ShouldHave8CoreModules()
    {
        // Act
        var coreModules = _catalog.GetAllModules().Where(m => m.IsCore).ToList();

        // Assert
        coreModules.Count().ShouldBe(8);
    }

    [Fact]
    public void GetAllModules_ShouldHave24ToggleableModules()
    {
        // Act
        var toggleable = _catalog.GetAllModules().Where(m => !m.IsCore).ToList();

        // Assert
        toggleable.Count().ShouldBe(27);
    }

    [Fact]
    public void GetModule_WithValidName_ShouldReturnModule()
    {
        // Act
        var module = _catalog.GetModule("Ecommerce.Products");

        // Assert
        module.ShouldNotBeNull();
        module!.Name.ShouldBe("Ecommerce.Products");
    }

    [Fact]
    public void GetModule_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var module = _catalog.GetModule("NonExistent.Module");

        // Assert
        module.ShouldBeNull();
    }

    [Fact]
    public void GetModule_ShouldBeCaseInsensitive()
    {
        // Act
        var module = _catalog.GetModule("ecommerce.products");

        // Assert
        module.ShouldNotBeNull();
    }

    [Fact]
    public void IsCore_WithCoreModule_ShouldReturnTrue()
    {
        // Act & Assert
        _catalog.IsCore("Core.Auth").ShouldBe(true);
        _catalog.IsCore("Core.Users").ShouldBe(true);
        _catalog.IsCore("Core.Dashboard").ShouldBe(true);
    }

    [Fact]
    public void IsCore_WithNonCoreModule_ShouldReturnFalse()
    {
        // Act & Assert
        _catalog.IsCore("Ecommerce.Products").ShouldBe(false);
        _catalog.IsCore("Content.Blog").ShouldBe(false);
    }

    [Fact]
    public void Exists_WithModuleName_ShouldReturnTrue()
    {
        // Act & Assert
        _catalog.Exists("Ecommerce.Products").ShouldBe(true);
    }

    [Fact]
    public void Exists_WithFeatureName_ShouldReturnTrue()
    {
        // Act & Assert
        _catalog.Exists("Ecommerce.Products.Variants").ShouldBe(true);
    }

    [Fact]
    public void Exists_WithInvalidName_ShouldReturnFalse()
    {
        // Act & Assert
        _catalog.Exists("Nonexistent").ShouldBe(false);
    }

    [Fact]
    public void GetParentModuleName_WithChildFeature_ShouldReturnParent()
    {
        // Act
        var parent = _catalog.GetParentModuleName("Ecommerce.Products.Variants");

        // Assert
        parent.ShouldBe("Ecommerce.Products");
    }

    [Fact]
    public void GetParentModuleName_WithTopLevelModule_ShouldReturnNull()
    {
        // Act
        var parent = _catalog.GetParentModuleName("Core.Auth");

        // Assert
        parent.ShouldBeNull();
    }

    [Fact]
    public void GetFeature_WithValidName_ShouldReturnFeature()
    {
        // Act
        var feature = _catalog.GetFeature("Ecommerce.Products.Variants");

        // Assert
        feature.ShouldNotBeNull();
        feature!.Name.ShouldBe("Ecommerce.Products.Variants");
    }

    [Fact]
    public void GetAllModules_ShouldBeOrderedBySortOrder()
    {
        // Act
        var modules = _catalog.GetAllModules();
        var sortOrders = modules.Select(m => m.SortOrder).ToList();

        // Assert
        sortOrders.ShouldBeInOrder(SortDirection.Ascending);
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
        featureNames.ShouldBeUnique();
    }
}
