using NOIR.Application.Features.Reports;
using NOIR.Application.Features.Reports.DTOs;
using NOIR.Application.Features.Reports.Queries.ExportReport;

namespace NOIR.Application.UnitTests.Features.Reports;

/// <summary>
/// Unit tests for ExportReportQueryHandler.
/// Tests delegation to IReportQueryService.ExportReportAsync with all parameter combinations.
/// </summary>
public class ExportReportQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IReportQueryService> _reportServiceMock;
    private readonly ExportReportQueryHandler _handler;

    public ExportReportQueryHandlerTests()
    {
        _reportServiceMock = new Mock<IReportQueryService>();
        _handler = new ExportReportQueryHandler(_reportServiceMock.Object);
    }

    private static ExportResultDto CreateExportResult(string fileName = "report.csv") =>
        new(
            FileBytes: new byte[] { 0x01, 0x02, 0x03 },
            ContentType: "text/csv",
            FileName: fileName);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_WithExplicitDates_ReturnsSuccess()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = DateTimeOffset.UtcNow;
        var expectedResult = CreateExportResult();

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                ReportType.Revenue,
                ExportFormat.CSV,
                startDate,
                endDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: startDate,
            EndDate: endDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeSameAs(expectedResult);
    }

    [Fact]
    public async Task Handle_WithNullDates_PassesNullsToService()
    {
        // Arrange
        var expectedResult = CreateExportResult();

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                ReportType.Revenue,
                ExportFormat.CSV,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(ReportType: ReportType.Revenue);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.ExportReportAsync(
                ReportType.Revenue,
                ExportFormat.CSV,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ExportFormat Variations

    [Fact]
    public async Task Handle_WithCsvFormat_PassesCsvToService()
    {
        // Arrange
        var expectedResult = CreateExportResult("report.csv");

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                It.IsAny<ReportType>(),
                ExportFormat.CSV,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("report.csv");
        _reportServiceMock.Verify(
            x => x.ExportReportAsync(
                ReportType.Revenue,
                ExportFormat.CSV,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExcelFormat_PassesExcelToService()
    {
        // Arrange
        var expectedResult = CreateExportResult("report.xlsx");

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                It.IsAny<ReportType>(),
                ExportFormat.Excel,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.Excel);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("report.xlsx");
        _reportServiceMock.Verify(
            x => x.ExportReportAsync(
                ReportType.Revenue,
                ExportFormat.Excel,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ReportType Variations

    [Theory]
    [InlineData(ReportType.Revenue)]
    [InlineData(ReportType.BestSellers)]
    [InlineData(ReportType.Inventory)]
    [InlineData(ReportType.CustomerAcquisition)]
    public async Task Handle_WithEachReportType_PassesCorrectTypeToService(ReportType reportType)
    {
        // Arrange
        var expectedResult = CreateExportResult();

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                reportType,
                It.IsAny<ExportFormat>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(ReportType: reportType, Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.ExportReportAsync(
                reportType,
                ExportFormat.CSV,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Result Passthrough

    [Fact]
    public async Task Handle_ReturnsExactDtoFromService()
    {
        // Arrange
        var fileBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var expectedResult = new ExportResultDto(
            FileBytes: fileBytes,
            ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName: "revenue-report-2026.xlsx");

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                It.IsAny<ReportType>(),
                It.IsAny<ExportFormat>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.Excel);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileBytes.Should().BeEquivalentTo(fileBytes);
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.FileName.Should().Be("revenue-report-2026.xlsx");
    }

    #endregion

    #region Cancellation Token

    [Fact]
    public async Task Handle_PassesCancellationTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expectedResult = CreateExportResult();

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                It.IsAny<ReportType>(),
                It.IsAny<ExportFormat>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                token))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(ReportType: ReportType.Revenue);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reportServiceMock.Verify(
            x => x.ExportReportAsync(
                It.IsAny<ReportType>(),
                It.IsAny<ExportFormat>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                token),
            Times.Once);
    }

    #endregion

    #region Service Exception Propagation

    [Fact]
    public async Task Handle_WhenServiceThrows_ExceptionPropagates()
    {
        // Arrange
        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                It.IsAny<ReportType>(),
                It.IsAny<ExportFormat>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Export service unavailable"));

        var query = new ExportReportQuery(ReportType: ReportType.Revenue, Format: ExportFormat.CSV);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Export service unavailable");
    }

    #endregion

    #region Service Invocation Verification

    [Fact]
    public async Task Handle_CallsServiceExactlyOnce()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var expectedResult = CreateExportResult();

        _reportServiceMock
            .Setup(x => x.ExportReportAsync(
                It.IsAny<ReportType>(),
                It.IsAny<ExportFormat>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new ExportReportQuery(
            ReportType: ReportType.BestSellers,
            Format: ExportFormat.Excel,
            StartDate: startDate,
            EndDate: endDate);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            x => x.ExportReportAsync(
                ReportType.BestSellers,
                ExportFormat.Excel,
                startDate,
                endDate,
                CancellationToken.None),
            Times.Once);

        _reportServiceMock.VerifyNoOtherCalls();
    }

    #endregion
}
