using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Queries.GetStockHistory;
using NOIR.Application.Features.Inventory.Specifications;
using NOIR.Domain.Entities.Product;

namespace NOIR.Application.UnitTests.Features.Inventory;

/// <summary>
/// Unit tests for GetStockHistoryQueryHandler.
/// Tests stock history retrieval with pagination scenarios.
/// </summary>
public class GetStockHistoryQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<InventoryMovement, Guid>> _repositoryMock;
    private readonly GetStockHistoryQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestVariantId = Guid.NewGuid();

    public GetStockHistoryQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<InventoryMovement, Guid>>();
        _handler = new GetStockHistoryQueryHandler(_repositoryMock.Object);
    }

    private static GetStockHistoryQuery CreateTestQuery(
        Guid? productId = null,
        Guid? variantId = null,
        int page = 1,
        int pageSize = 20)
    {
        return new GetStockHistoryQuery(
            productId ?? TestProductId,
            variantId ?? TestVariantId,
            page,
            pageSize);
    }

    private static InventoryMovement CreateTestMovement(
        InventoryMovementType movementType = InventoryMovementType.StockIn,
        int quantityBefore = 100,
        int quantityMoved = 10,
        string? reference = null,
        string? notes = null)
    {
        return InventoryMovement.Create(
            TestVariantId,
            TestProductId,
            movementType,
            quantityBefore,
            quantityMoved,
            TestTenantId,
            reference,
            notes,
            "test-user-id",
            "test-correlation-id");
    }

    private static List<InventoryMovement> CreateTestMovements(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestMovement(
                InventoryMovementType.StockIn,
                100 + (i * 10),
                10,
                $"REF-{i:D3}",
                $"Movement {i}"))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnMovements()
    {
        // Arrange
        var movements = CreateTestMovements(5);
        var query = CreateTestQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(5);
        result.Value.TotalCount.ShouldBe(5);
        result.Value.PageNumber.ShouldBe(1);
        result.Value.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var movements = CreateTestMovements(5);
        var query = CreateTestQuery(page: 2, pageSize: 5);

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(5);
        result.Value.TotalCount.ShouldBe(10);
        result.Value.PageNumber.ShouldBe(2);
        result.Value.PageSize.ShouldBe(5);
        result.Value.TotalPages.ShouldBe(2);
        result.Value.HasPreviousPage.ShouldBe(true);
        result.Value.HasNextPage.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldMapAllDtoFields()
    {
        // Arrange
        var movement = CreateTestMovement(
            InventoryMovementType.Adjustment,
            100,
            -5,
            "ORD-001",
            "Manual adjustment");
        var movements = new List<InventoryMovement> { movement };
        var query = CreateTestQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var item = result.Value.Items.First();
        item.Id.ShouldBe(movement.Id);
        item.ProductVariantId.ShouldBe(TestVariantId);
        item.ProductId.ShouldBe(TestProductId);
        item.MovementType.ShouldBe(InventoryMovementType.Adjustment);
        item.QuantityBefore.ShouldBe(100);
        item.QuantityMoved.ShouldBe(-5);
        item.QuantityAfter.ShouldBe(95);
        item.Reference.ShouldBe("ORD-001");
        item.Notes.ShouldBe("Manual adjustment");
        item.UserId.ShouldBe("test-user-id");
        item.CorrelationId.ShouldBe("test-correlation-id");
    }

    [Theory]
    [InlineData(InventoryMovementType.StockIn)]
    [InlineData(InventoryMovementType.StockOut)]
    [InlineData(InventoryMovementType.Adjustment)]
    [InlineData(InventoryMovementType.Return)]
    [InlineData(InventoryMovementType.Reservation)]
    [InlineData(InventoryMovementType.ReservationRelease)]
    [InlineData(InventoryMovementType.Damaged)]
    [InlineData(InventoryMovementType.Expired)]
    public async Task Handle_WithDifferentMovementTypes_ShouldReturnCorrectType(InventoryMovementType movementType)
    {
        // Arrange
        var movement = CreateTestMovement(movementType);
        var movements = new List<InventoryMovement> { movement };
        var query = CreateTestQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.First().MovementType.ShouldBe(movementType);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoMovements_ShouldReturnEmptyList()
    {
        // Arrange
        var query = CreateTestQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryMovement>());

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.TotalPages.ShouldBe(0);
        result.Value.HasPreviousPage.ShouldBe(false);
        result.Value.HasNextPage.ShouldBe(false);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var query = CreateTestQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryMovement>());

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _repositoryMock.Verify(
            x => x.ListAsync(It.IsAny<StockHistoryByVariantIdSpec>(), token),
            Times.Once);
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<StockHistoryByVariantIdCountSpec>(), token),
            Times.Once);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 20)]
    [InlineData(1, 50)]
    [InlineData(2, 10)]
    [InlineData(5, 20)]
    public async Task Handle_WithDifferentPageSizes_ShouldCalculatePaginationCorrectly(int page, int pageSize)
    {
        // Arrange
        var totalCount = 100;
        var query = CreateTestQuery(page: page, pageSize: pageSize);
        var movements = CreateTestMovements(Math.Min(pageSize, totalCount - ((page - 1) * pageSize)));

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.PageNumber.ShouldBe(page);
        result.Value.PageSize.ShouldBe(pageSize);
        result.Value.TotalCount.ShouldBe(totalCount);
        result.Value.TotalPages.ShouldBe((int)Math.Ceiling((double)totalCount / pageSize));
        result.Value.HasPreviousPage.ShouldBe(page > 1);
        result.Value.HasNextPage.ShouldBe(page < result.Value.TotalPages);
    }

    [Fact]
    public async Task Handle_WithNullOptionalFields_ShouldReturnNullsInDto()
    {
        // Arrange
        var movement = InventoryMovement.Create(
            TestVariantId,
            TestProductId,
            InventoryMovementType.StockIn,
            100,
            10,
            TestTenantId,
            null, // reference
            null, // notes
            null, // userId
            null); // correlationId

        var movements = new List<InventoryMovement> { movement };
        var query = CreateTestQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var item = result.Value.Items.First();
        item.Reference.ShouldBeNull();
        item.Notes.ShouldBeNull();
        item.UserId.ShouldBeNull();
        item.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithNegativeQuantityMoved_ShouldCalculateCorrectQuantityAfter()
    {
        // Arrange - StockOut should have negative quantity moved
        var movement = CreateTestMovement(
            InventoryMovementType.StockOut,
            100,
            -25);
        var movements = new List<InventoryMovement> { movement };
        var query = CreateTestQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<StockHistoryByVariantIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<StockHistoryByVariantIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var item = result.Value.Items.First();
        item.QuantityBefore.ShouldBe(100);
        item.QuantityMoved.ShouldBe(-25);
        item.QuantityAfter.ShouldBe(75); // 100 + (-25) = 75
    }

    #endregion
}
