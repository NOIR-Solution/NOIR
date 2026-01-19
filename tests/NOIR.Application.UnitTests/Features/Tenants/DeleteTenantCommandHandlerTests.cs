using NOIR.Application.Features.Tenants.Commands.DeleteTenant;

namespace NOIR.Application.UnitTests.Features.Tenants;

/// <summary>
/// Unit tests for DeleteTenantCommandHandler.
/// Tests tenant soft-deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteTenantCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly DeleteTenantCommandHandler _handler;

    public DeleteTenantCommandHandlerTests()
    {
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new DeleteTenantCommandHandler(
            _tenantStoreMock.Object,
            _localizationServiceMock.Object);
    }

    private static Tenant CreateTestTenant(
        string identifier = "test-tenant",
        string name = "Test Tenant",
        bool isActive = true)
    {
        return Tenant.Create(identifier, name, isActive: isActive);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidTenant_ShouldSucceed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existingTenant = CreateTestTenant(identifier: "tenant-to-delete", name: "Tenant To Delete");

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        _tenantStoreMock
            .Setup(x => x.UpdateAsync(It.Is<Tenant>(t => t.IsDeleted)))
            .ReturnsAsync(true);

        var command = new DeleteTenantCommand(tenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSetIsDeletedFlag()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existingTenant = CreateTestTenant(identifier: "tenant-to-delete");
        Tenant? capturedTenant = null;

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        _tenantStoreMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>()))
            .Callback<Tenant>(t => capturedTenant = t)
            .ReturnsAsync(true);

        var command = new DeleteTenantCommand(tenantId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedTenant.Should().NotBeNull();
        capturedTenant!.IsDeleted.Should().BeTrue();
        capturedTenant.DeletedAt.Should().NotBeNull();
        capturedTenant.ModifiedAt.Should().NotBeNull();
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

        var command = new DeleteTenantCommand(tenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.TenantNotFound);
        _tenantStoreMock.Verify(
            x => x.UpdateAsync(It.IsAny<Tenant>()),
            Times.Never);
    }

    #endregion

    #region Default Tenant Protection Scenarios

    [Fact]
    public async Task Handle_WhenDeletingDefaultTenant_ShouldReturnInvalidState()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var defaultTenant = CreateTestTenant(identifier: "default", name: "Default Tenant");

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(defaultTenant);

        var command = new DeleteTenantCommand(tenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.InvalidState);
        _tenantStoreMock.Verify(
            x => x.UpdateAsync(It.IsAny<Tenant>()),
            Times.Never);
    }

    #endregion

    #region Delete Failure Scenarios

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldReturnInternalError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existingTenant = CreateTestTenant(identifier: "tenant-to-delete");

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        _tenantStoreMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(false);

        var command = new DeleteTenantCommand(tenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.InternalError);
    }

    #endregion
}
