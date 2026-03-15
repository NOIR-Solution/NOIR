namespace NOIR.Application.UnitTests.Features.Promotions;

/// <summary>
/// Unit tests for GetPromotionsQueryHandler.
/// Tests paged promotion list retrieval with mocked dependencies.
/// </summary>
public class GetPromotionsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Promotion, Guid>> _promotionRepositoryMock;
    private readonly GetPromotionsQueryHandler _handler;

    public GetPromotionsQueryHandlerTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<Promotion, Guid>>();

        _handler = new GetPromotionsQueryHandler(_promotionRepositoryMock.Object);
    }

    private static Promotion CreateTestPromotion(
        string name,
        string code,
        PromotionType promotionType = PromotionType.VoucherCode)
    {
        return Promotion.Create(
            name,
            code,
            promotionType,
            DiscountType.Percentage,
            10m,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(30),
            tenantId: "tenant-123");
    }

    private static List<Promotion> CreateTestPromotions(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestPromotion($"Promo {i}", $"CODE{i}"))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var promotions = CreateTestPromotions(5);

        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotions);

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetPromotionsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(5);
        result.Value.TotalCount.ShouldBe(5);
        result.Value.PageIndex.ShouldBe(0);
        result.Value.PageNumber.ShouldBe(1);
        result.Value.PageSize.ShouldBe(20);
        result.Value.TotalPages.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithPaging_ShouldReturnCorrectPage()
    {
        // Arrange - Simulate page 2 of 25 total items
        var page2Promotions = CreateTestPromotions(10);

        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Promotions);

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetPromotionsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(10);
        result.Value.TotalCount.ShouldBe(25);
        result.Value.PageIndex.ShouldBe(1);
        result.Value.PageNumber.ShouldBe(2);
        result.Value.PageSize.ShouldBe(10);
        result.Value.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>());

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPromotionsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.TotalPages.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ShouldMapPromotionsToDto()
    {
        // Arrange
        var promotion = CreateTestPromotion("Flash Sale", "FLASH2026", PromotionType.FlashSale);

        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion> { promotion });

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetPromotionsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var item = result.Value.Items.First();
        item.Name.ShouldBe("Flash Sale");
        item.Code.ShouldBe("FLASH2026");
        item.PromotionType.ShouldBe(PromotionType.FlashSale);
    }

    #endregion

    #region Filter Scenarios

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassToSpecification()
    {
        // Arrange
        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>());

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPromotionsQuery(
            Search: "SUMMER",
            Page: 1,
            PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PromotionsFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _promotionRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<PromotionsCountSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassToSpecification()
    {
        // Arrange
        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>());

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPromotionsQuery(
            Status: PromotionStatus.Active,
            Page: 1,
            PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PromotionsFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPromotionTypeFilter_ShouldPassToSpecification()
    {
        // Arrange
        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>());

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPromotionsQuery(
            PromotionType: PromotionType.FlashSale,
            Page: 1,
            PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PromotionsFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_ShouldPassToSpecification()
    {
        // Arrange
        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>());

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPromotionsQuery(
            FromDate: DateTimeOffset.UtcNow.AddDays(-7),
            ToDate: DateTimeOffset.UtcNow.AddDays(7),
            Page: 1,
            PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PromotionsFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _promotionRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PromotionsFilterSpec>(),
                token))
            .ReturnsAsync(new List<Promotion>());

        _promotionRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PromotionsCountSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetPromotionsQuery(Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _promotionRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PromotionsFilterSpec>(), token),
            Times.Once);

        _promotionRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<PromotionsCountSpec>(), token),
            Times.Once);
    }

    #endregion
}
