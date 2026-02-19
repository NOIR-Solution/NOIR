using NOIR.Application.Features.Reports;
using NOIR.Application.Features.Reports.DTOs;
using NOIR.Application.Features.Reports.Queries.GetBestSellersReport;

namespace NOIR.Application.UnitTests.Features.Reports;

/// <summary>
/// Unit tests for GetBestSellersReportQueryHandler.
/// Tests delegation to IReportQueryService.GetBestSellersAsync with date defaulting and TopN logic.
/// </summary>
public class GetBestSellersReportQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IReportQueryService> _reportServiceMock;
    private readonly GetBestSellersReportQueryHandler _handler;

    public GetBestSellersReportQueryHandlerTests()
    {
        _reportServiceMock = new Mock<IReportQueryService>();
        _handler = new GetBestSellersReportQueryHandler(_reportServiceMock.Object);
    }

    private static BestSellersReportDto CreateBestSellersReport(
        int productCount = 3,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null) =>
        new(
            Products: Enumerable.Range(1, productCount)
                .Select(i => new BestSellerDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: $"Product {i}",
                    ImageUrl: $"https://img.example.com/{i}.jpg",
                    UnitsSold: 100 * i,
                    Revenue: 1000m * i))
                .ToList(),
            Period: "custom",
            StartDate: startDate ?? DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: endDate ?? DateTimeOffset.UtcNow);

    #endregion

    #region Happy Path - Explicit Dates

    [Fact]
    public async Task Handle_WithExplicitDates_ReturnsSuccess()
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 1, 31, 23, 59, 59, TimeSpan.Zero);
        var expectedReport = CreateBestSellersReport(5, startDate, endDate);

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                startDate,
                endDate,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(
            StartDate: startDate,
            EndDate: endDate,
            TopN: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Products.Should().HaveCount(5);
    }

    #endregion

    #region Default Dates (null StartDate/EndDate)

    [Fact]
    public async Task Handle_WithNullDates_DefaultsToLast30Days()
    {
        // Arrange
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateBestSellersReport();

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetBestSellersAsync(
                It.Is<DateTimeOffset>(d => d >= beforeExecution.AddDays(-30).AddSeconds(-1) && d <= afterExecution.AddDays(-30).AddSeconds(1)),
                It.Is<DateTimeOffset>(d => d >= beforeExecution && d <= afterExecution),
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyStartDate_DefaultsEndDateToNow()
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateBestSellersReport();

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(StartDate: startDate, EndDate: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetBestSellersAsync(
                startDate,
                It.Is<DateTimeOffset>(d => d >= beforeExecution && d <= afterExecution),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyEndDate_DefaultsStartDateToMinus30Days()
    {
        // Arrange
        var endDate = new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateBestSellersReport();

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(StartDate: null, EndDate: endDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetBestSellersAsync(
                It.Is<DateTimeOffset>(d => d >= beforeExecution.AddDays(-30).AddSeconds(-1) && d <= afterExecution.AddDays(-30).AddSeconds(1)),
                endDate,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TopN Variations

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    public async Task Handle_WithDifferentTopN_PassesTopNToService(int topN)
    {
        // Arrange
        var expectedReport = CreateBestSellersReport(topN);

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                topN,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-7),
            EndDate: DateTimeOffset.UtcNow,
            TopN: topN);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                topN,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Result Passthrough

    [Fact]
    public async Task Handle_ReturnsExactDtoFromService()
    {
        // Arrange
        var expectedReport = new BestSellersReportDto(
            Products: new List<BestSellerDto>
            {
                new(Guid.NewGuid(), "Widget A", "https://img.test/a.png", 500, 5000m),
                new(Guid.NewGuid(), "Widget B", null, 300, 3000m),
            },
            Period: "custom",
            StartDate: DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: DateTimeOffset.UtcNow);

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedReport);
        result.Value.Products.Should().HaveCount(2);
        result.Value.Products[0].ProductName.Should().Be("Widget A");
        result.Value.Products[1].ImageUrl.Should().BeNull();
    }

    #endregion

    #region Cancellation Token

    [Fact]
    public async Task Handle_PassesCancellationTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expectedReport = CreateBestSellersReport();

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                token))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-7),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                token),
            Times.Once);
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WithEmptyProducts_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var expectedReport = CreateBestSellersReport(productCount: 0);

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-7),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().BeEmpty();
    }

    #endregion

    #region Service Exception Propagation

    [Fact]
    public async Task Handle_WhenServiceThrows_ExceptionPropagates()
    {
        // Arrange
        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-7),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    #endregion

    #region Service Invocation Verification

    [Fact]
    public async Task Handle_CallsServiceExactlyOnce()
    {
        // Arrange
        var expectedReport = CreateBestSellersReport();

        _reportServiceMock
            .Setup(x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-14),
            EndDate: DateTimeOffset.UtcNow,
            TopN: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetBestSellersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _reportServiceMock.VerifyNoOtherCalls();
    }

    #endregion
}
