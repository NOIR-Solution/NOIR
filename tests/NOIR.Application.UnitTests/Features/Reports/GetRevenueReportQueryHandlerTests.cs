using NOIR.Application.Features.Reports;
using NOIR.Application.Features.Reports.DTOs;
using NOIR.Application.Features.Reports.Queries.GetRevenueReport;

namespace NOIR.Application.UnitTests.Features.Reports;

/// <summary>
/// Unit tests for GetRevenueReportQueryHandler.
/// Tests delegation to IReportQueryService.GetRevenueReportAsync with period, date defaulting logic.
/// </summary>
public class GetRevenueReportQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IReportQueryService> _reportServiceMock;
    private readonly GetRevenueReportQueryHandler _handler;

    public GetRevenueReportQueryHandlerTests()
    {
        _reportServiceMock = new Mock<IReportQueryService>();
        _handler = new GetRevenueReportQueryHandler(_reportServiceMock.Object);
    }

    private static RevenueReportDto CreateRevenueReport(
        string period = "monthly",
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null) =>
        new(
            Period: period,
            StartDate: startDate ?? DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: endDate ?? DateTimeOffset.UtcNow,
            TotalRevenue: 150000m,
            TotalOrders: 420,
            AverageOrderValue: 357.14m,
            RevenueByDay: new List<DailyRevenueDto>
            {
                new(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), 5000m, 14),
                new(DateOnly.FromDateTime(DateTime.UtcNow), 6000m, 18),
            },
            RevenueByCategory: new List<CategoryRevenueDto>
            {
                new(Guid.NewGuid(), "Electronics", 80000m, 200),
                new(Guid.NewGuid(), "Clothing", 70000m, 220),
            },
            RevenueByPaymentMethod: new List<PaymentMethodRevenueDto>
            {
                new("CreditCard", 100000m, 300),
                new("BankTransfer", 50000m, 120),
            },
            ComparedToPreviousPeriod: new RevenueComparisonDto(0.15m, 0.10m));

    #endregion

    #region Happy Path - Explicit Dates

    [Fact]
    public async Task Handle_WithExplicitDates_ReturnsSuccess()
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 1, 31, 23, 59, 59, TimeSpan.Zero);
        var expectedReport = CreateRevenueReport("monthly", startDate, endDate);

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                "monthly",
                startDate,
                endDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(
            Period: "monthly",
            StartDate: startDate,
            EndDate: endDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalRevenue.Should().Be(150000m);
        result.Value.TotalOrders.Should().Be(420);
    }

    #endregion

    #region Default Dates (null StartDate/EndDate)

    [Fact]
    public async Task Handle_WithNullDates_DefaultsToLast30Days()
    {
        // Arrange
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateRevenueReport();

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(Period: "monthly", StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetRevenueReportAsync(
                "monthly",
                It.Is<DateTimeOffset>(d => d >= beforeExecution.AddDays(-30).AddSeconds(-1) && d <= afterExecution.AddDays(-30).AddSeconds(1)),
                It.Is<DateTimeOffset>(d => d >= beforeExecution && d <= afterExecution),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyStartDate_DefaultsEndDateToNow()
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateRevenueReport();

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(Period: "weekly", StartDate: startDate, EndDate: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetRevenueReportAsync(
                "weekly",
                startDate,
                It.Is<DateTimeOffset>(d => d >= beforeExecution && d <= afterExecution),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyEndDate_DefaultsStartDateToMinus30Days()
    {
        // Arrange
        var endDate = new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateRevenueReport();

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(Period: "daily", StartDate: null, EndDate: endDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetRevenueReportAsync(
                "daily",
                It.Is<DateTimeOffset>(d => d >= beforeExecution.AddDays(-30).AddSeconds(-1) && d <= afterExecution.AddDays(-30).AddSeconds(1)),
                endDate,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Period Variations

    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    [InlineData("yearly")]
    public async Task Handle_WithDifferentPeriods_PassesPeriodToService(string period)
    {
        // Arrange
        var expectedReport = CreateRevenueReport(period);

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                period,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(
            Period: period,
            StartDate: DateTimeOffset.UtcNow.AddDays(-7),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetRevenueReportAsync(
                period,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDefaultPeriod_PassesMonthlyToService()
    {
        // Arrange
        var expectedReport = CreateRevenueReport();

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                "monthly",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetRevenueReportAsync(
                "monthly",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Result Passthrough

    [Fact]
    public async Task Handle_ReturnsExactDtoFromService()
    {
        // Arrange
        var expectedReport = new RevenueReportDto(
            Period: "weekly",
            StartDate: DateTimeOffset.UtcNow.AddDays(-7),
            EndDate: DateTimeOffset.UtcNow,
            TotalRevenue: 42000m,
            TotalOrders: 100,
            AverageOrderValue: 420m,
            RevenueByDay: new List<DailyRevenueDto>
            {
                new(DateOnly.FromDateTime(DateTime.UtcNow), 6000m, 15),
            },
            RevenueByCategory: new List<CategoryRevenueDto>
            {
                new(null, "Uncategorized", 2000m, 5),
            },
            RevenueByPaymentMethod: new List<PaymentMethodRevenueDto>
            {
                new("PayPal", 42000m, 100),
            },
            ComparedToPreviousPeriod: new RevenueComparisonDto(-0.05m, -0.02m));

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(
            Period: "weekly",
            StartDate: DateTimeOffset.UtcNow.AddDays(-7),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedReport);
        result.Value.Period.Should().Be("weekly");
        result.Value.TotalRevenue.Should().Be(42000m);
        result.Value.AverageOrderValue.Should().Be(420m);
        result.Value.RevenueByDay.Should().HaveCount(1);
        result.Value.RevenueByCategory.Should().HaveCount(1);
        result.Value.RevenueByPaymentMethod.Should().HaveCount(1);
        result.Value.ComparedToPreviousPeriod.RevenueChange.Should().Be(-0.05m);
    }

    #endregion

    #region Cancellation Token

    [Fact]
    public async Task Handle_PassesCancellationTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expectedReport = CreateRevenueReport();

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                token))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(
            Period: "monthly",
            StartDate: DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                token),
            Times.Once);
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WithNoRevenue_ReturnsSuccessWithEmptyCollections()
    {
        // Arrange
        var expectedReport = new RevenueReportDto(
            Period: "monthly",
            StartDate: DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: DateTimeOffset.UtcNow,
            TotalRevenue: 0m,
            TotalOrders: 0,
            AverageOrderValue: 0m,
            RevenueByDay: new List<DailyRevenueDto>(),
            RevenueByCategory: new List<CategoryRevenueDto>(),
            RevenueByPaymentMethod: new List<PaymentMethodRevenueDto>(),
            ComparedToPreviousPeriod: new RevenueComparisonDto(0m, 0m));

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRevenue.Should().Be(0m);
        result.Value.TotalOrders.Should().Be(0);
        result.Value.RevenueByDay.Should().BeEmpty();
        result.Value.RevenueByCategory.Should().BeEmpty();
        result.Value.RevenueByPaymentMethod.Should().BeEmpty();
    }

    #endregion

    #region Service Exception Propagation

    [Fact]
    public async Task Handle_WhenServiceThrows_ExceptionPropagates()
    {
        // Arrange
        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var query = new GetRevenueReportQuery(
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
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 1, 31, 23, 59, 59, TimeSpan.Zero);
        var expectedReport = CreateRevenueReport();

        _reportServiceMock
            .Setup(x => x.GetRevenueReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetRevenueReportQuery(
            Period: "daily",
            StartDate: startDate,
            EndDate: endDate);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetRevenueReportAsync(
                "daily",
                startDate,
                endDate,
                CancellationToken.None),
            Times.Once);

        _reportServiceMock.VerifyNoOtherCalls();
    }

    #endregion
}
