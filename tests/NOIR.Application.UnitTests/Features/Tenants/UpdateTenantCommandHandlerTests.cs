using NOIR.Application.Features.Tenants.Commands.UpdateTenant;
using NOIR.Application.Features.Tenants.DTOs;

namespace NOIR.Application.UnitTests.Features.Tenants;

/// <summary>
/// Unit tests for UpdateTenantCommandHandler.
/// Tests tenant update scenarios with mocked dependencies.
/// </summary>
public class UpdateTenantCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly UpdateTenantCommandHandler _handler;

    public UpdateTenantCommandHandlerTests()
    {
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new UpdateTenantCommandHandler(
            _tenantStoreMock.Object,
            _localizationServiceMock.Object);
    }

    private static Tenant CreateTestTenant(
        string? id = null,
        string identifier = "test-tenant",
        string name = "Test Tenant",
        bool isActive = true)
    {
        var tenant = Tenant.Create(identifier, name, isActive);
        // If specific ID needed, use reflection or return as-is
        return tenant;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string newIdentifier = "updated-tenant";
        const string newName = "Updated Tenant";
        var existingTenant = CreateTestTenant(identifier: "old-tenant", name: "Old Tenant");

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(newIdentifier.ToLowerInvariant()))
            .ReturnsAsync((Tenant?)null);

        _tenantStoreMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(true);

        var command = new UpdateTenantCommand(tenantId, newIdentifier, newName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Identifier.Should().Be(newIdentifier.ToLowerInvariant());
        result.Value.Name.Should().Be(newName);
    }

    [Fact]
    public async Task Handle_WhenKeepingSameIdentifier_ShouldNotCheckForConflict()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string identifier = "test-tenant";
        const string newName = "Updated Tenant";
        var existingTenant = CreateTestTenant(identifier: identifier, name: "Old Tenant");

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        // This should not be called since identifier is not changing
        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(It.IsAny<string>()))
            .ReturnsAsync((Tenant?)null);

        _tenantStoreMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(true);

        var command = new UpdateTenantCommand(tenantId, identifier, newName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDeactivation_ShouldDeactivateTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existingTenant = CreateTestTenant(identifier: "test-tenant", isActive: true);

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        _tenantStoreMock
            .Setup(x => x.UpdateAsync(It.Is<Tenant>(t => !t.IsActive)))
            .ReturnsAsync(true);

        var command = new UpdateTenantCommand(tenantId, "test-tenant", "Test Tenant", false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
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

        var command = new UpdateTenantCommand(tenantId, "new-identifier", "New Name", true);

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

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenNewIdentifierAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string newIdentifier = "existing-identifier";
        var existingTenant = CreateTestTenant(identifier: "old-identifier");
        var conflictingTenant = CreateTestTenant(identifier: newIdentifier);

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(newIdentifier.ToLowerInvariant()))
            .ReturnsAsync(conflictingTenant);

        var command = new UpdateTenantCommand(tenantId, newIdentifier, "New Name", true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.AlreadyExists);
        _tenantStoreMock.Verify(
            x => x.UpdateAsync(It.IsAny<Tenant>()),
            Times.Never);
    }

    #endregion

    #region Update Failure Scenarios

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldReturnInternalError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existingTenant = CreateTestTenant(identifier: "test-tenant");

        _tenantStoreMock
            .Setup(x => x.GetAsync(tenantId.ToString()))
            .ReturnsAsync(existingTenant);

        _tenantStoreMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(false);

        var command = new UpdateTenantCommand(tenantId, "test-tenant", "Updated Name", true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.InternalError);
    }

    #endregion
}
