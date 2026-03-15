using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Queries.GetInventoryReceipts;
using NOIR.Application.Features.Inventory.Specifications;
using NOIR.Domain.Entities.Inventory;

namespace NOIR.Application.UnitTests.Features.Inventory.Queries.GetInventoryReceipts;

/// <summary>
/// Unit tests for GetInventoryReceiptsQueryHandler.
/// Tests paginated receipt listing with optional filters.
/// </summary>
public class GetInventoryReceiptsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<InventoryReceipt, Guid>> _repositoryMock;
    private readonly GetInventoryReceiptsQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetInventoryReceiptsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<InventoryReceipt, Guid>>();
        _handler = new GetInventoryReceiptsQueryHandler(_repositoryMock.Object);
    }

    private static InventoryReceipt CreateTestReceipt(
        string receiptNumber = "RCV-20260218-0001",
        InventoryReceiptType type = InventoryReceiptType.StockIn)
    {
        var receipt = InventoryReceipt.Create(receiptNumber, type, "Notes", TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, 25.00m);
        return receipt;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPagination_ShouldReturnPagedResult()
    {
        // Arrange
        var receipts = new List<InventoryReceipt>
        {
            CreateTestReceipt("RCV-20260218-0001"),
            CreateTestReceipt("RCV-20260218-0002")
        };
        var query = new GetInventoryReceiptsQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<InventoryReceiptsListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<InventoryReceiptsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
        result.Value.PageNumber.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithNoReceipts_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        var query = new GetInventoryReceiptsQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<InventoryReceiptsListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryReceipt>());

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<InventoryReceiptsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnSummaryDtosWithCorrectFields()
    {
        // Arrange
        var receipt = CreateTestReceipt("RCV-20260218-0042", InventoryReceiptType.StockIn);
        var query = new GetInventoryReceiptsQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<InventoryReceiptsListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryReceipt> { receipt });

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<InventoryReceiptsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var item = result.Value.Items.First();
        item.ReceiptNumber.ShouldBe("RCV-20260218-0042");
        item.Type.ShouldBe(InventoryReceiptType.StockIn);
        item.Status.ShouldBe(InventoryReceiptStatus.Draft);
        item.TotalQuantity.ShouldBe(10);
        item.TotalCost.ShouldBe(250.00m);
        item.ItemCount.ShouldBe(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var query = new GetInventoryReceiptsQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<InventoryReceiptsListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryReceipt>());

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<InventoryReceiptsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _repositoryMock.Verify(
            x => x.ListAsync(It.IsAny<InventoryReceiptsListSpec>(), token),
            Times.Once);
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<InventoryReceiptsCountSpec>(), token),
            Times.Once);
    }

    #endregion
}
