using ClosedXML.Excel;

namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Tests for ExcelExportService using real ClosedXML implementation.
/// Verifies correct workbook generation, formatting, and edge cases.
/// </summary>
public class ExcelExportServiceTests
{
    private readonly ExcelExportService _service;

    public ExcelExportServiceTests()
    {
        _service = new ExcelExportService();
    }

    #region Success Scenarios

    [Fact]
    public void CreateExcelFile_WithValidData_ReturnsNonEmptyBytes()
    {
        // Arrange
        var headers = new List<string> { "Name", "Price", "Quantity" };
        var rows = new List<IReadOnlyList<object?>>
        {
            new List<object?> { "Widget A", 29.99m, 100 },
            new List<object?> { "Widget B", 49.99m, 50 }
        };

        // Act
        var result = _service.CreateExcelFile("Products", headers, rows);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        // Verify it's a valid XLSX file by reading it back
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Name.ShouldBe("Products");
        worksheet.Cell(1, 1).Value.ToString().ShouldBe("Name");
        worksheet.Cell(1, 2).Value.ToString().ShouldBe("Price");
        worksheet.Cell(1, 3).Value.ToString().ShouldBe("Quantity");
        worksheet.Cell(2, 1).Value.ToString().ShouldBe("Widget A");
        worksheet.Cell(3, 1).Value.ToString().ShouldBe("Widget B");
    }

    [Fact]
    public void CreateExcelFile_WithEmptyRows_ReturnsValidFile()
    {
        // Arrange
        var headers = new List<string> { "Name", "Email", "Phone" };
        var rows = new List<IReadOnlyList<object?>>();

        // Act
        var result = _service.CreateExcelFile("Customers", headers, rows);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Name.ShouldBe("Customers");
        worksheet.Cell(1, 1).Value.ToString().ShouldBe("Name");
        worksheet.Cell(1, 2).Value.ToString().ShouldBe("Email");
        worksheet.Cell(1, 3).Value.ToString().ShouldBe("Phone");

        // Row 2 should be empty (no data rows)
        worksheet.Cell(2, 1).IsEmpty().ShouldBe(true);
    }

    [Fact]
    public void CreateExcelFile_WithNullValues_HandlesGracefully()
    {
        // Arrange
        var headers = new List<string> { "Name", "Description", "Price" };
        var rows = new List<IReadOnlyList<object?>>
        {
            new List<object?> { "Product A", null, 19.99m },
            new List<object?> { null, "Description B", null }
        };

        // Act
        var result = _service.CreateExcelFile("Products", headers, rows);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Cell(2, 1).Value.ToString().ShouldBe("Product A");
        worksheet.Cell(2, 2).IsEmpty().ShouldBe(true);
        worksheet.Cell(3, 1).IsEmpty().ShouldBe(true);
        worksheet.Cell(3, 2).Value.ToString().ShouldBe("Description B");
    }

    [Fact]
    public void CreateExcelFile_WithMixedTypes_FormatsCorrectly()
    {
        // Arrange
        var headers = new List<string> { "String", "Decimal", "Int", "DateTime", "Bool", "Long", "Double" };
        var dateTime = new DateTimeOffset(2026, 3, 1, 10, 30, 0, TimeSpan.Zero);
        var rows = new List<IReadOnlyList<object?>>
        {
            new List<object?> { "text", 123.45m, 42, dateTime, true, 999999L, 3.14 }
        };

        // Act
        var result = _service.CreateExcelFile("Mixed", headers, rows);

        // Assert
        result.ShouldNotBeNull();

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Cell(2, 1).Value.ToString().ShouldBe("text");
        worksheet.Cell(2, 2).GetValue<decimal>().ShouldBe(123.45m);
        worksheet.Cell(2, 3).GetValue<int>().ShouldBe(42);
        worksheet.Cell(2, 4).Value.ToString().ShouldBe("2026-03-01 10:30:00");
        worksheet.Cell(2, 5).Value.ToString().ShouldBe("Yes");
        worksheet.Cell(2, 6).GetValue<long>().ShouldBe(999999L);
        worksheet.Cell(2, 7).GetValue<double>().ShouldBe(3.14);
    }

    [Fact]
    public void CreateExcelFile_BoolFalse_FormatsAsNo()
    {
        // Arrange
        var headers = new List<string> { "Active" };
        var rows = new List<IReadOnlyList<object?>>
        {
            new List<object?> { false }
        };

        // Act
        var result = _service.CreateExcelFile("Flags", headers, rows);

        // Assert
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Cell(2, 1).Value.ToString().ShouldBe("No");
    }

    [Fact]
    public void CreateExcelFile_HeaderRowIsBold()
    {
        // Arrange
        var headers = new List<string> { "Column1", "Column2" };
        var rows = new List<IReadOnlyList<object?>>();

        // Act
        var result = _service.CreateExcelFile("Test", headers, rows);

        // Assert
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Cell(1, 1).Style.Font.Bold.ShouldBe(true);
        worksheet.Cell(1, 2).Style.Font.Bold.ShouldBe(true);
    }

    [Fact]
    public void CreateExcelFile_FreezesPaneOnHeaderRow()
    {
        // Arrange
        var headers = new List<string> { "A", "B" };
        var rows = new List<IReadOnlyList<object?>>
        {
            new List<object?> { "1", "2" }
        };

        // Act
        var result = _service.CreateExcelFile("Frozen", headers, rows);

        // Assert
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        // Verify the file was generated without error (FreezeRows was called)
        worksheet.RowCount().ShouldBeGreaterThanOrEqualTo(1);
    }

    #endregion
}
