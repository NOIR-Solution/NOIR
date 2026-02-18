namespace NOIR.Application.UnitTests.Features.Promotions;

/// <summary>
/// Unit tests for GetPromotionByIdQueryHandler.
/// Tests promotion retrieval by ID with mocked dependencies.
/// </summary>
public class GetPromotionByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Promotion, Guid>> _promotionRepositoryMock;
    private readonly GetPromotionByIdQueryHandler _handler;

    public GetPromotionByIdQueryHandlerTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<Promotion, Guid>>();

        _handler = new GetPromotionByIdQueryHandler(_promotionRepositoryMock.Object);
    }

    private static Promotion CreateTestPromotion(
        string name = "Test Promo",
        string code = "TESTCODE",
        PromotionType promotionType = PromotionType.VoucherCode,
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 20m)
    {
        return Promotion.Create(
            name,
            code,
            promotionType,
            discountType,
            discountValue,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(30),
            tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenPromotionExists_ShouldReturnPromotionDto()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var query = new GetPromotionByIdQuery(promotionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Promo");
        result.Value.Code.Should().Be("TESTCODE");
        result.Value.PromotionType.Should().Be(PromotionType.VoucherCode);
        result.Value.DiscountType.Should().Be(DiscountType.Percentage);
        result.Value.DiscountValue.Should().Be(20m);
        result.Value.Status.Should().Be(PromotionStatus.Draft);
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithActivePromotion_ShouldReturnCorrectStatus()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        existingPromotion.Activate();

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var query = new GetPromotionByIdQuery(promotionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PromotionStatus.Active);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProductTargeting_ShouldReturnProductIds()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        existingPromotion.AddProduct(productId1);
        existingPromotion.AddProduct(productId2);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var query = new GetPromotionByIdQuery(promotionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductIds.Should().HaveCount(2);
        result.Value.ProductIds.Should().Contain(productId1);
        result.Value.ProductIds.Should().Contain(productId2);
    }

    [Fact]
    public async Task Handle_WithCategoryTargeting_ShouldReturnCategoryIds()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion();
        var categoryId = Guid.NewGuid();
        existingPromotion.AddCategory(categoryId);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var query = new GetPromotionByIdQuery(promotionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().HaveCount(1);
        result.Value.CategoryIds.Should().Contain(categoryId);
    }

    [Fact]
    public async Task Handle_WithFixedAmountDiscount_ShouldReturnCorrectValues()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var existingPromotion = CreateTestPromotion(
            discountType: DiscountType.FixedAmount,
            discountValue: 50000m);

        _promotionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PromotionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromotion);

        var query = new GetPromotionByIdQuery(promotionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountType.Should().Be(DiscountType.FixedAmount);
        result.Value.DiscountValue.Should().Be(50000m);
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
                It.IsAny<PromotionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var query = new GetPromotionByIdQuery(promotionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PROMO-002");
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
                It.IsAny<PromotionByIdSpec>(),
                token))
            .ReturnsAsync(existingPromotion);

        var query = new GetPromotionByIdQuery(promotionId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PromotionByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
