namespace NOIR.Application.UnitTests.Features.Promotions;

/// <summary>
/// Unit tests for ValidatePromoCodeQueryHandler.
/// Tests promo code validation scenarios with mocked dependencies.
/// </summary>
public class ValidatePromoCodeQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Promotion, Guid>> _promotionRepositoryMock;
    private readonly ValidatePromoCodeQueryHandler _handler;

    public ValidatePromoCodeQueryHandlerTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<Promotion, Guid>>();

        _handler = new ValidatePromoCodeQueryHandler(_promotionRepositoryMock.Object);
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

    #endregion

    #region Valid Code Scenarios

    [Fact]
    public async Task Handle_WithValidCode_ShouldReturnValid()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 20m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("TESTCODE", 500000m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Message.Should().Be("Promotion code is valid.");
        result.Value.DiscountAmount.Should().Be(100000m); // 20% of 500000
        result.Value.Code.Should().Be("TESTCODE");
        result.Value.DiscountType.Should().Be(DiscountType.Percentage);
        result.Value.DiscountValue.Should().Be(20m);
    }

    [Fact]
    public async Task Handle_WithFixedAmountDiscount_ShouldCalculateCorrectly()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FixedAmount,
            discountValue: 50000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("TESTCODE", 500000m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(50000m);
    }

    [Fact]
    public async Task Handle_WithMaxDiscountCap_ShouldReturnCappedAmount()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 50m,
            maxDiscountAmount: 100000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("TESTCODE", 1000000m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(100000m); // Capped
        result.Value.MaxDiscountAmount.Should().Be(100000m);
    }

    #endregion

    #region Invalid Code Scenarios

    [Fact]
    public async Task Handle_WhenCodeNotFound_ShouldReturnInvalid()
    {
        // Arrange
        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var query = new ValidatePromoCodeQuery("INVALID", 500000m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Returns Success with IsValid=false, not Failure
        result.Value.IsValid.Should().BeFalse();
        result.Value.Message.Should().Be("Invalid promotion code.");
        result.Value.Code.Should().Be("INVALID");
    }

    [Fact]
    public async Task Handle_WhenPromotionIsDraft_ShouldReturnNotActive()
    {
        // Arrange - Promotion not activated (Draft)
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
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("DRAFTCODE", 500000m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Message.Should().Be("This promotion is not yet active.");
    }

    [Fact]
    public async Task Handle_WhenPromotionIsCancelled_ShouldReturnCancelled()
    {
        // Arrange
        var promotion = Promotion.Create(
            "Cancelled Promo",
            "CANCELLED",
            PromotionType.VoucherCode,
            DiscountType.Percentage,
            10m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            tenantId: "tenant-123");
        promotion.Activate();
        promotion.Cancel();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("CANCELLED", 500000m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Message.Should().Be("This promotion has been cancelled.");
    }

    [Fact]
    public async Task Handle_WhenMinOrderValueNotMet_ShouldReturnInvalid()
    {
        // Arrange
        var promotion = CreateActivePromotion(minOrderValue: 500000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("TESTCODE", 100000m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Message.Should().Contain("Minimum order value");
    }

    #endregion

    #region User Limit Scenarios

    [Fact]
    public async Task Handle_WhenUserExceedsPerUserLimit_ShouldReturnInvalid()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitPerUser: 1);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        // Return the same promotion with usages loaded (simulate usage by user)
        var promotionWithUsages = CreateActivePromotion(usageLimitPerUser: 1);
        // Add a usage for this user
        promotionWithUsages.Usages.Add(new PromotionUsage(
            Guid.NewGuid(),
            promotionWithUsages.Id,
            "user-123",
            Guid.NewGuid(),
            10000m,
            "tenant-123"));

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotionWithUsages);

        var query = new ValidatePromoCodeQuery("TESTCODE", 500000m, UserId: "user-123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Message.Should().Contain("usage limit");
    }

    [Fact]
    public async Task Handle_WithoutUserId_ShouldSkipPerUserCheck()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitPerUser: 1);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("TESTCODE", 500000m, UserId: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();

        // Should NOT have called the PromotionByIdSpec for usage check
        _promotionRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PromotionByIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var promotion = CreateActivePromotion();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                token))
            .ReturnsAsync(promotion);

        var query = new ValidatePromoCodeQuery("TESTCODE", 500000m);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PromotionByCodeSpec>(), token),
            Times.Once);
    }

    #endregion
}
