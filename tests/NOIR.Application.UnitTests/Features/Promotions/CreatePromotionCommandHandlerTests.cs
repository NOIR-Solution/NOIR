namespace NOIR.Application.UnitTests.Features.Promotions;

/// <summary>
/// Unit tests for CreatePromotionCommandHandler.
/// Tests promotion creation scenarios with mocked dependencies.
/// </summary>
public class CreatePromotionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Promotion, Guid>> _promotionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreatePromotionCommandHandler _handler;

    public CreatePromotionCommandHandlerTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<Promotion, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        _handler = new CreatePromotionCommandHandler(
            _promotionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreatePromotionCommand CreateValidCommand(
        string name = "Summer Sale",
        string code = "SUMMER2026",
        string? description = null,
        PromotionType promotionType = PromotionType.VoucherCode,
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 20m,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        PromotionApplyLevel applyLevel = PromotionApplyLevel.Cart,
        decimal? maxDiscountAmount = null,
        decimal? minOrderValue = null,
        int? minItemQuantity = null,
        int? usageLimitTotal = null,
        int? usageLimitPerUser = null,
        List<Guid>? productIds = null,
        List<Guid>? categoryIds = null)
    {
        return new CreatePromotionCommand(
            name,
            code,
            description,
            promotionType,
            discountType,
            discountValue,
            startDate ?? DateTimeOffset.UtcNow.AddDays(1),
            endDate ?? DateTimeOffset.UtcNow.AddDays(30),
            applyLevel,
            maxDiscountAmount,
            minOrderValue,
            minItemQuantity,
            usageLimitTotal,
            usageLimitPerUser,
            productIds,
            categoryIds);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var command = CreateValidCommand();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Summer Sale");
        result.Value.Code.Should().Be("SUMMER2026");
        result.Value.DiscountType.Should().Be(DiscountType.Percentage);
        result.Value.DiscountValue.Should().Be(20m);
        result.Value.Status.Should().Be(PromotionStatus.Draft);
        result.Value.IsActive.Should().BeFalse();

        _promotionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDescriptionAndLimits_ShouldSetAllProperties()
    {
        // Arrange
        var command = CreateValidCommand(
            description: "Summer promotion with 20% off",
            maxDiscountAmount: 100000m,
            minOrderValue: 500000m,
            minItemQuantity: 2,
            usageLimitTotal: 1000,
            usageLimitPerUser: 3);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Summer promotion with 20% off");
        result.Value.MaxDiscountAmount.Should().Be(100000m);
        result.Value.MinOrderValue.Should().Be(500000m);
        result.Value.MinItemQuantity.Should().Be(2);
        result.Value.UsageLimitTotal.Should().Be(1000);
        result.Value.UsageLimitPerUser.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithProductIds_ShouldAddProductTargeting()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var command = CreateValidCommand(
            applyLevel: PromotionApplyLevel.Product,
            productIds: [productId1, productId2]);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApplyLevel.Should().Be(PromotionApplyLevel.Product);
        result.Value.ProductIds.Should().HaveCount(2);
        result.Value.ProductIds.Should().Contain(productId1);
        result.Value.ProductIds.Should().Contain(productId2);
    }

    [Fact]
    public async Task Handle_WithCategoryIds_ShouldAddCategoryTargeting()
    {
        // Arrange
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();
        var command = CreateValidCommand(
            applyLevel: PromotionApplyLevel.Category,
            categoryIds: [categoryId1, categoryId2]);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApplyLevel.Should().Be(PromotionApplyLevel.Category);
        result.Value.CategoryIds.Should().HaveCount(2);
        result.Value.CategoryIds.Should().Contain(categoryId1);
        result.Value.CategoryIds.Should().Contain(categoryId2);
    }

    [Fact]
    public async Task Handle_WithFixedAmountDiscount_ShouldSucceed()
    {
        // Arrange
        var command = CreateValidCommand(
            discountType: DiscountType.FixedAmount,
            discountValue: 50000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountType.Should().Be(DiscountType.FixedAmount);
        result.Value.DiscountValue.Should().Be(50000m);
    }

    [Fact]
    public async Task Handle_CodeShouldBeUppercased()
    {
        // Arrange
        var command = CreateValidCommand(code: "summer2026");

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("SUMMER2026");
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand(code: "EXISTING");

        var existingPromotion = Promotion.Create(
            "Existing Promo",
            "EXISTING",
            PromotionType.VoucherCode,
            DiscountType.Percentage,
            10m,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(30),
            tenantId: "tenant-123");

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-001");

        _promotionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        const string tenantId = "tenant-abc";
        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = CreateValidCommand();

        Promotion? capturedPromotion = null;

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .Callback<Promotion, CancellationToken>((promo, _) => capturedPromotion = promo)
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedPromotion.Should().NotBeNull();
        capturedPromotion!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var command = CreateValidCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                token))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), token))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PromotionByCodeSpec>(), token),
            Times.Once);

        _promotionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Promotion>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyProductAndCategoryIds_ShouldNotAddTargeting()
    {
        // Arrange
        var command = CreateValidCommand(
            productIds: [],
            categoryIds: []);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductIds.Should().BeEmpty();
        result.Value.CategoryIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InitialUsageCountShouldBeZero()
    {
        // Arrange
        var command = CreateValidCommand();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        _promotionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion promo, CancellationToken _) => promo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentUsageCount.Should().Be(0);
    }

    #endregion
}
