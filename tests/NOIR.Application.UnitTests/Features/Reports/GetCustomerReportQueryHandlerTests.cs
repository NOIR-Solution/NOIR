using NOIR.Application.Features.Reports;
using NOIR.Application.Features.Reports.DTOs;
using NOIR.Application.Features.Reports.Queries.GetCustomerReport;

namespace NOIR.Application.UnitTests.Features.Reports;

/// <summary>
/// Unit tests for GetCustomerReportQueryHandler.
/// Tests delegation to IReportQueryService.GetCustomerReportAsync with date defaulting logic.
/// </summary>
public class GetCustomerReportQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IReportQueryService> _reportServiceMock;
    private readonly GetCustomerReportQueryHandler _handler;

    public GetCustomerReportQueryHandlerTests()
    {
        _reportServiceMock = new Mock<IReportQueryService>();
        _handler = new GetCustomerReportQueryHandler(_reportServiceMock.Object);
    }

    private static CustomerReportDto CreateCustomerReport() =>
        new(
            NewCustomers: 150,
            ReturningCustomers: 320,
            ChurnRate: 0.05m,
            AcquisitionByMonth: new List<MonthlyAcquisitionDto>
            {
                new("2026-01", 50, 5000m),
                new("2026-02", 100, 12000m),
            },
            TopCustomers: new List<TopCustomerDto>
            {
                new(Guid.NewGuid(), "Alice", 15000m, 25),
                new(Guid.NewGuid(), "Bob", 12000m, 18),
            });

    #endregion

    #region Happy Path - Explicit Dates

    [Fact]
    public async Task Handle_WithExplicitDates_ReturnsSuccess()
    {
        // Arrange
        var startDate = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 2, 28, 23, 59, 59, TimeSpan.Zero);
        var expectedReport = CreateCustomerReport();

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                startDate,
                endDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(StartDate: startDate, EndDate: endDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.NewCustomers.Should().Be(150);
        result.Value.ReturningCustomers.Should().Be(320);
    }

    #endregion

    #region Default Dates (null StartDate/EndDate)

    [Fact]
    public async Task Handle_WithNullDates_DefaultsToLast12Months()
    {
        // Arrange
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateCustomerReport();

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetCustomerReportAsync(
                It.Is<DateTimeOffset>(d => d >= beforeExecution.AddMonths(-12).AddSeconds(-1) && d <= afterExecution.AddMonths(-12).AddSeconds(1)),
                It.Is<DateTimeOffset>(d => d >= beforeExecution && d <= afterExecution),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyStartDate_DefaultsEndDateToNow()
    {
        // Arrange
        var startDate = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateCustomerReport();

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(StartDate: startDate, EndDate: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetCustomerReportAsync(
                startDate,
                It.Is<DateTimeOffset>(d => d >= beforeExecution && d <= afterExecution),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyEndDate_DefaultsStartDateToMinus12Months()
    {
        // Arrange
        var endDate = new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var beforeExecution = DateTimeOffset.UtcNow;
        var expectedReport = CreateCustomerReport();

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(StartDate: null, EndDate: endDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetCustomerReportAsync(
                It.Is<DateTimeOffset>(d => d >= beforeExecution.AddMonths(-12).AddSeconds(-1) && d <= afterExecution.AddMonths(-12).AddSeconds(1)),
                endDate,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Result Passthrough

    [Fact]
    public async Task Handle_ReturnsExactDtoFromService()
    {
        // Arrange
        var expectedReport = new CustomerReportDto(
            NewCustomers: 42,
            ReturningCustomers: 88,
            ChurnRate: 0.12m,
            AcquisitionByMonth: new List<MonthlyAcquisitionDto>
            {
                new("2026-01", 42, 8400m),
            },
            TopCustomers: new List<TopCustomerDto>
            {
                new(Guid.NewGuid(), "VIP Customer", 50000m, 100),
            });

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddMonths(-6),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedReport);
        result.Value.ChurnRate.Should().Be(0.12m);
        result.Value.AcquisitionByMonth.Should().HaveCount(1);
        result.Value.TopCustomers.Should().HaveCount(1);
        result.Value.TopCustomers[0].Name.Should().Be("VIP Customer");
    }

    #endregion

    #region Cancellation Token

    [Fact]
    public async Task Handle_PassesCancellationTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expectedReport = CreateCustomerReport();

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                token))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddMonths(-3),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                token),
            Times.Once);
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WithNoCustomers_ReturnsSuccessWithZeros()
    {
        // Arrange
        var expectedReport = new CustomerReportDto(
            NewCustomers: 0,
            ReturningCustomers: 0,
            ChurnRate: 0m,
            AcquisitionByMonth: new List<MonthlyAcquisitionDto>(),
            TopCustomers: new List<TopCustomerDto>());

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddMonths(-6),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NewCustomers.Should().Be(0);
        result.Value.ReturningCustomers.Should().Be(0);
        result.Value.AcquisitionByMonth.Should().BeEmpty();
        result.Value.TopCustomers.Should().BeEmpty();
    }

    #endregion

    #region Service Exception Propagation

    [Fact]
    public async Task Handle_WhenServiceThrows_ExceptionPropagates()
    {
        // Arrange
        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var query = new GetCustomerReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddMonths(-3),
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
        var startDate = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedReport = CreateCustomerReport();

        _reportServiceMock
            .Setup(x => x.GetCustomerReportAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetCustomerReportQuery(StartDate: startDate, EndDate: endDate);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetCustomerReportAsync(
                startDate,
                endDate,
                CancellationToken.None),
            Times.Once);

        _reportServiceMock.VerifyNoOtherCalls();
    }

    #endregion
}
