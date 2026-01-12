using NOIR.Application.Features.Tenants.Commands.CreateTenant;
using NOIR.Application.Features.Tenants.DTOs;

namespace NOIR.Application.UnitTests.Features.Tenants;

/// <summary>
/// Unit tests for CreateTenantCommandHandler.
/// Tests tenant creation scenarios with mocked dependencies.
/// </summary>
public class CreateTenantCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new CreateTenantCommandHandler(
            _tenantStoreMock.Object,
            _localizationServiceMock.Object);
    }

    private static Tenant CreateTestTenant(
        string identifier = "test-tenant",
        string name = "Test Tenant",
        bool isActive = true)
    {
        return Tenant.Create(identifier, name, isActive);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string identifier = "new-tenant";
        const string name = "New Tenant";

        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(identifier))
            .ReturnsAsync((Tenant?)null);

        _tenantStoreMock
            .Setup(x => x.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(true);

        var command = new CreateTenantCommand(identifier, name);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Identifier.Should().Be(identifier.ToLowerInvariant());
        result.Value.Name.Should().Be(name);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInactiveTenant_ShouldCreateInactiveTenant()
    {
        // Arrange
        const string identifier = "inactive-tenant";
        const string name = "Inactive Tenant";

        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(identifier))
            .ReturnsAsync((Tenant?)null);

        _tenantStoreMock
            .Setup(x => x.AddAsync(It.Is<Tenant>(t => !t.IsActive)))
            .ReturnsAsync(true);

        var command = new CreateTenantCommand(identifier, name, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldNormalizeIdentifier()
    {
        // Arrange
        const string identifier = "NEW-TENANT";
        const string name = "New Tenant";

        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(identifier))
            .ReturnsAsync((Tenant?)null);

        _tenantStoreMock
            .Setup(x => x.AddAsync(It.Is<Tenant>(t => t.Identifier == identifier.ToLowerInvariant())))
            .ReturnsAsync(true);

        var command = new CreateTenantCommand(identifier, name);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Identifier.Should().Be("new-tenant");
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenIdentifierExists_ShouldReturnConflict()
    {
        // Arrange
        const string identifier = "existing-tenant";
        const string name = "New Tenant";

        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(identifier))
            .ReturnsAsync(CreateTestTenant(identifier, "Existing Tenant"));

        var command = new CreateTenantCommand(identifier, name);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.AlreadyExists);
        _tenantStoreMock.Verify(
            x => x.AddAsync(It.IsAny<Tenant>()),
            Times.Never);
    }

    #endregion

    #region Creation Failure Scenarios

    [Fact]
    public async Task Handle_WhenAddFails_ShouldReturnInternalError()
    {
        // Arrange
        const string identifier = "new-tenant";
        const string name = "New Tenant";

        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(identifier))
            .ReturnsAsync((Tenant?)null);

        _tenantStoreMock
            .Setup(x => x.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(false);

        var command = new CreateTenantCommand(identifier, name);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.InternalError);
    }

    #endregion
}
