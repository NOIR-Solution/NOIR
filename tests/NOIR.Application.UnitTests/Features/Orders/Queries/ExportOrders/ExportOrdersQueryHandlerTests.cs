using NOIR.Application.Features.Orders.Queries.ExportOrders;
using NOIR.Application.Features.Reports.DTOs;

namespace NOIR.Application.UnitTests.Features.Orders.Queries.ExportOrders;

/// <summary>
/// Unit tests for ExportOrdersQueryHandler.
/// Tests CSV and Excel export with various filter scenarios.
/// </summary>
public class ExportOrdersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IExcelExportService> _excelExportServiceMock;
    private readonly Mock<ILogger<ExportOrdersQueryHandler>> _loggerMock;
    private readonly ExportOrdersQueryHandler _handler;

    public ExportOrdersQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _excelExportServiceMock = new Mock<IExcelExportService>();
        _loggerMock = new Mock<ILogger<ExportOrdersQueryHandler>>();

        _handler = new ExportOrdersQueryHandler(
            _orderRepositoryMock.Object,
            _excelExportServiceMock.Object,
            _loggerMock.Object);
    }

    private static Order CreateTestOrder(
        string orderNumber = "ORD-20260301-0001",
        string customerEmail = "customer@example.com",
        decimal subTotal = 100m,
        decimal grandTotal = 110m)
    {
        return Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", "tenant-123");
    }

    private void SetupRepositoryToReturn(List<Order> orders)
    {
        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
    }

    #endregion

    #region CSV Format Tests

    [Fact]
    public async Task Handle_WithCsvFormat_ReturnsCsvFile()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder("ORD-001", "alice@test.com", 100m, 110m),
            CreateTestOrder("ORD-002", "bob@test.com", 200m, 220m)
        };
        SetupRepositoryToReturn(orders);

        var query = new ExportOrdersQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileName.ShouldStartWith("orders-");
        result.Value.FileName.ShouldEndWith(".csv");
        result.Value.FileBytes.ShouldNotBeEmpty();

        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        csvContent.ShouldContain("OrderNumber");
        csvContent.ShouldContain("ORD-001");
        csvContent.ShouldContain("ORD-002");
    }

    #endregion

    #region Excel Format Tests

    [Fact]
    public async Task Handle_WithExcelFormat_ReturnsExcelFile()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        SetupRepositoryToReturn(orders);

        var excelBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        _excelExportServiceMock
            .Setup(x => x.CreateExcelFile(
                "Orders",
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()))
            .Returns(excelBytes);

        var query = new ExportOrdersQuery(Format: ExportFormat.Excel);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.FileName.ShouldStartWith("orders-");
        result.Value.FileName.ShouldEndWith(".xlsx");
        result.Value.FileBytes.ShouldBe(excelBytes);

        _excelExportServiceMock.Verify(
            x => x.CreateExcelFile("Orders", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()),
            Times.Once);
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task Handle_WithStatusFilter_FiltersOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        SetupRepositoryToReturn(orders);

        var query = new ExportOrdersQuery(Status: OrderStatus.Pending);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        _orderRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<OrdersForExportSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_FiltersOrders()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        SetupRepositoryToReturn(orders);

        var fromDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var toDate = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var query = new ExportOrdersQuery(FromDate: fromDate, ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        _orderRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<OrdersForExportSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Empty Data Tests

    [Fact]
    public async Task Handle_WithNoOrders_ReturnsEmptyFile()
    {
        // Arrange
        SetupRepositoryToReturn(new List<Order>());
        var query = new ExportOrdersQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.FileBytes.ShouldNotBeEmpty();

        // CSV should still have headers
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        csvContent.ShouldContain("OrderNumber");
        csvContent.ShouldContain("Status");
    }

    #endregion

    #region Column Verification Tests

    [Fact]
    public async Task Handle_IncludesAllExpectedColumns()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        SetupRepositoryToReturn(orders);

        var query = new ExportOrdersQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        var headerLine = csvContent.Split('\n')[0];

        headerLine.ShouldContain("OrderNumber");
        headerLine.ShouldContain("Status");
        headerLine.ShouldContain("CustomerEmail");
        headerLine.ShouldContain("CustomerName");
        headerLine.ShouldContain("SubTotal");
        headerLine.ShouldContain("DiscountAmount");
        headerLine.ShouldContain("ShippingAmount");
        headerLine.ShouldContain("TaxAmount");
        headerLine.ShouldContain("GrandTotal");
        headerLine.ShouldContain("Currency");
        headerLine.ShouldContain("CouponCode");
        headerLine.ShouldContain("ShippingMethod");
        headerLine.ShouldContain("TrackingNumber");
        headerLine.ShouldContain("CreatedAt");
    }

    #endregion
}
