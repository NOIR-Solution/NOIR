using NOIR.Application.Features.FeatureManagement.DTOs;
using NOIR.Application.Features.FeatureManagement.Queries.GetTenantFeatureStates;

namespace NOIR.Application.UnitTests.Features.FeatureManagement.Queries.GetTenantFeatureStates;

/// <summary>
/// Unit tests for GetTenantFeatureStatesQueryHandler.
/// Tests platform admin view of module states overlaid with tenant DB state.
/// </summary>
public class GetTenantFeatureStatesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IModuleCatalog> _catalogMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetTenantFeatureStatesQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetTenantFeatureStatesQueryHandlerTests()
    {
        _catalogMock = new Mock<IModuleCatalog>();
        _dbContextMock = new Mock<IApplicationDbContext>();

        _handler = new GetTenantFeatureStatesQueryHandler(
            _catalogMock.Object,
            _dbContextMock.Object);
    }

    private void SetupDbSet(List<TenantModuleState> states)
    {
        var mockDbSet = states.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TenantModuleStates).Returns(mockDbSet.Object);
    }

    private static Mock<IModuleDefinition> CreateModuleDefinition(
        string name,
        bool isCore = false,
        bool defaultEnabled = true,
        List<FeatureDefinition>? features = null)
    {
        var mock = new Mock<IModuleDefinition>();
        mock.Setup(m => m.Name).Returns(name);
        mock.Setup(m => m.DisplayNameKey).Returns($"modules.{name.ToLower()}");
        mock.Setup(m => m.DescriptionKey).Returns($"modules.{name.ToLower()}.desc");
        mock.Setup(m => m.Icon).Returns("Package");
        mock.Setup(m => m.SortOrder).Returns(1);
        mock.Setup(m => m.IsCore).Returns(isCore);
        mock.Setup(m => m.DefaultEnabled).Returns(defaultEnabled);
        mock.Setup(m => m.Features).Returns(features ?? []);
        return mock;
    }

    #endregion

    #region Default Values (No DB State)

    [Fact]
    public async Task Handle_WhenNoDbState_ShouldUseDefaultValues()
    {
        // Arrange
        var module = CreateModuleDefinition("Ecommerce", defaultEnabled: true);
        _catalogMock.Setup(x => x.GetAllModules())
            .Returns(new List<IModuleDefinition> { module.Object });
        SetupDbSet([]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value.Modules[0];
        dto.IsAvailable.ShouldBe(true); // Default: true
        dto.IsEnabled.ShouldBe(true); // Uses DefaultEnabled from module
        dto.IsEffective.ShouldBe(true); // Available && Enabled
    }

    [Fact]
    public async Task Handle_WhenNoDbState_ShouldUseDefaultEnabledFalse()
    {
        // Arrange
        var module = CreateModuleDefinition("Ecommerce.Reviews", defaultEnabled: false);
        _catalogMock.Setup(x => x.GetAllModules())
            .Returns(new List<IModuleDefinition> { module.Object });
        SetupDbSet([]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value.Modules[0];
        dto.IsAvailable.ShouldBe(true);
        dto.IsEnabled.ShouldBe(false);
        dto.IsEffective.ShouldBe(false);
    }

    #endregion

    #region Overlaid DB States

    [Fact]
    public async Task Handle_ShouldOverlayDbStatesOnCatalog()
    {
        // Arrange
        var module = CreateModuleDefinition("Ecommerce", defaultEnabled: true);
        _catalogMock.Setup(x => x.GetAllModules())
            .Returns(new List<IModuleDefinition> { module.Object });

        var dbState = TenantModuleState.Create("Ecommerce", TestTenantId);
        dbState.SetAvailability(false);
        dbState.SetEnabled(true);
        SetupDbSet([dbState]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value.Modules[0];
        dto.IsAvailable.ShouldBe(false); // From DB
        dto.IsEnabled.ShouldBe(true); // From DB
        dto.IsEffective.ShouldBe(false); // Not available
    }

    [Fact]
    public async Task Handle_ShouldOverlayFeatureDbStates()
    {
        // Arrange
        var features = new List<FeatureDefinition>
        {
            new("Ecommerce.Products", "modules.ecommerce.products", "desc", true)
        };
        var module = CreateModuleDefinition("Ecommerce", features: features);
        _catalogMock.Setup(x => x.GetAllModules())
            .Returns(new List<IModuleDefinition> { module.Object });

        var featureState = TenantModuleState.Create("Ecommerce.Products", TestTenantId);
        featureState.SetAvailability(true);
        featureState.SetEnabled(false);
        SetupDbSet([featureState]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var featureDto = result.Value.Modules[0].Features[0];
        featureDto.IsEnabled.ShouldBe(false); // From DB
    }

    #endregion

    #region Core Module Behavior

    [Fact]
    public async Task Handle_CoreModules_ShouldAlwaysBeEffective()
    {
        // Arrange
        var module = CreateModuleDefinition("Auth", isCore: true);
        _catalogMock.Setup(x => x.GetAllModules())
            .Returns(new List<IModuleDefinition> { module.Object });
        SetupDbSet([]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value.Modules[0];
        dto.IsEffective.ShouldBe(true); // Core modules always effective
    }

    #endregion

    #region Multiple Modules

    [Fact]
    public async Task Handle_ShouldHandleMultipleModulesWithMixedStates()
    {
        // Arrange
        var modules = new List<IModuleDefinition>
        {
            CreateModuleDefinition("Auth", isCore: true).Object,
            CreateModuleDefinition("Ecommerce", defaultEnabled: true).Object,
            CreateModuleDefinition("Content", defaultEnabled: false).Object
        };
        _catalogMock.Setup(x => x.GetAllModules()).Returns(modules);

        var ecommerceState = TenantModuleState.Create("Ecommerce", TestTenantId);
        ecommerceState.SetAvailability(true);
        ecommerceState.SetEnabled(true);
        SetupDbSet([ecommerceState]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Modules.Count().ShouldBe(3);
        result.Value.Modules[0].IsEffective.ShouldBe(true); // Auth: core
        result.Value.Modules[1].IsEffective.ShouldBe(true); // Ecommerce: available & enabled
        result.Value.Modules[2].IsEffective.ShouldBe(false); // Content: default disabled
    }

    [Fact]
    public async Task Handle_ShouldFilterByTenantId()
    {
        // Arrange - DB contains states for different tenants
        var module = CreateModuleDefinition("Ecommerce");
        _catalogMock.Setup(x => x.GetAllModules())
            .Returns(new List<IModuleDefinition> { module.Object });

        var testTenantState = TenantModuleState.Create("Ecommerce", TestTenantId);
        testTenantState.SetAvailability(false);

        var otherTenantState = TenantModuleState.Create("Ecommerce", "other-tenant");
        otherTenantState.SetAvailability(true);

        SetupDbSet([testTenantState, otherTenantState]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value.Modules[0];
        dto.IsAvailable.ShouldBe(false); // Uses test-tenant's state, not other-tenant's
    }

    #endregion

    #region Feature Effective State Depends on Module

    [Fact]
    public async Task Handle_WhenModuleNotEffective_FeaturesShouldNotBeEffective()
    {
        // Arrange
        var features = new List<FeatureDefinition>
        {
            new("Ecommerce.Products", "key", "desc", true)
        };
        var module = CreateModuleDefinition("Ecommerce", features: features);
        _catalogMock.Setup(x => x.GetAllModules())
            .Returns(new List<IModuleDefinition> { module.Object });

        var moduleState = TenantModuleState.Create("Ecommerce", TestTenantId);
        moduleState.SetAvailability(false); // Module not available
        SetupDbSet([moduleState]);
        var query = new GetTenantFeatureStatesQuery(TestTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var featureDto = result.Value.Modules[0].Features[0];
        featureDto.IsEffective.ShouldBe(false); // Parent module not effective
    }

    #endregion
}
