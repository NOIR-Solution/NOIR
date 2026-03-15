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
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
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
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Summer Sale");
        result.Value.Code.ShouldBe("SUMMER2026");
        result.Value.DiscountType.ShouldBe(DiscountType.Percentage);
        result.Value.DiscountValue.ShouldBe(20m);
        result.Value.Status.ShouldBe(PromotionStatus.Draft);
        result.Value.IsActive.ShouldBe(false);

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
        result.IsSuccess.ShouldBe(true);
        result.Value.Description.ShouldBe("Summer promotion with 20% off");
        result.Value.MaxDiscountAmount.ShouldBe(100000m);
        result.Value.MinOrderValue.ShouldBe(500000m);
        result.Value.MinItemQuantity.ShouldBe(2);
        result.Value.UsageLimitTotal.ShouldBe(1000);
        result.Value.UsageLimitPerUser.ShouldBe(3);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.ApplyLevel.ShouldBe(PromotionApplyLevel.Product);
        result.Value.ProductIds.Count().ShouldBe(2);
        result.Value.ProductIds.ShouldContain(productId1);
        result.Value.ProductIds.ShouldContain(productId2);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.ApplyLevel.ShouldBe(PromotionApplyLevel.Category);
        result.Value.CategoryIds.Count().ShouldBe(2);
        result.Value.CategoryIds.ShouldContain(categoryId1);
        result.Value.CategoryIds.ShouldContain(categoryId2);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.DiscountType.ShouldBe(DiscountType.FixedAmount);
        result.Value.DiscountValue.ShouldBe(50000m);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Code.ShouldBe("SUMMER2026");
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
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-PROMO-001");

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
        result.IsSuccess.ShouldBe(true);
        capturedPromotion.ShouldNotBeNull();
        capturedPromotion!.TenantId.ShouldBe(tenantId);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.ProductIds.ShouldBeEmpty();
        result.Value.CategoryIds.ShouldBeEmpty();
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
        result.IsSuccess.ShouldBe(true);
        result.Value.CurrentUsageCount.ShouldBe(0);
    }

    #endregion
}
