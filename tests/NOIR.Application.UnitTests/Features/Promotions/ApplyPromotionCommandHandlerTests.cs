namespace NOIR.Application.UnitTests.Features.Promotions;

/// <summary>
/// Unit tests for ApplyPromotionCommandHandler.
/// Tests promotion application scenarios with mocked dependencies.
/// </summary>
public class ApplyPromotionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Promotion, Guid>> _promotionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ApplyPromotionCommandHandler _handler;

    public ApplyPromotionCommandHandlerTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<Promotion, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ApplyPromotionCommandHandler(
            _promotionRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Promotion CreateActivePromotion(
        string code = "TESTCODE",
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 20m,
        decimal? maxDiscountAmount = null,
        decimal? minOrderValue = null,
        int? usageLimitTotal = null,
        int? usageLimitPerUser = null)
    {
        var promotion = Promotion.Create(
            "Test Promo",
            code,
            PromotionType.VoucherCode,
            discountType,
            discountValue,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            maxDiscountAmount: maxDiscountAmount,
            minOrderValue: minOrderValue,
            usageLimitTotal: usageLimitTotal,
            usageLimitPerUser: usageLimitPerUser,
            tenantId: "tenant-123");
        promotion.Activate();
        return promotion;
    }

    private static ApplyPromotionCommand CreateValidCommand(
        string code = "TESTCODE",
        Guid? orderId = null,
        decimal orderTotal = 1000000m,
        string? userId = "user-123")
    {
        return new ApplyPromotionCommand(
            code,
            orderId ?? Guid.NewGuid(),
            orderTotal)
        { UserId = userId };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidPromoCode_ShouldApplySuccessfully()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 20m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(orderTotal: 500000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(100000m); // 20% of 500000
        result.Value.UserId.Should().Be("user-123");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFixedAmountDiscount_ShouldApplyCorrectAmount()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FixedAmount,
            discountValue: 50000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(orderTotal: 500000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(50000m);
    }

    [Fact]
    public async Task Handle_WithMaxDiscountCap_ShouldCapDiscount()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 50m,
            maxDiscountAmount: 100000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(orderTotal: 1000000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // 50% of 1000000 = 500000, but capped at 100000
        result.Value.DiscountAmount.Should().Be(100000m);
    }

    [Fact]
    public async Task Handle_ShouldIncrementUsageCount()
    {
        // Arrange
        var promotion = CreateActivePromotion();
        var initialCount = promotion.CurrentUsageCount;

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        promotion.CurrentUsageCount.Should().Be(initialCount + 1);
    }

    #endregion

    #region Authentication Scenarios

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand(userId: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-005");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand(userId: "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-005");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenPromotionCodeNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var command = CreateValidCommand(code: "INVALID");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-006");
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WhenPromotionIsNotValid_ShouldReturnFailure()
    {
        // Arrange - Promotion is in Draft status (not activated)
        var promotion = Promotion.Create(
            "Draft Promo",
            "DRAFTCODE",
            PromotionType.VoucherCode,
            DiscountType.Percentage,
            10m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            tenantId: "tenant-123");

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var command = CreateValidCommand(code: "DRAFTCODE");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-007");
    }

    [Fact]
    public async Task Handle_WhenMinOrderValueNotMet_ShouldReturnFailure()
    {
        // Arrange
        var promotion = CreateActivePromotion(minOrderValue: 500000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var command = CreateValidCommand(orderTotal: 100000m); // Below minimum

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-009");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WhenDiscountExceedsOrderTotal_ShouldCapAtOrderTotal()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FixedAmount,
            discountValue: 500000m); // Discount greater than order total

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(orderTotal: 200000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(200000m); // Capped at order total
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var promotion = CreateActivePromotion();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                token))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, token);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PromotionByCodeWithUsagesSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFreeShippingDiscount_ShouldReturnZeroDiscount()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FreeShipping,
            discountValue: 1m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeWithUsagesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(orderTotal: 500000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(0m); // Free shipping handled separately
    }

    #endregion
}
