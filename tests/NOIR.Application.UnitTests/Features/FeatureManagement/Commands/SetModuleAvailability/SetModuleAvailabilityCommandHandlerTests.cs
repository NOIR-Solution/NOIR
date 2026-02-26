using NOIR.Application.Features.FeatureManagement.Commands.SetModuleAvailability;
using NOIR.Application.Features.FeatureManagement.DTOs;

namespace NOIR.Application.UnitTests.Features.FeatureManagement.Commands.SetModuleAvailability;

/// <summary>
/// Unit tests for SetModuleAvailabilityCommandHandler.
/// Tests platform admin operations for setting module availability per tenant.
/// </summary>
public class SetModuleAvailabilityCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFeatureCacheInvalidator> _cacheInvalidatorMock;
    private readonly SetModuleAvailabilityCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestFeatureName = "Ecommerce.Products";

    public SetModuleAvailabilityCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheInvalidatorMock = new Mock<IFeatureCacheInvalidator>();

        _handler = new SetModuleAvailabilityCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _cacheInvalidatorMock.Object);
    }

    private void SetupDbSet(List<TenantModuleState> states)
    {
        var mockDbSet = states.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TenantModuleStates).Returns(mockDbSet.Object);
    }

    #endregion

    #region Create New State

    [Fact]
    public async Task Handle_WhenNoStateExists_ShouldCreateNewState()
    {
        // Arrange
        SetupDbSet([]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FeatureName.Should().Be(TestFeatureName);
        result.Value.IsAvailable.Should().BeTrue();
        _dbContextMock.Verify(x => x.TenantModuleStates.AddAsync(
            It.Is<TenantModuleState>(s => s.FeatureName == TestFeatureName && s.TenantId == TestTenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoStateExists_ShouldCreateWithAvailabilityFalse()
    {
        // Arrange
        SetupDbSet([]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeFalse();
        result.Value.IsEffective.Should().BeFalse();
    }

    #endregion

    #region Update Existing State

    [Fact]
    public async Task Handle_WhenStateExists_ShouldUpdateAvailability()
    {
        // Arrange
        var existingState = TenantModuleState.Create(TestFeatureName, TestTenantId);
        existingState.SetAvailability(false);
        SetupDbSet([existingState]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
        _dbContextMock.Verify(x => x.TenantModuleStates.AddAsync(
            It.IsAny<TenantModuleState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ShouldSetAvailabilityToFalse()
    {
        // Arrange
        var existingState = TenantModuleState.Create(TestFeatureName, TestTenantId);
        existingState.SetAvailability(true);
        SetupDbSet([existingState]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeFalse();
    }

    #endregion

    #region Effective State Calculation

    [Fact]
    public async Task Handle_WhenAvailableAndEnabled_ShouldReturnIsEffectiveTrue()
    {
        // Arrange
        var existingState = TenantModuleState.Create(TestFeatureName, TestTenantId);
        existingState.SetEnabled(true);
        SetupDbSet([existingState]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.IsEffective.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotAvailableAndEnabled_ShouldReturnIsEffectiveFalse()
    {
        // Arrange
        var existingState = TenantModuleState.Create(TestFeatureName, TestTenantId);
        existingState.SetEnabled(true);
        SetupDbSet([existingState]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.IsEffective.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAvailableAndNotEnabled_ShouldReturnIsEffectiveFalse()
    {
        // Arrange
        var existingState = TenantModuleState.Create(TestFeatureName, TestTenantId);
        existingState.SetEnabled(false);
        SetupDbSet([existingState]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.IsEffective.Should().BeFalse();
    }

    #endregion

    #region Side Effects

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        SetupDbSet([]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldInvalidateCache()
    {
        // Arrange
        SetupDbSet([]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cacheInvalidatorMock.Verify(
            x => x.InvalidateAsync(TestTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectDto()
    {
        // Arrange
        SetupDbSet([]);
        var command = new SetModuleAvailabilityCommand(TestTenantId, TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.FeatureName.Should().Be(TestFeatureName);
        dto.IsAvailable.Should().BeTrue();
        dto.IsEnabled.Should().BeTrue(); // Default on new TenantModuleState
        dto.IsEffective.Should().BeTrue(); // Available && Enabled
    }

    #endregion
}
