using NOIR.Application.Features.Products.Queries.ExportProductsFile;
using NOIR.Application.Features.Products.Queries.ExportProducts;
using NOIR.Application.Features.Reports.DTOs;

namespace NOIR.Application.UnitTests.Features.Products.Queries.ExportProductsFile;

/// <summary>
/// Unit tests for ExportProductsFileQueryHandler.
/// Tests CSV and Excel file generation from product data.
/// </summary>
public class ExportProductsFileQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IMessageBus> _busMock;
    private readonly Mock<IExcelExportService> _excelExportServiceMock;
    private readonly Mock<ILogger<ExportProductsFileQueryHandler>> _loggerMock;
    private readonly ExportProductsFileQueryHandler _handler;

    public ExportProductsFileQueryHandlerTests()
    {
        _busMock = new Mock<IMessageBus>();
        _excelExportServiceMock = new Mock<IExcelExportService>();
        _loggerMock = new Mock<ILogger<ExportProductsFileQueryHandler>>();

        _handler = new ExportProductsFileQueryHandler(
            _busMock.Object,
            _excelExportServiceMock.Object,
            _loggerMock.Object);
    }

    private static ExportProductsResultDto CreateTestData(int rowCount = 2)
    {
        var rows = Enumerable.Range(1, rowCount).Select(i =>
            new ExportProductRowDto(
                Name: $"Product {i}",
                Slug: $"product-{i}",
                Sku: $"SKU-{i:D4}",
                Barcode: $"BAR-{i:D4}",
                BasePrice: 100m * i,
                Currency: "VND",
                Status: "Active",
                CategoryName: "Electronics",
                Brand: "TestBrand",
                ShortDescription: $"Description for product {i}",
                VariantName: null,
                VariantPrice: null,
                CompareAtPrice: null,
                Stock: 50 * i,
                Images: "https://example.com/img1.jpg",
                Attributes: new Dictionary<string, string> { ["Color"] = "Red" }
            )).ToList();

        return new ExportProductsResultDto(rows, new List<string> { "Color" });
    }

    private void SetupBusToReturnData(ExportProductsResultDto data)
    {
        _busMock.Setup(x => x.InvokeAsync<Result<ExportProductsResultDto>>(
                It.IsAny<ExportProductsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(data));
    }

    #endregion

    #region CSV Format Tests

    [Fact]
    public async Task Handle_WithCsvFormat_ReturnsCsvBytes()
    {
        // Arrange
        var data = CreateTestData();
        SetupBusToReturnData(data);
        var query = new ExportProductsFileQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileName.ShouldStartWith("products-");
        result.Value.FileName.ShouldEndWith(".csv");
        result.Value.FileBytes.ShouldNotBeEmpty();

        // Decode and verify CSV content
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        csvContent.ShouldContain("Name");
        csvContent.ShouldContain("Product 1");
        csvContent.ShouldContain("Product 2");
    }

    #endregion

    #region Excel Format Tests

    [Fact]
    public async Task Handle_WithExcelFormat_ReturnsExcelBytes()
    {
        // Arrange
        var data = CreateTestData();
        SetupBusToReturnData(data);

        var excelBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        _excelExportServiceMock
            .Setup(x => x.CreateExcelFile(
                "Products",
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()))
            .Returns(excelBytes);

        var query = new ExportProductsFileQuery(Format: ExportFormat.Excel);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.FileName.ShouldStartWith("products-");
        result.Value.FileName.ShouldEndWith(".xlsx");
        result.Value.FileBytes.ShouldBe(excelBytes);

        _excelExportServiceMock.Verify(
            x => x.CreateExcelFile("Products", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()),
            Times.Once);
    }

    #endregion

    #region Empty Data Tests

    [Fact]
    public async Task Handle_WithNoProducts_ReturnsEmptyFile()
    {
        // Arrange
        var emptyData = new ExportProductsResultDto(new List<ExportProductRowDto>(), new List<string>());
        SetupBusToReturnData(emptyData);
        var query = new ExportProductsFileQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.FileBytes.ShouldNotBeEmpty();

        // CSV should still have headers
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        csvContent.ShouldContain("Name");
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task Handle_WhenBusReturnsFailure_ReturnsError()
    {
        // Arrange
        _busMock.Setup(x => x.InvokeAsync<Result<ExportProductsResultDto>>(
                It.IsAny<ExportProductsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ExportProductsResultDto>(Error.Internal("Export failed")));

        var query = new ExportProductsFileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
    }

    #endregion

    #region Column Inclusion Tests

    [Fact]
    public async Task Handle_WithIncludeImagesFalse_ExcludesImagesColumn()
    {
        // Arrange
        var data = CreateTestData();
        SetupBusToReturnData(data);
        var query = new ExportProductsFileQuery(Format: ExportFormat.CSV, IncludeImages: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        var headerLine = csvContent.Split('\n')[0];
        headerLine.ShouldNotContain("Images");
    }

    [Fact]
    public async Task Handle_WithIncludeAttributesFalse_ExcludesAttributeColumns()
    {
        // Arrange
        var data = CreateTestData();
        SetupBusToReturnData(data);
        var query = new ExportProductsFileQuery(Format: ExportFormat.CSV, IncludeAttributes: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        var headerLine = csvContent.Split('\n')[0];
        headerLine.ShouldNotContain("Color");
    }

    #endregion
}
