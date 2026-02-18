namespace NOIR.Application.UnitTests.Features.Promotions;

/// <summary>
/// Unit tests for ActivatePromotionCommandHandler.
/// Tests promotion activation scenarios with mocked dependencies.
/// </summary>
public class ActivatePromotionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Promotion, Guid>> _promotionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ActivatePromotionCommandHandler _handler;

    public ActivatePromotionCommandHandlerTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<Promotion, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ActivatePromotionCommandHandler(
            _promotionRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Promotion CreateTestPromotion(
        string name = "Test Promo",
        string code = "TESTCODE")
    {
        return Promotion.Create(
            name,
            code,
            PromotionType.VoucherCode,
            DiscountType.Percentage,
            10m,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(30),
            tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenPromotionIsDraft_ShouldActivateSuccessfully()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        // Promotion starts in Draft status

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ActivatePromotionCommand(promotionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.Status.Should().Be(PromotionStatus.Active);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPromotionIsScheduled_ShouldActivateSuccessfully()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        // Scheduled is not Cancelled or Expired, so Activate() should work

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ActivatePromotionCommand(promotionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PromotionStatus.Active);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenPromotionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var promotionId = Guid.NewGuid();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var command = new ActivatePromotionCommand(promotionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-002");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Invalid Status Scenarios

    [Fact]
    public async Task Handle_WhenPromotionIsCancelled_ShouldReturnFailure()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        // First activate, then cancel
        existingPromotion.Activate();
        existingPromotion.Cancel();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var command = new ActivatePromotionCommand(promotionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-003");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(existingPromotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new ActivatePromotionCommand(promotionId);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PromotionByIdForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
