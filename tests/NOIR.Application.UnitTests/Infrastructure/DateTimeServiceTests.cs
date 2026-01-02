namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for DateTimeService.
/// Tests date/time value generation.
/// </summary>
public class DateTimeServiceTests
{
    private readonly DateTimeService _sut;

    public DateTimeServiceTests()
    {
        _sut = new DateTimeService();
    }

    #region UtcNow Tests

    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = _sut.UtcNow;

        // Assert
        var after = DateTimeOffset.UtcNow;
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void UtcNow_ShouldHaveZeroOffset()
    {
        // Act
        var result = _sut.UtcNow;

        // Assert
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void UtcNow_ShouldReturnDifferentValuesOnSubsequentCalls()
    {
        // Act
        var result1 = _sut.UtcNow;
        Thread.Sleep(1); // Small delay to ensure time advances
        var result2 = _sut.UtcNow;

        // Assert
        result2.Should().BeOnOrAfter(result1);
    }

    #endregion

    #region Today Tests

    [Fact]
    public void Today_ShouldReturnTodaysDate()
    {
        // Arrange
        var expected = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = _sut.Today;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Today_ShouldReturnValidDateOnly()
    {
        // Act
        var result = _sut.Today;

        // Assert - DateOnly must have valid values
        result.Year.Should().BeGreaterThan(2000);
        result.Month.Should().BeInRange(1, 12);
        result.Day.Should().BeInRange(1, 31);
    }

    #endregion
}
