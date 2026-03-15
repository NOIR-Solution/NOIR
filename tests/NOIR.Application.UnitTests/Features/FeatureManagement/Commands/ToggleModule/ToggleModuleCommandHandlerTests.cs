using NOIR.Application.Features.FeatureManagement.Commands.ToggleModule;
using NOIR.Application.Features.FeatureManagement.DTOs;

namespace NOIR.Application.UnitTests.Features.FeatureManagement.Commands.ToggleModule;

/// <summary>
/// Unit tests for ToggleModuleCommandHandler.
/// Tests tenant admin operations for toggling modules on/off.
/// </summary>
public class ToggleModuleCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFeatureChecker> _featureCheckerMock;
    private readonly Mock<IFeatureCacheInvalidator> _cacheInvalidatorMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly ToggleModuleCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestFeatureName = "Ecommerce.Products";

    public ToggleModuleCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _featureCheckerMock = new Mock<IFeatureChecker>();
        _cacheInvalidatorMock = new Mock<IFeatureCacheInvalidator>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new ToggleModuleCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _featureCheckerMock.Object,
            _cacheInvalidatorMock.Object,
            _currentUserMock.Object);
    }

    private void SetupDbSet(List<TenantModuleState> states)
    {
        var mockDbSet = states.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TenantModuleStates).Returns(mockDbSet.Object);
    }

    private void SetupFeatureAvailable(bool isAvailable = true)
    {
        _featureCheckerMock.Setup(x => x.GetStateAsync(TestFeatureName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EffectiveFeatureState(isAvailable, true, isAvailable, false));
    }

    #endregion

    #region Tenant Context Validation

    [Fact]
    public async Task Handle_WhenNoTenantContext_ShouldReturnForbidden()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenEmptyTenantContext_ShouldReturnForbidden()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns(string.Empty);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    #endregion

    #region Feature Availability Check

    [Fact]
    public async Task Handle_WhenFeatureNotAvailable_ShouldReturnForbidden()
    {
        // Arrange
        SetupFeatureAvailable(false);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    #endregion

    #region Create New State

    [Fact]
    public async Task Handle_WhenNoStateExists_ShouldCreateNewState()
    {
        // Arrange
        SetupFeatureAvailable();
        SetupDbSet([]);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.FeatureName.ShouldBe(TestFeatureName);
        result.Value.IsEnabled.ShouldBe(true);
        _dbContextMock.Verify(x => x.TenantModuleStates.AddAsync(
            It.Is<TenantModuleState>(s => s.FeatureName == TestFeatureName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoStateExists_ShouldCreateWithEnabledFalse()
    {
        // Arrange
        SetupFeatureAvailable();
        SetupDbSet([]);
        var command = new ToggleModuleCommand(TestFeatureName, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsEnabled.ShouldBe(false);
        result.Value.IsEffective.ShouldBe(false);
    }

    #endregion

    #region Update Existing State

    [Fact]
    public async Task Handle_WhenStateExists_ShouldToggleEnabled()
    {
        // Arrange
        SetupFeatureAvailable();
        var existingState = TenantModuleState.Create(TestFeatureName, TestTenantId);
        existingState.SetEnabled(false);
        SetupDbSet([existingState]);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsEnabled.ShouldBe(true);
        _dbContextMock.Verify(x => x.TenantModuleStates.AddAsync(
            It.IsAny<TenantModuleState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ShouldDisableModule()
    {
        // Arrange
        SetupFeatureAvailable();
        var existingState = TenantModuleState.Create(TestFeatureName, TestTenantId);
        existingState.SetEnabled(true);
        SetupDbSet([existingState]);
        var command = new ToggleModuleCommand(TestFeatureName, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsEnabled.ShouldBe(false);
    }

    #endregion

    #region Effective State Uses FeatureChecker

    [Fact]
    public async Task Handle_ShouldUseFeatureCheckerIsAvailableInDto()
    {
        // Arrange
        _featureCheckerMock.Setup(x => x.GetStateAsync(TestFeatureName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EffectiveFeatureState(true, true, true, false));
        SetupDbSet([]);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.IsAvailable.ShouldBe(true);
        result.Value.IsEffective.ShouldBe(true);
    }

    #endregion

    #region Side Effects

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        SetupFeatureAvailable();
        SetupDbSet([]);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldInvalidateCache()
    {
        // Arrange
        SetupFeatureAvailable();
        SetupDbSet([]);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cacheInvalidatorMock.Verify(
            x => x.InvalidateAsync(TestTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenForbidden_ShouldNotSaveOrInvalidateCache()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        var command = new ToggleModuleCommand(TestFeatureName, true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheInvalidatorMock.Verify(
            x => x.InvalidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
