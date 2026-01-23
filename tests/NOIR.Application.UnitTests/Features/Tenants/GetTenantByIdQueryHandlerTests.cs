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
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(tenant.Id);
        result.Value.Identifier.Should().Be("acme-corp");
        result.Value.Name.Should().Be("Acme Corporation");
        result.Value.IsActive.Should().BeTrue();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
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
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(tenant.Id);
        dto.Identifier.Should().Be(tenant.Identifier);
        dto.Name.Should().Be(tenant.Name);
        dto.IsActive.Should().Be(tenant.IsActive);
        dto.CreatedAt.Should().Be(tenant.CreatedAt);
        dto.ModifiedAt.Should().Be(tenant.ModifiedAt);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ModifiedAt.Should().NotBeNull();
        result.Value.Name.Should().Be("Updated Name");
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
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.TenantNotFound);
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
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.TenantNotFound);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ModifiedAt.Should().BeNull();
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
