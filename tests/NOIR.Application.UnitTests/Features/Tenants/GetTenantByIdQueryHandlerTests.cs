using NOIR.Application.Features.Tenants.DTOs;
using NOIR.Application.Features.Tenants.Queries.GetTenantById;

namespace NOIR.Application.UnitTests.Features.Tenants;

/// <summary>
/// Unit tests for GetTenantByIdQueryHandler.
/// Tests single tenant retrieval scenarios with mocked dependencies.
/// </summary>
public class GetTenantByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetTenantByIdQueryHandler _handler;

    public GetTenantByIdQueryHandlerTests()
    {
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetTenantByIdQueryHandler(
            _tenantStoreMock.Object,
            _localizationServiceMock.Object);
    }

    private static Tenant CreateTestTenant(
        string identifier = "test-tenant",
        string name = "Test Tenant",
        bool isActive = true,
        bool isDeleted = false)
    {
        var tenant = Tenant.Create(identifier, name, isActive: isActive);

        if (isDeleted)
        {
            tenant.IsDeleted = true;
            tenant.DeletedAt = DateTimeOffset.UtcNow;
        }

        return tenant;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnTenant()
    {
        // Arrange
        var tenant = CreateTestTenant(
            identifier: "acme-corp",
            name: "Acme Corporation",
            isActive: true);
        var tenantId = tenant.GetGuidId();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(tenant.Id);
        result.Value.Identifier.ShouldBe("acme-corp");
        result.Value.Name.ShouldBe("Acme Corporation");
        result.Value.IsActive.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithInactiveTenant_ShouldReturnInactiveTenant()
    {
        // Arrange
        var tenant = CreateTestTenant(
            identifier: "inactive-tenant",
            name: "Inactive Tenant",
            isActive: false);
        var tenantId = tenant.GetGuidId();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsActive.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var tenant = CreateTestTenant(
            identifier: "full-tenant",
            name: "Full Tenant");
        var tenantId = tenant.GetGuidId();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Id.ShouldBe(tenant.Id);
        dto.Identifier.ShouldBe(tenant.Identifier);
        dto.Name.ShouldBe(tenant.Name);
        dto.IsActive.ShouldBe(tenant.IsActive);
        dto.CreatedAt.ShouldBe(tenant.CreatedAt);
        dto.ModifiedAt.ShouldBe(tenant.ModifiedAt);
    }

    [Fact]
    public async Task Handle_WithModifiedTenant_ShouldIncludeModifiedAt()
    {
        // Arrange
        var baseTenant = CreateTestTenant(
            identifier: "modified-tenant",
            name: "Original Name");
        var modifiedTenant = baseTenant.CreateUpdated(
            "modified-tenant",
            "Updated Name",
            null,
            null,
            null,
            isActive: true);
        var tenantId = modifiedTenant.GetGuidId();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(modifiedTenant);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ModifiedAt.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Updated Name");
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync((Tenant?)null);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe(ErrorCodes.Auth.TenantNotFound);
    }

    [Fact]
    public async Task Handle_WhenTenantIsDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var tenant = CreateTestTenant(
            identifier: "deleted-tenant",
            name: "Deleted Tenant",
            isDeleted: true);
        var tenantId = tenant.GetGuidId();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe(ErrorCodes.Auth.TenantNotFound);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassTenantIdToStore()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var tenantId = tenant.GetGuidId();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _tenantStoreMock.Verify(
            x => x.GetAsync(tenantId.ToString()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNewlyCreatedTenant_ShouldHaveNullModifiedAt()
    {
        // Arrange
        var tenant = CreateTestTenant(
            identifier: "new-tenant",
            name: "New Tenant");
        var tenantId = tenant.GetGuidId();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ModifiedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithGuidId_ShouldConvertToStringForStore()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync((Tenant?)null);

        var query = new GetTenantByIdQuery(tenantId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _tenantStoreMock.Verify(
            x => x.GetAsync(It.Is<string>(s => s == tenantId.ToString())),
            Times.Once);
    }

    #endregion
}
