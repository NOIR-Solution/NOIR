using NOIR.Application.Features.Customers.Queries.ExportCustomers;
using NOIR.Application.Features.Reports.DTOs;

namespace NOIR.Application.UnitTests.Features.Customers.Queries;

/// <summary>
/// Unit tests for ExportCustomersQueryHandler.
/// Tests CSV and Excel export with various filter scenarios.
/// </summary>
public class ExportCustomersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IExcelExportService> _excelExportServiceMock;
    private readonly Mock<ILogger<ExportCustomersQueryHandler>> _loggerMock;
    private readonly ExportCustomersQueryHandler _handler;

    public ExportCustomersQueryHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _excelExportServiceMock = new Mock<IExcelExportService>();
        _loggerMock = new Mock<ILogger<ExportCustomersQueryHandler>>();

        _handler = new ExportCustomersQueryHandler(
            _customerRepositoryMock.Object,
            _excelExportServiceMock.Object,
            _loggerMock.Object);
    }

    private static Customer CreateTestCustomer(
        string email = "john@example.com",
        string firstName = "John",
        string lastName = "Doe",
        string? phone = "0901234567")
    {
        return Customer.Create(null, email, firstName, lastName, phone, "tenant-123");
    }

    private void SetupRepositoryToReturn(List<Customer> customers)
    {
        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);
    }

    #endregion

    #region CSV Format Tests

    [Fact]
    public async Task Handle_WithCsvFormat_ReturnsCsvFile()
    {
        // Arrange
        var customers = new List<Customer>
        {
            CreateTestCustomer("alice@test.com", "Alice", "Smith"),
            CreateTestCustomer("bob@test.com", "Bob", "Jones")
        };
        SetupRepositoryToReturn(customers);

        var query = new ExportCustomersQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileName.ShouldStartWith("customers-");
        result.Value.FileName.ShouldEndWith(".csv");
        result.Value.FileBytes.ShouldNotBeEmpty();

        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        csvContent.ShouldContain("FirstName");
        csvContent.ShouldContain("LastName");
        csvContent.ShouldContain("Email");
        csvContent.ShouldContain("Alice");
        csvContent.ShouldContain("Bob");
    }

    #endregion

    #region Excel Format Tests

    [Fact]
    public async Task Handle_WithExcelFormat_ReturnsExcelFile()
    {
        // Arrange
        var customers = new List<Customer>
        {
            CreateTestCustomer("alice@test.com", "Alice", "Smith")
        };
        SetupRepositoryToReturn(customers);

        var excelBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        _excelExportServiceMock
            .Setup(x => x.CreateExcelFile(
                "Customers",
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()))
            .Returns(excelBytes);

        var query = new ExportCustomersQuery(Format: ExportFormat.Excel);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.FileName.ShouldStartWith("customers-");
        result.Value.FileName.ShouldEndWith(".xlsx");
        result.Value.FileBytes.ShouldBe(excelBytes);

        _excelExportServiceMock.Verify(
            x => x.CreateExcelFile("Customers", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()),
            Times.Once);
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task Handle_WithSegmentFilter_FiltersCustomers()
    {
        // Arrange
        var customers = new List<Customer> { CreateTestCustomer() };
        SetupRepositoryToReturn(customers);

        var query = new ExportCustomersQuery(Segment: CustomerSegment.VIP);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CustomersForExportSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Empty Data Tests

    [Fact]
    public async Task Handle_WithNoCustomers_ReturnsEmptyFile()
    {
        // Arrange
        SetupRepositoryToReturn(new List<Customer>());
        var query = new ExportCustomersQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.FileBytes.ShouldNotBeEmpty();

        // CSV should still have headers even with no data rows
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        csvContent.ShouldContain("FirstName");
        csvContent.ShouldContain("Email");
    }

    #endregion

    #region Column Verification Tests

    [Fact]
    public async Task Handle_IncludesAllExpectedColumns()
    {
        // Arrange
        var customers = new List<Customer> { CreateTestCustomer() };
        SetupRepositoryToReturn(customers);

        var query = new ExportCustomersQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        var headerLine = csvContent.Split('\n')[0];

        headerLine.ShouldContain("FirstName");
        headerLine.ShouldContain("LastName");
        headerLine.ShouldContain("Email");
        headerLine.ShouldContain("Phone");
        headerLine.ShouldContain("Segment");
        headerLine.ShouldContain("Tier");
        headerLine.ShouldContain("TotalOrders");
        headerLine.ShouldContain("TotalSpent");
        headerLine.ShouldContain("AverageOrderValue");
        headerLine.ShouldContain("LoyaltyPoints");
        headerLine.ShouldContain("IsActive");
        headerLine.ShouldContain("Tags");
        headerLine.ShouldContain("CreatedAt");
    }

    #endregion
}
