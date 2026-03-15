namespace NOIR.Application.UnitTests.Features.Promotions;

/// <summary>
/// Unit tests for UpdatePromotionCommandHandler.
/// Tests promotion update scenarios with mocked dependencies.
/// </summary>
public class UpdatePromotionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Promotion, Guid>> _promotionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdatePromotionCommandHandler _handler;

    public UpdatePromotionCommandHandlerTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<Promotion, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdatePromotionCommandHandler(
            _promotionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static Promotion CreateTestPromotion(
        string name = "Test Promo",
        string code = "TESTCODE",
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 10m)
    {
        return Promotion.Create(
            name,
            code,
            PromotionType.VoucherCode,
            discountType,
            discountValue,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(30),
            tenantId: "tenant-123");
    }

    private static UpdatePromotionCommand CreateValidCommand(
        Guid? id = null,
        string name = "Updated Promo",
        string code = "UPDATEDCODE",
        string? description = null,
        PromotionType promotionType = PromotionType.VoucherCode,
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 25m,
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
        return new UpdatePromotionCommand(
            id ?? Guid.NewGuid(),
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
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(id: promotionId, code: "TESTCODE");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Updated Promo");
        result.Value.Code.ShouldBe("TESTCODE");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSameCode_ShouldNotCheckForDuplicate()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion(code: "SAMECODE");

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var command = CreateValidCommand(id: promotionId, code: "SAMECODE");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        _promotionRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PromotionByCodeSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNewUniqueCode_ShouldSucceed()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion(code: "OLDCODE");

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var command = CreateValidCommand(id: promotionId, code: "NEWCODE");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Code.ShouldBe("NEWCODE");
    }

    [Fact]
    public async Task Handle_WithAllFields_ShouldUpdateAllProperties()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var startDate = DateTimeOffset.UtcNow.AddDays(5);
        var endDate = DateTimeOffset.UtcNow.AddDays(60);

        var command = new UpdatePromotionCommand(
            promotionId,
            "Flash Sale",
            "FLASH2026",
            "Flash sale description",
            PromotionType.FlashSale,
            DiscountType.FixedAmount,
            100000m,
            startDate,
            endDate,
            PromotionApplyLevel.Product,
            50000m,
            200000m,
            1,
            500,
            2,
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Flash Sale");
        result.Value.Code.ShouldBe("FLASH2026");
        result.Value.Description.ShouldBe("Flash sale description");
        result.Value.PromotionType.ShouldBe(PromotionType.FlashSale);
        result.Value.DiscountType.ShouldBe(DiscountType.FixedAmount);
        result.Value.DiscountValue.ShouldBe(100000m);
        result.Value.ApplyLevel.ShouldBe(PromotionApplyLevel.Product);
        result.Value.MaxDiscountAmount.ShouldBe(50000m);
        result.Value.MinOrderValue.ShouldBe(200000m);
        result.Value.MinItemQuantity.ShouldBe(1);
        result.Value.UsageLimitTotal.ShouldBe(500);
        result.Value.UsageLimitPerUser.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WithProductIds_ShouldUpdateProductTargeting()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var newProductId = Guid.NewGuid();
        var command = CreateValidCommand(
            id: promotionId,
            code: "TESTCODE",
            productIds: [newProductId]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProductIds.ShouldContain(newProductId);
    }

    [Fact]
    public async Task Handle_WithCategoryIds_ShouldUpdateCategoryTargeting()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var newCategoryId = Guid.NewGuid();
        var command = CreateValidCommand(
            id: promotionId,
            code: "TESTCODE",
            categoryIds: [newCategoryId]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CategoryIds.ShouldContain(newCategoryId);
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

        var command = CreateValidCommand(id: promotionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-PROMO-002");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenNewCodeExistsForDifferentPromotion_ShouldReturnConflict()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion(code: "OLDCODE");
        var conflictingPromotion = CreateTestPromotion(code: "TAKENCODE");

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByCodeSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictingPromotion);

        var command = CreateValidCommand(id: promotionId, code: "TAKENCODE");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-PROMO-001");

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

        var command = CreateValidCommand(id: promotionId, code: "TESTCODE");

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

    [Fact]
    public async Task Handle_WithNullProductAndCategoryIds_ShouldClearTargeting()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        // Add some initial products
        existingPromotion.AddProduct(Guid.NewGuid());

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var command = CreateValidCommand(
            id: promotionId,
            code: "TESTCODE",
            productIds: null,
            categoryIds: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProductIds.ShouldBeEmpty();
    }

    #endregion
}
