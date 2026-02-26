using NOIR.Application.Features.FeatureManagement.DTOs;
using NOIR.Application.Features.FeatureManagement.Queries.GetModuleCatalog;

namespace NOIR.Application.UnitTests.Features.FeatureManagement.Queries.GetModuleCatalog;

/// <summary>
/// Unit tests for GetModuleCatalogQueryHandler.
/// Tests retrieval and mapping of the full module catalog.
/// </summary>
public class GetModuleCatalogQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IModuleCatalog> _catalogMock;
    private readonly GetModuleCatalogQueryHandler _handler;

    public GetModuleCatalogQueryHandlerTests()
    {
        _catalogMock = new Mock<IModuleCatalog>();
        _handler = new GetModuleCatalogQueryHandler(_catalogMock.Object);
    }

    private static Mock<IModuleDefinition> CreateModuleDefinition(
        string name,
        string displayNameKey = "modules.test",
        string descriptionKey = "modules.test.desc",
        string icon = "Package",
        int sortOrder = 1,
        bool isCore = false,
        bool defaultEnabled = true,
        List<FeatureDefinition>? features = null)
    {
        var mock = new Mock<IModuleDefinition>();
        mock.Setup(m => m.Name).Returns(name);
        mock.Setup(m => m.DisplayNameKey).Returns(displayNameKey);
        mock.Setup(m => m.DescriptionKey).Returns(descriptionKey);
        mock.Setup(m => m.Icon).Returns(icon);
        mock.Setup(m => m.SortOrder).Returns(sortOrder);
        mock.Setup(m => m.IsCore).Returns(isCore);
        mock.Setup(m => m.DefaultEnabled).Returns(defaultEnabled);
        mock.Setup(m => m.Features).Returns(features ?? []);
        return mock;
    }

    #endregion

    #region Module Mapping

    [Fact]
    public async Task Handle_ShouldReturnAllModulesFromCatalog()
    {
        // Arrange
        var modules = new List<IModuleDefinition>
        {
            CreateModuleDefinition("Ecommerce", sortOrder: 1).Object,
            CreateModuleDefinition("Content", sortOrder: 2).Object,
            CreateModuleDefinition("Auth", isCore: true, sortOrder: 0).Object
        };
        _catalogMock.Setup(x => x.GetAllModules()).Returns(modules);
        var query = new GetModuleCatalogQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldMapModuleDtoCorrectly()
    {
        // Arrange
        var module = CreateModuleDefinition(
            "Ecommerce",
            "modules.ecommerce",
            "modules.ecommerce.desc",
            "ShoppingCart",
            5,
            false,
            true);
        _catalogMock.Setup(x => x.GetAllModules()).Returns(new List<IModuleDefinition> { module.Object });
        var query = new GetModuleCatalogQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value.Modules[0];
        dto.Name.Should().Be("Ecommerce");
        dto.DisplayNameKey.Should().Be("modules.ecommerce");
        dto.DescriptionKey.Should().Be("modules.ecommerce.desc");
        dto.Icon.Should().Be("ShoppingCart");
        dto.SortOrder.Should().Be(5);
        dto.IsCore.Should().BeFalse();
        dto.DefaultEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldMapFeatureDtoCorrectly()
    {
        // Arrange
        var features = new List<FeatureDefinition>
        {
            new("Ecommerce.Products", "modules.ecommerce.products", "modules.ecommerce.products.desc", true),
            new("Ecommerce.Reviews", "modules.ecommerce.reviews", "modules.ecommerce.reviews.desc", false)
        };
        var module = CreateModuleDefinition("Ecommerce", features: features);
        _catalogMock.Setup(x => x.GetAllModules()).Returns(new List<IModuleDefinition> { module.Object });
        var query = new GetModuleCatalogQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var moduleDto = result.Value.Modules[0];
        moduleDto.Features.Should().HaveCount(2);
        moduleDto.Features[0].Name.Should().Be("Ecommerce.Products");
        moduleDto.Features[0].DefaultEnabled.Should().BeTrue();
        moduleDto.Features[1].Name.Should().Be("Ecommerce.Reviews");
        moduleDto.Features[1].DefaultEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCatalogEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _catalogMock.Setup(x => x.GetAllModules()).Returns(new List<IModuleDefinition>());
        var query = new GetModuleCatalogQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenModuleHasNoFeatures_ShouldReturnEmptyFeaturesList()
    {
        // Arrange
        var module = CreateModuleDefinition("Dashboard", isCore: true);
        _catalogMock.Setup(x => x.GetAllModules()).Returns(new List<IModuleDefinition> { module.Object });
        var query = new GetModuleCatalogQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Modules[0].Features.Should().BeEmpty();
    }

    #endregion
}
